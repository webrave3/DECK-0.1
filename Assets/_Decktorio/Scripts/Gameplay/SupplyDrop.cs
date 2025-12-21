using UnityEngine;

public class SupplyDrop : BuildingBase
{
    [Header("Resource Definition")]
    public string resourceName = "Cardstock";

    [Header("Output Configuration")]
    // The exact card data this node provides
    public CardSuit suit = CardSuit.None;
    public int rank = 0; // 0 = Blank/Raw
    public CardMaterial material = CardMaterial.Cardstock;
    public CardInk ink = CardInk.Standard;

    [Header("Visuals")]
    public GameObject outputPrefab; // What the Unpacker should spawn visually

    protected override void Start()
    {
        // Register as a resource node so Unpacker can find it
        Vector2Int pos = CasinoGridManager.Instance.WorldToGrid(transform.position);
        CasinoGridManager.Instance.RegisterResource(this, pos);
    }

    protected override void OnTick(int tick) { /* Passive */ }
}