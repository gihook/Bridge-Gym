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

        if (board.Hands.Any(h => h.Seat == seat))
        {
            ModelState.AddModelError("", $"Seat {seat} for Board {boardNumber} is already filled.");
            return View();
        }

        var newHand = new BoardHand
        {
            Seat = seat,
            Status = HandProcessingStatus.Pending
        };
        board.Hands.Add(newHand);
        await _context.SaveChangesAsync();

        using var ms = new MemoryStream();
        await image.CopyToAsync(ms);
        var imageBytes = ms.ToArray();

        _backgroundJobClient.Enqueue<HandParsingJob>(job => job.ProcessHandImageAsync(newHand.Id, imageBytes));

        Response.StatusCode = 202;
        return RedirectToAction("Details", new { id = board.Id });
    }
}
