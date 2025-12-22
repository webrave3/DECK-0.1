using UnityEngine;

public abstract class BuildingBase : MonoBehaviour
{
    public Vector2Int GridPosition { get; private set; }
    public BuildingDefinition Definition;
    public int RotationIndex { get; private set; }

    [Header("Debug")]
    public bool showDebugLogs = false;
    public bool isGhost = false;

    // DATA
    public ItemPayload internalItem;
    public ItemPayload incomingItem;

    // VISUALS
    protected ItemVisualizer internalVisual;
    protected ItemVisualizer incomingVisual;

    protected virtual void Start()
    {
        if (isGhost) return;

        if (CasinoGridManager.Instance != null)
        {
            Vector2Int truePos = CasinoGridManager.Instance.WorldToGrid(transform.position);
            // Auto-align
            transform.position = CasinoGridManager.Instance.GridToWorld(truePos);

            if (CasinoGridManager.Instance.GetBuildingAt(truePos) != this)
            {
                Setup(truePos);
                CasinoGridManager.Instance.RegisterBuilding(truePos, this);
            }
        }

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

        if (CasinoGridManager.Instance != null && CasinoGridManager.Instance.GetBuildingAt(GridPosition) == this)
        {
            CasinoGridManager.Instance.RemoveBuilding(GridPosition);
        }
    }

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
            case 0: forwardDir = new Vector2Int(0, 1); break;
            case 1: forwardDir = new Vector2Int(1, 0); break;
            case 2: forwardDir = new Vector2Int(0, -1); break;
            case 3: forwardDir = new Vector2Int(-1, 0); break;
        }
        return GridPosition + forwardDir;
    }

    private void HandleTickSystem(int tick)
    {
        if (isGhost) return;

        // --- GLOBAL SELF-HEALING FIX ---
        // If we have Data, but the Visual Object is null (or was destroyed by physics/killplane)
        // We MUST clear the data, otherwise the machine thinks it's full forever.
        if (internalItem != null && internalVisual == null)
        {
            Debug.LogWarning($"<color=orange>[{name}]</color> Visual Object died unexpectedly! Clearing Ghost Data to unclog machine.");
            internalItem = null;
        }
        // -------------------------------

        OnTick(tick);

        // Move Mailbox to Internal
        if (incomingItem != null && internalItem == null)
        {
            internalItem = incomingItem;
            internalVisual = incomingVisual;

            incomingItem = null;
            incomingVisual = null;

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
        if (internalItem != null) return false;
        return true;
    }

    public virtual void ReceiveItem(ItemPayload item, ItemVisualizer visual)
    {
        // Safety: Reject if visual is missing
        if (visual == null)
        {
            Debug.LogError($"[{name}] Attempted to receive item with NULL visual! Rejecting to prevent ghosts.");
            return;
        }
        incomingItem = item;
        incomingVisual = visual;
    }

    public virtual bool CanBePlacedAt(Vector2Int gridPos) => true;

    [ContextMenu("Debug: Force Clear Inventory")]
    public void ForceClearInventory()
    {
        internalItem = null;
        incomingItem = null;
        if (internalVisual != null) Destroy(internalVisual.gameObject);
        if (incomingVisual != null) Destroy(incomingVisual.gameObject);
        internalVisual = null;
        incomingVisual = null;
        Debug.Log($"<color=yellow>[{name}]</color> Inventory Cleared Forcefully.");
    }
}