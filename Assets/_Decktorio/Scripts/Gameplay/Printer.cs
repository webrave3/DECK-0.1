using UnityEngine;

public class Printer : BuildingBase
{
    [Header("Processing")]
    public float processingTime = 1.0f;
    private float processTimer = 0f;

    // The ink this printer is currently providing (read from floor)
    private CardInk currentInkSource = CardInk.None;

    // FIX: Return type changed from void to bool to match BuildingBase
    public override bool CanBePlacedAt(Vector2Int gridPos)
    {
        // We allow placement anywhere (returns true). 
        // If there is no ink, it simply won't function (handled in Start).
        return true;
    }

    protected override void Start()
    {
        base.Start();
        // Check floor for ink
        if (CasinoGridManager.Instance != null)
        {
            ResourceNode node = CasinoGridManager.Instance.GetResourceAt(GridPosition);
            if (node != null && node.type == ResourceType.InkSource)
            {
                currentInkSource = node.inkGiven;
            }
        }
    }

    protected override void OnTick(int tick)
    {
        if (internalItem != null)
        {
            processTimer += TickManager.Instance.tickRate;
            if (processTimer >= processingTime)
            {
                TryPushItem();
            }
        }
    }

    protected override void OnItemArrived()
    {
        if (internalVisual != null)
        {
            internalVisual.transform.SetParent(this.transform);
            internalVisual.InitializeMovement(transform.position + Vector3.up * 0.5f, TickManager.Instance.tickRate);
        }
    }

    void ApplyInk(ItemPayload item)
    {
        // If we aren't on an ink source, we do nothing
        if (currentInkSource == CardInk.None) return;

        for (int i = 0; i < item.contents.Count; i++)
        {
            CardData card = item.contents[i];

            // LOGIC: Add the ink flag (Bitwise OR)
            card.ink |= currentInkSource;

            item.contents[i] = card;
        }

        if (internalVisual != null) internalVisual.SetVisuals(item);
    }

    void TryPushItem()
    {
        Vector2Int forwardPos = GetForwardGridPosition();
        BuildingBase target = CasinoGridManager.Instance.GetBuildingAt(forwardPos);

        if (target != null && target.CanAcceptItem(GridPosition))
        {
            ApplyInk(internalItem);
            target.ReceiveItem(internalItem, internalVisual);

            internalItem = null;
            internalVisual = null;
            processTimer = 0f;
        }
    }
}