using System;
using System.Collections.Generic;
using System.Linq;
using BridgeGym.Models;
using BridgeGym.Models.Bridge;

namespace BridgeGym.Services;

public class ExerciseService : IExerciseService
{
    private static readonly Random _random = new();

    public HandExerciseViewModel GenerateHand(int currentHand, int totalHands)
    {
        var deck = CreateDeck();
        Shuffle(deck);

        var southHand = deck.Take(13).OrderByDescending(c => c.Suit).ThenByDescending(c => c.Rank).ToList();
        var dummyHand = deck.Skip(13).Take(13).OrderByDescending(c => c.Suit).ThenByDescending(c => c.Rank).ToList();

        var dummyPosition = _random.Next(2) == 0 ? "West" : "East";
        
        int totalHcpShown = southHand.Sum(c => c.HcpValue) + dummyHand.Sum(c => c.HcpValue);
        int expectedHcp = 40 - totalHcpShown;

        return new HandExerciseViewModel
        {
            SouthHand = southHand,
            DummyHand = dummyHand,
            DummyPosition = dummyPosition,
            ExpectedHcp = expectedHcp,
            CurrentHandNumber = currentHand,
            TotalHandsInSession = totalHands
        };
    }

    private List<Card> CreateDeck()
    {
        var deck = new List<Card>();
        foreach (Suit suit in Enum.GetValues(typeof(Suit)))
        {
            foreach (Rank rank in Enum.GetValues(typeof(Rank)))
            {
                deck.Add(new Card { Suit = suit, Rank = rank });
            }
        }
        return deck;
    }

    private void Shuffle<T>(IList<T> list)
    {
        int n = list.Count;
        while (n > 1)
        {
            n--;
            int k = _random.Next(n + 1);
            T value = list[k];
            list[k] = list[n];
            list[n] = value;
        }
    }
}
