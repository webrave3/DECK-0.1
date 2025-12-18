using UnityEngine;

public enum CardSuit { None, Heart, Diamond, Club, Spade }
public enum CardColor { White, Red, Black, Gold, Neon }

[System.Serializable]
public class CardPayload
{
    public int rank;        // 1 (Ace) to 13 (King)
    public CardSuit suit;
    public CardColor color;
    public int stackSize;   // How many cards are compressed in this item

    public CardPayload(int rank = 1, CardSuit suit = CardSuit.Heart, int stackSize = 1)
    {
        this.rank = rank;
        this.suit = suit;
        this.color = CardColor.White;
        this.stackSize = stackSize;
    }

    public static CardPayload CreateBlank()
    {
        return new CardPayload(0, CardSuit.None, 1); // Rank 0 = Blank Stock
    }
}