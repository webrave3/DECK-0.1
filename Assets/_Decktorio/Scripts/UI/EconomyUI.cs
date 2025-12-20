using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class EconomyUI : MonoBehaviour
{
    [Header("References")]
    public Slider debtSlider; // Assign a UI Slider
    public TextMeshProUGUI debtText;
    public TextMeshProUGUI cashText;
    public Image fillImage; // The fill of the slider (to change color)

    private void Start()
    {
        if (EconomyManager.Instance != null)
        {
            EconomyManager.Instance.OnEconomyUpdated += UpdateUI;
            UpdateUI(); // Initial refresh
        }
    }

    private void OnDestroy()
    {
        if (EconomyManager.Instance != null)
            EconomyManager.Instance.OnEconomyUpdated -= UpdateUI;
    }

    private void UpdateUI()
    {
        float current = EconomyManager.Instance.currentDebt;
        float max = EconomyManager.Instance.creditLimit;
        float profit = EconomyManager.Instance.liquidCash;

        // Update Slider
        if (debtSlider != null)
        {
            debtSlider.value = current / max;

            // Visual Flair: Turn Red if near limit
            if (fillImage != null)
            {
                fillImage.color = Color.Lerp(Color.green, Color.red, debtSlider.value);
            }
        }

        // Update Text
        if (debtText != null) debtText.text = $"DEBT: ${current:F0} / ${max:F0}";
        if (cashText != null) cashText.text = $"CASH: ${profit:F0}";
    }
}