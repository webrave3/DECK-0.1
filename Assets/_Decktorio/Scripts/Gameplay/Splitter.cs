using UnityEngine;
using System.Collections.Generic;

public class Splitter : BuildingBase, IConfigurable
{
    public enum SplitterPriority { RoundRobin = 0, Forward = 1, Left = 2, Right = 3 }

    [Header("Settings")]
    public SplitterPriority priorityMode = SplitterPriority.RoundRobin;

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
        // Define the check order based on priority
        List<int> checkOrder = new List<int>();

        if (priorityMode == SplitterPriority.RoundRobin)
        {
            // Standard cycle
            checkOrder.Add(outputIndex);
            checkOrder.Add((outputIndex + 1) % 3);
            checkOrder.Add((outputIndex + 2) % 3);
        }
        else
        {
            // Priority first
            int pIndex = 0; // Default Forward
            if (priorityMode == SplitterPriority.Left) pIndex = 1;
            if (priorityMode == SplitterPriority.Right) pIndex = 2;

            checkOrder.Add(pIndex);
            // Then the others
            checkOrder.Add((pIndex + 1) % 3);
            checkOrder.Add((pIndex + 2) % 3);
        }

        // Try pushing
        foreach (int idx in checkOrder)
        {
            Vector2Int targetPos = Vector2Int.zero;
            switch (idx)
            {
                case 0: targetPos = GetForwardGridPosition(); break;
                case 1: targetPos = GetLeftGridPosition(); break;
                case 2: targetPos = GetRightGridPosition(); break;
            }

            if (AttemptPush(targetPos))
            {
                // If Round Robin, advance index
                if (priorityMode == SplitterPriority.RoundRobin)
                {
                    outputIndex = (outputIndex + 1) % 3;
                }
                return; // Success
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

    // --- IConfigurable ---

    public string GetInspectorTitle() => "Splitter";

    public string GetInspectorStatus() => $"Priority: {priorityMode}";

    public List<BuildingSetting> GetSettings()
    {
        return new List<BuildingSetting>
        {
            new BuildingSetting
            {
                settingId = "priority",
                displayName = "Output Priority",
                options = new List<string> { "Round Robin", "Forward", "Left", "Right" },
                currentIndex = (int)priorityMode
            }
        };
    }

    public void OnSettingChanged(string settingId, int newValue)
    {
        if (settingId == "priority") priorityMode = (SplitterPriority)newValue;
    }

    public Dictionary<string, int> GetConfigurationState()
    {
        return new Dictionary<string, int> { { "priority", (int)priorityMode } };
    }

    public void SetConfigurationState(Dictionary<string, int> state)
    {
        if (state.ContainsKey("priority")) priorityMode = (SplitterPriority)state["priority"];
    }
}