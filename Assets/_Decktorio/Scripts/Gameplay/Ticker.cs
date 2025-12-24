using UnityEngine;

public class Ticker : BuildingBase
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
                TickCard();
                processTimer = 0f;
            }
        }
    }

    private void TickCard()
    {
        for (int i = 0; i < internalItem.contents.Count; i++)
        {
            CardData c = internalItem.contents[i];
            c.rank += 1.0f; // Precision increment
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