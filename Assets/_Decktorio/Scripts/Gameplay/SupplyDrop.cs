using UnityEngine;

public class SupplyDrop : BuildingBase
{
    [Header("Resource Settings")]
    public GameObject itemPrefab; // The visual card to spawn
    public CardSuit suit = CardSuit.Heart;
    public int rank = 1;

    protected override void Start()
    {
        // Auto-register ourselves to the Resource Grid on start
        // This handles pre-placed crates in the scene
        Vector2Int pos = CasinoGridManager.Instance.WorldToGrid(transform.position);
        CasinoGridManager.Instance.RegisterResource(this, pos);
    }

    protected override void HandleTick(int tick)
    {
        // Passive. Does nothing.
    }
}