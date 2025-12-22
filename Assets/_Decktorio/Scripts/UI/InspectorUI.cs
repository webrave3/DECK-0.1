using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class InspectorUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private GameObject panelRoot;
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private TextMeshProUGUI statusText;
    [SerializeField] private Button closeButton;

    [Header("Footer Buttons")]
    [SerializeField] private Button copySettingsButton;
    [SerializeField] private Button pasteSettingsButton;
    [SerializeField] private Button deleteButton; // NEW: Drag your red Trash button here

    [Header("Settings Generation")]
    [SerializeField] private Transform settingsContainer;
    [SerializeField] private GameObject settingRowPrefab;

    private IConfigurable currentTarget;
    private List<GameObject> activeSettingRows = new List<GameObject>();

    // Static clipboard so settings persist even if you close the panel
    private static Dictionary<string, int> staticSettingsClipboard;

    private void Start()
    {
        ClosePanel();
        if (closeButton != null) closeButton.onClick.AddListener(ClosePanel);

        // Footer Logic
        if (copySettingsButton != null) copySettingsButton.onClick.AddListener(OnCopyClicked);
        if (pasteSettingsButton != null) pasteSettingsButton.onClick.AddListener(OnPasteClicked);
        if (deleteButton != null) deleteButton.onClick.AddListener(OnDeleteClicked);
    }

    private void Update()
    {
        if (panelRoot.activeSelf && currentTarget != null)
        {
            statusText.text = currentTarget.GetInspectorStatus();

            // Optional: Disable paste button if clipboard is empty
            if (pasteSettingsButton != null)
                pasteSettingsButton.interactable = (staticSettingsClipboard != null);
        }
    }

    private void OnCopyClicked()
    {
        if (currentTarget != null)
        {
            staticSettingsClipboard = currentTarget.GetConfigurationState();
            Debug.Log("Settings copied to clipboard.");
        }
    }

    private void OnPasteClicked()
    {
        if (currentTarget != null && staticSettingsClipboard != null)
        {
            currentTarget.SetConfigurationState(staticSettingsClipboard);
            GenerateSettingsUI(); // Refresh the dropdowns visuals
            Debug.Log("Settings pasted.");
        }
    }

    private void OnDeleteClicked()
    {
        // 1. Verify we have a valid target
        if (currentTarget is BuildingBase building)
        {
            // 2. Calculate Refund (using logic from Economy/Building systems)
            if (building.Definition != null && BuildingSystem.Instance != null)
            {
                float refund = building.Definition.baseDebtCost * (BuildingSystem.Instance.refundPercentage / 100f);
                if (EconomyManager.Instance != null) EconomyManager.Instance.Refund(refund);
            }

            // 3. Remove from Grid (This usually destroys the GameObject too)
            if (CasinoGridManager.Instance != null)
            {
                CasinoGridManager.Instance.RemoveBuilding(building.GridPosition);
            }
            else
            {
                Destroy(building.gameObject); // Fallback
            }

            // 4. Clear Global Selection (So the yellow highlight goes away)
            if (SelectionManager.Instance != null)
            {
                SelectionManager.Instance.DeselectAll();
            }

            // 5. Close this panel
            ClosePanel();
        }
    }

    public void OpenInspector(IConfigurable target)
    {
        if (target == null) return;

        currentTarget = target;
        panelRoot.SetActive(true);
        if (titleText != null) titleText.text = currentTarget.GetInspectorTitle();
        GenerateSettingsUI();
    }

    private void GenerateSettingsUI()
    {
        foreach (var row in activeSettingRows) Destroy(row);
        activeSettingRows.Clear();

        if (settingsContainer == null || settingRowPrefab == null) return;

        List<BuildingSetting> settings = currentTarget.GetSettings();
        if (settings == null) return;

        foreach (var setting in settings)
        {
            GameObject newRow = Instantiate(settingRowPrefab, settingsContainer);
            activeSettingRows.Add(newRow);

            TextMeshProUGUI label = newRow.GetComponentInChildren<TextMeshProUGUI>();
            TMP_Dropdown dropdown = newRow.GetComponentInChildren<TMP_Dropdown>();

            if (label != null) label.text = setting.displayName;

            if (dropdown != null)
            {
                dropdown.ClearOptions();
                dropdown.AddOptions(setting.options);
                dropdown.value = setting.currentIndex;

                string capturedId = setting.settingId;
                dropdown.onValueChanged.RemoveAllListeners();
                dropdown.onValueChanged.AddListener((val) =>
                {
                    if (currentTarget != null)
                    {
                        currentTarget.OnSettingChanged(capturedId, val);
                        GenerateSettingsUI();
                    }
                });
            }
        }
    }

    public void ClosePanel()
    {
        currentTarget = null;
        if (panelRoot != null) panelRoot.SetActive(false);
    }
}