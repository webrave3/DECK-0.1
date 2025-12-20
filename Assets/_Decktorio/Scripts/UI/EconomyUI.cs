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

        // Ensure slider is not interactive (it's a meter)
        if (debtSlider != null) debtSlider.interactable = false;
    }

    private void OnDestroy()
    {
        if (EconomyManager.Instance != null)
            EconomyManager.Instance.OnEconomyUpdated -= UpdateUI;
    }

    private void UpdateUI()
    {
        float debt = EconomyManager.Instance.currentDebt;
        float max = EconomyManager.Instance.creditLimit;
        float cash = EconomyManager.Instance.availableCash;

        // Update Slider
        if (debtSlider != null && max > 0)
        {
            // Value is % of credit limit used
            debtSlider.value = debt / max;

            // Visual Flair: Turn Red if near limit
            if (fillImage != null)
            {
                fillImage.color = Color.Lerp(Color.green, Color.red, debtSlider.value);
            }
        }

        // Update Text
        if (debtText != null) debtText.text = $"DEBT: ${debt:F0} / ${max:F0}";
        if (cashText != null) cashText.text = $"CASH: ${cash:F0}";
    }
}