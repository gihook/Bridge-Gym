using System;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using BridgeGym.Data;
using BridgeGym.Models.Bridge;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BridgeGym.Controllers;

public class BoardSetController : Controller
{
    private readonly BridgeGymContext _context;

    public BoardSetController(BridgeGymContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index()
    {
        var boardSets = await _context
            .BoardSets.Include(bs => bs.Boards)
            .OrderByDescending(bs => bs.CreatedAt)
            .ToListAsync();
        return View(boardSets);
    }

    public async Task<IActionResult> Details(int id)
    {
        var boardSet = await _context
            .BoardSets.Include(bs => bs.Boards)
            .FirstOrDefaultAsync(m => m.Id == id);

        if (boardSet == null)
        {
            return NotFound();
        }

        return View(boardSet);
    }

    [HttpGet]
    public async Task<IActionResult> Export(int id)
    {
        var boardSet = await _context.BoardSets
            .Include(bs => bs.Boards)
            .ThenInclude(b => b.Hands)
            .FirstOrDefaultAsync(m => m.Id == id);

        if (boardSet == null)
        {
            return NotFound();
        }

        var sb = new System.Text.StringBuilder();
        var dateStr = boardSet.CreatedAt.ToString("yyyy.MM.dd");

        foreach (var board in boardSet.Boards.OrderBy(b => b.BoardNumber))
        {
            sb.AppendLine($"[Event \"{boardSet.Name}\"]");
            sb.AppendLine("[Site \"BridgeGym\"]");
            sb.AppendLine($"[Date \"{dateStr}\"]");
            sb.AppendLine($"[Board \"{board.BoardNumber}\"]");
            sb.AppendLine("[West \"\"]");
            sb.AppendLine("[North \"\"]");
            sb.AppendLine("[East \"\"]");
            sb.AppendLine("[South \"\"]");

            // Standard dealer/vul based on board number
            int dealerIdx = (board.BoardNumber - 1) % 4;
            var dealer = dealerIdx switch
            {
                0 => "N",
                1 => "E",
                2 => "S",
                3 => "W",
                _ => "N"
            };
            sb.AppendLine($"[Dealer \"{dealer}\"]");

            var vul = GetVulnerability(board.BoardNumber);
            sb.AppendLine($"[Vulnerable \"{vul}\"]");

            var dealString = FormatPbnDeal(board, dealer);
            sb.AppendLine($"[Deal \"{dealString}\"]");
            sb.AppendLine("[Scoring \"\"]");
            sb.AppendLine("[Declarer \"\"]");
            sb.AppendLine("[Contract \"\"]");
            sb.AppendLine("[Result \"\"]");
            sb.AppendLine();
        }

        var pbn = sb.ToString();
        var fileName = $"{boardSet.Name.Replace(" ", "_")}_export.pbn";
        return File(System.Text.Encoding.UTF8.GetBytes(pbn), "application/x-pbn", fileName);
    }

    private string GetVulnerability(int boardNumber)
    {
        // Standard rotation: None, NS, EW, All, NS, EW, All, None, EW, All, None, NS, All, None, NS, EW
        string[] vuls = { "None", "NS", "EW", "All", "NS", "EW", "All", "None", "EW", "All", "None", "NS", "All", "None", "NS", "EW" };
        return vuls[(boardNumber - 1) % 16];
    }

    private string FormatPbnDeal(Board board, string dealer)
    {
        var hands = new Dictionary<Seat, List<Card>>();
        foreach (var hand in board.Hands)
        {
            if (!string.IsNullOrEmpty(hand.CardsJson))
            {
                var cards = JsonSerializer.Deserialize<List<Card>>(hand.CardsJson);
                if (cards != null) hands[hand.Seat] = cards;
            }
        }

        // If we have 3 hands, calculate the 4th
        if (hands.Count == 3)
        {
            var existingHands = hands.Values.ToList();
            var missingSeat = Enum.GetValues<Seat>().FirstOrDefault(s => !hands.ContainsKey(s));
            hands[missingSeat] = Board.CalculateFourthHand(existingHands);
        }

        var dealerSeat = dealer switch
        {
            "N" => Seat.North,
            "E" => Seat.East,
            "S" => Seat.South,
            "W" => Seat.West,
            _ => Seat.North
        };

        var seatsClockwise = new[] { Seat.North, Seat.East, Seat.South, Seat.West };
        var dealerIndex = Array.IndexOf(seatsClockwise, dealerSeat);

        var resultHands = new List<string>();
        for (int i = 0; i < 4; i++)
        {
            var currentSeat = seatsClockwise[(dealerIndex + i) % 4];
            if (hands.TryGetValue(currentSeat, out var cards))
            {
                resultHands.Add(FormatHand(cards));
            }
            else
            {
                resultHands.Add("...");
            }
        }

        return $"{dealer}:{string.Join(" ", resultHands)}";
    }

    private string FormatHand(List<Card> cards)
    {
        var suits = new[] { Suit.Spades, Suit.Hearts, Suit.Diamonds, Suit.Clubs };
        return string.Join(".", suits.Select(s => 
            string.Concat(cards.Where(c => c.Suit == s)
                               .OrderByDescending(c => c.Rank)
                               .Select(c => GetRankChar(c.Rank)))
        ));
    }

    private string GetRankChar(Rank rank) => rank switch
    {
        Rank.Ace => "A",
        Rank.King => "K",
        Rank.Queen => "Q",
        Rank.Jack => "J",
        Rank.Ten => "T",
        _ => ((int)rank).ToString()
    };


    [HttpGet]
    public IActionResult Create()
    {
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([Bind("Name")] BoardSet boardSet)
    {
        if (ModelState.IsValid)
        {
            boardSet.CreatedAt = DateTime.UtcNow;
            _context.Add(boardSet);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }
        return View(boardSet);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var boardSet = await _context.BoardSets.FindAsync(id);
        if (boardSet != null)
        {
            _context.BoardSets.Remove(boardSet);
            await _context.SaveChangesAsync();
        }
        return RedirectToAction(nameof(Index));
    }
}
