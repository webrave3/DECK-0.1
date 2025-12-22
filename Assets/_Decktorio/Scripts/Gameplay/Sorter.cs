using UnityEngine;
using System.Collections.Generic;

public class Sorter : BuildingBase, IConfigurable
{
    public enum SortMode
    {
        MatchSuit = 0,
        MatchRank = 1,
        RankGreater = 2,
        RankLess = 3
    }

    [Header("Filter Settings")]
    [SerializeField] private SortMode currentMode = SortMode.MatchSuit;
    [SerializeField] private CardSuit targetSuit = CardSuit.Heart;
    [SerializeField] private int targetRank = 1;

    [Header("Visuals")]
    public float itemHeightOffset = 0.2f;

    protected override void OnTick(int tick)
    {
        if (internalItem != null) HandleSorting();
    }

    private void HandleSorting()
    {
        if (internalItem.contents.Count == 0) return;

        // Look at the top card (Visual match)
        CardData topCard = internalItem.contents[internalItem.contents.Count - 1];

        bool isMatch = CheckMatch(topCard);

        if (showDebugLogs)
        {
            string filterDebug = (currentMode == SortMode.MatchSuit) ? targetSuit.ToString() : $"Rank {targetRank}";
            Debug.Log($"[Sorter] Card: {topCard.suit} vs Filter: {filterDebug} = {isMatch}");
        }

        Vector2Int targetPos = isMatch ? GetLeftGridPosition() : GetForwardGridPosition();

        if (AttemptPush(targetPos))
        {
            // Success
        }
    }

    private bool CheckMatch(CardData card)
    {
        switch (currentMode)
        {
            case SortMode.MatchSuit:
                return card.suit == targetSuit;
            case SortMode.MatchRank:
                return card.rank == targetRank;
            case SortMode.RankGreater:
                return card.rank > targetRank;
            case SortMode.RankLess:
                return card.rank < targetRank;
            default:
                return false;
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
            Vector3 targetPos = transform.position + Vector3.up * itemHeightOffset;
            float duration = TickManager.Instance.tickRate;
            internalVisual.InitializeMovement(targetPos, duration);
        }
    }

    private Vector2Int GetLeftGridPosition()
    {
        int idx = (RotationIndex + 3) % 4;
        return GridPosition + ConveyorBelt.GetDirFromIndex(idx);
    }

    // --- IConfigurable Implementation ---

    public string GetInspectorTitle() => "Smart Sorter";

    public string GetInspectorStatus()
    {
        string s = $"Mode: {currentMode}\nFilter: ";
        if (currentMode == SortMode.MatchSuit) s += targetSuit.ToString();
        else s += GetRankName(targetRank);
        return s;
    }

    public List<BuildingSetting> GetSettings()
    {
        List<BuildingSetting> settings = new List<BuildingSetting>();

        // 1. Mode Setting
        settings.Add(new BuildingSetting
        {
            settingId = "mode",
            displayName = "Filter Mode",
            options = new List<string> { "Match Suit", "Match Rank", "Rank >", "Rank <" },
            currentIndex = (int)currentMode
        });

        // 2. Dynamic Setting (Suit or Rank)
        if (currentMode == SortMode.MatchSuit)
        {
            // FIX: Order must match Enum (Heart=1, Diamond=2, Club=3, Spade=4)
            // We subtract 1 from targetSuit to get the Index (Heart(1) -> Index 0)
            settings.Add(new BuildingSetting
            {
                settingId = "suit",
                displayName = "Target Suit",
                options = new List<string> { "Hearts", "Diamonds", "Clubs", "Spades" },
                currentIndex = Mathf.Clamp((int)targetSuit - 1, 0, 3)
            });
        }
        else
        {
            List<string> ranks = new List<string>();
            for (int i = 1; i <= 13; i++) ranks.Add(GetRankName(i));

            settings.Add(new BuildingSetting
            {
                settingId = "rank",
                displayName = "Target Rank",
                options = ranks,
                currentIndex = Mathf.Clamp(targetRank - 1, 0, 12)
            });
        }
        return settings;
    }

    public void OnSettingChanged(string settingId, int newValue)
    {
        if (settingId == "mode")
        {
            currentMode = (SortMode)newValue;
        }
        else if (settingId == "suit")
        {
            // FIX: Dropdown 0 is Hearts (Enum 1). Add 1 to convert Index -> Enum.
            targetSuit = (CardSuit)(newValue + 1);
        }
        else if (settingId == "rank")
        {
            targetRank = newValue + 1;
        }
    }

    // --- Copy / Paste Support ---

    public Dictionary<string, int> GetConfigurationState()
    {
        return new Dictionary<string, int>
        {
            { "mode", (int)currentMode },
            { "suit", (int)targetSuit },
            { "rank", targetRank - 1 }
        };
    }

    public void SetConfigurationState(Dictionary<string, int> state)
    {
        if (state.ContainsKey("mode")) currentMode = (SortMode)state["mode"];
        if (state.ContainsKey("suit")) targetSuit = (CardSuit)state["suit"];
        if (state.ContainsKey("rank")) targetRank = state["rank"] + 1;
    }

    private string GetRankName(int r)
    {
        if (r == 1) return "Ace";
        if (r == 11) return "Jack";
        if (r == 12) return "Queen";
        if (r == 13) return "King";
        return r.ToString();
    }
}