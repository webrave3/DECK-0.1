using System.Collections.Generic;
using System.Linq;
using UnityEngine;

// Expanded Hand Types
public enum PokerHandType
{
    HighCard = 0,
    Pair,
    TwoPair,
    ThreeOfAKind,
    Straight,
    Flush,
    FullHouse,
    FourOfAKind,
    StraightFlush,
    RoyalFlush,
    FiveOfAKind,  // New: Possible with modified decks
    IllegalTech   // New: Contains glitched/illegal cards (Rank 15+)
}

public static class PokerEvaluator
{
    public static PokerHandType Evaluate(List<CardData> cards)
    {
        if (cards == null || cards.Count == 0) return PokerHandType.HighCard;

        // 1. Check for Illegal Cards (The Emperor / The Fool)
        // If any card is Rank 15+, the whole hand becomes "Illegal Tech"
        if (cards.Any(c => c.rank >= 15)) return PokerHandType.IllegalTech;

        // Grouping for standard poker logic
        var rankGroups = cards.GroupBy(c => c.rank).OrderByDescending(g => g.Count()).ThenByDescending(g => g.Key).ToList();
        var suitGroups = cards.GroupBy(c => c.suit);

        bool isFlush = suitGroups.Any(g => g.Count() >= 5);
        bool isStraight = CheckStraight(cards);

        // Evaluation Hierarchy
        if (rankGroups.Any(g => g.Count() >= 5)) return PokerHandType.FiveOfAKind;
        if (isFlush && isStraight)
        {
            // Check for Royal Flush (Straight Flush ending in Ace)
            // Note: In our data, Ace is 14.
            var straightRanks = cards.Select(c => c.rank).Distinct().OrderBy(r => r).ToList();
            if (straightRanks.Contains(14) && straightRanks.Contains(13)) return PokerHandType.RoyalFlush;

            return PokerHandType.StraightFlush;
        }
        if (rankGroups.Any(g => g.Count() == 4)) return PokerHandType.FourOfAKind;
        if (rankGroups.Any(g => g.Count() == 3) && rankGroups.Any(g => g.Count() == 2)) return PokerHandType.FullHouse;
        if (isFlush) return PokerHandType.Flush;
        if (isStraight) return PokerHandType.Straight;
        if (rankGroups.Any(g => g.Count() == 3)) return PokerHandType.ThreeOfAKind;
        if (rankGroups.Count(g => g.Count() == 2) >= 2) return PokerHandType.TwoPair;
        if (rankGroups.Any(g => g.Count() == 2)) return PokerHandType.Pair;

        return PokerHandType.HighCard;
    }

    private static bool CheckStraight(List<CardData> cards)
    {
        var ranks = cards.Select(c => c.rank).Distinct().OrderBy(r => r).ToList();

        // Edge Case: Ace Low Straight (A, 2, 3, 4, 5)
        // In our system Ace is 14. We need to treat it as 1 for this check.
        if (ranks.Contains(14)) ranks.Insert(0, 1);

        int consecutive = 0;
        for (int i = 0; i < ranks.Count - 1; i++)
        {
            if (ranks[i + 1] == ranks[i] + 1)
            {
                consecutive++;
                if (consecutive >= 4) return true; // 4 steps = 5 cards
            }
            else
            {
                consecutive = 0;
            }
        }
        return false;
    }

    public static int GetHandValue(PokerHandType type)
    {
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
            case PokerHandType.RoyalFlush: return 20000;
            case PokerHandType.FiveOfAKind: return 50000;
            case PokerHandType.IllegalTech: return 666666; // Massive payout but generates Heat (future feature)
            default: return 1;
        }
    }
}