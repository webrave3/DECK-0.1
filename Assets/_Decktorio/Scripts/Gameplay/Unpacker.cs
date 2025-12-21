using UnityEngine;

public class Unpacker : BuildingBase
{
    [Header("Production")]
    public float productionSpeed = 3f;

    [Tooltip("Where the item visually appears. If empty, uses building center.")]
    public Transform outputAnchor;

    private float progress = 0;
    private SupplyDrop linkedResource;

    // Only placeable on SupplyDrops
    public override bool CanBePlacedAt(Vector2Int gridPos)
    {
        return CasinoGridManager.Instance.GetResourceAt(gridPos) != null;
    }

    protected override void Start()
    {
        base.Start();
        // Cache the resource node we are sitting on
        linkedResource = CasinoGridManager.Instance.GetResourceAt(GridPosition);

        // Debug warning if no resource found (just in case)
        if (linkedResource == null)
        {
            // Optional: You might want to log this if testing
            // GameLogger.Log($"Unpacker at {GridPosition} has no resource below it!");
        }
    }

    protected override void OnTick(int tick)
    {
        // 1. If output is full, try to push and do nothing else
        if (internalItem != null)
        {
            TryPushItem();
            return;
        }

        // 2. If we have a resource, mine it
        if (linkedResource != null)
        {
            progress += TickManager.Instance.tickRate;
            if (progress >= productionSpeed)
            {
                progress = 0;
                SpawnFromResource();
            }
        }
    }

    void SpawnFromResource()
    {
        if (linkedResource == null) return;

        // 1. Create Data based on the Node's settings
        CardData newData = new CardData(
            linkedResource.rank,
            linkedResource.suit,
            linkedResource.material,
            linkedResource.ink
        );

        // 2. Wrap in Payload
        internalItem = new ItemPayload(newData);

        // 3. Create Visuals
        if (linkedResource.outputPrefab != null)
        {
            // FIX: Check if outputAnchor exists. If not, use transform.position + Up offset
            Vector3 spawnPos = (outputAnchor != null)
                ? outputAnchor.position
                : transform.position + Vector3.up * 0.5f;

            GameObject visualObj = Instantiate(linkedResource.outputPrefab, spawnPos, Quaternion.identity);

            internalVisual = visualObj.GetComponent<ItemVisualizer>();
            if (internalVisual == null) internalVisual = visualObj.AddComponent<ItemVisualizer>();

            internalVisual.transform.SetParent(this.transform);
            internalVisual.SetVisuals(internalItem);
        }
    }

    void TryPushItem()
    {
        Vector2Int forward = GetForwardGridPosition();
        var target = CasinoGridManager.Instance.GetBuildingAt(forward);

        if (target != null && target.CanAcceptItem(GridPosition))
        {
            target.ReceiveItem(internalItem, internalVisual);
            internalItem = null;
            internalVisual = null;
        }
    }
}