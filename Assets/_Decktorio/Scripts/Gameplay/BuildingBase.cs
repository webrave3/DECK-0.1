using UnityEngine;

public abstract class BuildingBase : MonoBehaviour
{
    public Vector2Int GridPosition { get; private set; }
    public BuildingDefinition Definition; // Assign in Inspector

    // Direction the building is facing (0=Up, 1=Right, 2=Down, 3=Left)
    public int RotationIndex { get; private set; }

    protected virtual void Start()
    {
        TickManager.Instance.OnTick += HandleTick;
    }

    protected virtual void OnDestroy()
    {
        if (TickManager.Instance != null)
            TickManager.Instance.OnTick -= HandleTick;
    }

    public void Setup(Vector2Int pos)
    {
        GridPosition = pos;
        transform.position = CasinoGridManager.Instance.GridToWorld(pos);
    }

    // Use this to rotate the building logic
    public void SetRotation(int rotIndex)
    {
        RotationIndex = rotIndex;
        // Visual rotation
        transform.rotation = Quaternion.Euler(0, rotIndex * 90, 0);
    }

    // Returns the grid coordinate "Forward" of this building
    public Vector2Int GetForwardGridPosition()
    {
        Vector2Int forwardDir = Vector2Int.zero;
        switch (RotationIndex)
        {
            case 0: forwardDir = new Vector2Int(0, 1); break; // Up (North)
            case 1: forwardDir = new Vector2Int(1, 0); break; // Right (East)
            case 2: forwardDir = new Vector2Int(0, -1); break; // Down (South)
            case 3: forwardDir = new Vector2Int(-1, 0); break; // Left (West)
        }
        return GridPosition + forwardDir;
    }

    // Abstract: Every building MUST implement what it does on a tick
    protected abstract void HandleTick(int tick);

    // Optional: Can this building accept an item from a specific direction?
    public virtual bool CanAcceptItem(Vector2Int fromPos) { return false; }

    // Optional: Receive an item (used by belts/assemblers)
    public virtual void ReceiveItem(CardPayload item) { }
}