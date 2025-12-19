using UnityEngine;

public class Unpacker : BuildingBase
{
    [Header("Production")]
    public float productionSpeed = 5f;
    public Transform outputAnchor;

    private float progress = 0;

    public override bool CanBePlacedAt(Vector2Int gridPos)
    {
        return CasinoGridManager.Instance.GetResourceAt(gridPos) != null;
    }

    protected override void OnTick(int tick)
    {
        if (internalCard != null)
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
        // 1. Create Data
        internalCard = new CardPayload(source.rank, source.suit, 1);

        // 2. Create Visual
        GameObject itemObj = Instantiate(source.itemPrefab, outputAnchor.position, Quaternion.identity);
        internalVisual = itemObj.GetComponent<ItemVisualizer>();
        internalVisual.transform.SetParent(this.transform);

        // 3. APPLY VISUALS (New Line)
        internalVisual.SetVisuals(internalCard);
    }

    void TryPushItem()
    {
        Vector2Int forwardPos = GetForwardGridPosition();
        BuildingBase target = CasinoGridManager.Instance.GetBuildingAt(forwardPos);

        if (target != null && target.CanAcceptItem(GridPosition))
        {
            target.ReceiveItem(internalCard, internalVisual);
            internalCard = null;
            internalVisual = null;
        }
    }
}