using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using BridgeGym.Data;
using BridgeGym.Models.Bridge;
using BridgeGym.Services;
using Hangfire.Console;
using Hangfire.Server;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace BridgeGym.BackgroundJobs;

public class BoardParsingJob
{
    private readonly BridgeGymContext _context;
    private readonly IGeminiService _geminiService;
    private readonly ILogger<BoardParsingJob> _logger;

    public BoardParsingJob(
        BridgeGymContext context,
        IGeminiService geminiService,
        ILogger<BoardParsingJob> logger
    )
    {
        _context = context;
        _geminiService = geminiService;
        _logger = logger;
    }

    public async Task ProcessBoardDiagramsAsync(int boardSetId, List<byte[]> imagesBytes, PerformContext context)
    {
        var boardSet = await _context.BoardSets.FindAsync(boardSetId);
        if (boardSet == null)
        {
            _logger.LogError("Board set not found: {Id}", boardSetId);
            context.WriteLine($"Error: Board set not found {boardSetId}");
            return;
        }

        context.WriteLine($"Processing {imagesBytes.Count} board diagrams for set: {boardSet.Name}");

        try
        {
            var imageStreams = imagesBytes.Select(b => new MemoryStream(b)).ToList();
            var results = await _geminiService.ParseBoardDiagramsAsync(imageStreams);

            foreach (var stream in imageStreams)
            {
                stream.Dispose();
            }

            if (results == null || !results.Any())
            {
                _logger.LogError("Failed to parse board diagrams for set {Id}", boardSetId);
                context.WriteLine("Error: Gemini failed to parse the diagrams or returned no results.");
                return;
            }

            context.WriteLine($"Successfully parsed {results.Count} boards.");

            foreach (var result in results)
            {
                // Find or create board
                var board = await _context.Boards
                    .Include(b => b.Hands)
                    .FirstOrDefaultAsync(b => b.BoardSetId == boardSetId && b.BoardNumber == result.BoardNumber);

                if (board == null)
                {
                    board = new Board { BoardSetId = boardSetId, BoardNumber = result.BoardNumber };
                    _context.Boards.Add(board);
                    context.WriteLine($"Created new board #{result.BoardNumber}");
                }
                else
                {
                    context.WriteLine($"Updating existing board #{result.BoardNumber}");
                }

                UpdateHand(board, Seat.North, result.North, context);
                UpdateHand(board, Seat.South, result.South, context);
                UpdateHand(board, Seat.East, result.East, context);
                UpdateHand(board, Seat.West, result.West, context);
            }

            await _context.SaveChangesAsync();
            context.WriteLine("All boards saved successfully.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing board diagrams for set {Id}", boardSetId);
            context.WriteLine($"Exception: {ex.Message}");
            throw; // Re-throw to fail the job
        }
    }

    public async Task ProcessBoardDiagramAsync(int boardSetId, byte[] imageBytes, PerformContext context)
    {
        await ProcessBoardDiagramsAsync(boardSetId, new List<byte[]> { imageBytes }, context);
    }

    private void UpdateHand(Board board, Seat seat, List<Card> cards, PerformContext context)
    {
        if (cards == null || cards.Count != 13)
        {
             _logger.LogWarning("Invalid card count for {Seat} in board {BoardNumber}: {Count}", seat, board.BoardNumber, cards?.Count ?? 0);
             context.WriteLine($"Warning: {seat} has {cards?.Count ?? 0} cards (expected 13). Skipping.");
             return;
        }

        var hand = board.Hands.FirstOrDefault(h => h.Seat == seat);
        if (hand == null)
        {
            hand = new BoardHand { Seat = seat, Board = board };
            board.Hands.Add(hand);
        }

        hand.CardsJson = JsonSerializer.Serialize(cards);
        hand.Status = HandProcessingStatus.Success;
        hand.ErrorMessage = null;
        hand.IsAutoCalculated = false;
        context.WriteLine($"Updated {seat} hand.");
    }
}
