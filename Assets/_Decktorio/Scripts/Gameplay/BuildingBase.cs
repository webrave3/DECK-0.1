using UnityEngine;

public abstract class BuildingBase : MonoBehaviour
{
    public Vector2Int GridPosition { get; private set; }
    public BuildingDefinition Definition;
    public int RotationIndex { get; private set; }

    protected CardPayload internalCard;
    protected int lastProcessedTick = -1; // Fix for cascading speed bug

    protected virtual void Start()
    {
        if (TickManager.Instance != null)
            TickManager.Instance.OnTick += OnTickInternal;
    }

    protected virtual void OnDestroy()
    {
        if (TickManager.Instance != null)
            TickManager.Instance.OnTick -= OnTickInternal;
    }

    private void OnTickInternal(int tick)
    {
        // Prevent double-updates in the same tick
        if (lastProcessedTick == tick) return;

        lastProcessedTick = tick;
        HandleTick(tick);
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

    // --- LOGIC METHODS ---
    protected abstract void HandleTick(int tick);

    public virtual bool CanAcceptItem(Vector2Int fromPos) { return false; }

    public virtual void ReceiveItem(CardPayload item, ItemVisualizer visual)
    {
        // When receiving an item, we mark ourselves as "processed" for this tick
        // so we don't immediately pass it on (preventing infinite speed)
        // However, we access the current tick via the Manager
        // (Use a simple workaround: If we receive, we wait for NEXT tick to push)
        internalCard = item;
    }

    // --- PLACEMENT VALIDATION ---
    // Override this in Unpacker to check for resources
    public virtual bool CanBePlacedAt(Vector2Int gridPos)
    {
        return true; // Default: Can be placed anywhere valid
    }
}