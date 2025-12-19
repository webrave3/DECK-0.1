using UnityEngine;

public abstract class BuildingBase : MonoBehaviour
{
    public Vector2Int GridPosition { get; private set; }
    public BuildingDefinition Definition;
    public int RotationIndex { get; private set; }

    [Header("Debug")]
    public bool showDebugLogs = false; // TOGGLE THIS IN INSPECTOR TO SEE LOGS

    // MAIN INVENTORY
    protected CardPayload internalCard;
    protected ItemVisualizer internalVisual;

    // MAILBOX
    protected CardPayload incomingCard;
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

        // FIX: Ensure visuals are destroyed when building is deleted
        if (internalVisual != null) Destroy(internalVisual.gameObject);
        if (incomingVisual != null) Destroy(incomingVisual.gameObject);
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
        // Phase 1: Logic (Try to push output)
        OnTick(tick);

        // Phase 2: Commit (Accept incoming mail)
        if (incomingCard != null && internalCard == null)
        {
            internalCard = incomingCard;
            internalVisual = incomingVisual;

            incomingCard = null;
            incomingVisual = null;

            if (showDebugLogs) Debug.Log($"[{name} at {GridPosition}] Accepted item.");

            OnItemArrived();
        }
    }

    protected abstract void OnTick(int tick);

    // Helper for child classes to handle standard visual movement
    protected virtual void OnItemArrived()
    {
        if (internalVisual != null)
        {
            internalVisual.transform.SetParent(this.transform);
            // Default behavior: Move to center of tile
            Vector3 center = transform.position + Vector3.up * 0.2f;
            internalVisual.InitializeMovement(internalVisual.transform.position, center);
        }
    }

    public virtual bool CanAcceptItem(Vector2Int fromPos)
    {
        // Default: Accept if empty
        bool canAccept = internalCard == null && incomingCard == null;
        if (!canAccept && showDebugLogs) Debug.Log($"[{name}] Refused item from {fromPos} (Full)");
        return canAccept;
    }

    public virtual void ReceiveItem(CardPayload item, ItemVisualizer visual)
    {
        incomingCard = item;
        incomingVisual = visual;
    }

    public virtual bool CanBePlacedAt(Vector2Int gridPos) => true;
}