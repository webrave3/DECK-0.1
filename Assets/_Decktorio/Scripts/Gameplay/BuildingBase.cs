using UnityEngine;

public abstract class BuildingBase : MonoBehaviour
{
    public Vector2Int GridPosition { get; private set; }
    public BuildingDefinition Definition;
    public int RotationIndex { get; private set; }

    [Header("Debug")]
    public bool showDebugLogs = false;
    // CRITICAL: New flag to stop ghosts from acting like real buildings
    public bool isGhost = false;

    // MAIN INVENTORY (What is currently on the belt)
    public ItemPayload internalItem;
    protected ItemVisualizer internalVisual;

    // MAILBOX (Buffer for the NEXT tick)
    protected ItemPayload incomingItem;
    protected ItemVisualizer incomingVisual;

    protected virtual void Start()
    {
        // GHOST CHECK: If this is a preview object, DO NOT RUN LOGIC
        if (isGhost) return;

        // --- SAFETY REGISTRATION ---
        Vector2Int truePos = CasinoGridManager.Instance.WorldToGrid(transform.position);
        if (CasinoGridManager.Instance.GetBuildingAt(truePos) != this)
        {
            Setup(truePos);
            CasinoGridManager.Instance.RegisterBuilding(truePos, this);
            if (showDebugLogs) Debug.Log($"<color=green>[{name}]</color> Auto-Registered at {truePos}");
        }
        // ---------------------------

        if (TickManager.Instance != null)
            TickManager.Instance.OnTick += HandleTickSystem;
    }

    protected virtual void OnDestroy()
    {
        if (isGhost) return;

        if (TickManager.Instance != null)
            TickManager.Instance.OnTick -= HandleTickSystem;

        if (internalVisual != null) Destroy(internalVisual.gameObject);
        if (incomingVisual != null) Destroy(incomingVisual.gameObject);

        // Safety: If we are destroyed, free the grid cell
        if (CasinoGridManager.Instance != null && CasinoGridManager.Instance.GetBuildingAt(GridPosition) == this)
        {
            CasinoGridManager.Instance.RemoveBuilding(GridPosition);
        }
    }

    // --- INITIALIZATION ---
    public virtual void Initialize(Vector2Int pos, int rot)
    {
        Setup(pos);
        SetRotation(rot);
    }

    public void Setup(Vector2Int pos)
    {
        GridPosition = pos;
        transform.position = CasinoGridManager.Instance.GridToWorld(pos);
    }

    public void SetRotation(int rotIndex)
    {
        RotationIndex = rotIndex;
        transform.rotation = Quaternion.Euler(0, rotIndex * 90, 0);
    }

    public Vector2Int GetForwardGridPosition()
    {
        Vector2Int forwardDir = Vector2Int.zero;
        switch (RotationIndex)
        {
            case 0: forwardDir = new Vector2Int(0, 1); break; // North
            case 1: forwardDir = new Vector2Int(1, 0); break; // East
            case 2: forwardDir = new Vector2Int(0, -1); break; // South
            case 3: forwardDir = new Vector2Int(-1, 0); break; // West
        }
        return GridPosition + forwardDir;
    }

    private void HandleTickSystem(int tick)
    {
        if (isGhost) return;

        // 1. Process Logic
        OnTick(tick);

        // 2. Move Mailbox to Internal
        if (incomingItem != null && internalItem == null)
        {
            internalItem = incomingItem;
            internalVisual = incomingVisual;

            incomingItem = null;
            incomingVisual = null;

            if (showDebugLogs) GameLogger.Log($"[{name}] Processed Mailbox.");

            OnItemArrived();
        }
    }

    protected abstract void OnTick(int tick);

    protected virtual void OnItemArrived()
    {
        if (internalVisual != null)
        {
            internalVisual.transform.SetParent(this.transform);
            float duration = TickManager.Instance.tickRate;
            internalVisual.InitializeMovement(transform.position + Vector3.up * 0.2f, duration);
        }
    }

    public virtual bool CanAcceptItem(Vector2Int fromPos)
    {
        if (isGhost) return false;
        if (incomingItem != null) return false;
        return true;
    }

    public virtual void ReceiveItem(ItemPayload item, ItemVisualizer visual)
    {
        incomingItem = item;
        incomingVisual = visual;
    }

    public virtual bool CanBePlacedAt(Vector2Int gridPos) => true;
}