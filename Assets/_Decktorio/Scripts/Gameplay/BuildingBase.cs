using UnityEngine;

public abstract class BuildingBase : MonoBehaviour
{
    public Vector2Int GridPosition { get; private set; }
    public BuildingDefinition Definition;
    public int RotationIndex { get; private set; }

    [Header("Debug")]
    public bool showDebugLogs = false;

    // MAIN INVENTORY (What is currently on the belt)
    public ItemPayload internalItem;
    protected ItemVisualizer internalVisual;

    // MAILBOX (Buffer for the NEXT tick)
    protected ItemPayload incomingItem;
    protected ItemVisualizer incomingVisual;

    protected virtual void Start()
    {
        if (TickManager.Instance != null)
            TickManager.Instance.OnTick += HandleTickSystem;
    }

    protected virtual void OnDestroy()
    {
        if (TickManager.Instance != null)
            TickManager.Instance.OnTick -= HandleTickSystem;

        if (internalVisual != null) Destroy(internalVisual.gameObject);
        if (incomingVisual != null) Destroy(incomingVisual.gameObject);
    }

    // --- INITIALIZATION (Fixes CS1061 Error) ---
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
        // 1. Process Logic (Try to push 'Internal' out)
        OnTick(tick);

        // 2. Move Mailbox to Internal (If Internal is now empty)
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
            // Visual smoothing: Move to center
            float duration = TickManager.Instance.tickRate;
            internalVisual.InitializeMovement(transform.position + Vector3.up * 0.2f, duration);
        }
    }

    public virtual bool CanAcceptItem(Vector2Int fromPos)
    {
        // Double Buffering: Only reject if the MAILBOX is full. 
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