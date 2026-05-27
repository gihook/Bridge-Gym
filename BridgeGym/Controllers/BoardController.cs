using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using BridgeGym.BackgroundJobs;
using BridgeGym.Data;
using BridgeGym.Models.Bridge;
using BridgeGym.Services;
using Hangfire;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BridgeGym.Controllers;

public class BoardController : Controller
{
    private readonly BridgeGymContext _context;
    private readonly IBackgroundJobClient _backgroundJobClient;

    public BoardController(BridgeGymContext context, IBackgroundJobClient backgroundJobClient)
    {
        _context = context;
        _backgroundJobClient = backgroundJobClient;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var boards = await _context
            .Boards.Include(b => b.Hands)
            .OrderBy(b => b.BoardNumber)
            .ToListAsync();
        return View(boards);
    }

    [HttpGet]
    public async Task<IActionResult> Details(int id)
    {
        var board = await _context
            .Boards.Include(b => b.Hands)
            .FirstOrDefaultAsync(b => b.Id == id);

        if (board == null)
        {
            return NotFound();
        }

        return View(board);
    }

    [HttpGet]
    public IActionResult Upload()
    {
        return View();
    }

    [HttpPost]
    public async Task<IActionResult> Upload(int boardNumber, Seat seat, IFormFile image)
    {
        if (image == null || image.Length == 0)
        {
            ModelState.AddModelError("", "Please select an image.");
            return View();
        }

        var board = await _context
            .Boards.Include(b => b.Hands)
            .FirstOrDefaultAsync(b => b.BoardNumber == boardNumber);

        if (board == null)
        {
            board = new Board { BoardNumber = boardNumber };
            _context.Boards.Add(board);
        }

        var hand = board.Hands.FirstOrDefault(h => h.Seat == seat);
        if (hand == null)
        {
            hand = new BoardHand { Seat = seat, Status = HandProcessingStatus.Pending };
            board.Hands.Add(hand);
        }
        else
        {
            hand.Status = HandProcessingStatus.Pending;
            hand.ErrorMessage = null;
            hand.CardsJson = string.Empty;
            hand.IsAutoCalculated = false; // Just in case
        }

        var autoHand = board.Hands.FirstOrDefault(h => h.IsAutoCalculated);
        if (autoHand != null)
        {
            _context.BoardHands.Remove(autoHand);
        }

        await _context.SaveChangesAsync();

        using var ms = new MemoryStream();
        await image.CopyToAsync(ms);
        var imageBytes = ms.ToArray();

        _backgroundJobClient.Enqueue<HandParsingJob>(job =>
            job.ProcessHandImageAsync(hand.Id, imageBytes)
        );

        Response.StatusCode = 202;
        return RedirectToAction("Details", new { id = board.Id });
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var hand = await _context
            .BoardHands.Include(h => h.Board)
                .ThenInclude(b => b.Hands)
            .FirstOrDefaultAsync(h => h.Id == id);

        if (hand == null)
        {
            return NotFound();
        }

        var cards = string.IsNullOrEmpty(hand.CardsJson)
            ? new List<Card>()
            : JsonSerializer.Deserialize<List<Card>>(hand.CardsJson) ?? new List<Card>();

        var otherHandsCards = GetOtherHandsCards(hand);

        ViewBag.BoardNumber = hand.Board.BoardNumber;
        ViewBag.BoardId = hand.BoardId;
        ViewBag.Seat = hand.Seat;
        ViewBag.OtherHandsCards = otherHandsCards;

        return View(cards);
    }

