using UnityEngine;
using System;

[Serializable]
public struct CardData
{
    public string uid; // Tracks exact instances (good for "Glitch" detection later)
    public int rank;   // 1 (Ace) - 13 (King)
    public CardSuit suit;
    public CardColor color;
    public bool isFoil;

    public CardData(int rank, CardSuit suit, bool isFoil = false)
    {
        this.uid = Guid.NewGuid().ToString();
        this.rank = rank;
        this.suit = suit;
        // Auto-determine color based on standard poker rules
        this.color = (suit == CardSuit.Heart || suit == CardSuit.Diamond) ? CardColor.Red : CardColor.Black;
        this.isFoil = isFoil;
    }
}

public enum CardSuit { None, Heart, Diamond, Club, Spade }
public enum CardColor { None, Red, Black, Gold, Neon }