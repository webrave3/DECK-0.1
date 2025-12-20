using UnityEngine;
using System.Collections.Generic;
using System;

[Serializable]
public class ItemPayload
{
    // The "Vertical Stack"
    public List<CardData> contents = new List<CardData>();

    // Physics / Gameplay Tags
    public float velocityBonus = 1.0f;
    public float valueMultiplier = 1.0f;

    // Default Constructor (Empty Carrier)
    public ItemPayload() { }

    // Constructor for a single card (Created by Printer/Press)
    public ItemPayload(CardData singleCard)
    {
        contents.Add(singleCard);
    }

    // Constructor for merging existing stacks
    public ItemPayload(List<CardData> cards)
    {
        contents.AddRange(cards);
    }

    public string GetDebugLabel()
    {
        if (contents.Count == 0) return "Empty Stack";
        if (contents.Count == 1) return $"{contents[0].rank} of {contents[0].suit}";
        return $"Stack (Size: {contents.Count})";
    }
}