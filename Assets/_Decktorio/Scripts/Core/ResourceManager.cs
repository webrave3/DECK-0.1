using UnityEngine;
using TMPro;

public class ResourceManager : MonoBehaviour
{
    public static ResourceManager Instance { get; private set; }

    [Header("Currencies")]
    [SerializeField] private int credits = 500;
    [SerializeField] private int intel = 0;

    [Header("UI References")]
    public TextMeshProUGUI creditsText;
    public TextMeshProUGUI intelText;

    private void Awake()
    {
        if (Instance != null && Instance != this) Destroy(this);
        else Instance = this;

        UpdateUI();
    }

    public bool CanAfford(int amount)
    {
        return credits >= amount;
    }

    public void SpendCredits(int amount)
    {
        if (credits >= amount)
        {
            credits -= amount;
            UpdateUI();
        }
    }

    public void AddCredits(int amount)
    {
        credits += amount;
        UpdateUI();
    }

    public void AddIntel(int amount)
    {
        intel += amount;
        UpdateUI();
    }

    private void UpdateUI()
    {
        if (creditsText) creditsText.text = $"$: {credits}";
        if (intelText) intelText.text = $"XP: {intel}";
    }
}