using UnityEngine;
using System.Collections.Generic;

public class Splitter : BuildingBase
{
    private int outputIndex = 0;

    protected override void OnTick(int tick)
    {
        if (internalCard != null)
        {
            TryDistributeItem();
        }
    }

    private void TryDistributeItem()
    {
        // 1. Gather all connected belts
        List<Vector2Int> validTargets = new List<Vector2Int>();

        // Check Front, Left, Right
        AddIfValid(GetForwardGridPosition(), validTargets);
        AddIfValid(GetLeftGridPosition(), validTargets);
        AddIfValid(GetRightGridPosition(), validTargets);

        // If nowhere to go, stop
        if (validTargets.Count == 0) return;

        // 2. Round-Robin Push
        int attempts = 0;

        while (attempts < validTargets.Count)
        {
            // Cycle through available targets
            int index = outputIndex % validTargets.Count;
            Vector2Int targetPos = validTargets[index];

            if (AttemptPush(targetPos))
            {
                outputIndex++; // Move to next for next time
                break;
            }

            // If that target was full, try the next one immediately
            outputIndex++;
            attempts++;
        }
    }

    private void AddIfValid(Vector2Int pos, List<Vector2Int> list)
    {
        BuildingBase b = CasinoGridManager.Instance.GetBuildingAt(pos);
        if (b != null)
        {
            list.Add(pos);
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

    // Helper duplicated to avoid dependency
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