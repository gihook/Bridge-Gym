using System.Linq;
using System.Security.Claims;
using BridgeGym.Data;
using BridgeGym.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BridgeGym.Controllers;

[Authorize]
public class HistoryController : Controller
{
    private readonly BridgeGymContext _context;

    public HistoryController(BridgeGymContext context)
    {
        _context = context;
    }

    [HttpGet]
    public IActionResult Index()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var userSessions = _context.GameSessions.Where(s => s.UserId == userId).ToList();

        var viewModel = new HistoryIndexViewModel();

        // 1. Defence HCP
        var defenceSessions = userSessions.Where(s => s.Mode == ExerciseMode.DefenceHcp).ToList();
        viewModel.Summaries.Add(
            new ModeSummaryViewModel
            {
                Mode = ExerciseMode.DefenceHcp,
                ModeName = "Defence HCP",
                Description = "Calculate missing points in the deck based on South and Dummy.",
                TotalSessions = defenceSessions.Count,
                AverageTimePerHand = defenceSessions.Any()
                    ? defenceSessions.Average(s => s.AverageTimePerHand)
                    : 0,
                AverageAccuracy = defenceSessions.Any()
                    ? defenceSessions.Average(s =>
                        s.NumberOfHands > 0
                            ? (double)s.CorrectAnswersCount * 100 / s.NumberOfHands
                            : 0
                    )
                    : 0,
            }
        );

        // 2. Hand HCP
        var handSessions = userSessions.Where(s => s.Mode == ExerciseMode.HandHcp).ToList();
        viewModel.Summaries.Add(
            new ModeSummaryViewModel
            {
                Mode = ExerciseMode.HandHcp,
                ModeName = "Hand HCP",
                Description = "Quickly count the High Card Points in your own hand.",
                TotalSessions = handSessions.Count,
                AverageTimePerHand = handSessions.Any()
                    ? handSessions.Average(s => s.AverageTimePerHand)
                    : 0,
                AverageAccuracy = handSessions.Any()
                    ? handSessions.Average(s =>
                        s.NumberOfHands > 0
                            ? (double)s.CorrectAnswersCount * 100 / s.NumberOfHands
                            : 0
                    )
                    : 0,
            }
        );

        // 3. Distribution
        var distSessions = userSessions.Where(s => s.Mode == ExerciseMode.Distribution).ToList();
        viewModel.Summaries.Add(
            new ModeSummaryViewModel
            {
                Mode = ExerciseMode.Distribution,
                ModeName = "Distribution",
                Description = "Calculate the missing 4th suit length given the other 3.",
                TotalSessions = distSessions.Count,
                AverageTimePerHand = distSessions.Any()
                    ? distSessions.Average(s => s.AverageTimePerHand)
                    : 0,
                AverageAccuracy = distSessions.Any()
                    ? distSessions.Average(s =>
                        s.NumberOfHands > 0
                            ? (double)s.CorrectAnswersCount * 100 / s.NumberOfHands
                            : 0
                    )
                    : 0,
            }
        );

        return View(viewModel);
    }
}
