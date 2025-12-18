using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;
using System.Collections.Generic;

public class BuildingSystem : MonoBehaviour
{
    public static BuildingSystem Instance { get; private set; }

    [Header("Settings")]
    public LayerMask groundLayer = 1;
    public Material validMat;
    public Material invalidMat; // Red Transparent Material

    [Header("Economy")]
    public int refundPercentage = 100;

    // State
    private BuildingDefinition selectedBuilding;
    private int currentRotation = 0;
    private GameObject singleGhost;

    // Dragging Logic
    private bool isDragging = false;
    private bool isDeleting = false;
    private Vector2Int dragStartPos;
    private List<GameObject> dragGhosts = new List<GameObject>();

    // Input
    private Camera mainCam;
    private InputActionMap buildInput;
    private InputAction pointAction;
    private InputAction clickAction; // Left Click
    private InputAction rotateAction;
    private InputAction cancelAction; // Right Click

    private void Awake() { Instance = this; }

    private void Start()
    {
        mainCam = Camera.main;
        SetupInput();
    }

    private void SetupInput()
    {
        buildInput = new InputActionMap("Building");
        pointAction = buildInput.AddAction("Point", binding: "<Mouse>/position");
        clickAction = buildInput.AddAction("Click", binding: "<Mouse>/leftButton");
        cancelAction = buildInput.AddAction("Cancel", binding: "<Mouse>/rightButton");
        rotateAction = buildInput.AddAction("Rotate", binding: "<Keyboard>/r");

        buildInput.Enable();
    }

    public void SelectBuilding(BuildingDefinition def)
    {
        selectedBuilding = def;
        ClearGhosts();
        CreateSingleGhost();
    }

    public void Deselect()
    {
        selectedBuilding = null;
        ClearGhosts();
    }

    private void ClearGhosts()
    {
        if (singleGhost != null) { Destroy(singleGhost); singleGhost = null; }
        foreach (var g in dragGhosts) if (g != null) Destroy(g);
        dragGhosts.Clear();
        isDragging = false;
        isDeleting = false;
    }

    private void Update()
    {
        // 1. Raycast
        Ray ray = mainCam.ScreenPointToRay(pointAction.ReadValue<Vector2>());
        bool hitGround = Physics.Raycast(ray, out RaycastHit hit, 1000f, groundLayer);
        Vector2Int currentGridPos = Vector2Int.zero;
        if (hitGround) currentGridPos = CasinoGridManager.Instance.WorldToGrid(hit.point);

        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject()) return;

        // --- GLOBAL CANCEL ---
        // If Deleting -> Left Click Cancels
        if (isDeleting && clickAction.WasPressedThisFrame())
        {
            ClearGhosts();
            return;
        }
        // If Building -> Right Click Cancels
        if (selectedBuilding != null && cancelAction.WasPressedThisFrame() && !isDragging)
        {
            Deselect();
            return;
        }

        // --- DELETE MODE (Right Click Hold on empty space) ---
        if (selectedBuilding == null)
        {
            if (cancelAction.WasPressedThisFrame() && hitGround)
            {
                isDragging = true;
                isDeleting = true;
                dragStartPos = currentGridPos;
            }

            if (isDeleting)
            {
                UpdateDeletePreview(currentGridPos);

                if (cancelAction.WasReleasedThisFrame())
                {
                    ConfirmDeleteDrag();
                }
            }
            return;
        }

