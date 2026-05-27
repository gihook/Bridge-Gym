using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using BridgeGym.Data;
using BridgeGym.Models.Bridge;
using BridgeGym.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BridgeGym.Controllers;

public class BoardController : Controller
{
    private readonly BridgeGymContext _context;
    private readonly IGeminiService _geminiService;

    public BoardController(BridgeGymContext context, IGeminiService geminiService)
    {
        _context = context;
        _geminiService = geminiService;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var boards = await _context.Boards
            .Include(b => b.Hands)
            .OrderBy(b => b.BoardNumber)
            .ToListAsync();
        return View(boards);
    }

    [HttpGet]
    public async Task<IActionResult> Details(int id)
    {
        var board = await _context.Boards
            .Include(b => b.Hands)
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

        using var ms = new MemoryStream();
        await image.CopyToAsync(ms);
        var imageBytes = ms.ToArray();

        var cards = await _geminiService.ParseHandImageAsync(imageBytes);

        if (cards == null || cards.Count != 13)
        {
            ModelState.AddModelError("", $"Failed to identify 13 cards. Identified: {cards?.Count ?? 0}");
            return View();
        }

        // Check for duplicates in the identified cards
        var uniqueCardsCount = cards.GroupBy(c => new { c.Suit, c.Rank }).Count();
        if (uniqueCardsCount != 13)
        {
            ModelState.AddModelError("", "The identified cards contain duplicates.");
            return View();
        }

        var board = await _context.Boards
            .Include(b => b.Hands)
            .FirstOrDefaultAsync(b => b.BoardNumber == boardNumber);

        if (board == null)
        {
            board = new Board { BoardNumber = boardNumber };
            _context.Boards.Add(board);
        }

        if (board.Hands.Any(h => h.Seat == seat))
        {
            ModelState.AddModelError("", $"Seat {seat} for Board {boardNumber} is already filled.");
            return View();
        }

        // Validate no overlapping cards with existing hands
        var existingCards = board.Hands
            .SelectMany(h => JsonSerializer.Deserialize<List<Card>>(h.CardsJson)!)
            .ToList();

        var overlappingCards = cards.Where(c => existingCards.Any(ec => ec.Suit == c.Suit && ec.Rank == c.Rank)).ToList();
        if (overlappingCards.Any())
        {
            ModelState.AddModelError("", $"Some cards are already present in other hands: {string.Join(", ", overlappingCards.Select(c => c.ToString()))}");
            return View();
        }

        var newHand = new BoardHand
        {
            Board = board,
            Seat = seat,
            CardsJson = JsonSerializer.Serialize(cards)
        };
        _context.BoardHands.Add(newHand);
        board.Hands.Add(newHand);

        if (board.Hands.Count == 3)
        {
            // Auto-calculate 4th hand
            var filledSeats = board.Hands.Select(h => h.Seat).ToList();
            var allSeats = Enum.GetValues(typeof(Seat)).Cast<Seat>();
            var missingSeat = allSeats.First(s => !filledSeats.Contains(s));

            var existingHands = board.Hands.Select(h => JsonSerializer.Deserialize<List<Card>>(h.CardsJson)!).ToList();
            var fourthHandCards = CalculateFourthHand(existingHands);

            var fourthHand = new BoardHand
            {
                Board = board,
                Seat = missingSeat,
                CardsJson = JsonSerializer.Serialize(fourthHandCards)
            };
            _context.BoardHands.Add(fourthHand);
            board.Hands.Add(fourthHand);
        }

        await _context.SaveChangesAsync();

        return RedirectToAction("Details", new { id = board.Id });
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
        var remainingCards = allCards.Where(c => !usedCards.Any(uc => uc.Suit == c.Suit && uc.Rank == c.Rank)).ToList();

        return remainingCards;
    }
}
