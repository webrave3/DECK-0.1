using UnityEngine;

public class Merger : BuildingBase
{
    // Round-Robin logic to act fairly
    private int inputIndex = 0;

    protected override void OnTick(int tick)
    {
        // 1. Output Logic: If we hold an item, try to push it out forward
        if (internalItem != null)
        {
            TryPushForward();
            return;
        }

        // 2. Input Logic: If empty, try to pull from neighbors
        TryPullInputs();
    }

    private void TryPullInputs()
    {
        // We check 3 sides: Left, Back, Right (relative to rotation)
        // We cycle through them so one side doesn't starve the others.
        for (int i = 0; i < 3; i++)
        {
            int checkIndex = (inputIndex + i) % 3;
            Vector2Int checkPos = Vector2Int.zero;

            switch (checkIndex)
            {
                case 0: checkPos = GetBackGridPosition(); break;
                case 1: checkPos = GetLeftGridPosition(); break;
                case 2: checkPos = GetRightGridPosition(); break;
            }

            if (AttemptPullFrom(checkPos))
            {
                // Success! Advance index for fairness next time
                inputIndex = (inputIndex + 1) % 3;
                return;
            }
        }
    }

    private bool AttemptPullFrom(Vector2Int sourcePos)
    {
        BuildingBase source = CasinoGridManager.Instance.GetBuildingAt(sourcePos);

        // Custom check: We want to pull from a ConveyorBelt or similar that is trying to push to us.
        // Usually belts push proactively. But the Merger can also act as a "Join" spot.
        // Ideally, belts pointing AT the merger will push INTO it via ReceiveItem.
        // So this script might not even need to "Pull". 
        // Standard Factory game logic: Mergers rely on belts pushing into them.
        // However, to ensure it acts as a merger, we accept items from 3 sides.
        return false;
    }

    // IMPORTANT: The Merger acts passively. It accepts items from 3 directions.
    public override bool CanAcceptItem(Vector2Int fromPos)
    {
        if (internalItem != null || incomingItem != null) return false;

        // Accept from Back, Left, Right
        if (fromPos == GetBackGridPosition()) return true;
        if (fromPos == GetLeftGridPosition()) return true;
        if (fromPos == GetRightGridPosition()) return true;

        return false;
    }

    private void TryPushForward()
    {
        Vector2Int forward = GetForwardGridPosition();
        BuildingBase target = CasinoGridManager.Instance.GetBuildingAt(forward);

        if (target != null && target.CanAcceptItem(GridPosition))
        {
            target.ReceiveItem(internalItem, internalVisual);
            internalItem = null;
            internalVisual = null;
        }
    }

    // Helpers
    public Vector2Int GetLeftGridPosition() => GridPosition + GetDirFromIndex((RotationIndex + 3) % 4);
    public Vector2Int GetRightGridPosition() => GridPosition + GetDirFromIndex((RotationIndex + 1) % 4);
    public Vector2Int GetBackGridPosition() => GridPosition + GetDirFromIndex((RotationIndex + 2) % 4);

    public static Vector2Int GetDirFromIndex(int index)
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