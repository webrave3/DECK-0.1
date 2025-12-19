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
                if (singleGhost != null) singleGhost.SetActive(false);
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
        // 1. Calculate Drag Vector
        int dx = current.x - dragStartPos.x;
        int dy = current.y - dragStartPos.y;
        Vector2Int dir = (Mathf.Abs(dx) >= Mathf.Abs(dy)) ? new Vector2Int((int)Mathf.Sign(dx), 0) : new Vector2Int(0, (int)Mathf.Sign(dy));
        if (dir == Vector2Int.zero) dir = new Vector2Int(1, 0);

        // 2. Set Rotation based on Drag Direction
        if (dir.x > 0) currentRotation = 1;      // East
        else if (dir.x < 0) currentRotation = 3; // West
        else if (dir.y > 0) currentRotation = 0; // North
        else if (dir.y < 0) currentRotation = 2; // South

        // 3. Smart Start Logic
        // Check if there is already a building at the start position
        bool startOccupied = !CasinoGridManager.Instance.IsBuildable(dragStartPos);
        int startIndex = startOccupied ? 1 : 0; // Skip the first tile if occupied (branching mode)

        int dragLength = Mathf.Max(Mathf.Abs(dx), Mathf.Abs(dy)) + 1;

        EnsureGhostPool(dragLength, selectedBuilding.prefab);

        for (int i = 0; i < dragLength; i++)
        {
            // Skip the start position if we are branching
            if (i < startIndex)
            {
                dragGhosts[i].SetActive(false);
                continue;
            }

            Vector2Int pos = dragStartPos + (dir * i);
            dragGhosts[i].transform.position = CasinoGridManager.Instance.GridToWorld(pos);
            dragGhosts[i].transform.rotation = Quaternion.Euler(0, currentRotation * 90, 0);
            dragGhosts[i].SetActive(true);

            bool valid = CasinoGridManager.Instance.IsBuildable(pos);
            SetGhostMaterial(dragGhosts[i], valid);

            // Fix: ensure ghost mesh is active
            ResetGhostVisuals(dragGhosts[i]);
        }

        // Hide unused
        for (int i = dragLength; i < dragGhosts.Count; i++) dragGhosts[i].SetActive(false);
    }

    void ResetGhostVisuals(GameObject ghost)
    {
        // Assuming ghost hierarchy is: Root -> Visual_Straight, Visual_Corner_Left...
        Transform visualStraight = ghost.transform.Find("Visual_Straight");
        Transform visualCL = ghost.transform.Find("Visual_Corner_Left");
        Transform visualCR = ghost.transform.Find("Visual_Corner_Right");

        if (visualStraight) visualStraight.gameObject.SetActive(true);
        if (visualCL) visualCL.gameObject.SetActive(false);
        if (visualCR) visualCR.gameObject.SetActive(false);
    }

    void UpdateDeletePreview(Vector2Int current)
    {
        int dx = current.x - dragStartPos.x;
        int dy = current.y - dragStartPos.y;
        int count = Mathf.Max(Mathf.Abs(dx), Mathf.Abs(dy)) + 1;
        Vector2Int dir = (Mathf.Abs(dx) >= Mathf.Abs(dy)) ? new Vector2Int((int)Mathf.Sign(dx), 0) : new Vector2Int(0, (int)Mathf.Sign(dy));
        if (dir == Vector2Int.zero) dir = new Vector2Int(1, 0);

        EnsureGhostPool(count, null);

        for (int i = 0; i < count; i++)
        {
            Vector2Int pos = dragStartPos + (dir * i);
            GameObject g = dragGhosts[i];
            BuildingBase target = CasinoGridManager.Instance.GetBuildingAt(pos);

            if (target != null)
            {
                g.transform.position = CasinoGridManager.Instance.GridToWorld(pos);
                g.transform.rotation = Quaternion.identity;
                g.SetActive(true);
                SetGhostMaterial(g, false);
            }
            else
            {
                g.SetActive(false);
            }
        }
        for (int i = count; i < dragGhosts.Count; i++) dragGhosts[i].SetActive(false);
    }

    // --- LOGIC METHODS ---

    void ConfirmDragBuild()
    {
        // Safety: Check if anything is actually active (to avoid accidental clicks)
        int activeGhosts = 0;
        foreach (var g in dragGhosts) if (g.activeSelf) activeGhosts++;

        if (activeGhosts == 0)
        {
            ClearGhosts();
            CreateSingleGhost();
            return;
        }

        foreach (var ghost in dragGhosts)
        {
            if (!ghost.activeSelf) continue;

            Vector2Int pos = CasinoGridManager.Instance.WorldToGrid(ghost.transform.position);

            if (ResourceManager.Instance != null && !ResourceManager.Instance.CanAfford(selectedBuilding.cost)) continue;

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

                // 1. Place in Grid
                CasinoGridManager.Instance.PlaceBuilding(baseScript, pos);

                // 2. FORCE AUTO-TILER UPDATE
                BeltAutotiler tiler = b.GetComponent<BeltAutotiler>();
                if (tiler != null)
                {
                    tiler.Initialize();
                }
            }
        }
        ClearGhosts();
        CreateSingleGhost();
    }

    void ConfirmDeleteDrag()
    {
        foreach (var ghost in dragGhosts)
        {
            if (!ghost.activeSelf) continue;
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
                int refund = Mathf.FloorToInt(building.Definition.cost * (refundPercentage / 100f));
                ResourceManager.Instance.AddCredits(refund);
            }
            CasinoGridManager.Instance.RemoveBuilding(pos);
        }
    }

    // --- HELPERS ---

    void EnsureGhostPool(int count, GameObject prefab)
    {
        string prefabName = prefab != null ? prefab.name : "Cube_Delete_Marker";
        if (dragGhosts.Count > 0 && dragGhosts[0].name != prefabName + "(Clone)")
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
                g = GameObject.CreatePrimitive(PrimitiveType.Cube);
                Destroy(g.GetComponent<Collider>());
                g.name = "Cube_Delete_Marker(Clone)";
                g.transform.localScale = new Vector3(0.9f, 1f, 0.9f);
            }
            dragGhosts.Add(g);
        }

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