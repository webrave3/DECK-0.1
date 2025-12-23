using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Text.RegularExpressions; // Used to split "RoyalFlush" into "Royal Flush"

public class MarketRowUI : MonoBehaviour
{
    [Header("UI References")]
    public TextMeshProUGUI handNameText;
    public TextMeshProUGUI valueText;    // e.g. "$500"
    public TextMeshProUGUI demandText;   // e.g. "120%"
    public Image trendIcon;              // Optional arrow icon
    public Image rowBackground;

    [Header("Visual Settings")]
    public Color colorHighDemand = new Color(0.2f, 1f, 0.2f); // Neon Green
    public Color colorLowDemand = new Color(1f, 0.2f, 0.2f); // Neon Red
    public Color colorStable = new Color(0.8f, 0.8f, 0.8f); // Grey

    private PokerHandType _type;

    public void Setup(PokerHandType type)
    {
        _type = type;

        // Format Enum: "RoyalFlush" -> "Royal Flush"
        string formattedName = Regex.Replace(type.ToString(), "(\\B[A-Z])", " $1");
        if (handNameText) handNameText.text = formattedName;
    }

    public void UpdateRow()
    {
        if (MarketManager.Instance == null) return;

        // 1. Fetch Data
        float multiplier = MarketManager.Instance.GetMultiplier(_type);
        int baseValue = PokerEvaluator.GetHandValue(_type);

        // 2. Calculate Display Value (Float)
        float finalValue = baseValue * multiplier;

        // 3. Update Text (F2 = 2 Decimal Places)
        if (valueText) valueText.text = $"${finalValue:F2}";

        if (demandText)
        {
            demandText.text = $"{multiplier:P0}"; // Keep percentage as whole number (120%)
        }

        // 4. Update Colors (Same as before)
        UpdateColors(multiplier);
    }

    private void UpdateColors(float multiplier)
    {
        // ... (Keep your existing color logic here) ...
        // If you need the color logic again, let me know!
        if (multiplier > 1.05f)
        {
            if (demandText) demandText.color = colorHighDemand;
            if (valueText) valueText.color = colorHighDemand;
            if (trendIcon) { trendIcon.color = colorHighDemand; trendIcon.transform.localRotation = Quaternion.Euler(0, 0, 0); }
        }
        else if (multiplier < 0.95f)
        {
            if (demandText) demandText.color = colorLowDemand;
            if (valueText) valueText.color = colorLowDemand;
            if (trendIcon) { trendIcon.color = colorLowDemand; trendIcon.transform.localRotation = Quaternion.Euler(0, 0, 180); }
        }
        else
        {
            if (demandText) demandText.color = colorStable;
            if (valueText) valueText.color = colorStable;
            if (trendIcon) { trendIcon.color = colorStable; trendIcon.transform.localRotation = Quaternion.Euler(0, 0, -90); }
        }
    }
}