using System;

namespace BridgeGym.Models;

public class GameSession
{
    public int Id { get; set; }
    public DateTime Date { get; set; } = DateTime.UtcNow;
    public int NumberOfHands { get; set; }
    public double TotalTimeSeconds { get; set; }
    public double AverageTimePerHand => NumberOfHands > 0 ? TotalTimeSeconds / NumberOfHands : 0;
    
    public double MinTimeSeconds { get; set; }
    public double MaxTimeSeconds { get; set; }
    public double StdDevTimeSeconds { get; set; }
}
