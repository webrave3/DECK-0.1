using UnityEngine;

// NOTE: This acts as a "Resource Extractor" based on your original code.
// Consider renaming to "CardPress" or "Extractor" later.
public class Unpacker : BuildingBase
{
    [Header("Production")]
    public float productionSpeed = 5f;
    public Transform outputAnchor;

    private float progress = 0;

    // Only placeable on SupplyDrops (Resources)
    public override bool CanBePlacedAt(Vector2Int gridPos)
    {
        return CasinoGridManager.Instance.GetResourceAt(gridPos) != null;
    }

    protected override void OnTick(int tick)
    {
        if (internalItem != null)
        {
            TryPushItem();
            return;
        }
        TryMine();
    }

    void TryMine()
    {
        SupplyDrop drop = CasinoGridManager.Instance.GetResourceAt(GridPosition);
        if (drop != null)
        {
            progress += 1f;
            if (progress >= productionSpeed)
            {
                progress = 0;
                SpawnItem(drop);
            }
        }
    }

    void SpawnItem(SupplyDrop source)
    {
        // 1. Create Data (Single Card struct)
        CardData newCard = new CardData(source.rank, source.suit);

        // 2. Wrap in Payload (Stack of 1)
        internalItem = new ItemPayload(newCard);

        // 3. Create Visual
        if (source.itemPrefab != null && outputAnchor != null)
        {
            GameObject itemObj = Instantiate(source.itemPrefab, outputAnchor.position, Quaternion.identity);
            internalVisual = itemObj.GetComponent<ItemVisualizer>();
            if (internalVisual == null) internalVisual = itemObj.AddComponent<ItemVisualizer>();

            internalVisual.transform.SetParent(this.transform);
            internalVisual.SetVisuals(internalItem);
        }
    }

    void TryPushItem()
    {
        Vector2Int forwardPos = GetForwardGridPosition();
        BuildingBase target = CasinoGridManager.Instance.GetBuildingAt(forwardPos);

        if (target != null && target.CanAcceptItem(GridPosition))
        {
            target.ReceiveItem(internalItem, internalVisual);
            internalItem = null;
            internalVisual = null;
        }
    }
}