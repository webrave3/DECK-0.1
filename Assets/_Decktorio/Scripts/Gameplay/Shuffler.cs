using UnityEngine;

public class Shuffler : BuildingBase
{
    [Header("Settings")]
    public float processingTime = 0.5f;
    public float minRank = 1f;
    public float maxRank = 6f;

    private float processTimer = 0f;

    protected override void OnTick(int tick)
    {
        if (internalItem != null)
        {
            processTimer += TickManager.Instance.tickRate;
            if (processTimer >= processingTime)
            {
                ShuffleCard();
                processTimer = 0f;
            }
        }
    }

    private void ShuffleCard()
    {
        // FIX: Used 'for' loop instead of 'foreach' to modify struct data
        for (int i = 0; i < internalItem.contents.Count; i++)
        {
            // 1. Get a copy
            CardData card = internalItem.contents[i];

            // 2. Modify the copy
            card.rank = Random.Range((int)minRank, (int)maxRank + 1);

            // 3. Put the modified copy back into the list
            internalItem.contents[i] = card;
        }

        // Update Visuals
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