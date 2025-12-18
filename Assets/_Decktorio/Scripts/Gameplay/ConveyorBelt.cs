using UnityEngine;

public class ConveyorBelt : BuildingBase
{
    [Header("Visuals")]
    public Transform visualAnchor;
    public float itemHeightOffset = 0.15f;

    private ItemVisualizer currentVisual;

    protected override void HandleTick(int tick)
    {
        if (internalCard != null)
        {
            TryPushItem();
        }
    }

    private void TryPushItem()
    {
        Vector2Int forwardPos = GetForwardGridPosition();
        BuildingBase targetBuilding = CasinoGridManager.Instance.GetBuildingAt(forwardPos);

        if (targetBuilding != null && targetBuilding.CanAcceptItem(GridPosition))
        {
            // Move Data
            targetBuilding.ReceiveItem(internalCard, currentVisual);

            internalCard = null;
            currentVisual = null;
        }
    }

    public override void ReceiveItem(CardPayload item, ItemVisualizer visual)
    {
        // Mark as processed so we don't push it again in this SAME tick
        // (This fixes the "belt becomes faster the longer it is" bug)
        lastProcessedTick = Time.frameCount; // Or use a specialized counter if frameCount is risky, but works for MVP

        internalCard = item;
        currentVisual = visual;

        if (currentVisual != null)
        {
            currentVisual.transform.SetParent(this.transform);

            // Calculate strictly local position to prevent "floating away"
            Vector3 myCenter = transform.position + Vector3.up * itemHeightOffset;

            // Start visual movement
            currentVisual.InitializeMovement(currentVisual.transform.position, myCenter);
        }
    }

    public override bool CanAcceptItem(Vector2Int fromPos)
    {
        if (internalCard != null) return false;
        Vector2Int myOutput = GetForwardGridPosition();
        return fromPos != myOutput;
    }
}