using UnityEngine;

public class SinkBuilding : BuildingBase
{
    [Header("Sink Settings")]
    public bool isIncinerator = false;
    public float sellMultiplier = 1.0f;

    protected override void OnTick(int tick)
    {
        // Passive receiver - Logic happens in OnItemArrived
    }

    protected override void OnItemArrived()
    {
        // Use internalItem (ItemPayload)
        if (internalItem != null)
        {
            ProcessItem(internalItem);

            if (internalVisual != null) Destroy(internalVisual.gameObject);
            internalItem = null;
            internalVisual = null;
        }
    }

    void ProcessItem(ItemPayload item)
    {
        if (isIncinerator) return;

        // 1. Evaluate the Stack
        PokerHandType handType = PokerEvaluator.Evaluate(item.contents);

        // 2. Base Value
        int baseValue = PokerEvaluator.GetHandValue(handType);

        // 3. Apply Multipliers (Velocity * Stack Bonuses)
        float totalValue = baseValue * item.velocityBonus * item.valueMultiplier;

        int finalPayout = Mathf.RoundToInt(totalValue * sellMultiplier);

        // 4. Pay (UPDATED FOR DEBT ECONOMY)
        GameLogger.Log($"[Sink] Sold {handType} ({item.contents.Count} cards) for ${finalPayout}");

        if (EconomyManager.Instance != null)
        {
            EconomyManager.Instance.ProcessPayout(finalPayout);
        }
    }

    public override bool CanAcceptItem(Vector2Int fromPos) => internalItem == null && incomingItem == null;
}