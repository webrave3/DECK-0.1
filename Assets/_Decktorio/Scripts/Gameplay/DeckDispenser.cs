using UnityEngine;

public class DeckDispenser : BuildingBase
{
    [Header("Visual Settings")]
    public GameObject itemPrefab;
    [Tooltip("The exact point where the card visually appears.")]
    public Transform outputAnchor;

    [Header("Dispenser Settings")]
    public float dispenseInterval = 2.0f;
    public CardSuit outputSuit = CardSuit.Heart;
    public int outputRank = 2;

    [Header("I/O Configuration")]
    [Tooltip("Adjust this to point the yellow line to the output belt. 0=Front, 1=Right, 2=Back, 3=Left")]
    [Range(0, 3)]
    public int outputDirectionOffset = 0;

    private float timer = 0f;
    public string lastStatus = "Initializing";

    protected override void Start()
    {
        base.Start();
        if (itemPrefab == null) Debug.LogError($"[{name}] ERROR: Item Prefab MISSING!");

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
    }

    protected override void OnTick(int tick)
    {
        if (internalItem == null)
        {
            timer += TickManager.Instance.tickRate;
            if (timer >= dispenseInterval)
            {
                SpawnCard();
                timer = 0f;
            }
            else lastStatus = $"Charging: {(int)((timer / dispenseInterval) * 100)}%";
        }
        else
        {
            TryPushDirectional();
        }
    }

    public override bool CanBePlacedAt(Vector2Int gridPos)
    {
        // Only allow placement if there is a Resource (SupplyDrop) at this position
        return CasinoGridManager.Instance.GetResourceAt(gridPos) != null;
    }

    void SpawnCard()
    {
        if (itemPrefab == null) { lastStatus = "Error: No Prefab"; return; }

        CardData newCard = new CardData(outputRank, outputSuit, CardMaterial.Cardstock, CardInk.Standard);
        internalItem = new ItemPayload(newCard);

        Vector3 spawnPos = outputAnchor != null ? outputAnchor.position : transform.position + Vector3.up * 0.6f;
        GameObject visualObj = Instantiate(itemPrefab, spawnPos, Quaternion.identity);

        if (visualObj == null)
        {
            Debug.LogError("Spawn Failed. Clearing Data.");
            internalItem = null;
            return;
        }

        internalVisual = visualObj.GetComponent<ItemVisualizer>();
        if (internalVisual == null) internalVisual = visualObj.AddComponent<ItemVisualizer>();

        internalVisual.transform.SetParent(this.transform);
        internalVisual.SetVisuals(internalItem);
        lastStatus = "Card Ready";
    }

    private Vector2Int GetOutputGridPosition()
    {
        int currentRot = Application.isPlaying ? RotationIndex : Mathf.RoundToInt(transform.eulerAngles.y / 90f) % 4;
        int finalIndex = (currentRot + outputDirectionOffset) % 4;
        Vector2Int dir = Vector2Int.zero;
        switch (finalIndex) { case 0: dir = Vector2Int.up; break; case 1: dir = Vector2Int.right; break; case 2: dir = Vector2Int.down; break; case 3: dir = Vector2Int.left; break; }
        return GridPosition + dir;
    }

    void TryPushDirectional()
    {
        Vector2Int targetPos = GetOutputGridPosition();
        if (CasinoGridManager.Instance == null) return;
        BuildingBase target = CasinoGridManager.Instance.GetBuildingAt(targetPos);

        if (target == null) { lastStatus = "Blocked: No Target"; return; }

        if (target.CanAcceptItem(GridPosition))
        {
            target.ReceiveItem(internalItem, internalVisual);
            internalItem = null;
            internalVisual = null;
            lastStatus = "Pushed Success";
        }
        else lastStatus = "Blocked: Target Full";
    }

    private void OnDrawGizmos()
    {
        if (CasinoGridManager.Instance == null) return;
        Vector2Int targetPos = GetOutputGridPosition();
        Vector3 start = transform.position + Vector3.up;
        Vector3 end = CasinoGridManager.Instance.GridToWorld(targetPos) + Vector3.up;
        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(start, end);
        Gizmos.DrawSphere(end, 0.2f);
    }
}