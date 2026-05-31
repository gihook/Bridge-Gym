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

        var options = new JsonSerializerOptions
        {
            WriteIndented = true,
            Converters = { new JsonStringEnumConverter() }
        };

        var exportData = new
        {
            boardSet.Name,
            boardSet.CreatedAt,
            Boards = boardSet.Boards.OrderBy(b => b.BoardNumber).Select(b => new
            {
                b.BoardNumber,
                Hands = b.Hands.OrderBy(h => h.Seat).Select(h => new
                {
                    h.Seat,
                    Cards = string.IsNullOrEmpty(h.CardsJson)
                        ? null
                        : JsonSerializer.Deserialize<List<Card>>(h.CardsJson, options)?
                            .Select(c => new { c.Suit, c.Rank }),
                    h.Status,
                    h.IsAutoCalculated
                })
            })
        };

        var json = JsonSerializer.Serialize(exportData, options);
        var fileName = $"{boardSet.Name.Replace(" ", "_")}_export.json";
        return File(System.Text.Encoding.UTF8.GetBytes(json), "application/json", fileName);
    }

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
