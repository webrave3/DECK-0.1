using System;

// Layer 1: The Substrate
public enum CardMaterial
{
    Cardstock,  // Standard, Flammable
    Steel,      // Heavy, Slows belts, High Knockback
    Glass,      // x5 Value, Shatters on impact/high speed
    GoldLeaf    // Conductive, Transmits logic signals
}

// Layer 2: The Fluid (Bitmask)
[Flags]
public enum CardInk
{
    None = 0,
    Red = 1 << 0,
    Black = 1 << 1,
    Blue = 1 << 2, // Added for variety
    Gold = 1 << 3,
    Neon = 1 << 4, // "Illegal" Ink
    Invisible = 1 << 5  // "Illegal" Ink
}

// Layer 3: The Shape (Bitmask)
[Flags]
public enum CardSuit
{
    None = 0,
    Heart = 1 << 0,
    Diamond = 1 << 1,
    Club = 1 << 2,
    Spade = 1 << 3,
    Gear = 1 << 4  // Mechanical suit
}

// Layer 4: The Logic (Rank) definition helper
// We use floats now, but these constants help with code readability.
public enum SpecialRank
{
    Jack = 11,
    Queen = 12,
    King = 13,
    Ace = 14,     // High Ace
    Emperor = 15, // Illegal: Overclocked
    Fool = 16     // Illegal: Wildcard / Chaos
}