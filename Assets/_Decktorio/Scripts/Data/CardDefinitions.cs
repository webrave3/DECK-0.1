using UnityEngine;

// Layer 1: The Substrate
public enum CardMaterial
{
    Cardstock,  // Standard, Flammable
    Steel,      // Heavy, Slows belts, High Knockback
    Glass,      // x5 Value, Shatters on impact/high speed
    GoldLeaf    // Conductive, Transmits logic signals
}

// Layer 2: The Fluid
public enum CardInk
{
    Standard,   // Normal Red/Black behavior
    Invisible,  // $0 Value, Special Debuff effects
    Neon        // Value randomizes every second
}

// Layer 3: The Shape (Suit)
public enum CardSuit
{
    None,
    Heart,
    Diamond,
    Club,
    Spade,
    Gear // Mechanical suit that powers buildings
}

// Layer 4: The Logic (Rank) definition helper
// We will still store rank as an 'int' in CardData for math, 
// but this Enum helps code readability for special ranks.
public enum SpecialRank
{
    Jack = 11,
    Queen = 12,
    King = 13,
    Ace = 14,     // High Ace
    Emperor = 15, // Illegal: Overclocked
    Fool = 16     // Illegal: Wildcard / Chaos
}