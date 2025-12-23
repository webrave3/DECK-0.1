using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.InputSystem; // REQUIRED for New Input System

public class ManagementWindowUI : MonoBehaviour
{
    [Header("Window State")]
    [Tooltip("The actual panel that pops up")]
    public GameObject windowContent;
    public bool startOpen = false;

    [Header("The Dock (Top Left Buttons)")]
    public Button btnOpenMarket;
    public Button btnOpenTech;
    public Button btnOpenDeck;

    [Header("Window Tabs (Inside the Panel)")]
    public Button[] internalTabButtons; // The buttons inside the window (Top bar of the tablet)
    public GameObject[] tabPages;       // The content parent for each tab

    [Header("Visuals")]
    public Color activeTabColor = Color.white;
    public Color inactiveTabColor = new Color(0.5f, 0.5f, 0.5f);

    private void Start()
    {
        // 1. Setup Dock Buttons (Clicking them opens the window AND correct tab)
        if (btnOpenMarket) btnOpenMarket.onClick.AddListener(() => OpenWindow(0));
        if (btnOpenTech) btnOpenTech.onClick.AddListener(() => OpenWindow(1));
        if (btnOpenDeck) btnOpenDeck.onClick.AddListener(() => OpenWindow(2));

        // 2. Setup Internal Tab Buttons
        for (int i = 0; i < internalTabButtons.Length; i++)
        {
            int x = i; // Local copy for closure
            internalTabButtons[i].onClick.AddListener(() => SwitchTab(x));
        }

        // 3. Initialize
        if (windowContent != null)
            windowContent.SetActive(startOpen);

        if (startOpen) SwitchTab(0);
    }

    private void Update()
    {
        // FIX: Using New Input System to check for Escape key
        if (windowContent != null && windowContent.activeSelf)
        {
            if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                CloseWindow();
            }
        }
    }

    // Opens the main window and jumps to a specific tab
    public void OpenWindow(int tabIndex)
    {
        if (windowContent != null)
            windowContent.SetActive(true);

        SwitchTab(tabIndex);
    }

    public void CloseWindow()
    {
        if (windowContent != null)
            windowContent.SetActive(false);
    }

    private void SwitchTab(int tabIndex)
    {
        // Loop through all tabs
        for (int i = 0; i < tabPages.Length; i++)
        {
            bool isActive = (i == tabIndex);

            // Toggle Page Content
            if (tabPages[i] != null)
                tabPages[i].SetActive(isActive);

            // Update Tab Button Visuals
            if (i < internalTabButtons.Length && internalTabButtons[i] != null)
            {
                var colors = internalTabButtons[i].colors;
                colors.normalColor = isActive ? activeTabColor : inactiveTabColor;
                internalTabButtons[i].colors = colors;

                // Optional: Make text bold if active
                var txt = internalTabButtons[i].GetComponentInChildren<TextMeshProUGUI>();
                if (txt) txt.fontStyle = isActive ? FontStyles.Bold : FontStyles.Normal;
            }
        }
    }
}