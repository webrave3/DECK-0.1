using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;
using System.Collections.Generic;

public class BuildingSystem : MonoBehaviour
{
    public static BuildingSystem Instance { get; private set; }

    [Header("Settings")]
    public LayerMask groundLayer = 1; // Default
    public Material validMat;
    public Material invalidMat;

    [Header("Economy")]
    public int refundPercentage = 100; // 100% refund for MVP

    // State
    private BuildingDefinition selectedBuilding;
    private int currentRotation = 0; // 0=Up, 1=Right, 2=Down, 3=Left
    private GameObject singleGhost;

    // Dragging Logic
    private bool isDragging = false;
    private Vector2Int dragStartPos;
    private List<GameObject> dragGhosts = new List<GameObject>();

    // Input
    private Camera mainCam;
    private InputActionMap buildInput;
    private InputAction pointAction;
    private InputAction clickAction;
    private InputAction rotateAction;
    private InputAction cancelAction;

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
        rotateAction = buildInput.AddAction("Rotate", binding: "<Keyboard>/r");
        cancelAction = buildInput.AddAction("Cancel", binding: "<Mouse>/rightButton");

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
        if (singleGhost != null)
        {
            Destroy(singleGhost);
            singleGhost = null;
        }

        foreach (var g in dragGhosts)
        {
            if (g != null) Destroy(g);
        }
        dragGhosts.Clear();
        isDragging = false;
    }

    private void Update()
    {
        // 1. Raycast
        Ray ray = mainCam.ScreenPointToRay(pointAction.ReadValue<Vector2>());
        bool hitGround = Physics.Raycast(ray, out RaycastHit hit, 1000f, groundLayer);
        Vector2Int currentGridPos = Vector2Int.zero;

        if (hitGround)
            currentGridPos = CasinoGridManager.Instance.WorldToGrid(hit.point);

        // 2. Block building if mouse is over UI
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
            return;

        // --- MODE A: BUILD MODE ---
        if (selectedBuilding != null)
        {
            if (cancelAction.WasPressedThisFrame())
            {
                Deselect();
                return;
            }

            if (rotateAction.WasPressedThisFrame())
                currentRotation = (currentRotation + 1) % 4;

            if (hitGround)
            {
                if (isDragging)
                {
                    UpdateDragPreview(currentGridPos);
                    if (clickAction.WasReleasedThisFrame()) ConfirmDragBuild();
                }
                else
                {
                    UpdateSingleGhost(currentGridPos);
                    if (clickAction.WasPressedThisFrame()) StartDrag(currentGridPos);
                }
            }
        }
        // --- MODE B: DELETE MODE (No building selected + Right Click) ---
        else
        {
            if (hitGround && cancelAction.WasPressedThisFrame())
            {
                DeleteBuildingAt(currentGridPos);
            }
        }
    }

    // --- DELETION LOGIC (This was missing!) ---
    void DeleteBuildingAt(Vector2Int pos)
    {
        BuildingBase building = CasinoGridManager.Instance.GetBuildingAt(pos);
        if (building != null)
        {
            // Refund Logic
            if (ResourceManager.Instance != null && building.Definition != null)
            {
                int refund = Mathf.FloorToInt(building.Definition.cost * (refundPercentage / 100f));
                ResourceManager.Instance.AddCredits(refund);
            }

            // Remove from Grid
            CasinoGridManager.Instance.RemoveBuilding(pos);
        }
    }

    // --- GHOST LOGIC ---

    void CreateSingleGhost()
    {
        if (singleGhost != null) Destroy(singleGhost);
        singleGhost = Instantiate(selectedBuilding.prefab);
        CleanupGhostVisuals(singleGhost);
    }

    void CleanupGhostVisuals(GameObject ghost)
    {
        // Strip logic components so the ghost is visual-only
        foreach (var comp in ghost.GetComponentsInChildren<MonoBehaviour>()) Destroy(comp);
        foreach (var col in ghost.GetComponentsInChildren<Collider>()) Destroy(col);
    }

    void UpdateSingleGhost(Vector2Int pos)
    {
        if (singleGhost == null) CreateSingleGhost();

        singleGhost.transform.position = CasinoGridManager.Instance.GridToWorld(pos);
        singleGhost.transform.rotation = Quaternion.Euler(0, currentRotation * 90, 0);
        singleGhost.SetActive(true);

        // Visual Validation (Optional: Tint Red/Green)
        // BuildingBase logic = selectedBuilding.prefab.GetComponent<BuildingBase>();
        // bool isValid = CasinoGridManager.Instance.IsBuildable(pos) && logic.CanBePlacedAt(pos);
    }

    // --- DRAG LOGIC ---

    void StartDrag(Vector2Int start)
    {
        isDragging = true;
        dragStartPos = start;
        singleGhost.SetActive(false);
    }

    void UpdateDragPreview(Vector2Int current)
    {
        int dx = current.x - dragStartPos.x;
        int dy = current.y - dragStartPos.y;

        int count;
        Vector2Int dir;

        if (Mathf.Abs(dx) >= Mathf.Abs(dy))
        {
            count = Mathf.Abs(dx);
            dir = new Vector2Int((int)Mathf.Sign(dx), 0);
            currentRotation = (dx >= 0) ? 1 : 3;
        }
        else
        {
            count = Mathf.Abs(dy);
            dir = new Vector2Int(0, (int)Mathf.Sign(dy));
            currentRotation = (dy >= 0) ? 0 : 2;
        }

        int needed = count + 1;
        while (dragGhosts.Count < needed)
        {
            GameObject g = Instantiate(selectedBuilding.prefab);
            CleanupGhostVisuals(g);
            dragGhosts.Add(g);
        }
        while (dragGhosts.Count > needed)
        {
            Destroy(dragGhosts[dragGhosts.Count - 1]);
            dragGhosts.RemoveAt(dragGhosts.Count - 1);
        }

        for (int i = 0; i < needed; i++)
        {
            Vector2Int pos = dragStartPos + (dir * i);
            dragGhosts[i].transform.position = CasinoGridManager.Instance.GridToWorld(pos);
            dragGhosts[i].transform.rotation = Quaternion.Euler(0, currentRotation * 90, 0);
            dragGhosts[i].SetActive(true);
        }
    }

    void ConfirmDragBuild()
    {
        foreach (var ghost in dragGhosts)
        {
            Vector2Int pos = CasinoGridManager.Instance.WorldToGrid(ghost.transform.position);

            // 1. Check Cost
            if (ResourceManager.Instance != null && !ResourceManager.Instance.CanAfford(selectedBuilding.cost))
            {
                Debug.Log($"Not enough credits! Need {selectedBuilding.cost}");
                continue;
            }

            // 2. Check Logic & Grid Validity
            // We need the PREFAB to check "CanBePlacedAt" logic (like Unpacker on Ore)
            BuildingBase logic = selectedBuilding.prefab.GetComponent<BuildingBase>();

            bool validPos = CasinoGridManager.Instance.IsBuildable(pos);
            bool validLogic = logic.CanBePlacedAt(pos);

            if (validPos && validLogic)
            {
                // 3. Deduct Cost
                if (ResourceManager.Instance != null)
                    ResourceManager.Instance.SpendCredits(selectedBuilding.cost);

                // 4. Build
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
}