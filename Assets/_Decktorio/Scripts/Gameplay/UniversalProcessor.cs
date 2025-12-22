using UnityEngine;
using System.Collections.Generic;

public class UniversalProcessor : BuildingBase, IConfigurable
{
    public enum ProcessorMode
    {
        SetSuit = 0,
        SetRank = 1,
        AddRank = 2,
        CycleSuit = 3
    }

    [Header("Processor Configuration")]
    [SerializeField] private ProcessorMode currentMode = ProcessorMode.SetSuit;
    [SerializeField] private CardSuit targetSuit = CardSuit.Heart;
    [SerializeField] private int rankValue = 1;

    [Header("Visuals")]
    public Transform animationPart;
    public float animationSpeed = 360f;
    public float processDuration = 1.0f;

    private float processTimer = 0f;
    private bool isProcessing = false;

    // --- Processing Logic ---

    protected override void OnTick(int tick)
    {
        if (internalItem != null && !isProcessing)
        {
            if (internalItem.contents.Count > 0)
            {
                isProcessing = true;
                processTimer = 0f;
            }
            else
            {
                TryPushItem();
            }
        }

        if (isProcessing)
        {
            processTimer += TickManager.Instance.tickRate;

            if (animationPart != null)
                animationPart.Rotate(Vector3.up * animationSpeed * TickManager.Instance.tickRate);

            if (processTimer >= processDuration)
            {
                CompleteProcessing();
            }
        }
    }

    void CompleteProcessing()
    {
        if (internalItem == null) return;

        // 1. Modify Data
        for (int i = 0; i < internalItem.contents.Count; i++)
        {
            CardData original = internalItem.contents[i];
            internalItem.contents[i] = ApplyOperation(original);
        }

        // 2. Force Visual Update
        // This fixes the bug where the card data changed but the visual looked the same until it moved
        if (internalVisual != null)
        {
            internalVisual.SetVisuals(internalItem);
        }

        isProcessing = false;
        TryPushItem();
    }

    private CardData ApplyOperation(CardData card)
    {
        switch (currentMode)
        {
            case ProcessorMode.SetSuit:
                card.suit = targetSuit;
                break;

            case ProcessorMode.SetRank:
                card.rank = Mathf.Clamp(rankValue, 1, 13);
                break;

            case ProcessorMode.AddRank:
                card.rank += rankValue;
                // Wrap 14->1 (Ace), 15->2, etc.
                while (card.rank > 13) card.rank -= 13;
                break;

            case ProcessorMode.CycleSuit:
                // FIX: Safe cycle that skips 'None' (0)
                int next = (int)card.suit + 1;
                if (next > 4) next = 1; // Wrap Spade(4) -> Heart(1)
                card.suit = (CardSuit)next;
                break;
        }
        return card;
    }

    void TryPushItem()
    {
        Vector2Int forwardPos = GetForwardGridPosition();
        BuildingBase target = CasinoGridManager.Instance.GetBuildingAt(forwardPos);

        if (target != null && target.CanAcceptItem(GridPosition))
        {
            target.ReceiveItem(internalItem, internalVisual);
            internalItem = null;
            internalVisual = null;
        }
    }

    // --- IConfigurable Implementation ---

    public string GetInspectorTitle() => "Universal Processor";

    public string GetInspectorStatus()
    {
        string txt = $"Mode: {currentMode}\n";
        if (isProcessing) txt += $"Processing: {(processTimer / processDuration * 100):0}%";
        else txt += "Idle";
        return txt;
    }

    public List<BuildingSetting> GetSettings()
    {
        List<BuildingSetting> settings = new List<BuildingSetting>();

        // 1. Operation Mode
        settings.Add(new BuildingSetting
        {
            settingId = "mode",
            displayName = "Operation",
            options = new List<string> { "Set Suit", "Set Rank", "Add Rank", "Cycle Suit" },
            currentIndex = (int)currentMode
        });

        // 2. Dynamic Second Setting
        if (currentMode == ProcessorMode.SetSuit)
        {
            // FIX: Reordered to match Enum order (Heart=1, Diamond=2, Club=3, Spade=4)
            // Note: We skip 'None' so index 0 = Heart
            settings.Add(new BuildingSetting
            {
                settingId = "suit",
                displayName = "Target Suit",
                options = new List<string> { "Hearts", "Diamonds", "Clubs", "Spades" },
                currentIndex = Mathf.Clamp((int)targetSuit - 1, 0, 3)
            });
        }
        else if (currentMode == ProcessorMode.SetRank || currentMode == ProcessorMode.AddRank)
        {
            // Create a list 1-13 (Ace..King)
            List<string> rankOptions = new List<string>();
            for (int i = 1; i <= 13; i++) rankOptions.Add(GetRankName(i));

            settings.Add(new BuildingSetting
            {
                settingId = "rank",
                displayName = currentMode == ProcessorMode.AddRank ? "Value to Add" : "Target Rank",
                options = rankOptions,
                currentIndex = Mathf.Clamp(rankValue - 1, 0, 12)
            });
        }

        return settings;
    }

    public void OnSettingChanged(string settingId, int newValue)
    {
        if (settingId == "mode")
        {
            currentMode = (ProcessorMode)newValue;
        }
        else if (settingId == "suit")
        {
            // FIX: Map Dropdown Index (0-3) back to Enum (1-4)
            targetSuit = (CardSuit)(newValue + 1);
        }
        else if (settingId == "rank")
        {
            rankValue = newValue + 1; // Index 0 is Rank 1
        }
    }

    // --- Copy / Paste Support ---

    public Dictionary<string, int> GetConfigurationState()
    {
        return new Dictionary<string, int>
        {
            { "mode", (int)currentMode },
            { "suit", (int)targetSuit },
            { "rank", rankValue - 1 } // Store as index
        };
    }

    public void SetConfigurationState(Dictionary<string, int> state)
    {
        if (state.ContainsKey("mode")) currentMode = (ProcessorMode)state["mode"];
        if (state.ContainsKey("suit")) targetSuit = (CardSuit)state["suit"];
        if (state.ContainsKey("rank")) rankValue = state["rank"] + 1;
    }

    private string GetRankName(int r)
    {
        if (r == 1) return "Ace (1)";
        if (r == 11) return "Jack (11)";
        if (r == 12) return "Queen (12)";
        if (r == 13) return "King (13)";
        return r.ToString();
    }
}