using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using BridgeGym.Data;
using BridgeGym.Models.Bridge;
using BridgeGym.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace BridgeGym.BackgroundJobs;

public class HandParsingJob
{
    private readonly BridgeGymContext _context;
    private readonly IGeminiService _geminiService;
    private readonly ILogger<HandParsingJob> _logger;

    public HandParsingJob(
        BridgeGymContext context,
        IGeminiService geminiService,
        ILogger<HandParsingJob> logger
    )
    {
        _context = context;
        _geminiService = geminiService;
        _logger = logger;
    }

    public async Task ProcessHandImageAsync(int boardHandId, byte[] imageBytes)
    {
        var hand = await _context
            .BoardHands.Include(h => h.Board)
                .ThenInclude(b => b.Hands)
            .FirstOrDefaultAsync(h => h.Id == boardHandId);
        if (hand == null)
        {
            _logger.LogError("Hand not found: {Id}", boardHandId);
            return;
        }

        hand.Status = HandProcessingStatus.Processing;
        await _context.SaveChangesAsync();

        try
        {
            var cards = await _geminiService.ParseHandImageAsync(imageBytes);

            if (cards == null || cards.Count != 13)
            {
                hand.Status = HandProcessingStatus.Error;
                hand.ErrorMessage = $"Failed to identify 13 cards. Identified: {cards?.Count ?? 0}";
            }
            else
            {
                // Check for duplicates
                var uniqueCardsCount = cards.GroupBy(c => new { c.Suit, c.Rank }).Count();
                if (uniqueCardsCount != 13)
                {
                    hand.Status = HandProcessingStatus.Error;
                    hand.ErrorMessage = "The identified cards contain duplicates.";
                }
                else
                {
                    // Check for overlaps
                    var existingCards = hand
                        .Board.Hands.Where(h =>
                            h.Id != hand.Id && h.Status == HandProcessingStatus.Success
                        )
                        .SelectMany(h => JsonSerializer.Deserialize<List<Card>>(h.CardsJson)!)
                        .ToList();

                    var overlappingCards = cards
                        .Where(c => existingCards.Any(ec => ec.Suit == c.Suit && ec.Rank == c.Rank))
                        .ToList();
                    if (overlappingCards.Any())
                    {
                        hand.Status = HandProcessingStatus.Error;
                        hand.ErrorMessage =
                            $"Some cards are already present in other hands: {string.Join(", ", overlappingCards.Select(c => c.ToString()))}";
                    }
                    else
                    {
                        hand.CardsJson = JsonSerializer.Serialize(cards);
                        hand.Status = HandProcessingStatus.Success;

                        // Check if we can auto-calculate 4th hand
                        // Refresh board hands from DB to be sure
                        var board = await _context
                            .Boards.Include(b => b.Hands)
                            .FirstAsync(b => b.Id == hand.BoardId);
                        var successHands = board
                            .Hands.Where(h => h.Status == HandProcessingStatus.Success)
                            .ToList();

                        if (successHands.Count == 3)
                        {
                            var filledSeats = successHands.Select(h => h.Seat).ToList();
                            var allSeats = Enum.GetValues(typeof(Seat)).Cast<Seat>();
                            var missingSeat = allSeats.First(s => !filledSeats.Contains(s));

                            if (!board.Hands.Any(h => h.Seat == missingSeat))
                            {
                                var existingHandsList = successHands
                                    .Select(h =>
                                        JsonSerializer.Deserialize<List<Card>>(h.CardsJson)!
                                    )
                                    .ToList();
                                var fourthHandCards = CalculateFourthHand(existingHandsList);

                                var fourthHand = new BoardHand
                                {
                                    Seat = missingSeat,
                                    CardsJson = JsonSerializer.Serialize(fourthHandCards),
                                    Status = HandProcessingStatus.Success,
                                };
                                board.Hands.Add(fourthHand);
                            }
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing hand {Id}", boardHandId);
            hand.Status = HandProcessingStatus.Error;
            hand.ErrorMessage = ex.Message;
        }

        await _context.SaveChangesAsync();
    }

    private List<Card> CalculateFourthHand(List<List<Card>> existingHands)
    {
        var allCards = new List<Card>();
        foreach (Suit suit in Enum.GetValues(typeof(Suit)))
        {
            foreach (Rank rank in Enum.GetValues(typeof(Rank)))
            {
                allCards.Add(new Card { Suit = suit, Rank = rank });
            }
        }

        var usedCards = existingHands.SelectMany(h => h).ToList();
        var remainingCards = allCards
            .Where(c => !usedCards.Any(uc => uc.Suit == c.Suit && uc.Rank == c.Rank))
            .ToList();

        return remainingCards;
    }
}
