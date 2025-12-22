using UnityEngine;

public class DeckDispenser : BuildingBase
{
    [Header("Visual Settings")]
    public GameObject itemPrefab;
    public Transform outputAnchor;

    [Header("Dispenser Settings")]
    public float dispenseInterval = 2.0f;
    public CardSuit outputSuit = CardSuit.Heart;
    public int outputRank = 2;

    private float timer = 0f;
    public string lastStatus = "Initializing";

    protected override void Start()
    {
        base.Start();

        // --- SAFETY REGISTRATION ---
        // Force register if manually placed
        Vector2Int truePos = CasinoGridManager.Instance.WorldToGrid(transform.position);
        if (CasinoGridManager.Instance.GetBuildingAt(truePos) != this)
        {
            Setup(truePos);
            // FIXED: Arguments swapped to (Position, Building)
            CasinoGridManager.Instance.RegisterBuilding(truePos, this);
            Debug.Log($"<color=green>[DeckDispenser]</color> Auto-Registered at {truePos}");
        }
        // ---------------------------
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
            else
            {
                lastStatus = $"Charging: {(int)((timer / dispenseInterval) * 100)}%";
            }
        }
        else
        {
            TryPushDirectional();
        }
    }

    void SpawnCard()
    {
        CardData newCard = new CardData(outputRank, outputSuit, CardMaterial.Cardstock, CardInk.Standard);
        internalItem = new ItemPayload(newCard);

        if (itemPrefab != null)
        {
            Vector3 spawnPos = outputAnchor != null ? outputAnchor.position : transform.position + Vector3.up * 0.6f;
            GameObject visualObj = Instantiate(itemPrefab, spawnPos, Quaternion.identity);

            internalVisual = visualObj.GetComponent<ItemVisualizer>();
            if (internalVisual == null) internalVisual = visualObj.AddComponent<ItemVisualizer>();

            internalVisual.transform.SetParent(this.transform);
            internalVisual.SetVisuals(internalItem);
        }

        lastStatus = "Card Ready (Waiting for Belt)";
    }

    void TryPushDirectional()
    {
        Vector2Int forwardPos = GetForwardGridPosition();
        BuildingBase target = CasinoGridManager.Instance.GetBuildingAt(forwardPos);

        if (target == null)
        {
            lastStatus = $"Blocked: No Building at {forwardPos}";
            Debug.DrawLine(transform.position, CasinoGridManager.Instance.GridToWorld(forwardPos), Color.red, 1.0f);
            return;
        }

        if (target.CanAcceptItem(GridPosition))
        {
            target.ReceiveItem(internalItem, internalVisual);
            internalItem = null;
            internalVisual = null;
            lastStatus = "Pushed Success";
            Debug.Log($"<color=green>[Dispenser]</color> Pushed item to {target.name} at {forwardPos}");
        }
        else
        {
            lastStatus = "Blocked: Target Full/Refused";
        }
    }

    private void OnDrawGizmos()
    {
        if (Application.isPlaying)
        {
            Vector2Int fwd = GetForwardGridPosition();
            if (CasinoGridManager.Instance != null)
            {
                Vector3 start = transform.position + Vector3.up;
                Vector3 end = CasinoGridManager.Instance.GridToWorld(fwd) + Vector3.up;

                Gizmos.color = Color.yellow;
                Gizmos.DrawLine(start, end);
                Gizmos.DrawSphere(end, 0.2f);
            }
        }
    }
}