using UnityEngine;
using System.Collections.Generic;

public class PitBoss : BuildingBase, IConfigurable
{
    public enum LogicOperation { ContainsSuit, RankGreaterThan, RankLessThan, HeatGreaterThan }

    [Header("Logic Configuration")]
    public LogicOperation operation = LogicOperation.RankGreaterThan;
    public float targetValue = 10f; // For Rank/Heat
    public CardSuit targetSuit = CardSuit.Heart; // For Suit check

    protected override void OnTick(int tick)
    {
        if (internalItem != null)
        {
            EvaluateAndSort();
        }
    }

    private void EvaluateAndSort()
    {
        if (internalItem.contents.Count == 0) return;
        CardData card = internalItem.contents[internalItem.contents.Count - 1];

        bool conditionMet = false;

        switch (operation)
        {
            case LogicOperation.ContainsSuit:
                // Bitwise check: Does the card contain this suit flag?
                conditionMet = card.suit.HasFlag(targetSuit);
                break;
            case LogicOperation.RankGreaterThan:
                conditionMet = card.rank > targetValue;
                break;
            case LogicOperation.RankLessThan:
                conditionMet = card.rank < targetValue;
                break;
            case LogicOperation.HeatGreaterThan:
                conditionMet = card.heat > targetValue;
                break;
        }

        // True -> Forward, False -> Right
        Vector2Int targetPos = conditionMet ? GetForwardGridPosition() : GetRightGridPosition();

        if (AttemptPush(targetPos))
        {
            // Success
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

    private Vector2Int GetRightGridPosition()
    {
        int idx = (RotationIndex + 1) % 4;
        Vector2Int dir = Vector2Int.zero;
        if (idx == 0) dir = Vector2Int.up;
        if (idx == 1) dir = Vector2Int.right;
        if (idx == 2) dir = Vector2Int.down;
        if (idx == 3) dir = Vector2Int.left;
        return GridPosition + dir;
    }

    protected override void OnItemArrived()
    {
        if (internalVisual != null)
        {
            internalVisual.transform.SetParent(this.transform);
            internalVisual.InitializeMovement(transform.position + Vector3.up * 0.2f, TickManager.Instance.tickRate);
        }
    }

    // --- IConfigurable ---
    public string GetInspectorTitle() => "Pit Boss (Logic)";
    public string GetInspectorStatus() => $"Op: {operation}";

    public List<BuildingSetting> GetSettings()
    {
        return new List<BuildingSetting>
        {
            new BuildingSetting { settingId = "op", displayName = "Operation", options = new List<string> { "Contains Suit", "Rank >", "Rank <", "Heat >" }, currentIndex = (int)operation }
        };
    }

    public void OnSettingChanged(string settingId, int newValue)
    {
        if (settingId == "op") operation = (LogicOperation)newValue;
    }

    public Dictionary<string, int> GetConfigurationState() => new Dictionary<string, int> { { "op", (int)operation } };
    public void SetConfigurationState(Dictionary<string, int> state) { if (state.ContainsKey("op")) operation = (LogicOperation)state["op"]; }
}