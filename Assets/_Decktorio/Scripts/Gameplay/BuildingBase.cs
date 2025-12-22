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
        if (TickManager.Instance != null) TickManager.Instance.OnTick -= HandleTickSystem;
        if (internalVisual != null) Destroy(internalVisual.gameObject);
        if (incomingVisual != null) Destroy(incomingVisual.gameObject);
        if (CasinoGridManager.Instance != null && CasinoGridManager.Instance.GetBuildingAt(GridPosition) == this)
            CasinoGridManager.Instance.RemoveBuilding(GridPosition);
    }

    public void Setup(Vector2Int pos)
    {
        GridPosition = pos;
        transform.position = CasinoGridManager.Instance.GridToWorld(pos);
    }

    public void Initialize(Vector2Int pos, int rot)
    {
        Setup(pos);
        SetRotation(rot);
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

        // Safety: If visual destroyed, clear data to prevent blockages
        if (internalItem != null && internalVisual == null)
        {
           
            internalItem = null;
        }

        OnTick(tick);

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
            // Default behavior: Move to center
            internalVisual.InitializeMovement(transform.position + Vector3.up * 0.2f, TickManager.Instance.tickRate);
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
        if (visual == null) return;
        incomingItem = item;
        incomingVisual = visual;
    }

    public virtual bool CanBePlacedAt(Vector2Int gridPos) => true;

    [ContextMenu("Debug: Force Clear")]
    public void ForceClearInventory()
    {
        internalItem = null;
        incomingItem = null;
        if (internalVisual != null) Destroy(internalVisual.gameObject);
        if (incomingVisual != null) Destroy(incomingVisual.gameObject);
    }
}