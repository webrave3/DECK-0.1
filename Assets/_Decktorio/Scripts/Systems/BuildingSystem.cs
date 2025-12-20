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

    // State
    public BuildingDefinition SelectedBuilding => selectedBuilding;
    private BuildingDefinition selectedBuilding;
    private List<SelectionManager.BuildingBlueprint> pasteClipboard;

    private int currentRotation = 0;
    private GameObject singleGhost;

    private bool isDragging = false;
    private Vector2Int dragStartPos;

    private List<GameObject> dragGhosts = new List<GameObject>();
    private List<(Vector2Int pos, int rot)> currentDragPath = new List<(Vector2Int pos, int rot)>();

    public int LastCancelFrame { get; private set; } = -1;
    public bool IsBusyOrJustCancelled => IsBuildingOrPasteActive() || LastCancelFrame == Time.frameCount;

    private Camera mainCam;
    private InputActionMap buildInput;
    private InputAction pointAction;
    private InputAction clickAction;
    private InputAction rotateAction;
    private InputAction escapeAction;
    private InputAction pickerAction;

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
        escapeAction = buildInput.AddAction("Escape", binding: "<Keyboard>/escape");
        pickerAction = buildInput.AddAction("PickBuilding", binding: "<Keyboard>/q");
        buildInput.Enable();
    }

    public bool IsBuildingOrPasteActive() => selectedBuilding != null || (pasteClipboard != null && pasteClipboard.Count > 0);

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

    private void Update()
    {
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject()) return;
        if (pickerAction.WasPressedThisFrame()) HandlePicker();

        Ray ray = mainCam.ScreenPointToRay(pointAction.ReadValue<Vector2>());
        bool hitSomething = Physics.Raycast(ray, out RaycastHit hit, 1000f, groundLayer | buildingLayer);
        Vector2Int currentGridPos = Vector2Int.zero;
        if (hitSomething) currentGridPos = CasinoGridManager.Instance.WorldToGrid(hit.point);

        bool rightClick = Mouse.current != null && Mouse.current.rightButton.wasPressedThisFrame;
        bool escape = escapeAction.WasPressedThisFrame();

        if (rightClick || escape)
        {
            if (isDragging)
            {
                ClearGhosts();
                if (selectedBuilding != null) CreateSingleGhost();
                return;
            }
            if (IsBuildingOrPasteActive())
            {
                LastCancelFrame = Time.frameCount;
                Deselect();
                return;
            }
        }

        if (pasteClipboard != null && pasteClipboard.Count > 0 && hitSomething)
        {
            if (rotateAction.WasPressedThisFrame()) RotateClipboard();
            UpdatePastePreview(currentGridPos);
            if (clickAction.WasPressedThisFrame()) ConfirmPaste(currentGridPos);
            return;
        }

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
            }
        }
    }

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
        if (EconomyManager.Instance != null && !EconomyManager.Instance.CanBuild(selectedBuilding.baseDebtCost)) valid = false;

        SetGhostMaterial(singleGhost, valid);
        UpdateGhostAppearance(singleGhost, pos, currentRotation, null, -1);
    }

    void UpdateDragPreview(Vector2Int current)
    {
        // Use Smart Path Generation (Worm Style)
        currentDragPath = GenerateSmartPath(dragStartPos, current);
        EnsureGhostPool(currentDragPath.Count);
        float totalCost = 0;

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
            totalCost += selectedBuilding.baseDebtCost;
            if (EconomyManager.Instance != null && !EconomyManager.Instance.CanBuild(totalCost)) valid = false;

            SetGhostMaterial(g, valid);
            UpdateGhostAppearance(g, data.pos, data.rot, currentDragPath, i);
        }

        for (int i = currentDragPath.Count; i < dragGhosts.Count; i++) dragGhosts[i].SetActive(false);
    }

    // --- SMART PATH LOGIC ---
    // Fixes the "Starts turn at beginning" issue.
    // If you drag mostly Vertically, it moves Y then X.
    // If you drag mostly Horizontally, it moves X then Y.
    List<(Vector2Int pos, int rot)> GenerateSmartPath(Vector2Int start, Vector2Int end)
    {
        var path = new List<(Vector2Int pos, int rot)>();
        int dx = end.x - start.x;
        int dy = end.y - start.y;

        // Determine "Major Axis" of drag
        bool moveYFirst = Mathf.Abs(dy) > Mathf.Abs(dx);

        Vector2Int cursor = start;

        if (!moveYFirst) // Horizontal First (X then Y)
        {
            int xDir = (int)Mathf.Sign(dx);
            for (int i = 0; i < Mathf.Abs(dx); i++)
            {
                // While moving X, rotation is East/West
                path.Add((cursor, (xDir > 0) ? 1 : 3));
                cursor.x += xDir;
            }
            int yDir = (int)Mathf.Sign(dy);
            for (int i = 0; i <= Mathf.Abs(dy); i++)
            {
                // Turning point or Vertical segment
                int rot = (dy == 0) ? ((xDir > 0) ? 1 : 3) : ((yDir > 0) ? 0 : 2);
                if (dx == 0 && dy == 0) rot = currentRotation; // Single point
                path.Add((cursor, rot));
                if (i < Mathf.Abs(dy)) cursor.y += yDir;
            }
        }
        else // Vertical First (Y then X) - This handles the "Vertical Worm" feel
        {
            int yDir = (int)Mathf.Sign(dy);
            for (int i = 0; i < Mathf.Abs(dy); i++)
            {
                // While moving Y, rotation is North/South
                path.Add((cursor, (yDir > 0) ? 0 : 2));
                cursor.y += yDir;
            }
            int xDir = (int)Mathf.Sign(dx);
            for (int i = 0; i <= Mathf.Abs(dx); i++)
            {
                // Turning point or Horizontal segment
                int rot = (dx == 0) ? ((yDir > 0) ? 0 : 2) : ((xDir > 0) ? 1 : 3);
                if (dx == 0 && dy == 0) rot = currentRotation;
                path.Add((cursor, rot));
                if (i < Mathf.Abs(dx)) cursor.x += xDir;
            }
        }
        return path;
    }

    void ConfirmDragBuild()
    {
        foreach (var data in currentDragPath)
        {
            Vector2Int pos = data.pos;
            if (EconomyManager.Instance != null && !EconomyManager.Instance.CanBuild(selectedBuilding.baseDebtCost)) break;

            BuildingBase logic = selectedBuilding.prefab.GetComponent<BuildingBase>();
            if (IsValidPlacement(pos, logic))
            {
                if (CasinoGridManager.Instance.IsOccupied(pos)) CasinoGridManager.Instance.RemoveBuilding(pos);
                if (EconomyManager.Instance != null) EconomyManager.Instance.SpendMoney(selectedBuilding.baseDebtCost);

                GameObject b = Instantiate(selectedBuilding.prefab);
                BuildingBase baseScript = b.GetComponent<BuildingBase>();
                baseScript.Definition = selectedBuilding;
                baseScript.Initialize(pos, data.rot);
                CasinoGridManager.Instance.PlaceBuilding(baseScript, pos);
            }
        }
        ClearGhosts();
        CreateSingleGhost();
    }

    void UpdatePastePreview(Vector2Int center)
    {
        EnsureGhostPool(pasteClipboard.Count);
        float totalCost = 0;
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
            totalCost += bp.definition.baseDebtCost;
            if (EconomyManager.Instance != null && !EconomyManager.Instance.CanBuild(totalCost)) valid = false;
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
            if (EconomyManager.Instance != null && !EconomyManager.Instance.CanBuild(bp.definition.baseDebtCost)) break;
            BuildingBase logic = bp.definition.prefab.GetComponent<BuildingBase>();
            if (IsValidPlacement(pos, logic))
            {
                if (CasinoGridManager.Instance.IsOccupied(pos)) CasinoGridManager.Instance.RemoveBuilding(pos);
                if (EconomyManager.Instance != null) EconomyManager.Instance.SpendMoney(bp.definition.baseDebtCost);
                GameObject b = Instantiate(bp.definition.prefab);
                BuildingBase baseScript = b.GetComponent<BuildingBase>();
                baseScript.Definition = bp.definition;
                baseScript.Initialize(pos, bp.rotation);
                CasinoGridManager.Instance.PlaceBuilding(baseScript, pos);
            }
        }
    }

    void RotateClipboard()
    {
        for (int i = 0; i < pasteClipboard.Count; i++)
        {
            var bp = pasteClipboard[i];
            Vector2Int newPos = new Vector2Int(bp.relPos.y, -bp.relPos.x);
            int newRot = (bp.rotation + 1) % 4;
            pasteClipboard[i] = new SelectionManager.BuildingBlueprint { definition = bp.definition, relPos = newPos, rotation = newRot };
        }
    }

    // --- PREVIEW VISUAL FIX ---
    void UpdateGhostAppearance(GameObject ghost, Vector2Int pos, int rot, List<(Vector2Int pos, int rot)> dragList, int index)
    {
        if (ghost.GetComponent<ConveyorBelt>() == null) return;

        bool inputLeft = false, inputRight = false;

        // 1. Check Drag Neighbors
        if (dragList != null && index > 0)
        {
            Vector2Int prevPos = dragList[index - 1].pos;
            CheckInputDirection(pos, rot, prevPos, ref inputLeft, ref inputRight);
        }
        else
        {
            // 2. Check World Neighbors (for the first piece)
            CheckWorldInputs(pos, rot, ref inputLeft, ref inputRight);
        }

        Transform vStraight = ghost.transform.Find("Visual_Straight");
        Transform vLeft = ghost.transform.Find("Visual_Corner_Left");
        Transform vRight = ghost.transform.Find("Visual_Corner_Right");

        if (vStraight) vStraight.gameObject.SetActive(false);
        if (vLeft) vLeft.gameObject.SetActive(false);
        if (vRight) vRight.gameObject.SetActive(false);

        // Correct Visual Logic
        if (inputLeft) vLeft.gameObject.SetActive(true);
        else if (inputRight) vRight.gameObject.SetActive(true);
        else if (vStraight) vStraight.gameObject.SetActive(true);
    }

    void CheckWorldInputs(Vector2Int myPos, int myRot, ref bool left, ref bool right)
    {
        // Check surrounding neighbors to see if they feed into me
        CheckNeighbor(myPos, myRot, myPos + GetDirFromIndex((myRot + 3) % 4), ref left, ref right); // Left Neighbor
        CheckNeighbor(myPos, myRot, myPos + GetDirFromIndex((myRot + 1) % 4), ref left, ref right); // Right Neighbor
    }

    void CheckNeighbor(Vector2Int myPos, int myRot, Vector2Int neighborPos, ref bool left, ref bool right)
    {
        BuildingBase b = CasinoGridManager.Instance.GetBuildingAt(neighborPos);
        if (b != null && b is ConveyorBelt && b.GetForwardGridPosition() == myPos)
        {
            CheckInputDirection(myPos, myRot, neighborPos, ref left, ref right);
        }
    }

    void CheckInputDirection(Vector2Int myPos, int myRot, Vector2Int sourcePos, ref bool left, ref bool right)
    {
        // Helper to check relative direction
        Vector2Int leftDir = GetDirFromIndex((myRot + 3) % 4);
        Vector2Int rightDir = GetDirFromIndex((myRot + 1) % 4);
        Vector2Int incomingDir = sourcePos - myPos; // Direction from Me to Source

        // If source is at my "Left", incomingDir should equal leftDir
        if (incomingDir == leftDir) left = true;
        if (incomingDir == rightDir) right = true;
    }

    bool IsValidPlacement(Vector2Int pos, BuildingBase logic)
    {
        if (!CasinoGridManager.Instance.IsUnlocked(pos)) return false;
        if (CasinoGridManager.Instance.IsOccupied(pos))
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
            GameObject g = (selectedBuilding != null) ? Instantiate(selectedBuilding.prefab) : GameObject.CreatePrimitive(PrimitiveType.Cube);
            CleanupGhostVisuals(g);
            dragGhosts.Add(g);
        }
    }

    void CleanupGhostVisuals(GameObject ghost)
    {
        foreach (var col in ghost.GetComponentsInChildren<Collider>()) col.enabled = false;
        foreach (var comp in ghost.GetComponentsInChildren<MonoBehaviour>())
            if (!(comp is BuildingBase)) Destroy(comp);
    }

    void ResetGhostVisuals(GameObject ghost)
    {
        Transform vStraight = ghost.transform.Find("Visual_Straight");
        if (vStraight) vStraight.gameObject.SetActive(true);
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