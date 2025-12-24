using UnityEngine;

public class Laminator : BuildingBase
{
    [Header("Settings")]
    public float processingTime = 1.0f; // Slower than normal
    private float processTimer = 0f;

    // Inputs
    private ItemPayload slotA;
    private ItemVisualizer visualA;

    private ItemPayload slotB;
    private ItemVisualizer visualB;

    protected override void OnTick(int tick)
    {
        // 1. PROCESS: Need both A and B
        if (slotA != null && slotB != null && internalItem == null)
        {
            processTimer += TickManager.Instance.tickRate;
            if (processTimer >= processingTime)
            {
                FuseCards();
                processTimer = 0f;
            }
        }
        // 2. OUTPUT
        else if (internalItem != null)
        {
            TryPushResult();
        }
    }

    public override bool CanAcceptItem(Vector2Int fromPos)
    {
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
            visualA.InitializeMovement(transform.position - transform.right * 0.2f + Vector3.up * 0.5f, 0.2f);
        }
        else if (slotB == null)
        {
            slotB = item;
            visualB = visual;
            visualB.InitializeMovement(transform.position + transform.right * 0.2f + Vector3.up * 0.5f, 0.2f);
        }
    }

    private void FuseCards()
    {
        CardData cardA = slotA.contents[slotA.contents.Count - 1];
        CardData cardB = slotB.contents[slotB.contents.Count - 1];

        // --- FUSION LOGIC ---
        // 1. Merge Flags (Bitwise OR)
        CardSuit newSuit = cardA.suit | cardB.suit;
        CardInk newInk = cardA.ink | cardB.ink;

        // 2. Rank is the Highest of the two
        float newRank = Mathf.Max(cardA.rank, cardB.rank);

        // 3. Heat Stacks + Penalty
        float newHeat = cardA.heat + cardB.heat + 10f;

        CardData result = new CardData(newRank, newSuit, cardA.material, newInk);
        result.heat = newHeat;

        internalItem = new ItemPayload(result);

        // --- VISUALS ---
        internalVisual = visualA;
        internalVisual.SetVisuals(internalItem);
        internalVisual.InitializeMovement(transform.position + Vector3.up * 0.5f, 0.1f);

        Destroy(visualB.gameObject);
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