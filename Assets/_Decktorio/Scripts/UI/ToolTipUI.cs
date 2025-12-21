using UnityEngine;
using TMPro;
using UnityEngine.InputSystem;

public class TooltipUI : MonoBehaviour
{
    public static TooltipUI Instance;

    [Header("UI References")]
    public GameObject tooltipPanel; // DRAG YOUR PANEL HERE
    public TextMeshProUGUI headerText;
    public TextMeshProUGUI bodyText;

    // Offset so the tooltip doesn't cover the cursor
    public Vector3 offset = new Vector3(20, -20, 0);

    private void Awake()
    {
        Instance = this;
        if (tooltipPanel != null) tooltipPanel.SetActive(false);
    }

    private void Update()
    {
        if (Mouse.current == null) return;

        // Get Mouse Position
        Vector2 mousePos2D = Mouse.current.position.ReadValue();

        // Raycast to find things to hover over
        Ray ray = Camera.main.ScreenPointToRay(mousePos2D);
        if (Physics.Raycast(ray, out RaycastHit hit))
        {
            // Check for Item Visuals (Cards on belts)
            ItemVisualizer itemVis = hit.collider.GetComponentInParent<ItemVisualizer>();
            // Check for Buildings
            HoverInfo info = hit.collider.GetComponentInParent<HoverInfo>();

            if (itemVis != null && itemVis.cachedPayload != null)
            {
                ShowTooltip("Card Data", itemVis.cachedPayload.GetDebugLabel());
                FollowMouse(mousePos2D);
                return; // Stop here so we don't flicker
            }
            else if (info != null)
            {
                ShowTooltip(info.objectName, info.GetDynamicContent());
                FollowMouse(mousePos2D);
                return;
            }
        }

        // If we hit nothing, hide
        HideTooltip();
    }

    void ShowTooltip(string header, string body)
    {
        if (tooltipPanel != null)
        {
            if (!tooltipPanel.activeSelf) tooltipPanel.SetActive(true);
            if (headerText != null) headerText.text = header;
            if (bodyText != null) bodyText.text = body;
        }
    }

    void HideTooltip()
    {
        if (tooltipPanel != null && tooltipPanel.activeSelf)
            tooltipPanel.SetActive(false);
    }

    void FollowMouse(Vector2 screenPos)
    {
        if (tooltipPanel != null)
        {
            // Offset: Move 40px Right and 40px Down to clear the mouse cursor
            float xOffset = 85f;
            float yOffset = -42f;

            Vector3 finalPos = new Vector3(screenPos.x + xOffset, screenPos.y + yOffset, 0);

            // Optional: Simple Screen Edge Protection
            // (Assumes standard 1920x1080 canvas logic, adjust if needed)
            if (finalPos.x > Screen.width - 200) finalPos.x -= 250; // Flip to left if too far right
            if (finalPos.y < 100) finalPos.y += 150; // Flip up if too far down

            tooltipPanel.transform.position = finalPos;
        }
    }
}