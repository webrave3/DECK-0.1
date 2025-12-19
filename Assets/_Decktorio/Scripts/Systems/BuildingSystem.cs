using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;
using System.Collections.Generic;

public class BuildingSystem : MonoBehaviour
{
    public static BuildingSystem Instance { get; private set; }

    [Header("Settings")]
    public LayerMask groundLayer = 1;
    public LayerMask buildingLayer = 1;

    [Header("Visuals")]
    public Color validColor = new Color(0, 1, 0, 0.5f);
    public Color invalidColor = new Color(1, 0, 0, 0.5f);

    [Header("Economy")]
    public int refundPercentage = 100;

    // --- STATE ---
    public BuildingDefinition SelectedBuilding => selectedBuilding;
    private BuildingDefinition selectedBuilding;
    private List<SelectionManager.BuildingBlueprint> pasteClipboard;

    private int currentRotation = 0;
    private GameObject singleGhost;

    private bool isDragging = false;
    private Vector2Int dragStartPos;

    private List<GameObject> dragGhosts = new List<GameObject>();
    private List<(Vector2Int pos, int rot)> currentDragPath = new List<(Vector2Int pos, int rot)>();

    // FIX: Using Frame Count to prevent execution order issues
    public int LastCancelFrame { get; private set; } = -1;

    // Helper for SelectionManager
    public bool IsBusyOrJustCancelled => IsBuildingOrPasteActive() || LastCancelFrame == Time.frameCount;

    // --- INPUT ---
    private Camera mainCam;
    private InputActionMap buildInput;
    private InputAction pointAction;
    private InputAction clickAction;
    private InputAction rotateAction;
    private InputAction escapeAction; // Kept for ESC key
    private InputAction pickerAction;
    // Note: We will use Mouse.current for Right Click to sync perfectly with SelectionManager

