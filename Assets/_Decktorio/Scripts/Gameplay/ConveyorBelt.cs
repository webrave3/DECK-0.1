using UnityEngine;

public class ConveyorBelt : BuildingBase
{
    [Header("Visuals")]
    public float itemHeightOffset = 0.2f;

    protected override void OnTick(int tick)
    {
        if (internalCard != null)
        {
            TryPushItem();
        }
    }

    private void TryPushItem()
    {
        // 1. Try Forward First
        if (AttemptPush(GetForwardGridPosition())) return;

        // 2. If Forward Blocked, try Left/Right (Fluid Splitting)
        // Check rotations relative to current facing
        // (This makes a belt act like a "Smart Splitter" if facing a wall)
        // Use GridPosition + rotated vectors...
    }

    private bool AttemptPush(Vector2Int targetPos)
    {
        BuildingBase target = CasinoGridManager.Instance.GetBuildingAt(targetPos);
        if (target != null && target.CanAcceptItem(GridPosition))
        {
            target.ReceiveItem(internalCard, internalVisual);
            internalCard = null;
            internalVisual = null;
            return true;
        }
        return false;
    }

    public override bool CanAcceptItem(Vector2Int fromPos)
    {
        if (internalCard != null || incomingCard != null) return false;

        // SIDE LOADING FIX:
        // We accept items from ANY direction, EXCEPT our own output (prevent backflow).
        // This allows side-merging automatically.
        Vector2Int myOutput = GetForwardGridPosition();
        if (fromPos == myOutput) return false;

        return true;
    }

    protected override void OnItemArrived()
    {
        if (internalVisual != null)
        {
            internalVisual.transform.SetParent(this.transform);

            // Move from wherever it was -> to the center of this belt
            Vector3 targetPos = transform.position + Vector3.up * itemHeightOffset;
            internalVisual.InitializeMovement(internalVisual.transform.position, targetPos);
        }
    }
}