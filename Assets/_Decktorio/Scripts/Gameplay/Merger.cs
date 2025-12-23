using UnityEngine;
using System.Collections.Generic;

public class Merger : BuildingBase, IConfigurable
{
    // Round-Robin logic to act fairly
    private int inputIndex = 0;

    protected override void OnTick(int tick)
    {
        if (internalItem != null)
        {
            TryPushForward();
            return;
        }
        TryPullInputs();
    }

    private void TryPullInputs()
    {
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
                inputIndex = (inputIndex + 1) % 3;
                return;
            }
        }
    }

    private bool AttemptPullFrom(Vector2Int sourcePos)
    {
        // Mergers are generally passive in this system, relying on belts pushing IN.
        // This is a placeholder for future active pulling logic.
        return false;
    }

    public override bool CanAcceptItem(Vector2Int fromPos)
    {
        if (internalItem != null || incomingItem != null) return false;
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

    // --- IConfigurable ---

    public string GetInspectorTitle() => "Merger";
    public string GetInspectorStatus() => "Status: Active\nMerges 3 inputs to 1 output.";
    public List<BuildingSetting> GetSettings() => null; // No settings yet
    public void OnSettingChanged(string settingId, int newValue) { }
    public Dictionary<string, int> GetConfigurationState() => null;
    public void SetConfigurationState(Dictionary<string, int> state) { }
}