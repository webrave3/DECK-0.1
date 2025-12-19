using UnityEngine;

public class Splitter : BuildingBase
{
    // 0 = Forward, 1 = Left, 2 = Right
    private int outputIndex = 0;

    protected override void OnTick(int tick)
    {
        if (internalCard != null)
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
            string debugDir = "";

            // Determine direction based on current round-robin index
            switch (outputIndex)
            {
                case 0: // Forward
                    targetPos = GetForwardGridPosition();
                    debugDir = "Forward";
                    break;
                case 1: // Left
                    targetPos = GetLeftGridPosition();
                    debugDir = "Left";
                    break;
                case 2: // Right
                    targetPos = GetRightGridPosition();
                    debugDir = "Right";
                    break;
            }

            // Attempt to push
            if (AttemptPush(targetPos))
            {
                if (showDebugLogs) GameLogger.Log($"Splitter: Sent {debugDir}");

                // Cycle to next port for the NEXT item
                outputIndex = (outputIndex + 1) % 3;
                return;
            }
            else
            {
                // If blocked, temporarily try the next port in this same tick
                // But do NOT update outputIndex permanently, so we maintain order logic
                // (Or do we? Simple splitters usually cycle index only on success.
                //  Standard logic: If index 0 blocked, try 1. If 1 success, next time try 2.)

                int tempIndex = (outputIndex + 1) % 3;

                // Advance locally to try next loop iteration
                outputIndex = tempIndex;
            }
        }
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

    protected override void OnItemArrived()
    {
        if (internalVisual != null)
        {
            internalVisual.transform.SetParent(this.transform);
            // Move to center
            float duration = TickManager.Instance.tickRate;
            internalVisual.InitializeMovement(transform.position + Vector3.up * 0.2f, duration);
        }
    }

    // --- Helpers ---
    private Vector2Int GetLeftGridPosition()
    {
        int idx = (RotationIndex + 3) % 4;
        return GridPosition + GetDirFromIndex(idx);
    }

    private Vector2Int GetRightGridPosition()
    {
        int idx = (RotationIndex + 1) % 4;
        return GridPosition + GetDirFromIndex(idx);
    }

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