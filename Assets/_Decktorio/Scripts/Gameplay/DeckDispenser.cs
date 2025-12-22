using UnityEngine;

public class DeckDispenser : BuildingBase
{
    [Header("Visual Settings")]
    public GameObject itemPrefab;
    [Tooltip("The exact point where the card visually appears (e.g. the mouth of the machine). If null, spawns at center.")]
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

        // Safety Check at Startup
        if (itemPrefab == null)
        {
            Debug.LogError($"<color=red>[{name}]</color> CRITICAL: Item Prefab is MISSING! Dispenser disabled to prevent bugs.");
        }

        if (CasinoGridManager.Instance != null)
        {
            Vector2Int truePos = CasinoGridManager.Instance.WorldToGrid(transform.position);

            // Snap to grid center to ensure alignment
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
        // --- CRITICAL FIX: STOP GHOST ITEMS ---
        // Do not create data if we cannot create a visual. 
        // This prevents the "Invisible Item" clog downstream.
        if (itemPrefab == null)
        {
            lastStatus = "ERROR: Missing Prefab";
            return;
        }
        // --------------------------------------

        CardData newCard = new CardData(outputRank, outputSuit, CardMaterial.Cardstock, CardInk.Standard);
        internalItem = new ItemPayload(newCard);

        // Determine spawn position
        Vector3 spawnPos = outputAnchor != null ? outputAnchor.position : transform.position + Vector3.up * 0.6f;

        GameObject visualObj = Instantiate(itemPrefab, spawnPos, Quaternion.identity);

        // --- SECONDARY SAFETY CHECK ---
        if (visualObj == null)
        {
            Debug.LogError($"[{name}] Instantiation failed! Clearing data to prevent ghost item.");
            internalItem = null;
            return;
        }

        internalVisual = visualObj.GetComponent<ItemVisualizer>();
        if (internalVisual == null) internalVisual = visualObj.AddComponent<ItemVisualizer>();

        internalVisual.transform.SetParent(this.transform);
        internalVisual.SetVisuals(internalItem);

        lastStatus = "Card Ready";
    }

    // --- LOGIC CALCULATIONS ---

    private int GetCurrentRotationIndex()
    {
        // In Play Mode, use the cached RotationIndex
        if (Application.isPlaying) return RotationIndex;

        // In Edit Mode, calculate from Transform rotation
        float y = transform.eulerAngles.y;
        return Mathf.RoundToInt(y / 90f) % 4;
    }

    private Vector2Int GetOutputGridPosition()
    {
        int currentRot = GetCurrentRotationIndex();
        int finalIndex = (currentRot + outputDirectionOffset) % 4;

        Vector2Int basePos = GridPosition;
        if (!Application.isPlaying && CasinoGridManager.Instance != null)
        {
            basePos = CasinoGridManager.Instance.WorldToGrid(transform.position);
        }

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
        Vector2Int targetPos = GetOutputGridPosition();
        if (CasinoGridManager.Instance == null) return;

        BuildingBase target = CasinoGridManager.Instance.GetBuildingAt(targetPos);

        if (target == null)
        {
            lastStatus = $"Blocked: Empty Space at {targetPos}";
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
            // DETAILED DEBUGGING FOR STATUS
            string reason = "Unknown";
            if (target is ConveyorBelt belt && belt.GetForwardGridPosition() == GridPosition)
                reason = "Backflow (Belt facing wrong way)";
            else if (target.internalItem != null)
                reason = "Target Full";

            lastStatus = $"Blocked: {reason}";
        }
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

        // Draw small arrow to indicate flow direction
        Vector3 direction = (end - start).normalized;
        Gizmos.DrawRay(start, direction * 0.5f);
    }
}