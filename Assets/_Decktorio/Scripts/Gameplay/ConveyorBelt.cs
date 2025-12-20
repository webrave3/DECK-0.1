using UnityEngine;

public class ConveyorBelt : BuildingBase
{
    [Header("Visuals")]
    public float itemHeightOffset = 0.2f;
    [Tooltip("0 = Center, 0.5 = Edge of tile. Use 0.5 to make items queue seamlessly.")]
    public float forwardVisualOffset = 0.5f;

    [Header("Gameplay")]
    public float speedModifier = 1.0f; // Velocity Bonus

    protected override void OnTick(int tick)
    {
        // Try to push the item if we have one
        if (internalItem != null)
        {
            TryPushItem();
        }
    }

    private void TryPushItem()
    {
        // 1. Try Forward
        if (AttemptPush(GetForwardGridPosition(), "Forward")) return;
    }

    private bool AttemptPush(Vector2Int targetPos, string debugDir)
    {
        BuildingBase target = CasinoGridManager.Instance.GetBuildingAt(targetPos);

        // Check if target exists AND can accept items from ME
        if (target != null && target.CanAcceptItem(GridPosition))
        {
            // Apply Velocity Bonus to the item as it travels
            internalItem.velocityBonus = this.speedModifier;

            // Pass the item AND the visual to the next building
            target.ReceiveItem(internalItem, internalVisual);

            // Clear our inventory
            internalItem = null;
            internalVisual = null;
            return true;
        }
        return false;
    }

    public override bool CanAcceptItem(Vector2Int fromPos)
    {
        // Standard check: Is full?
        if (internalItem != null || incomingItem != null) return false;

        // PREVENT BACKFLOW:
        // Do not accept items coming from the direction we are facing.
        if (fromPos == GetForwardGridPosition()) return false;

        return true;
    }

    public override bool CanBePlacedAt(Vector2Int gridPos)
    {
        // Restriction: Cannot place belts on top of Fixed Resources
        if (CasinoGridManager.Instance.GetResourceAt(gridPos) != null) return false;

        return base.CanBePlacedAt(gridPos);
    }

    protected override void OnItemArrived()
    {
        if (internalVisual != null)
        {
            internalVisual.transform.SetParent(this.transform);

            // CALCULATE EXIT POINT (Edge of tile in forward direction)
            Vector3 worldForward = (CasinoGridManager.Instance.GridToWorld(GetForwardGridPosition()) - transform.position).normalized;
            Vector3 targetPos = transform.position + (Vector3.up * itemHeightOffset) + (worldForward * forwardVisualOffset);

            // Move over the duration of one tick
            float duration = TickManager.Instance.tickRate;
            internalVisual.InitializeMovement(targetPos, duration);
        }
    }

    // --- PUBLIC HELPERS (Required for Auto-Tiler) ---

    public Vector2Int GetLeftGridPosition()
    {
        int leftIndex = (RotationIndex + 3) % 4;
        return GridPosition + GetDirFromIndex(leftIndex);
    }

    public Vector2Int GetRightGridPosition()
    {
        int rightIndex = (RotationIndex + 1) % 4;
        return GridPosition + GetDirFromIndex(rightIndex);
    }

    public Vector2Int GetBackGridPosition()
    {
        int backIndex = (RotationIndex + 2) % 4;
        return GridPosition + GetDirFromIndex(backIndex);
    }

    public static Vector2Int GetDirFromIndex(int index)
    {
        switch (index)
        {
            case 0: return new Vector2Int(0, 1);  // North
            case 1: return new Vector2Int(1, 0);  // East
            case 2: return new Vector2Int(0, -1); // South
            case 3: return new Vector2Int(-1, 0); // West
        }
        return Vector2Int.zero;
    }
}