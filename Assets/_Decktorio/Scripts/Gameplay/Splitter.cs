using UnityEngine;

public class Splitter : BuildingBase
{
    // 0 = Forward, 1 = Left, 2 = Right
    private int outputIndex = 0;

    protected override void OnTick(int tick)
    {
        if (internalItem != null)
        {
            HandleSplit();
        }
    }

    private void HandleSplit()
    {
        // Try up to 3 ports to find a free one
        for (int i = 0; i < 3; i++)
        {
            Vector2Int targetPos = Vector2Int.zero;

            switch (outputIndex)
            {
                case 0: targetPos = GetForwardGridPosition(); break;
                case 1: targetPos = GetLeftGridPosition(); break;
                case 2: targetPos = GetRightGridPosition(); break;
            }

            // Attempt to push
            if (AttemptPush(targetPos))
            {
                // Cycle to next port for the NEXT item
                outputIndex = (outputIndex + 1) % 3;
                return;
            }
            else
            {
                // If blocked, try next port immediately
                // but keep current outputIndex logically pending until we succeed?
                // Or just skip. Let's skip to keep flow moving.
                outputIndex = (outputIndex + 1) % 3;
            }
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
            float duration = TickManager.Instance.tickRate;
            internalVisual.InitializeMovement(transform.position + Vector3.up * 0.2f, duration);
        }
    }

    // --- Helpers ---
    private Vector2Int GetLeftGridPosition()
    {
        int idx = (RotationIndex + 3) % 4;
        return GridPosition + ConveyorBelt.GetDirFromIndex(idx);
    }

    private Vector2Int GetRightGridPosition()
    {
        int idx = (RotationIndex + 1) % 4;
        return GridPosition + ConveyorBelt.GetDirFromIndex(idx);
    }
}