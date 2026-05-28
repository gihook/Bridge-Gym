using System;
using System.Collections.Generic;

namespace BridgeGym.Models.Bridge;

public class BoardSet
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public List<Board> Boards { get; set; } = new();
}
