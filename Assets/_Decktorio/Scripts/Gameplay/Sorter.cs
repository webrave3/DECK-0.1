using UnityEngine;

public class Sorter : BuildingBase
{
    [Header("Filter Settings")]
    public CardSuit filterSuit = CardSuit.Heart;

    [Header("Visuals")]
    public float itemHeightOffset = 0.2f;

    protected override void OnTick(int tick)
    {
        if (internalCard != null)
        {
            HandleSorting();
        }
    }

    private void HandleSorting()
    {
        // 1. Check Condition
        bool isMatch = (internalCard.suit == filterSuit);

        // 2. Determine Target Direction
        // Match -> Left
        // No Match -> Forward (Base method)
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

        // Check if target exists AND accepts items from us
        if (target != null && target.CanAcceptItem(GridPosition))
        {
            target.ReceiveItem(internalCard, internalVisual);
            internalCard = null;
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

            // Move over one tick duration
            float duration = TickManager.Instance.tickRate;
            internalVisual.InitializeMovement(targetPos, duration);
        }
    }

    // --- Helpers ---

    private Vector2Int GetLeftGridPosition()
    {
        int idx = (RotationIndex + 3) % 4; // -90 degrees
        return GridPosition + GetDirFromIndex(idx);
    }

    // Helper duplicated here to avoid dependency on ConveyorBelt class
    private Vector2Int GetDirFromIndex(int index)
    {
        switch (index)
        {
            case 0: return new Vector2Int(0, 1);
            case 1: return new Vector2Int(1, 0);
            case 2: return new Vector2Int(0, -1);
            case 3: return new Vector2Int(-1, 0);
        }
        return Vector2Int.zero;
    }
}