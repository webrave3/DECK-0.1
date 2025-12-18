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
        if (isIncinerator) return; // Just destroy

        // Economy Logic
        float value = 0.1f; // Base for Blank

        if (card.rank > 0) value += card.rank * 1.0f; // Value for ranks
        if (card.suit != CardSuit.None) value *= 2.0f; // Multiplier for Suits

        int payout = Mathf.CeilToInt(value * sellMultiplier);

        if (ResourceManager.Instance != null)
        {
            ResourceManager.Instance.AddCredits(payout);
            // Optional: Floating Text popup here later
        }
    }

    // Sinks accept everything
    public override bool CanAcceptItem(Vector2Int fromPos) => internalCard == null && incomingCard == null;
}