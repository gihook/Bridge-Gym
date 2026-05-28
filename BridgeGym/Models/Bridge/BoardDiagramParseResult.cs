using System.Collections.Generic;

namespace BridgeGym.Models.Bridge;

public class BoardDiagramParseResult
{
    public int BoardNumber { get; set; }
    public List<Card> North { get; set; } = new();
    public List<Card> South { get; set; } = new();
    public List<Card> East { get; set; } = new();
    public List<Card> West { get; set; } = new();
}
