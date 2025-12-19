using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;

public class SelectionManager : MonoBehaviour
{
    public static SelectionManager Instance { get; private set; }

    [Header("UI References")]
    public RectTransform selectionBoxUI;
    private Canvas parentCanvas;

    [Header("Settings")]
    public LayerMask buildingLayer;
    public Color selectColor = Color.yellow;
    public Color deleteColor = new Color(1f, 0.2f, 0.2f, 1f); // Red tint

    // --- STATE ---
    private Vector2 startMousePos;
    private bool isDragging;
    private bool isDeconstructMode; // True = Deleting, False = Selecting

    // Selection Cache
    private List<BuildingBase> selectedBuildings = new List<BuildingBase>();
    private BuildingBase[] sceneBuildingsCache;
    private Dictionary<int, Color> originalColors = new Dictionary<int, Color>();

    // Copy/Paste Data
    private List<BuildingBlueprint> clipboard = new List<BuildingBlueprint>();

    // Input
    private Mouse mouse;
    private Keyboard keyboard;

    public struct BuildingBlueprint
    {
        public BuildingDefinition definition;
        public Vector2Int relPos;
        public int rotation;
    }

    private void Awake()
    {
        Instance = this;
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

        // CRITICAL FIX: Use the property that includes the LastCancelFrame check
        if (BuildingSystem.Instance != null && BuildingSystem.Instance.IsBusyOrJustCancelled)
        {
            return;
        }

        // 1. INPUT START (Only if NOT already dragging)
        if (!isDragging)
        {
            // Left Click -> Normal Selection
            if (mouse.leftButton.wasPressedThisFrame)
            {
                StartSelection(false);
            }
            // Right Click -> Deconstruct (Delete) Selection
            else if (mouse.rightButton.wasPressedThisFrame)
            {
                StartSelection(true);
            }
        }

        // 2. INPUT DRAG & RELEASE
        if (isDragging)
        {
            UpdateSelectionBox();
            UpdateHighlightPreview();

            // Release Left Click (Confirm Selection)
            if (!isDeconstructMode && mouse.leftButton.wasReleasedThisFrame)
            {
                FinishSelection();
            }
            // Release Right Click (Confirm Deletion)
            else if (isDeconstructMode && mouse.rightButton.wasReleasedThisFrame)
            {
                FinishDeconstruction();
            }
        }

        // 3. SHORTCUTS (Only when not dragging)
        if (!isDragging && selectedBuildings.Count > 0)
        {
            // DELETE Key
            if (keyboard.deleteKey.wasPressedThisFrame)
            {
                DeleteSelected();
            }

            // COPY (Ctrl + C)
            if (keyboard.ctrlKey.isPressed && keyboard.cKey.wasPressedThisFrame)
            {
                CopySelection();
            }
        }

        // PASTE (Ctrl + V)
        if (!isDragging && clipboard.Count > 0 && keyboard.ctrlKey.isPressed && keyboard.vKey.wasPressedThisFrame)
        {
            PasteSelection();
        }
    }

    void StartSelection(bool deconstructMode)
    {
        if (!keyboard.shiftKey.isPressed || deconstructMode)
        {
            DeselectAll();
        }

        startMousePos = mouse.position.ReadValue();
        isDragging = true;
        isDeconstructMode = deconstructMode;

        if (selectionBoxUI)
        {
            selectionBoxUI.gameObject.SetActive(true);
            var img = selectionBoxUI.GetComponent<UnityEngine.UI.Image>();
            if (img) img.color = deconstructMode ? new Color(1, 0, 0, 0.2f) : new Color(0, 1, 1, 0.2f);
        }

        sceneBuildingsCache = FindObjectsByType<BuildingBase>(FindObjectsSortMode.None);
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

    void UpdateHighlightPreview()
    {
        HashSet<BuildingBase> currentFrameSelection = new HashSet<BuildingBase>();

        Vector2 min = Vector2.Min(startMousePos, mouse.position.ReadValue());
        Vector2 max = Vector2.Max(startMousePos, mouse.position.ReadValue());
        bool isClick = (max - min).magnitude < 5f;

        if (isClick)
        {
            Ray ray = Camera.main.ScreenPointToRay(mouse.position.ReadValue());
            if (Physics.Raycast(ray, out RaycastHit hit, 1000f, buildingLayer))
            {
                BuildingBase b = hit.collider.GetComponentInParent<BuildingBase>();
                if (b != null) currentFrameSelection.Add(b);
            }
        }
        else if (sceneBuildingsCache != null)
        {
            foreach (var building in sceneBuildingsCache)
            {
                if (building == null) continue;
                Vector3 screenPos = Camera.main.WorldToScreenPoint(building.transform.position);
                if (screenPos.x > min.x && screenPos.x < max.x && screenPos.y > min.y && screenPos.y < max.y)
                {
                    currentFrameSelection.Add(building);
                }
            }
        }

        // Logic for Highlight / Restore

        // 1. Un-highlight items that are NO LONGER in the selection
        foreach (var b in selectedBuildings)
        {
            if (b != null && !currentFrameSelection.Contains(b))
            {
                RestoreBuildingColor(b);
            }
        }

        // 2. Highlight items that are NEWLY in the selection
        foreach (var b in currentFrameSelection)
        {
            if (!selectedBuildings.Contains(b))
            {
                SaveAndTintBuilding(b, isDeconstructMode ? deleteColor : selectColor);
            }
        }

        // 3. Update the list
        selectedBuildings = new List<BuildingBase>(currentFrameSelection);
    }

    void SaveAndTintBuilding(BuildingBase b, Color c)
    {
        if (b == null) return;
        var r = b.GetComponentInChildren<Renderer>();
        if (r)
        {
            int id = r.GetInstanceID();
            if (!originalColors.ContainsKey(id))
            {
                originalColors[id] = r.material.color;
            }
            r.material.color = c;
        }
    }

    void RestoreBuildingColor(BuildingBase b)
    {
        if (b == null) return;
        var r = b.GetComponentInChildren<Renderer>();
        if (r)
        {
            int id = r.GetInstanceID();
            if (originalColors.TryGetValue(id, out Color original))
            {
                r.material.color = original;
                originalColors.Remove(id);
            }
        }
    }

    void FinishSelection()
    {
        isDragging = false;
        sceneBuildingsCache = null;
        if (selectionBoxUI) selectionBoxUI.gameObject.SetActive(false);
    }

    void FinishDeconstruction()
    {
        isDragging = false;
        sceneBuildingsCache = null;
        if (selectionBoxUI) selectionBoxUI.gameObject.SetActive(false);

        DeleteSelected();
    }

    public void DeselectAll()
    {
        foreach (var b in selectedBuildings)
        {
            RestoreBuildingColor(b);
        }
        selectedBuildings.Clear();
        originalColors.Clear();
    }

    void DeleteSelected()
    {
        foreach (var b in selectedBuildings)
        {
            if (b != null)
                CasinoGridManager.Instance.RemoveBuilding(b.GridPosition);
        }
        selectedBuildings.Clear();
        originalColors.Clear();
    }

    void CopySelection()
    {
        clipboard.Clear();
        if (selectedBuildings.Count == 0) return;

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