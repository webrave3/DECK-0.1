using UnityEngine;

public class Unpacker : BuildingBase
{
    [Header("Production")]
    public float productionSpeed = 5f;
    public Transform outputAnchor;

    private float progress = 0;
    private ItemVisualizer currentVisual;

    // --- NEW CONSTRAINT ---
    public override bool CanBePlacedAt(Vector2Int gridPos)
    {
        // Only allow placement if a Resource is underneath
        return CasinoGridManager.Instance.GetResourceAt(gridPos) != null;
    }
    // ---------------------

    protected override void HandleTick(int tick)
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
        internalCard = new CardPayload(source.rank, source.suit, 1);
        GameObject itemObj = Instantiate(source.itemPrefab, outputAnchor.position, Quaternion.identity);
        currentVisual = itemObj.GetComponent<ItemVisualizer>();
        currentVisual.transform.SetParent(this.transform);
    }

    void TryPushItem()
    {
        Vector2Int forwardPos = GetForwardGridPosition();
        BuildingBase target = CasinoGridManager.Instance.GetBuildingAt(forwardPos);

        if (target != null && target.CanAcceptItem(GridPosition))
        {
            target.ReceiveItem(internalCard, currentVisual);
            internalCard = null;
            currentVisual = null;
        }
    }
}