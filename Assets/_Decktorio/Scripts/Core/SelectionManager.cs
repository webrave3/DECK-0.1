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
    public Color deleteColor = Color.red;

    // State
    private Vector2 startMousePos;
    private bool isSelecting;
    private bool isDeconstructMode;
    private List<BuildingBase> selectedBuildings = new List<BuildingBase>();

    // Cache for coloring
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
        // Singleton Setup that destroys duplicates automatically
        if (Instance != null && Instance != this)
        {
            Debug.LogError($"[SelectionManager] Duplicate detected on {gameObject.name}. Destroying it.");
            Destroy(this);
            return;
        }
        Instance = this;

        mouse = Mouse.current;
        keyboard = Keyboard.current;

        if (selectionBoxUI != null)
        {
            parentCanvas = selectionBoxUI.GetComponentInParent<Canvas>();
            selectionBoxUI.gameObject.SetActive(false);
        }
    }

    // --- BRUTE FORCE FIX: SAFETY SHUTDOWN ---
    private void OnDisable()
    {
        // If this script gets disabled (by BuildingSystem), force the UI to hide immediately.
        isSelecting = false;
        if (selectionBoxUI != null) selectionBoxUI.gameObject.SetActive(false);
    }
    // ----------------------------------------

    private void Update()
    {
        if (mouse == null || keyboard == null) return;

        // 1. Handle Selection Input
        bool leftClick = mouse.leftButton.wasPressedThisFrame;
        bool rightClick = mouse.rightButton.wasPressedThisFrame;

        if (!isSelecting && (leftClick || rightClick))
        {
            // Safety: Don't start dragging from (0,0) or if mouse is over UI
            if (mouse.position.ReadValue().sqrMagnitude < 1) return;
            if (UnityEngine.EventSystems.EventSystem.current != null &&
                UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject()) return;

            StartSelection(rightClick);
        }

        if (isSelecting)
        {
            UpdateSelectionBox();
            UpdateHighlightPreview();

            // Finish on Release
            if (!isDeconstructMode && mouse.leftButton.wasReleasedThisFrame)
            {
                FinishSelection();
            }
            else if (isDeconstructMode && mouse.rightButton.wasReleasedThisFrame)
            {
                FinishDeconstruction();
            }
        }

        // 2. Handle Shortcuts
        if (selectedBuildings.Count > 0 && !isSelecting)
        {
            if (keyboard.deleteKey.wasPressedThisFrame) DeleteSelected();

            if (keyboard.ctrlKey.isPressed && keyboard.cKey.wasPressedThisFrame) CopySelection();
        }

        if (clipboard.Count > 0 && keyboard.ctrlKey.isPressed && keyboard.vKey.wasPressedThisFrame)
        {
            PasteSelection();
        }
    }

    void StartSelection(bool deconstruct)
    {
        if (!keyboard.shiftKey.isPressed || deconstruct) DeselectAll();

        startMousePos = mouse.position.ReadValue();
        isSelecting = true;
        isDeconstructMode = deconstruct;

        if (selectionBoxUI)
        {
            selectionBoxUI.gameObject.SetActive(true);
            var img = selectionBoxUI.GetComponent<UnityEngine.UI.Image>();
            if (img) img.color = deconstruct ? new Color(1, 0, 0, 0.2f) : new Color(0, 1, 1, 0.2f);
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

    void UpdateHighlightPreview()
    {
        Vector2 min = Vector2.Min(startMousePos, mouse.position.ReadValue());
        Vector2 max = Vector2.Max(startMousePos, mouse.position.ReadValue());

        // Single click check
        if ((max - min).magnitude < 5f)
        {
            Ray ray = Camera.main.ScreenPointToRay(mouse.position.ReadValue());
            if (Physics.Raycast(ray, out RaycastHit hit, 1000f, buildingLayer))
            {
                BuildingBase b = hit.collider.GetComponentInParent<BuildingBase>();
                if (b != null && !selectedBuildings.Contains(b)) HighlightBuilding(b);
            }
            return;
        }

        BuildingBase[] allBuildings = FindObjectsByType<BuildingBase>(FindObjectsSortMode.None);
        foreach (var b in allBuildings)
        {
            Vector3 screenPos = Camera.main.WorldToScreenPoint(b.transform.position);
            bool inBox = screenPos.x > min.x && screenPos.x < max.x && screenPos.y > min.y && screenPos.y < max.y;

            if (inBox && !selectedBuildings.Contains(b))
            {
                HighlightBuilding(b);
            }
            else if (!inBox && selectedBuildings.Contains(b))
            {
                RestoreBuildingColor(b);
                selectedBuildings.Remove(b);
            }
        }
    }

    void HighlightBuilding(BuildingBase b)
    {
        selectedBuildings.Add(b);
        var r = b.GetComponentInChildren<Renderer>();
        if (r)
        {
            int id = r.GetInstanceID();
            if (!originalColors.ContainsKey(id)) originalColors[id] = r.material.color;
            r.material.color = isDeconstructMode ? deleteColor : selectColor;
        }
    }

    void RestoreBuildingColor(BuildingBase b)
    {
        if (b == null) return;
        var r = b.GetComponentInChildren<Renderer>();
        if (r && originalColors.TryGetValue(r.GetInstanceID(), out Color original))
        {
            r.material.color = original;
            originalColors.Remove(r.GetInstanceID());
        }
    }

    void FinishSelection()
    {
        isSelecting = false;
        if (selectionBoxUI) selectionBoxUI.gameObject.SetActive(false);
    }

    void FinishDeconstruction()
    {
        FinishSelection();
        DeleteSelected();
    }

    public void DeselectAll()
    {
        foreach (var b in selectedBuildings) RestoreBuildingColor(b);
        selectedBuildings.Clear();
        originalColors.Clear();
    }

    void DeleteSelected()
    {
        float totalRefund = 0;
        foreach (var b in selectedBuildings)
        {
            if (b != null)
            {
                if (b.Definition != null && BuildingSystem.Instance != null)
                    totalRefund += b.Definition.baseDebtCost * (BuildingSystem.Instance.refundPercentage / 100f);

                CasinoGridManager.Instance.RemoveBuilding(b.GridPosition);
            }
        }

        if (totalRefund > 0 && EconomyManager.Instance != null) EconomyManager.Instance.Refund(totalRefund);

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
        Debug.Log($"Copied {clipboard.Count} items.");
    }

    void PasteSelection()
    {
        if (clipboard.Count > 0) BuildingSystem.Instance.StartPasteMode(clipboard);
    }

    public List<BuildingBlueprint> GetClipboard() => clipboard;
}