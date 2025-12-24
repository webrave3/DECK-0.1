using UnityEngine;

public class Shaver : BuildingBase
{
    [Header("Settings")]
    public float processingTime = 0.2f; // Fast
    private float processTimer = 0f;

    protected override void OnTick(int tick)
    {
        if (internalItem != null)
        {
            processTimer += TickManager.Instance.tickRate;
            if (processTimer >= processingTime)
            {
                ShaveCard();
                processTimer = 0f;
            }
        }
    }

    private void ShaveCard()
    {
        for (int i = 0; i < internalItem.contents.Count; i++)
        {
            CardData c = internalItem.contents[i];
            c.rank -= 1.0f;

            // Safety: Don't go below 0
            if (c.rank < 0f) c.rank = 0f;

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