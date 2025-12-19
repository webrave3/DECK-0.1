using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public enum PokerHandType
{
    HighCard = 0,
    Pair = 1,
    TwoPair = 2,
    ThreeOfAKind = 3,
    Straight = 4,
    Flush = 5,
    FullHouse = 6,
    FourOfAKind = 7,
    StraightFlush = 8,
    RoyalFlush = 9
}

public static class PokerEvaluator
{
    // Returns the type of hand found in the list of cards
    public static PokerHandType Evaluate(List<CardPayload> cards)
    {
        if (cards == null || cards.Count == 0) return PokerHandType.HighCard;

        // Group by Rank and Suit for easier checking
        var rankGroups = cards.GroupBy(c => c.rank).OrderByDescending(g => g.Count()).ThenByDescending(g => g.Key).ToList();
        var suitGroups = cards.GroupBy(c => c.suit);

        bool isFlush = suitGroups.Any(g => g.Count() >= 5);
        bool isStraight = CheckStraight(cards);

        // Logic Tree
        if (isFlush && isStraight) return PokerHandType.StraightFlush; // (Royal check skipped for brevity)
        if (rankGroups.Any(g => g.Count() == 4)) return PokerHandType.FourOfAKind;
        if (rankGroups.Any(g => g.Count() == 3) && rankGroups.Any(g => g.Count() == 2)) return PokerHandType.FullHouse;
        if (isFlush) return PokerHandType.Flush;
        if (isStraight) return PokerHandType.Straight;
        if (rankGroups.Any(g => g.Count() == 3)) return PokerHandType.ThreeOfAKind;
        if (rankGroups.Count(g => g.Count() == 2) >= 2) return PokerHandType.TwoPair;
        if (rankGroups.Any(g => g.Count() == 2)) return PokerHandType.Pair;

        return PokerHandType.HighCard;
    }

    private static bool CheckStraight(List<CardPayload> cards)
    {
        // Get unique ranks, sorted
        var ranks = cards.Select(c => c.rank).Distinct().OrderBy(r => r).ToList();

        // Check for 5 consecutive cards
        int consecutive = 0;
        for (int i = 0; i < ranks.Count - 1; i++)
        {
            if (ranks[i + 1] == ranks[i] + 1)
            {
                consecutive++;
                if (consecutive >= 4) return true; // Found 5 cards (4 steps)
            }
            else
            {
                consecutive = 0;
            }
        }

        // Special Ace Low check (A, 2, 3, 4, 5) -> 13, 1, 2, 3, 4? 
        // Depending on your Rank mapping (usually Ace is 1 or 14).
        // Assuming Ace = 1 for now.
        return false;
    }

    public static int GetHandValue(PokerHandType type)
    {
        // Configurable multipliers for money
        switch (type)
        {
            case PokerHandType.Pair: return 10;
            case PokerHandType.TwoPair: return 20;
            case PokerHandType.ThreeOfAKind: return 40;
            case PokerHandType.Straight: return 100;
            case PokerHandType.Flush: return 150;
            case PokerHandType.FullHouse: return 300;
            case PokerHandType.FourOfAKind: return 1000;
            case PokerHandType.StraightFlush: return 5000;
            default: return 1; // High Card / Junk
        }
    }
}