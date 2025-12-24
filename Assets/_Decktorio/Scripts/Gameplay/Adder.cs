using UnityEngine;

public class Adder : BuildingBase
{
    [Header("Settings")]
    public float processingTime = 0.5f;
    private float processTimer = 0f;

    // We store inputs in specific "Slots"
    private ItemPayload slotA;
    private ItemVisualizer visualA;

    private ItemPayload slotB;
    private ItemVisualizer visualB;

    protected override void OnTick(int tick)
    {
        // 1. PROCESS: If both slots are full, start calculating
        if (slotA != null && slotB != null && internalItem == null)
        {
            processTimer += TickManager.Instance.tickRate;
            if (processTimer >= processingTime)
            {
                PerformAddition();
                processTimer = 0f;
            }
        }
        // 2. OUTPUT: If we have a result, push it out
        else if (internalItem != null)
        {
            TryPushResult();
        }
    }

    public override bool CanAcceptItem(Vector2Int fromPos)
    {
        // Simple Logic: Fill A first, then B
        // (Improved logic would check input direction, but this works for now)
        if (slotA == null) return true;
        if (slotB == null) return true;
        return false;
    }

    public override void ReceiveItem(ItemPayload item, ItemVisualizer visual)
    {
        visual.transform.SetParent(transform);

        if (slotA == null)
        {
            slotA = item;
            visualA = visual;
            // Visual: Move to Left Input
            visualA.InitializeMovement(transform.position - transform.right * 0.2f + Vector3.up * 0.5f, 0.2f);
        }
        else if (slotB == null)
        {
            slotB = item;
            visualB = visual;
            // Visual: Move to Right Input
            visualB.InitializeMovement(transform.position + transform.right * 0.2f + Vector3.up * 0.5f, 0.2f);
        }
    }

    private void PerformAddition()
    {
        CardData cardA = slotA.contents[slotA.contents.Count - 1];
        CardData cardB = slotB.contents[slotB.contents.Count - 1];

        // --- THE MATH ---
        float newRank = cardA.rank + cardB.rank;

        // Construct Result
        // We inherit properties from Input A
        CardData result = new CardData(newRank, cardA.suit, cardA.material, cardA.ink);

        // Sum the Heat
        result.heat = cardA.heat + cardB.heat;

        internalItem = new ItemPayload(result);

        // --- VISUALS ---
        // Reuse Visual A for the result, Destroy Visual B
        internalVisual = visualA;
        internalVisual.SetVisuals(internalItem); // Updates text to show new Rank
        internalVisual.InitializeMovement(transform.position + Vector3.up * 0.5f, 0.1f); // Move to center

        Destroy(visualB.gameObject);

        // Clear Slots
        slotA = null; visualA = null;
        slotB = null; visualB = null;
    }

    private void TryPushResult()
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