    [HttpPost]
    public async Task<IActionResult> Edit(int id, List<Card> cards)
    {
        var hand = await _context
            .BoardHands.Include(h => h.Board)
                .ThenInclude(b => b.Hands)
            .FirstOrDefaultAsync(h => h.Id == id);

        if (hand == null)
        {
            return NotFound();
        }

        if (cards.Count != 13)
        {
            ModelState.AddModelError("", "A hand must have exactly 13 cards.");
            ViewBag.BoardNumber = hand.Board.BoardNumber;
            ViewBag.BoardId = hand.BoardId;
            ViewBag.Seat = hand.Seat;
            ViewBag.OtherHandsCards = GetOtherHandsCards(hand);
            return View(cards);
        }

        // Check for duplicates in current hand
        if (cards.GroupBy(c => new { c.Suit, c.Rank }).Any(g => g.Count() > 1))
        {
            ModelState.AddModelError("", "Duplicate cards detected in the hand.");
            ViewBag.BoardNumber = hand.Board.BoardNumber;
            ViewBag.BoardId = hand.BoardId;
            ViewBag.Seat = hand.Seat;
            ViewBag.OtherHandsCards = GetOtherHandsCards(hand);
            return View(cards);
        }

        // Check for overlaps with other hands
        var otherHandsCards = GetOtherHandsCards(hand);
        var overlappingCards = cards
            .Where(c => otherHandsCards.Any(ohc => ohc.Suit == c.Suit && ohc.Rank == c.Rank))
            .ToList();

        if (overlappingCards.Any())
        {
            ModelState.AddModelError("", $"Some cards are already present in other hands: {string.Join(", ", overlappingCards.Select(c => c.ToString()))}");
            ViewBag.BoardNumber = hand.Board.BoardNumber;
            ViewBag.BoardId = hand.BoardId;
            ViewBag.Seat = hand.Seat;
            ViewBag.OtherHandsCards = otherHandsCards;
            return View(cards);
        }

        hand.CardsJson = JsonSerializer.Serialize(cards);
        hand.Status = HandProcessingStatus.Success;
        hand.ErrorMessage = null;
        hand.IsAutoCalculated = false;

        EnsureAutoHand(hand.Board);

        await _context.SaveChangesAsync();

        return RedirectToAction("Details", new { id = hand.BoardId });
    }

    [HttpPost]
    public async Task<IActionResult> Delete(int id)
    {
        var hand = await _context.BoardHands
            .Include(h => h.Board)
                .ThenInclude(b => b.Hands)
            .FirstOrDefaultAsync(h => h.Id == id);
            
        if (hand == null)
        {
            return NotFound();
        }

        var boardId = hand.BoardId;
        var board = hand.Board;
        
        _context.BoardHands.Remove(hand);
        
        // After deleting a manual hand, we might need to remove or update the auto-calculated one
        if (!hand.IsAutoCalculated)
        {
            EnsureAutoHand(board);
        }
        
        await _context.SaveChangesAsync();

        return RedirectToAction("Details", new { id = boardId });
    }

    private void EnsureAutoHand(Board board)
    {
        var manualHands = board.Hands
            .Where(h => !h.IsAutoCalculated && h.Status == HandProcessingStatus.Success)
            .ToList();

        var autoHand = board.Hands.FirstOrDefault(h => h.IsAutoCalculated);

        if (manualHands.Count == 3)
        {
            var filledSeats = manualHands.Select(h => h.Seat).ToList();
            var allSeats = Enum.GetValues<Seat>();
            var missingSeat = allSeats.First(s => !filledSeats.Contains(s));

            // Only proceed if the missing seat is not occupied by another manual hand
            if (!board.Hands.Any(h => h.Seat == missingSeat && !h.IsAutoCalculated))
            {
                var existingHandsList = manualHands
                    .Select(h => JsonSerializer.Deserialize<List<Card>>(h.CardsJson)!)
                    .ToList();
                var fourthHandCards = Board.CalculateFourthHand(existingHandsList);

                if (autoHand == null)
                {
                    autoHand = new BoardHand
                    {
                        Seat = missingSeat,
                        CardsJson = JsonSerializer.Serialize(fourthHandCards),
                        Status = HandProcessingStatus.Success,
                        IsAutoCalculated = true,
                        BoardId = board.Id
                    };
                    _context.BoardHands.Add(autoHand);
                }
                else
                {
                    autoHand.Seat = missingSeat;
                    autoHand.CardsJson = JsonSerializer.Serialize(fourthHandCards);
                    autoHand.Status = HandProcessingStatus.Success;
                }
            }
        }
        else if (autoHand != null)
        {
            _context.BoardHands.Remove(autoHand);
        }
    }

    private List<Card> GetOtherHandsCards(BoardHand hand)
    {
        return hand.Board.Hands
            .Where(h => h.Id != hand.Id && h.Status == HandProcessingStatus.Success)
            .SelectMany(h => string.IsNullOrEmpty(h.CardsJson)
                ? new List<Card>()
                : JsonSerializer.Deserialize<List<Card>>(h.CardsJson) ?? new List<Card>())
            .ToList();
    }
}
