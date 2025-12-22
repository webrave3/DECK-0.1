using UnityEngine;

public class SupplyDrop : BuildingBase
{
    [Header("Resource Definition")]
    public string resourceName = "Cardstock";

    [Header("Output Configuration")]
    public CardSuit suit = CardSuit.None;
    public int rank = 0;
    public CardMaterial material = CardMaterial.Cardstock;
    public CardInk ink = CardInk.Standard;

    [Header("Visuals")]
    public GameObject outputPrefab;

    // CHANGED: Use Start (not Awake) to wait for CasinoGridManager initialization
    protected override void Start()
    {
        // We DO NOT call base.Start() because SupplyDrop is a passive resource,
        // not a ticking machine.

        // --- SAFETY REGISTRATION & SNAPPING ---
        if (CasinoGridManager.Instance != null)
        {
            Vector2Int pos = CasinoGridManager.Instance.WorldToGrid(transform.position);

            // 1. Register with the Grid
            CasinoGridManager.Instance.RegisterResource(this, pos);

            // 2. FIX: Snap visual position to the center of the grid cell
            // This ensures Unpackers can snap to it correctly.
            transform.position = CasinoGridManager.Instance.GridToWorld(pos);

            Debug.Log($"<color=cyan>[SupplyDrop]</color> Registered & Aligned at {pos}");
        }
        else
        {
            Debug.LogError("[SupplyDrop] GridManager not found! Is it in the scene?");
        }
    }

    protected override void OnTick(int tick) { }
}