    private void Awake()
    {
        Instance = this;
    }

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
        escapeAction = buildInput.AddAction("Escape", binding: "<Keyboard>/escape");
        pickerAction = buildInput.AddAction("PickBuilding", binding: "<Keyboard>/t");
        buildInput.Enable();
    }

    public bool IsBuildingOrPasteActive()
    {
        return selectedBuilding != null || (pasteClipboard != null && pasteClipboard.Count > 0);
    }

    public void SelectBuilding(BuildingDefinition def)
    {
        Deselect();
        if (SelectionManager.Instance) SelectionManager.Instance.DeselectAll();

        selectedBuilding = def;
        CreateSingleGhost();
    }

    public void StartPasteMode(List<SelectionManager.BuildingBlueprint> clipboard)
    {
        Deselect();
        if (SelectionManager.Instance) SelectionManager.Instance.DeselectAll();

        pasteClipboard = new List<SelectionManager.BuildingBlueprint>(clipboard);
    }

    public void Deselect()
    {
        selectedBuilding = null;
        pasteClipboard = null;
        ClearGhosts();
    }

    // --- UPDATE LOOP ---

    private void Update()
    {
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject()) return;

        // Picker Input
        if (pickerAction.WasPressedThisFrame())
        {
            HandlePicker();
        }

        Ray ray = mainCam.ScreenPointToRay(pointAction.ReadValue<Vector2>());
        bool hitSomething = Physics.Raycast(ray, out RaycastHit hit, 1000f, groundLayer | buildingLayer);
        Vector2Int currentGridPos = Vector2Int.zero;
        if (hitSomething) currentGridPos = CasinoGridManager.Instance.WorldToGrid(hit.point);

        // Cancel Logic (Right Click OR Escape)
        // FIX: Using Mouse.current directly to ensure it matches SelectionManager's input logic perfectly
        bool rightClickPressed = Mouse.current != null && Mouse.current.rightButton.wasPressedThisFrame;
        bool escapePressed = escapeAction.WasPressedThisFrame();

        if (rightClickPressed || escapePressed)
        {
            // 1. If dragging a line of buildings, cancel the drag but keep the ghost
            if (isDragging)
            {
                ClearGhosts();
                if (selectedBuilding != null) CreateSingleGhost();
                return;
            }

            // 2. If holding a building/clipboard, cancel it
            if (IsBuildingOrPasteActive())
            {
                LastCancelFrame = Time.frameCount; // Record frame BEFORE deselecting
                Deselect();
                return;
            }
        }

        // PASTE MODE
        if (pasteClipboard != null && pasteClipboard.Count > 0 && hitSomething)
        {
            if (rotateAction.WasPressedThisFrame()) RotateClipboard();
            UpdatePastePreview(currentGridPos);
            if (clickAction.WasPressedThisFrame()) ConfirmPaste(currentGridPos);
            return;
        }

        // BUILD MODE
        if (selectedBuilding != null && hitSomething)
        {
            if (rotateAction.WasPressedThisFrame()) currentRotation = (currentRotation + 1) % 4;

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

    void HandlePicker()
    {
        Ray ray = mainCam.ScreenPointToRay(pointAction.ReadValue<Vector2>());
        if (Physics.Raycast(ray, out RaycastHit hit, 1000f, buildingLayer))
        {
            BuildingBase b = hit.collider.GetComponentInParent<BuildingBase>();
            if (b != null && b.Definition != null)
            {
                SelectBuilding(b.Definition);
                currentRotation = b.RotationIndex;
                if (singleGhost != null)
                    singleGhost.transform.rotation = Quaternion.Euler(0, currentRotation * 90, 0);
            }
        }
    }

    // --- GHOST & LOGIC METHODS ---

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

        BuildingBase logic = singleGhost.GetComponent<BuildingBase>();
        bool valid = IsValidPlacement(pos, logic);
        SetGhostMaterial(singleGhost, valid);

        UpdateGhostAppearance(singleGhost, 0, pos, currentRotation);
    }

    void UpdateDragPreview(Vector2Int current)
    {
        currentDragPath = GeneratePath(dragStartPos, current);
        EnsureGhostPool(currentDragPath.Count);

        for (int i = 0; i < currentDragPath.Count; i++)
        {
            var data = currentDragPath[i];
            GameObject g = dragGhosts[i];

            if (g.name != selectedBuilding.prefab.name + "(Clone)")
            {
                Destroy(g);
                g = Instantiate(selectedBuilding.prefab);
                CleanupGhostVisuals(g);
                dragGhosts[i] = g;
            }

            g.transform.position = CasinoGridManager.Instance.GridToWorld(data.pos);
            g.transform.rotation = Quaternion.Euler(0, data.rot * 90, 0);
            g.SetActive(true);

            BuildingBase logic = g.GetComponent<BuildingBase>();
            bool valid = IsValidPlacement(data.pos, logic);
            SetGhostMaterial(g, valid);

            UpdateGhostAppearance(g, i, data.pos, data.rot);
        }

        for (int i = currentDragPath.Count; i < dragGhosts.Count; i++) dragGhosts[i].SetActive(false);
    }

    void ConfirmDragBuild()
    {
        foreach (var data in currentDragPath)
        {
            Vector2Int pos = data.pos;
            if (ResourceManager.Instance != null && !ResourceManager.Instance.CanAfford(selectedBuilding.cost)) continue;

            BuildingBase logic = selectedBuilding.prefab.GetComponent<BuildingBase>();

            if (IsValidPlacement(pos, logic))
            {
                if (CasinoGridManager.Instance.IsOccupied(pos))
                    CasinoGridManager.Instance.RemoveBuilding(pos);

                if (ResourceManager.Instance != null)
                    ResourceManager.Instance.SpendCredits(selectedBuilding.cost);

                GameObject b = Instantiate(selectedBuilding.prefab);
                BuildingBase baseScript = b.GetComponent<BuildingBase>();
                baseScript.Definition = selectedBuilding;
                baseScript.SetRotation(data.rot);

                CasinoGridManager.Instance.PlaceBuilding(baseScript, pos);
                b.SendMessage("Initialize", SendMessageOptions.DontRequireReceiver);
            }
        }
        ClearGhosts();
        CreateSingleGhost();
    }

    // --- PASTE LOGIC ---

    void RotateClipboard()
    {
        for (int i = 0; i < pasteClipboard.Count; i++)
        {
            var bp = pasteClipboard[i];
            Vector2Int newPos = new Vector2Int(bp.relPos.y, -bp.relPos.x);
            int newRot = (bp.rotation + 1) % 4;

            pasteClipboard[i] = new SelectionManager.BuildingBlueprint
            {
                definition = bp.definition,
                relPos = newPos,
                rotation = newRot
            };
        }
    }

    void UpdatePastePreview(Vector2Int center)
    {
        EnsureGhostPool(pasteClipboard.Count);

        for (int i = 0; i < pasteClipboard.Count; i++)
        {
            var bp = pasteClipboard[i];
            GameObject g = dragGhosts[i];

            if (g.name != bp.definition.prefab.name + "(Clone)")
            {
                Destroy(g);
                g = Instantiate(bp.definition.prefab);
                CleanupGhostVisuals(g);
                dragGhosts[i] = g;
            }

            Vector2Int targetPos = center + bp.relPos;
            g.transform.position = CasinoGridManager.Instance.GridToWorld(targetPos);
            g.transform.rotation = Quaternion.Euler(0, bp.rotation * 90, 0);
            g.SetActive(true);

            BuildingBase logic = g.GetComponent<BuildingBase>();
            bool valid = IsValidPlacement(targetPos, logic);
            SetGhostMaterial(g, valid);

            ResetGhostVisuals(g);
        }

        for (int i = pasteClipboard.Count; i < dragGhosts.Count; i++) dragGhosts[i].SetActive(false);
    }

    void ConfirmPaste(Vector2Int center)
    {
        foreach (var bp in pasteClipboard)
        {
            Vector2Int pos = center + bp.relPos;
            BuildingBase logic = bp.definition.prefab.GetComponent<BuildingBase>();

            if (IsValidPlacement(pos, logic))
            {
                if (CasinoGridManager.Instance.IsOccupied(pos))
                    CasinoGridManager.Instance.RemoveBuilding(pos);

                if (ResourceManager.Instance != null)
                    ResourceManager.Instance.SpendCredits(bp.definition.cost);

                GameObject b = Instantiate(bp.definition.prefab);
                BuildingBase baseScript = b.GetComponent<BuildingBase>();
                baseScript.Definition = bp.definition;
                baseScript.SetRotation(bp.rotation);

                CasinoGridManager.Instance.PlaceBuilding(baseScript, pos);
                b.SendMessage("Initialize", SendMessageOptions.DontRequireReceiver);
            }
        }
    }

    // --- HELPERS ---

    void UpdateGhostAppearance(GameObject ghost, int index, Vector2Int pos, int rot)
    {
        if (ghost.GetComponent<ConveyorBelt>() == null) return;

        bool inputLeft = false, inputRight = false, inputBack = false;

        if (index > 0 && index < currentDragPath.Count)
        {
            Vector2Int prevPos = currentDragPath[index - 1].pos;
            CheckInputDirection(pos, rot, prevPos, ref inputBack, ref inputLeft, ref inputRight);
        }
        else
        {
            CheckWorldInputs(pos, rot, ref inputBack, ref inputLeft, ref inputRight);
            if (!inputLeft && !inputRight) inputBack = true;
        }

        Transform vStraight = ghost.transform.Find("Visual_Straight");
        Transform vLeft = ghost.transform.Find("Visual_Corner_Left");
        Transform vRight = ghost.transform.Find("Visual_Corner_Right");

        if (vStraight) vStraight.gameObject.SetActive(false);
        if (vLeft) vLeft.gameObject.SetActive(false);
        if (vRight) vRight.gameObject.SetActive(false);

        if (inputLeft && vLeft) vLeft.gameObject.SetActive(true);
        else if (inputRight && vRight) vRight.gameObject.SetActive(true);
        else if (vStraight) vStraight.gameObject.SetActive(true);
    }

    void CheckWorldInputs(Vector2Int myPos, int myRot, ref bool back, ref bool left, ref bool right)
    {
        CheckNeighbor(myPos, myRot, myPos - GetDirFromIndex(myRot), ref back, ref left, ref right);
        CheckNeighbor(myPos, myRot, myPos + GetDirFromIndex((myRot + 3) % 4), ref back, ref left, ref right);
        CheckNeighbor(myPos, myRot, myPos + GetDirFromIndex((myRot + 1) % 4), ref back, ref left, ref right);
    }

    void CheckNeighbor(Vector2Int myPos, int myRot, Vector2Int neighborPos, ref bool back, ref bool left, ref bool right)
    {
        BuildingBase b = CasinoGridManager.Instance.GetBuildingAt(neighborPos);
        if (b != null && b is ConveyorBelt)
        {
            if (b.GetForwardGridPosition() == myPos)
            {
                CheckInputDirection(myPos, myRot, neighborPos, ref back, ref left, ref right);
            }
        }
    }

    void CheckInputDirection(Vector2Int myPos, int myRot, Vector2Int inputSourcePos, ref bool back, ref bool left, ref bool right)
    {
        Vector2Int dir = myPos - inputSourcePos;
        Vector2Int forward = GetDirFromIndex(myRot);
        Vector2Int rVec = GetDirFromIndex((myRot + 1) % 4);
        Vector2Int lVec = GetDirFromIndex((myRot + 3) % 4);

        if (dir == forward) back = true;
        if (dir == rVec) right = true;
        if (dir == lVec) left = true;
    }

    List<(Vector2Int pos, int rot)> GeneratePath(Vector2Int start, Vector2Int end)
    {
        var path = new List<(Vector2Int pos, int rot)>();
        int dx = end.x - start.x;
        int dy = end.y - start.y;
        int xDir = (int)Mathf.Sign(dx);
        int yDir = (int)Mathf.Sign(dy);
        Vector2Int cursor = start;

        for (int i = 0; i < Mathf.Abs(dx); i++)
        {
            int rot = (xDir > 0) ? 1 : 3;
            path.Add((cursor, rot));
            cursor.x += xDir;
        }

        for (int i = 0; i <= Mathf.Abs(dy); i++)
        {
            int rot = (dy == 0) ? ((xDir > 0) ? 1 : 3) : ((yDir > 0) ? 0 : 2);
            if (dx == 0 && dy == 0) rot = currentRotation;
            path.Add((cursor, rot));
            if (i < Mathf.Abs(dy)) cursor.y += yDir;
        }
        return path;
    }

    bool IsValidPlacement(Vector2Int pos, BuildingBase logic)
    {
        if (!CasinoGridManager.Instance.IsUnlocked(pos)) return false;

        bool occupied = CasinoGridManager.Instance.IsOccupied(pos);
        if (occupied)
        {
            var existing = CasinoGridManager.Instance.GetBuildingAt(pos);
            if (existing is ConveyorBelt && logic is ConveyorBelt) return true;
            return false;
        }
        return logic.CanBePlacedAt(pos);
    }

    void ClearGhosts()
    {
        if (singleGhost != null) { Destroy(singleGhost); singleGhost = null; }
        foreach (var g in dragGhosts) if (g != null) Destroy(g);
        dragGhosts.Clear();
        currentDragPath.Clear();
        isDragging = false;
    }

    void EnsureGhostPool(int count)
    {
        while (dragGhosts.Count < count)
        {
            GameObject g;
            if (selectedBuilding != null) g = Instantiate(selectedBuilding.prefab);
            else g = GameObject.CreatePrimitive(PrimitiveType.Cube);

            CleanupGhostVisuals(g);
            dragGhosts.Add(g);
        }
    }

    void CleanupGhostVisuals(GameObject ghost)
    {
        foreach (var col in ghost.GetComponentsInChildren<Collider>()) col.enabled = false;
        foreach (var comp in ghost.GetComponentsInChildren<MonoBehaviour>())
        {
            if (!(comp is BuildingBase)) Destroy(comp);
        }
    }

    void ResetGhostVisuals(GameObject ghost)
    {
        Transform vStraight = ghost.transform.Find("Visual_Straight");
        Transform vLeft = ghost.transform.Find("Visual_Corner_Left");
        Transform vRight = ghost.transform.Find("Visual_Corner_Right");

        if (vStraight) vStraight.gameObject.SetActive(true);
        if (vLeft) vLeft.gameObject.SetActive(false);
        if (vRight) vRight.gameObject.SetActive(false);
    }

    void SetGhostMaterial(GameObject ghost, bool isValid)
    {
        Color c = isValid ? validColor : invalidColor;
        foreach (var r in ghost.GetComponentsInChildren<Renderer>()) r.material.color = c;
    }

    Vector2Int GetDirFromIndex(int index)
    {
        switch (index)
        {
            case 0: return new Vector2Int(0, 1);
            case 1: return new Vector2Int(1, 0);
            case 2: return new Vector2Int(0, -1);
            case 3: return new Vector2Int(-1, 0);
        }
        return Vector2Int.zero;
    }
}