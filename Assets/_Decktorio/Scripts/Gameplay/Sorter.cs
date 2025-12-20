using UnityEngine;

public class Sorter : BuildingBase
{
    [Header("Filter Settings")]
    public CardSuit filterSuit = CardSuit.Heart;

    [Header("Visuals")]
    public float itemHeightOffset = 0.2f;

    protected override void OnTick(int tick)
    {
        if (internalItem != null)
        {
            HandleSorting();
        }
    }

    private void HandleSorting()
    {
        // Safety check for empty stacks
        if (internalItem.contents.Count == 0) return;

        // 1. Check Condition (Look at the top card)
        CardData topCard = internalItem.contents[0];
        bool isMatch = (topCard.suit == filterSuit);

        // 2. Determine Target Direction
        // Match -> Left
        // No Match -> Forward
        Vector2Int targetPos = isMatch ? GetLeftGridPosition() : GetForwardGridPosition();

        // 3. Attempt Push
        if (AttemptPush(targetPos))
        {
            if (showDebugLogs) GameLogger.Log($"Sorter: Sent item {(isMatch ? "Left" : "Forward")}");
        }
    }

    private bool AttemptPush(Vector2Int targetPos)
    {
        BuildingBase target = CasinoGridManager.Instance.GetBuildingAt(targetPos);

        if (target != null && target.CanAcceptItem(GridPosition))
        {
            target.ReceiveItem(internalItem, internalVisual);
            internalItem = null;
            internalVisual = null;
            return true;
        }
        return false;
    }

    protected override void OnItemArrived()
    {
        if (internalVisual != null)
        {
            internalVisual.transform.SetParent(this.transform);
            Vector3 targetPos = transform.position + Vector3.up * itemHeightOffset;

            float duration = TickManager.Instance.tickRate;
            internalVisual.InitializeMovement(targetPos, duration);
        }
    }

    // --- Helpers ---
    private Vector2Int GetLeftGridPosition()
    {
        int idx = (RotationIndex + 3) % 4; // -90 degrees
        return GridPosition + ConveyorBelt.GetDirFromIndex(idx);
    }
}