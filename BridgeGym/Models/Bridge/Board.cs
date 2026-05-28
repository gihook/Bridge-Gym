using System.Collections.Generic;

namespace BridgeGym.Models.Bridge;

public class Board
{
    public int Id { get; set; }
    public int BoardSetId { get; set; }
    public BoardSet BoardSet { get; set; } = null!;
    public int BoardNumber { get; set; }
    public List<BoardHand> Hands { get; set; } = new();

    public static List<Card> CalculateFourthHand(List<List<Card>> existingHands)
    {
        var allCards = new List<Card>();
        foreach (Suit suit in Enum.GetValues(typeof(Suit)))
        {
            foreach (Rank rank in Enum.GetValues(typeof(Rank)))
            {
                allCards.Add(new Card { Suit = suit, Rank = rank });
            }
        }

        var usedCards = existingHands.SelectMany(h => h).ToList();
        var remainingCards = allCards
            .Where(c => !usedCards.Any(uc => uc.Suit == c.Suit && uc.Rank == c.Rank))
            .ToList();

        return remainingCards;
    }
}
