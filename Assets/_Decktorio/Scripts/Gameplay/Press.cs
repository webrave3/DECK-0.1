using UnityEngine;

public class Press : BuildingBase
{
    [Header("Processing")]
    public float processingTime = 1.0f;
    private float processTimer = 0f;

    // The suit this press is currently providing (read from floor)
    private CardSuit currentSuitSource = CardSuit.None;

    protected override void Start()
    {
        base.Start();
        // Check floor for mold
        if (CasinoGridManager.Instance != null)
        {
            ResourceNode node = CasinoGridManager.Instance.GetResourceAt(GridPosition);
            if (node != null && node.type == ResourceType.SuitMold)
            {
                currentSuitSource = node.suitGiven;
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

    void ApplySuit(ItemPayload item)
    {
        if (currentSuitSource == CardSuit.None) return;

        for (int i = 0; i < item.contents.Count; i++)
        {
            CardData card = item.contents[i];

            // LOGIC: Add the suit flag (Bitwise OR)
            card.suit |= currentSuitSource;

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
            ApplySuit(internalItem);
            target.ReceiveItem(internalItem, internalVisual);

            internalItem = null;
            internalVisual = null;
            processTimer = 0f;
        }
    }
}