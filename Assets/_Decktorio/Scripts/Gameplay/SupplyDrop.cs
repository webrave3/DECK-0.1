using UnityEngine;

public class SupplyDrop : BuildingBase
{
    [Header("Resource Settings")]
    public GameObject itemPrefab; // The visual card to spawn
    public CardSuit suit = CardSuit.Heart;
    public int rank = 1;

    protected override void Start()
    {
        // We override Start to register ourselves as a Resource, 
        // but we DON'T call base.Start() because we don't want to receive Ticks.
        // This is an optimization (passive buildings shouldn't wake up every frame).

        Vector2Int pos = CasinoGridManager.Instance.WorldToGrid(transform.position);
        CasinoGridManager.Instance.RegisterResource(this, pos);
    }

    // We must implement this to satisfy the compiler, even if we don't use it.
    protected override void OnTick(int tick)
    {
        // Passive. Does nothing.
    }
}