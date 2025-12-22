using UnityEngine;

public class Unpacker : BuildingBase
{
    [Header("Production")]
    public float productionSpeed = 3f;
    [Tooltip("The exact point where the card visually appears.")]
    public Transform outputAnchor;

    [Header("I/O Configuration")]
    [Tooltip("Adjust this to point the Cyan line to the output belt. 0=Front, 1=Right, 2=Back, 3=Left")]
    [Range(0, 3)]
    public int outputDirectionOffset = 0;

    private float progress = 0;
    private SupplyDrop linkedResource;
    public string lastStatus = "Initializing";

    // Only placeable on SupplyDrops
    public override bool CanBePlacedAt(Vector2Int gridPos)
    {
        return CasinoGridManager.Instance.GetResourceAt(gridPos) != null;
    }

    protected override void Start()
    {
        base.Start();
        if (CasinoGridManager.Instance == null) return;

        // Auto-Register & Snap
        Vector2Int truePos = CasinoGridManager.Instance.WorldToGrid(transform.position);
        transform.position = CasinoGridManager.Instance.GridToWorld(truePos);

        if (CasinoGridManager.Instance.GetBuildingAt(truePos) != this)
        {
            Setup(truePos);
            CasinoGridManager.Instance.RegisterBuilding(truePos, this);
        }

        linkedResource = CasinoGridManager.Instance.GetResourceAt(GridPosition);
        if (linkedResource == null)
        {
            lastStatus = "ERROR: No Resource Below";
            Debug.LogError($"[Unpacker] Placed at {GridPosition} but found NO Resource Node!");
        }
    }

    protected override void OnTick(int tick)
    {
        if (internalItem != null)
        {
            TryPushDirectional();
            return;
        }

        if (linkedResource != null)
        {
            progress += TickManager.Instance.tickRate;
            if (progress >= productionSpeed)
            {
                progress = 0;
                SpawnFromResource();
            }
            lastStatus = "Extracting...";
        }
    }

    void SpawnFromResource()
    {
        if (linkedResource == null) return;

        // --- GHOST PREVENTION CHECK ---
        if (linkedResource.outputPrefab == null)
        {
            lastStatus = "ERROR: Resource has no Prefab";
            return;
        }
        // ------------------------------

        CardData newData = new CardData(linkedResource.rank, linkedResource.suit, linkedResource.material, linkedResource.ink);
        internalItem = new ItemPayload(newData);

        Vector3 spawnPos = outputAnchor != null ? outputAnchor.position : transform.position + Vector3.up * 0.5f;
        GameObject visualObj = Instantiate(linkedResource.outputPrefab, spawnPos, Quaternion.identity);

        // --- INSTANTIATION SAFETY CHECK ---
        if (visualObj == null)
        {
            Debug.LogError($"[{name}] Failed to spawn resource visual! Clearing data.");
            internalItem = null;
            return;
        }

        internalVisual = visualObj.GetComponent<ItemVisualizer>();
        if (internalVisual == null) internalVisual = visualObj.AddComponent<ItemVisualizer>();

        internalVisual.transform.SetParent(this.transform);
        internalVisual.SetVisuals(internalItem);

        lastStatus = "Item Extracted";
    }

    private int GetCurrentRotationIndex()
    {
        if (Application.isPlaying) return RotationIndex;
        float y = transform.eulerAngles.y;
        return Mathf.RoundToInt(y / 90f) % 4;
    }

    private Vector2Int GetOutputGridPosition()
    {
        int currentRot = GetCurrentRotationIndex();
        int finalIndex = (currentRot + outputDirectionOffset) % 4;

        Vector2Int basePos = GridPosition;
        if (!Application.isPlaying && CasinoGridManager.Instance != null)
            basePos = CasinoGridManager.Instance.WorldToGrid(transform.position);

        Vector2Int dir = Vector2Int.zero;
        switch (finalIndex)
        {
            case 0: dir = new Vector2Int(0, 1); break; // North
            case 1: dir = new Vector2Int(1, 0); break; // East
            case 2: dir = new Vector2Int(0, -1); break; // South
            case 3: dir = new Vector2Int(-1, 0); break; // West
        }
        return basePos + dir;
    }

    void TryPushDirectional()
    {
        Vector2Int forwardPos = GetOutputGridPosition();
        if (CasinoGridManager.Instance == null) return;

        BuildingBase target = CasinoGridManager.Instance.GetBuildingAt(forwardPos);

        if (target == null)
        {
            lastStatus = "Blocked: No Building";
            return;
        }

        if (target.CanAcceptItem(GridPosition))
        {
            target.ReceiveItem(internalItem, internalVisual);
            internalItem = null;
            internalVisual = null;
            lastStatus = "Pushed Success";
        }
        else
        {
            if (target is ConveyorBelt belt && belt.GetForwardGridPosition() == GridPosition)
                lastStatus = "Blocked: Backflow";
            else
                lastStatus = "Blocked: Target Full";
        }
    }

    private void OnDrawGizmos()
    {
        if (CasinoGridManager.Instance == null) return;
        Vector2Int targetPos = GetOutputGridPosition();
        Vector3 start = transform.position + Vector3.up;
        Vector3 end = CasinoGridManager.Instance.GridToWorld(targetPos) + Vector3.up;

        Gizmos.color = Color.cyan;
        Gizmos.DrawLine(start, end);
        Gizmos.DrawSphere(end, 0.2f);
        Gizmos.DrawRay(start, (end - start).normalized * 0.5f);
    }
}