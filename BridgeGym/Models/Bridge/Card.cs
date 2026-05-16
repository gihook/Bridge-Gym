namespace BridgeGym.Models.Bridge;

public class Card
{
    public Suit Suit { get; set; }
    public Rank Rank { get; set; }

    public int HcpValue => Rank switch
    {
        Rank.Ace => 4,
        Rank.King => 3,
        Rank.Queen => 2,
        Rank.Jack => 1,
        _ => 0
    };

    public override string ToString() => $"{Rank} of {Suit}";
}
