using UnityEngine;

public class Divider : BuildingBase
{
    [Header("Settings")]
    public float processingTime = 0.5f;
    private float processTimer = 0f;

    // Output Buffer for the second half
    private ItemPayload secondOutputItem;
    private ItemVisualizer secondOutputVisual;

    protected override void OnTick(int tick)
    {
        // 1. PROCESS: Split input into two
        if (internalItem != null && secondOutputItem == null)
        {
            processTimer += TickManager.Instance.tickRate;
            if (processTimer >= processingTime)
            {
                PerformDivision();
                processTimer = 0f;
            }
        }

        // 2. OUTPUT: Push both halves
        bool mainPushed = false;
        bool secondPushed = false;

        if (internalItem != null) mainPushed = PushPrimary();
        if (secondOutputItem != null) secondPushed = PushSecondary();

        // (We don't need to do anything else, the Push functions handle clearing)
    }

    private void PerformDivision()
    {
        CardData original = internalItem.contents[0];

        // --- THE MATH ---
        float halfRank = original.rank / 2f;

        // Create 2 identical cards
        CardData c1 = new CardData(halfRank, original.suit, original.material, original.ink);
        CardData c2 = new CardData(halfRank, original.suit, original.material, original.ink);

        // Heat is split too
        c1.heat = original.heat / 2f;
        c2.heat = original.heat / 2f;

        // Assign
        internalItem = new ItemPayload(c1);
        secondOutputItem = new ItemPayload(c2);

        // --- VISUALS ---
        // Update the main visual
        internalVisual.SetVisuals(internalItem);

        // Clone for the second visual
        GameObject v2 = Instantiate(internalVisual.gameObject, transform.position, Quaternion.identity);
        secondOutputVisual = v2.GetComponent<ItemVisualizer>();
        secondOutputVisual.SetVisuals(secondOutputItem);
    }

    private bool PushPrimary()
    {
        // Output A: Forward
        Vector2Int fwd = GetForwardGridPosition();
        if (AttemptPush(internalItem, internalVisual, fwd))
        {
            internalItem = null;
            internalVisual = null;
            return true;
        }
        return false;
    }

    private bool PushSecondary()
    {
        // Output B: Right (Relative)
        int rightIndex = (RotationIndex + 1) % 4;
        Vector2Int dir = Vector2Int.zero;
        if (rightIndex == 0) dir = Vector2Int.up;
        if (rightIndex == 1) dir = Vector2Int.right;
        if (rightIndex == 2) dir = Vector2Int.down;
        if (rightIndex == 3) dir = Vector2Int.left;

        Vector2Int targetPos = GridPosition + dir;

        if (AttemptPush(secondOutputItem, secondOutputVisual, targetPos))
        {
            secondOutputItem = null;
            secondOutputVisual = null;
            return true;
        }
        return false;
    }

    private bool AttemptPush(ItemPayload item, ItemVisualizer visual, Vector2Int targetPos)
    {
        if (CasinoGridManager.Instance == null) return false;
        BuildingBase target = CasinoGridManager.Instance.GetBuildingAt(targetPos);

        if (target != null && target.CanAcceptItem(GridPosition))
        {
            target.ReceiveItem(item, visual);
            return true;
        }
        return false;
    }
}