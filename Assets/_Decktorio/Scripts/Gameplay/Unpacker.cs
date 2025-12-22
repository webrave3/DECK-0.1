using UnityEngine;

public class Unpacker : BuildingBase
{
    [Header("Production")]
    public float productionSpeed = 3f;
    public Transform outputAnchor;

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

        // --- SAFETY REGISTRATION ---
        Vector2Int truePos = CasinoGridManager.Instance.WorldToGrid(transform.position);
        if (CasinoGridManager.Instance.GetBuildingAt(truePos) != this)
        {
            Setup(truePos);
            // FIXED: Arguments swapped to (Position, Building)
            CasinoGridManager.Instance.RegisterBuilding(truePos, this);
            Debug.Log($"<color=orange>[Unpacker]</color> Auto-Registered at {truePos}");
        }
        // ---------------------------

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

        CardData newData = new CardData(linkedResource.rank, linkedResource.suit, linkedResource.material, linkedResource.ink);
        internalItem = new ItemPayload(newData);

        if (linkedResource.outputPrefab != null)
        {
            Vector3 spawnPos = outputAnchor != null ? outputAnchor.position : transform.position + Vector3.up * 0.5f;
            GameObject visualObj = Instantiate(linkedResource.outputPrefab, spawnPos, Quaternion.identity);

            internalVisual = visualObj.GetComponent<ItemVisualizer>();
            if (internalVisual == null) internalVisual = visualObj.AddComponent<ItemVisualizer>();

            internalVisual.transform.SetParent(this.transform);
            internalVisual.SetVisuals(internalItem);
        }
        lastStatus = "Item Extracted";
    }

    void TryPushDirectional()
    {
        Vector2Int forwardPos = GetForwardGridPosition();
        BuildingBase target = CasinoGridManager.Instance.GetBuildingAt(forwardPos);

        if (target == null)
        {
            lastStatus = $"Blocked: Nothing at {forwardPos}";
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
            lastStatus = "Blocked: Target Refused";
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawRay(transform.position + Vector3.up, transform.forward * 1.0f);
    }
}