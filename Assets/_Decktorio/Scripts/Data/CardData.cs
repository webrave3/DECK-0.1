using UnityEngine;
using System;

[Serializable]
public struct CardData
{
    public string uid;

    // Layer 4: Logic (Rank)
    // 2-14 are standard. 15+ are "Illegal/Overclocked".
    public int rank;

    // Layer 3: Shape
    public CardSuit suit;

    // Layer 1: Material (Physics & Multipliers)
    public CardMaterial material;

    // Layer 2: Ink (Status Effects)
    public CardInk ink;

    // Constructor
    public CardData(int rank, CardSuit suit, CardMaterial material = CardMaterial.Cardstock, CardInk ink = CardInk.Standard)
    {
        this.uid = Guid.NewGuid().ToString();
        this.rank = rank;
        this.suit = suit;
        this.material = material;
        this.ink = ink;
    }

    // --- Helper Logic for Gameplay mechanics ---

    public float GetValueMultiplier()
    {
        float mult = 1.0f;
        if (material == CardMaterial.Glass) mult *= 5.0f;
        if (ink == CardInk.Invisible) mult = 0f;
        return mult;
    }

    public float GetMass()
    {
        if (material == CardMaterial.Steel) return 5.0f; // Very Heavy
        if (material == CardMaterial.GoldLeaf) return 0.5f; // Very Light
        return 1.0f;
    }

    public bool IsConductive()
    {
        return material == CardMaterial.GoldLeaf;
    }

    public bool IsFlammable()
    {
        return material == CardMaterial.Cardstock;
    }

    // Returns the BACKGROUND color of the card
    public Color GetDisplayColor()
    {
        if (ink == CardInk.Invisible) return new Color(1, 1, 1, 0.2f);
        if (ink == CardInk.Neon) return Color.magenta;

        if (material == CardMaterial.GoldLeaf) return new Color(1f, 0.84f, 0f); // Gold
        if (material == CardMaterial.Steel) return new Color(0.6f, 0.6f, 0.7f); // Grey Metal

        switch (suit)
        {
            case CardSuit.Heart:
            case CardSuit.Diamond:
                return Color.red;
            case CardSuit.Club:
            case CardSuit.Spade:
                return Color.black;
            case CardSuit.None:
                return new Color(0.96f, 0.96f, 0.86f); // Beige
            default:
                return Color.white;
        }
    }

    // NEW: Returns White or Black text depending on how dark the background is
    public Color GetContrastingTextColor()
    {
        Color bg = GetDisplayColor();
        // Calculate Luminance (standard formula)
        float luminance = 0.299f * bg.r + 0.587f * bg.g + 0.114f * bg.b;

        // If bright (> 0.5), use Black text. If dark, use White text.
        return (luminance > 0.5f) ? Color.black : Color.white;
    }
}