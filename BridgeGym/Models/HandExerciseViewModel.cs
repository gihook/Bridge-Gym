using System.Collections.Generic;
using BridgeGym.Models.Bridge;

namespace BridgeGym.Models;

public class HandExerciseViewModel
{
    public List<Card> SouthHand { get; set; } = new();
    public List<Card> DummyHand { get; set; } = new();
    public string DummyPosition { get; set; } = "West"; // West or East
    public int ExpectedHcp { get; set; }

    // For tracking the session progress in the view
    public int CurrentHandNumber { get; set; }
    public int TotalHandsInSession { get; set; }
    public double CurrentSessionTotalTime { get; set; }
    public ExerciseMode Mode { get; set; }
}
