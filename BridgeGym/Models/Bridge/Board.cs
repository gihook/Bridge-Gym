using System.Collections.Generic;

namespace BridgeGym.Models.Bridge;

public class Board
{
    public int Id { get; set; }
    public int BoardNumber { get; set; }
    public List<BoardHand> Hands { get; set; } = new();
}
