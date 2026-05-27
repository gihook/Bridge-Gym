namespace BridgeGym.Models.Bridge;

public class BoardHand
{
    public int Id { get; set; }
    public int BoardId { get; set; }
    public Board Board { get; set; } = null!;
    public Seat Seat { get; set; }
    public string CardsJson { get; set; } = string.Empty; // List<Card> serialized
}
