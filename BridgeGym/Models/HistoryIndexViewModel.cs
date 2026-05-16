using BridgeGym.Models;

namespace BridgeGym.Models;

public class ModeSummaryViewModel
{
    public ExerciseMode Mode { get; set; }
    public string ModeName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int TotalSessions { get; set; }
    public double AverageAccuracy { get; set; }
    public double AverageTimePerHand { get; set; }
}

public class HistoryIndexViewModel
{
    public List<ModeSummaryViewModel> Summaries { get; set; } = new();
}