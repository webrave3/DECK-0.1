using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public enum PokerHandType
{
    HighCard = 0, Pair, TwoPair, ThreeOfAKind, Straight, Flush, FullHouse, FourOfAKind, StraightFlush, RoyalFlush
}

public static class PokerEvaluator
{
    // Updated to take List<CardData>
    public static PokerHandType Evaluate(List<CardData> cards)
    {
        if (cards == null || cards.Count == 0) return PokerHandType.HighCard;

        var rankGroups = cards.GroupBy(c => c.rank).OrderByDescending(g => g.Count()).ThenByDescending(g => g.Key).ToList();
        var suitGroups = cards.GroupBy(c => c.suit);

        bool isFlush = suitGroups.Any(g => g.Count() >= 5);
        bool isStraight = CheckStraight(cards);

        if (isFlush && isStraight) return PokerHandType.StraightFlush;
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
        int consecutive = 0;
        for (int i = 0; i < ranks.Count - 1; i++)
        {
            if (ranks[i + 1] == ranks[i] + 1)
            {
                consecutive++;
                if (consecutive >= 4) return true;
            }
            else consecutive = 0;
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
            default: return 1;
        }
    }
}