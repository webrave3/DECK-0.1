using UnityEngine;

public class SinkBuilding : BuildingBase
{
    [Header("Sink Settings")]
    public bool isIncinerator = false; // If true, just destroy. If false, Sell.
    public float sellMultiplier = 1.0f;

    protected override void OnTick(int tick)
    {
        // Sinks don't "push" items. They just wait for items to arrive.
        // The logic happens in OnItemArrived.
    }

    protected override void OnItemArrived()
    {
        // Item has visually and logically arrived in our inventory.
        if (internalCard != null)
        {
            ProcessItem(internalCard);

            // Destroy visual
            if (internalVisual != null) Destroy(internalVisual.gameObject);
            internalCard = null;
            internalVisual = null;
        }
    }

    void ProcessItem(CardPayload card)
    {
        if (isIncinerator)
        {
            // Just destroy. No money.
            // Optional: Spawn a little "Smoke" particle effect here later.
            return;
        }

        // Casino Floor Logic (Sell)
        float value = 0.1f;
        if (card.rank > 0) value += card.rank * 1.0f;
        if (card.suit != CardSuit.None) value *= 2.0f;

        int payout = Mathf.CeilToInt(value * sellMultiplier);
        if (ResourceManager.Instance != null) ResourceManager.Instance.AddCredits(payout);
    }

    // Sinks accept everything
    public override bool CanAcceptItem(Vector2Int fromPos) => internalCard == null && incomingCard == null;
}