using UnityEngine;
using System.Collections.Generic;

public class SinkBuilding : BuildingBase, IConfigurable
{
    [Header("Sink Settings")]
    public bool isIncinerator = false;
    public float sellMultiplier = 1.0f;

    private string lastProcessInfo = "Idle";

    protected override void OnTick(int tick)
    {
        // Passive receiver - Logic happens in OnItemArrived
    }

    protected override void OnItemArrived()
    {
        if (internalItem != null)
        {
            ProcessItem(internalItem);

            if (internalVisual != null) Destroy(internalVisual.gameObject);
            internalItem = null;
            internalVisual = null;
        }
    }

    void ProcessItem(ItemPayload item)
    {
        if (isIncinerator) return;

        // 1. Evaluate
        PokerHandType handType = PokerEvaluator.Evaluate(item.contents);

        // 2. Base Value
        int baseValue = PokerEvaluator.GetHandValue(handType);

        // 3. MARKET LOGIC
        float marketMult = 1.0f;
        if (MarketManager.Instance != null)
        {
            marketMult = MarketManager.Instance.GetMultiplier(handType);
            MarketManager.Instance.RegisterSale(handType);
        }

        // 4. Calculate Final Payout (FLOAT PRECISION)
        float totalValue = baseValue * item.velocityBonus * item.valueMultiplier * marketMult;

        // Apply the Sell Multiplier (if upgrades exist)
        float finalPayout = totalValue * sellMultiplier;

        // 5. Pay
        string marketMsg = marketMult < 1.0f ? $" (Sat: {marketMult:P0})" : "";

        // Log with 2 decimals (F2)
        GameLogger.Log($"[Sink] Sold {handType}{marketMsg} for ${finalPayout:F2}");

        if (EconomyManager.Instance != null)
        {
            EconomyManager.Instance.EarnMoney(finalPayout);
        }
    }

    public override bool CanAcceptItem(Vector2Int fromPos) => internalItem == null && incomingItem == null;

    // --- IConfigurable Implementation ---

    public string GetInspectorTitle() => isIncinerator ? "Incinerator" : "Sales Terminal";

    public string GetInspectorStatus() => lastProcessInfo;

    public List<BuildingSetting> GetSettings()
    {
        return new List<BuildingSetting>
        {
            new BuildingSetting
            {
                settingId = "mode",
                displayName = "Operation Mode",
                options = new List<string> { "Sell Items", "Incinerate (Trash)" },
                currentIndex = isIncinerator ? 1 : 0
            }
        };
    }

    public void OnSettingChanged(string settingId, int newValue)
    {
        if (settingId == "mode") isIncinerator = (newValue == 1);
    }

    public Dictionary<string, int> GetConfigurationState()
    {
        return new Dictionary<string, int> { { "mode", isIncinerator ? 1 : 0 } };
    }

    public void SetConfigurationState(Dictionary<string, int> state)
    {
        if (state.ContainsKey("mode")) isIncinerator = (state["mode"] == 1);
    }
}