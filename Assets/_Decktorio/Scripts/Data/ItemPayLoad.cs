using UnityEngine;
using System.Collections.Generic;
using System;
using System.Linq;

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
        RecalculatePhysics();
    }

    // Constructor for merging existing stacks
    public ItemPayload(List<CardData> cards)
    {
        contents.AddRange(cards);
        RecalculatePhysics();
    }

    // Recalculate physical properties based on the cards inside
    // Example: A stack of 5 Steel cards should be very heavy and slow.
    public void RecalculatePhysics()
    {
        float totalMass = 0;
        foreach (var card in contents)
        {
            totalMass += card.GetMass();
        }

        // Heuristic: Heavier stacks might move slower on belts
        // This is just data; the Belt script will apply the drag.
        velocityBonus = Mathf.Clamp(1.0f - (totalMass * 0.1f), 0.1f, 2.0f);
    }

    public string GetDebugLabel()
    {
        if (contents.Count == 0) return "Empty Stack";

        if (contents.Count == 1)
        {
            var c = contents[0];
            return $"[{c.material} / {c.ink}] {c.rank} of {c.suit}";
        }
        return $"Stack ({contents.Count} items)";
    }
}