using UnityEngine;

public abstract class BuildingBase : MonoBehaviour
{
    public Vector2Int GridPosition { get; private set; }
    public BuildingDefinition Definition;
    public int RotationIndex { get; private set; }

    // MAIN INVENTORY (Current State)
    protected CardPayload internalCard;
    protected ItemVisualizer internalVisual;

    // MAILBOX (Next State Buffer)
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

    // --- THE TWO-PHASE TICK SYSTEM ---
    private void HandleTickSystem(int tick)
    {
        // Phase 1: Logic (Try to push output)
        OnTick(tick);

        // Phase 2: Commit (Accept incoming mail)
        // This runs AFTER everyone has decided where to push
        if (incomingCard != null && internalCard == null)
        {
            internalCard = incomingCard;
            internalVisual = incomingVisual;

            incomingCard = null;
            incomingVisual = null;

            // Trigger Visual Animation logic here
            OnItemArrived();
        }
    }

    protected abstract void OnTick(int tick); // Child classes implement this
    protected virtual void OnItemArrived() { } // Optional hook

    // --- RECEIVE LOGIC ---
    public virtual bool CanAcceptItem(Vector2Int fromPos)
    {
        // We can only accept if our main slot AND our mailbox are empty
        return internalCard == null && incomingCard == null;
    }

    public virtual void ReceiveItem(CardPayload item, ItemVisualizer visual)
    {
        // Place in Mailbox. Do NOT put in internalCard yet.
        incomingCard = item;
        incomingVisual = visual;
    }

    public virtual bool CanBePlacedAt(Vector2Int gridPos) => true;
}