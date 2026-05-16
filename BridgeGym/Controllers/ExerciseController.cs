using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using BridgeGym.Models;
using BridgeGym.Services;
using BridgeGym.Data;
using System;

namespace BridgeGym.Controllers;

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
    public IActionResult StartSession(int numHands)
    {
        if (numHands <= 0) numHands = 10;
        
        return RedirectToAction("Play", new { handIndex = 1, totalHands = numHands, totalTime = 0.0, handTimes = "" });
    }

    [HttpGet]
    public IActionResult Play(int handIndex, int totalHands, double totalTime, string handTimes = "")
    {
        if (handIndex > totalHands)
        {
            return RedirectToAction("SaveResult", new { totalHands, totalTime, handTimes });
        }

        var viewModel = _exerciseService.GenerateHand(handIndex, totalHands);
        viewModel.CurrentSessionTotalTime = totalTime;
        ViewBag.HandTimes = handTimes;
        
        return View(viewModel);
    }

    [HttpGet]
    public async Task<IActionResult> SaveResult(int totalHands, double totalTime, string handTimes = "")
    {
        double minTime = 0;
        double maxTime = 0;
        double stdDev = 0;

        if (!string.IsNullOrEmpty(handTimes))
        {
            var times = handTimes.Split(',', StringSplitOptions.RemoveEmptyEntries)
                                 .Select(t => double.TryParse(t, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out double val) ? val : 0)
                                 .ToList();

            if (times.Any())
            {
                minTime = times.Min();
                maxTime = times.Max();
                double avg = times.Average();
                double sumOfSquaresOfDifferences = times.Select(val => (val - avg) * (val - avg)).Sum();
                stdDev = Math.Sqrt(sumOfSquaresOfDifferences / times.Count);
            }
        }

        var session = new GameSession
        {
            Date = DateTime.UtcNow,
            NumberOfHands = totalHands,
            TotalTimeSeconds = totalTime,
            MinTimeSeconds = minTime,
            MaxTimeSeconds = maxTime,
            StdDevTimeSeconds = stdDev
        };

        _context.GameSessions.Add(session);
        await _context.SaveChangesAsync();

        return RedirectToAction("Results", new { sessionId = session.Id });
    }

    [HttpGet]
    public async Task<IActionResult> Results(int sessionId)
    {
        var session = await _context.GameSessions.FindAsync(sessionId);
        var allSessions = _context.GameSessions.OrderByDescending(s => s.Date).Take(10).ToList();
        
        ViewBag.AllSessions = allSessions;
        return View(session);
    }
}
