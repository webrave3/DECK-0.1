using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;

public class SelectionManager : MonoBehaviour
{
    public static SelectionManager Instance { get; private set; }

    [Header("UI References")]
    public RectTransform selectionBoxUI; // Drag your UI Image here
    private Canvas parentCanvas;

    [Header("Settings")]
    public LayerMask buildingLayer;

    // State
    private Vector2 startMousePos;
    private bool isSelecting;
    private List<BuildingBase> selectedBuildings = new List<BuildingBase>();

    // Copy/Paste Data
    private List<BuildingBlueprint> clipboard = new List<BuildingBlueprint>();

    // Input
    private Mouse mouse;
    private Keyboard keyboard;

    // FIX: Changed from 'private' to 'public' so BuildingSystem can see it
    public struct BuildingBlueprint
    {
        public BuildingDefinition definition;
        public Vector2Int relPos;
        public int rotation;
    }

    private void Awake()
    {
        Instance = this;
        // Cache input devices safely
        mouse = Mouse.current;
        keyboard = Keyboard.current;

        if (selectionBoxUI != null)
        {
            parentCanvas = selectionBoxUI.GetComponentInParent<Canvas>();
            selectionBoxUI.gameObject.SetActive(false);
        }
    }

    private void Update()
    {
        if (mouse == null || keyboard == null) return;

        // 1. Handle Selection Input
        if (mouse.leftButton.wasPressedThisFrame)
        {
            // Check if we are hovering UI or if BuildingSystem is busy
            if (BuildingSystem.Instance != null && BuildingSystem.Instance.IsBuildingOrPasteActive()) return;

            startMousePos = mouse.position.ReadValue();
            isSelecting = true;
            if (selectionBoxUI) selectionBoxUI.gameObject.SetActive(true);

            if (!keyboard.shiftKey.isPressed) DeselectAll();
        }

        if (isSelecting)
        {
            UpdateSelectionBox();

            if (mouse.leftButton.wasReleasedThisFrame)
            {
                FinishSelection();
            }
        }

        // 2. Handle Shortcuts
        if (selectedBuildings.Count > 0)
        {
            // DELETE
            if (keyboard.deleteKey.wasPressedThisFrame)
            {
                DeleteSelected();
            }

            // COPY
            if (keyboard.ctrlKey.isPressed && keyboard.cKey.wasPressedThisFrame)
            {
                CopySelection();
            }
        }

        // PASTE
        if (clipboard.Count > 0 && keyboard.ctrlKey.isPressed && keyboard.vKey.wasPressedThisFrame)
        {
            PasteSelection();
        }
    }

    void UpdateSelectionBox()
    {
        if (selectionBoxUI == null) return;

        Vector2 currentPos = mouse.position.ReadValue();

        float width = currentPos.x - startMousePos.x;
        float height = currentPos.y - startMousePos.y;

        selectionBoxUI.sizeDelta = new Vector2(Mathf.Abs(width), Mathf.Abs(height));
        selectionBoxUI.anchoredPosition = startMousePos + new Vector2(width / 2, height / 2);
    }

    void FinishSelection()
    {
        isSelecting = false;
        if (selectionBoxUI) selectionBoxUI.gameObject.SetActive(false);

        // Handle Single Click vs Drag
        if (selectionBoxUI != null && selectionBoxUI.sizeDelta.magnitude < 5f)
        {
            Ray ray = Camera.main.ScreenPointToRay(mouse.position.ReadValue());
            if (Physics.Raycast(ray, out RaycastHit hit, 1000f, buildingLayer))
            {
                BuildingBase b = hit.collider.GetComponentInParent<BuildingBase>();
                if (b != null) HighlightBuilding(b);
            }
            return;
        }

        // Find all buildings in Screen Rect
        // Optimization: In a huge game, use a Spatial Grid. For now, FindObjects is okay or use GridManager list.
        foreach (var building in FindObjectsOfType<BuildingBase>())
        {
            Vector3 screenPos = Camera.main.WorldToScreenPoint(building.transform.position);

            // Check bounds
            float minX = Mathf.Min(startMousePos.x, mouse.position.ReadValue().x);
            float maxX = Mathf.Max(startMousePos.x, mouse.position.ReadValue().x);
            float minY = Mathf.Min(startMousePos.y, mouse.position.ReadValue().y);
            float maxY = Mathf.Max(startMousePos.y, mouse.position.ReadValue().y);

            if (screenPos.x > minX && screenPos.x < maxX && screenPos.y > minY && screenPos.y < maxY)
            {
                HighlightBuilding(building);
            }
        }
    }

    void HighlightBuilding(BuildingBase b)
    {
        if (!selectedBuildings.Contains(b))
        {
            selectedBuildings.Add(b);
            // Visual Feedback: Tint Yellow
            var r = b.GetComponentInChildren<Renderer>();
            if (r) r.material.color = Color.yellow;
        }
    }

    public void DeselectAll()
    {
        foreach (var b in selectedBuildings)
        {
            if (b != null)
            {
                var r = b.GetComponentInChildren<Renderer>();
                if (r) r.material.color = Color.white; // Reset to white (or original color)
            }
        }
        selectedBuildings.Clear();
    }

    void DeleteSelected()
    {
        foreach (var b in selectedBuildings)
        {
            if (b != null)
                CasinoGridManager.Instance.RemoveBuilding(b.GridPosition);
        }
        selectedBuildings.Clear();
    }

    void CopySelection()
    {
        clipboard.Clear();
        if (selectedBuildings.Count == 0) return;

        // Pivot is the first selected object
        Vector2Int pivot = selectedBuildings[0].GridPosition;

        foreach (var b in selectedBuildings)
        {
            clipboard.Add(new BuildingBlueprint
            {
                definition = b.Definition,
                relPos = b.GridPosition - pivot,
                rotation = b.RotationIndex
            });
        }
        Debug.Log($"[SelectionManager] Copied {clipboard.Count} items.");
    }

    void PasteSelection()
    {
        if (clipboard.Count == 0) return;
        BuildingSystem.Instance.StartPasteMode(clipboard);
    }

    public List<BuildingBlueprint> GetClipboard() => clipboard;
}