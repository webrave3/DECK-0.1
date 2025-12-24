using UnityEngine;
using System;

[Serializable]
public struct CardData
{
    public string uid;

    // Layer 4: Logic (Rank) - Now a FLOAT for precision math
    public float rank;

    // Layer 3: Shape (Bitmask - can hold multiple suits)
    public CardSuit suit;

    // Layer 1: Material (Physics & Multipliers)
    public CardMaterial material;

    // Layer 2: Ink (Bitmask - can hold multiple inks)
    public CardInk ink;

    // NEW: The Heist System
    // Tracks how "Hot" this card is. High heat triggers raids.
    public float heat;

    // Constructor
    public CardData(float rank, CardSuit suit, CardMaterial material = CardMaterial.Cardstock, CardInk ink = CardInk.None)
    {
        this.uid = Guid.NewGuid().ToString();
        this.rank = rank;
        this.suit = suit;
        this.material = material;
        this.ink = ink;
        this.heat = 0f;
    }

    // --- Helper Logic for Gameplay mechanics ---

    public float GetValueMultiplier()
    {
        float mult = 1.0f;
        if (material == CardMaterial.Glass) mult *= 5.0f;

        // If it has Invisible ink, value is 0 (unless revealed later)
        if (ink.HasFlag(CardInk.Invisible)) mult = 0f;

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
        // Priority Checks for Special Inks
        if (ink.HasFlag(CardInk.Invisible)) return new Color(1, 1, 1, 0.1f);
        if (ink.HasFlag(CardInk.Neon)) return Color.magenta; // Placeholder for shifting color logic

        // Material Overrides
        if (material == CardMaterial.GoldLeaf) return new Color(1f, 0.84f, 0f); // Gold
        if (material == CardMaterial.Steel) return new Color(0.6f, 0.6f, 0.7f); // Grey Metal

        // Suit-based Colors (Simple check for primary colors)
        bool hasRed = ink.HasFlag(CardInk.Red);
        bool hasBlack = ink.HasFlag(CardInk.Black);

        if (hasRed && hasBlack) return new Color(0.5f, 0f, 0f); // Dark Red (Composite)
        if (hasRed) return Color.red;
        if (hasBlack) return Color.black;

        // Default Beige
        return new Color(0.96f, 0.96f, 0.86f);
    }

    // Returns White or Black text depending on how dark the background is
    public Color GetContrastingTextColor()
    {
        Color bg = GetDisplayColor();
        float luminance = 0.299f * bg.r + 0.587f * bg.g + 0.114f * bg.b;
        return (luminance > 0.5f) ? Color.black : Color.white;
    }
}