using UnityEngine;

public class ConveyorBelt : BuildingBase
{
    [Header("Visuals")]
    public float itemHeightOffset = 0.15f;

    protected override void OnTick(int tick)
    {
        // Only try to push if we actually HAVE an item in our main slot
        if (internalCard != null)
        {
            TryPushItem();
        }
    }

    private void TryPushItem()
    {
        Vector2Int forwardPos = GetForwardGridPosition();
        BuildingBase target = CasinoGridManager.Instance.GetBuildingAt(forwardPos);

        if (target != null && target.CanAcceptItem(GridPosition))
        {
            // Send to target's mailbox
            target.ReceiveItem(internalCard, internalVisual);

            // Clear our main slot immediately (so we are empty for Phase 2)
            internalCard = null;
            internalVisual = null;
        }
    }

    public override bool CanAcceptItem(Vector2Int fromPos)
    {
        // Basic check: Are we full?
        if (internalCard != null || incomingCard != null) return false;

        // Logic check: Don't accept items from our own output side (no back-flow)
        Vector2Int myOutput = GetForwardGridPosition();
        return fromPos != myOutput;
    }

    protected override void OnItemArrived()
    {
        // This runs in Phase 2, when an item officially enters our main slot
        if (internalVisual != null)
        {
            internalVisual.transform.SetParent(this.transform);

            // Visual Target: The center of this belt
            Vector3 targetPos = transform.position + Vector3.up * itemHeightOffset;

            // Tell the visualizer to move from wherever it was -> to here
            internalVisual.InitializeMovement(internalVisual.transform.position, targetPos);
        }
    }
}