        // --- BUILD MODE (Left Click) ---
        if (selectedBuilding != null && hitGround)
        {
            if (rotateAction.WasPressedThisFrame())
            {
                currentRotation = (currentRotation + 1) % 4;
            }

            if (clickAction.WasPressedThisFrame())
            {
                isDragging = true;
                dragStartPos = currentGridPos;
                singleGhost.SetActive(false);
            }

            if (isDragging)
            {
                UpdateDragPreview(currentGridPos);
                if (clickAction.WasReleasedThisFrame()) ConfirmDragBuild();
            }
            else
            {
                UpdateSingleGhost(currentGridPos);
            }
        }
    }

    // --- VISUAL METHODS ---

    void CreateSingleGhost()
    {
        if (singleGhost != null) Destroy(singleGhost);
        singleGhost = Instantiate(selectedBuilding.prefab);
        CleanupGhostVisuals(singleGhost);
    }

    void UpdateSingleGhost(Vector2Int pos)
    {
        if (singleGhost == null) CreateSingleGhost();

        singleGhost.transform.position = CasinoGridManager.Instance.GridToWorld(pos);
        singleGhost.transform.rotation = Quaternion.Euler(0, currentRotation * 90, 0);
        singleGhost.SetActive(true);

        BuildingBase logic = selectedBuilding.prefab.GetComponent<BuildingBase>();
        bool valid = CasinoGridManager.Instance.IsBuildable(pos) && logic.CanBePlacedAt(pos);
        SetGhostMaterial(singleGhost, valid);
    }

    void UpdateDragPreview(Vector2Int current)
    {
        // Calculate Line
        int dx = current.x - dragStartPos.x;
        int dy = current.y - dragStartPos.y;
        Vector2Int dir = (Mathf.Abs(dx) >= Mathf.Abs(dy)) ? new Vector2Int((int)Mathf.Sign(dx), 0) : new Vector2Int(0, (int)Mathf.Sign(dy));
        if (dir == Vector2Int.zero) dir = new Vector2Int(1, 0);

        // Rotation lock
        if (dir.x != 0) currentRotation = (dir.x > 0) ? 1 : 3;
        else currentRotation = (dir.y > 0) ? 0 : 2;

        int count = Mathf.Max(Mathf.Abs(dx), Mathf.Abs(dy)) + 1;

        EnsureGhostPool(count, selectedBuilding.prefab);

        for (int i = 0; i < count; i++)
        {
            Vector2Int pos = dragStartPos + (dir * i);
            dragGhosts[i].transform.position = CasinoGridManager.Instance.GridToWorld(pos);
            dragGhosts[i].transform.rotation = Quaternion.Euler(0, currentRotation * 90, 0);
            dragGhosts[i].SetActive(true);

            bool valid = CasinoGridManager.Instance.IsBuildable(pos);
            SetGhostMaterial(dragGhosts[i], valid);
        }
    }

    void UpdateDeletePreview(Vector2Int current)
    {
        // For Deletion, we visualize a Red Box over the area
        int dx = current.x - dragStartPos.x;
        int dy = current.y - dragStartPos.y;

        // Line logic for consistency with belts
        int count = Mathf.Max(Mathf.Abs(dx), Mathf.Abs(dy)) + 1;
        Vector2Int dir = (Mathf.Abs(dx) >= Mathf.Abs(dy)) ? new Vector2Int((int)Mathf.Sign(dx), 0) : new Vector2Int(0, (int)Mathf.Sign(dy));
        if (dir == Vector2Int.zero) dir = new Vector2Int(1, 0);

        // Use NULL to signal "Generic Red Box" to the pool
        EnsureGhostPool(count, null);

        for (int i = 0; i < count; i++)
        {
            Vector2Int pos = dragStartPos + (dir * i);
            GameObject g = dragGhosts[i];

            // Set Position
            g.transform.position = CasinoGridManager.Instance.GridToWorld(pos);
            g.transform.rotation = Quaternion.identity;
            g.SetActive(true);

            // Force Red Color (Invalid)
            SetGhostMaterial(g, false);
        }
    }

    // --- LOGIC METHODS ---

    void ConfirmDragBuild()
    {
        foreach (var ghost in dragGhosts)
        {
            Vector2Int pos = CasinoGridManager.Instance.WorldToGrid(ghost.transform.position);

            // Cost Check
            if (ResourceManager.Instance != null && !ResourceManager.Instance.CanAfford(selectedBuilding.cost)) continue;

            // Logic Check
            BuildingBase logic = selectedBuilding.prefab.GetComponent<BuildingBase>();
            bool validPos = CasinoGridManager.Instance.IsBuildable(pos);
            bool validLogic = logic.CanBePlacedAt(pos);

            if (validPos && validLogic)
            {
                if (ResourceManager.Instance != null) ResourceManager.Instance.SpendCredits(selectedBuilding.cost);

                GameObject b = Instantiate(selectedBuilding.prefab);
                BuildingBase baseScript = b.GetComponent<BuildingBase>();
                baseScript.Definition = selectedBuilding;
                baseScript.SetRotation(currentRotation);
                CasinoGridManager.Instance.PlaceBuilding(baseScript, pos);
            }
        }
        ClearGhosts();
        CreateSingleGhost();
    }

    void ConfirmDeleteDrag()
    {
        foreach (var ghost in dragGhosts)
        {
            Vector2Int pos = CasinoGridManager.Instance.WorldToGrid(ghost.transform.position);
            DeleteBuildingAt(pos);
        }
        ClearGhosts();
    }

    void DeleteBuildingAt(Vector2Int pos)
    {
        BuildingBase building = CasinoGridManager.Instance.GetBuildingAt(pos);
        if (building != null)
        {
            if (ResourceManager.Instance != null && building.Definition != null)
            {
                // Refund
                int refund = Mathf.FloorToInt(building.Definition.cost * (refundPercentage / 100f));
                ResourceManager.Instance.AddCredits(refund);
            }
            CasinoGridManager.Instance.RemoveBuilding(pos);
        }
    }

    // --- HELPERS ---

    void EnsureGhostPool(int count, GameObject prefab)
    {
        // If we switch from "Belt Ghost" to "Delete Cube", clear the pool
        string prefabName = prefab != null ? prefab.name : "Cube";
        if (dragGhosts.Count > 0 && !dragGhosts[0].name.StartsWith(prefabName))
        {
            foreach (var g in dragGhosts) Destroy(g);
            dragGhosts.Clear();
        }

        while (dragGhosts.Count < count)
        {
            GameObject g;
            if (prefab != null)
            {
                g = Instantiate(prefab);
                CleanupGhostVisuals(g);
            }
            else
            {
                // Create a primitive cube for the deletion marker
                g = GameObject.CreatePrimitive(PrimitiveType.Cube);
                Destroy(g.GetComponent<Collider>());
                g.name = "Cube_Delete_Marker";
                g.transform.localScale = new Vector3(1.8f, 1f, 1.8f); // Slightly smaller than grid 2.0
            }
            dragGhosts.Add(g);
        }

        // Hide unused
        for (int i = 0; i < dragGhosts.Count; i++) dragGhosts[i].SetActive(i < count);
    }

    void CleanupGhostVisuals(GameObject ghost)
    {
        foreach (var comp in ghost.GetComponentsInChildren<MonoBehaviour>()) Destroy(comp);
        foreach (var col in ghost.GetComponentsInChildren<Collider>()) Destroy(col);
    }

    void SetGhostMaterial(GameObject ghost, bool isValid)
    {
        Material targetMat = isValid ? validMat : invalidMat;
        if (targetMat == null) return;
        Renderer[] renderers = ghost.GetComponentsInChildren<Renderer>();
        foreach (var r in renderers) r.material = targetMat;
    }
}