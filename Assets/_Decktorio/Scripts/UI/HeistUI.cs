using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class HeistUI : MonoBehaviour
{
    [Header("Vault References")]
    public Slider vaultHpSlider;
    public Image vaultFillImage;
    public TextMeshProUGUI vaultText; // NEW: Drag text here

    private void Start()
    {
        if (HeistManager.Instance != null)
        {
            HeistManager.Instance.OnHeistUpdated += UpdateUI;
            UpdateUI();
        }
    }

    private void OnDestroy()
    {
        if (HeistManager.Instance != null)
            HeistManager.Instance.OnHeistUpdated -= UpdateUI;
    }

    private void UpdateUI()
    {
        if (HeistManager.Instance == null) return;

        float current = HeistManager.Instance.vaultCurrentHP;
        float max = HeistManager.Instance.vaultMaxHP;

        // 1. Update Slider (Standard HP Bar: 1.0 is Full, 0.0 is Empty)
        if (vaultHpSlider != null)
        {
            vaultHpSlider.value = current / max;
        }

        // 2. Update Color (Blue -> Purple -> Red as it breaks)
        if (vaultFillImage != null)
        {
            // Optional: Change color based on damage
            vaultFillImage.color = Color.Lerp(Color.red, Color.cyan, current / max);
        }

        // 3. Update Text Overlay
        if (vaultText != null)
        {
            if (current <= 0)
                vaultText.text = "VAULT DESTROYED";
            else
                vaultText.text = $"{current:N0} / {max:N0} HP";
        }
    }
}