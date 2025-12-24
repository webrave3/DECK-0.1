using UnityEngine;

public class Bleacher : BuildingBase
{
    [Header("Settings")]
    public float processingTime = 0.8f;
    private float processTimer = 0f;

    protected override void OnTick(int tick)
    {
        if (internalItem != null)
        {
            processTimer += TickManager.Instance.tickRate;
            if (processTimer >= processingTime)
            {
                BleachCard();
                processTimer = 0f;
            }
        }
    }

    private void BleachCard()
    {
        for (int i = 0; i < internalItem.contents.Count; i++)
        {
            CardData c = internalItem.contents[i];

            c.suit = CardSuit.None; // Wipe Suit
            c.ink = CardInk.None;   // Wipe Ink
            c.heat = 0f;            // Reset Heat (Clean card)

            internalItem.contents[i] = c;
        }

        if (internalVisual != null) internalVisual.SetVisuals(internalItem);
        TryPushItem();
    }

    private void TryPushItem()
    {
        Vector2Int fwd = GetForwardGridPosition();
        BuildingBase target = CasinoGridManager.Instance.GetBuildingAt(fwd);

        if (target != null && target.CanAcceptItem(GridPosition))
        {
            target.ReceiveItem(internalItem, internalVisual);
            internalItem = null;
            internalVisual = null;
        }
    }
}