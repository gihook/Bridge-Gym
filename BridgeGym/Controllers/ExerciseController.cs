using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using BridgeGym.Data;
using BridgeGym.Models;
using BridgeGym.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BridgeGym.Controllers;

[Authorize]
public class ExerciseController : Controller
{
    private readonly IExerciseService _exerciseService;
    private readonly BridgeGymContext _context;

    public ExerciseController(IExerciseService exerciseService, BridgeGymContext context)
    {
        _exerciseService = exerciseService;
        _context = context;
    }

    [HttpGet]
    public IActionResult Index()
    {
        return View();
    }

    [HttpPost]
    public IActionResult StartSession(int numHands, ExerciseMode mode)
    {
        if (numHands <= 0)
            numHands = 10;

        return RedirectToAction(
            "Play",
            new
            {
                handIndex = 1,
                totalHands = numHands,
                totalTime = 0.0,
                handTimes = "",
                correctAnswers = 0,
                mode = mode,
            }
        );
    }

    [HttpGet]
    public IActionResult Play(
        int handIndex,
        int totalHands,
        double totalTime,
        int correctAnswers,
        ExerciseMode mode,
        string handTimes = ""
    )
    {
        if (handIndex > totalHands)
        {
            return RedirectToAction(
                "SaveResult",
                new
                {
                    totalHands,
                    totalTime,
                    handTimes,
                    correctAnswers,
                    mode,
                }
            );
        }

        var viewModel = _exerciseService.GenerateHand(handIndex, totalHands, mode);
        viewModel.CurrentSessionTotalTime = totalTime;
        ViewBag.HandTimes = handTimes;
        ViewBag.CorrectAnswers = correctAnswers;

        return View(viewModel);
    }

    [HttpGet]
    public async Task<IActionResult> SaveResult(
        int totalHands,
        double totalTime,
        int correctAnswers,
        ExerciseMode mode,
        string handTimes = ""
    )
    {
        double minTime = 0;
        double maxTime = 0;
        double stdDev = 0;

        if (!string.IsNullOrEmpty(handTimes))
        {
            var times = handTimes
                .Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(t =>
                    double.TryParse(
                        t,
                        System.Globalization.NumberStyles.Any,
                        System.Globalization.CultureInfo.InvariantCulture,
                        out double val
                    )
                        ? val
                        : 0
                )
                .ToList();

            if (times.Any())
            {
                minTime = times.Min();
                maxTime = times.Max();
                double avg = times.Average();
                double sumOfSquaresOfDifferences = times
                    .Select(val => (val - avg) * (val - avg))
                    .Sum();
                stdDev = Math.Sqrt(sumOfSquaresOfDifferences / times.Count);
            }
        }

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        var session = new GameSession
        {
            Date = DateTime.UtcNow,
            NumberOfHands = totalHands,
            TotalTimeSeconds = totalTime,
            MinTimeSeconds = minTime,
            MaxTimeSeconds = maxTime,
            StdDevTimeSeconds = stdDev,
            CorrectAnswersCount = correctAnswers,
            Mode = mode,
            UserId = userId,
        };

        _context.GameSessions.Add(session);
        await _context.SaveChangesAsync();

        return RedirectToAction("Results", new { sessionId = session.Id });
    }

    [HttpGet]
    public async Task<IActionResult> Results(int sessionId)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var session = await _context.GameSessions.FindAsync(sessionId);

        if (session == null || session.UserId != userId)
        {
            return NotFound();
        }

        var allSessions = _context.GameSessions
            .Where(s => s.UserId == userId && s.Mode == session.Mode)
            .OrderByDescending(s => s.Date)
            .Take(10)
            .ToList();

        ViewBag.AllSessions = allSessions;
        return View(session);
    }

    [HttpGet]
    public IActionResult HistorySimple()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var sessions = _context.GameSessions
            .Where(s => s.UserId == userId && s.Mode == ExerciseMode.Simple)
            .OrderByDescending(s => s.Date)
            .ToList();

        ViewBag.Title = "Simple Mode History";
        ViewBag.Mode = ExerciseMode.Simple;
        return View("History", sessions);
    }

    [HttpGet]
    public IActionResult HistoryDefence()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var sessions = _context.GameSessions
            .Where(s => s.UserId == userId && s.Mode == ExerciseMode.Defence)
            .OrderByDescending(s => s.Date)
            .ToList();

        ViewBag.Title = "Defence Mode History";
        ViewBag.Mode = ExerciseMode.Defence;
        return View("History", sessions);
    }
    }
