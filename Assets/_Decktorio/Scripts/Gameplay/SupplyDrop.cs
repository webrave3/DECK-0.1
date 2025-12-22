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

    // CHANGED: Use Awake to ensure registration happens BEFORE buildings try to find it
    private void Awake()
    {
        // Safety check for GridManager
        if (CasinoGridManager.Instance != null)
        {
            Vector2Int pos = CasinoGridManager.Instance.WorldToGrid(transform.position);
            CasinoGridManager.Instance.RegisterResource(this, pos);
        }
    }

    protected override void Start()
    {
        // Don't call base.Start() to avoid auto-registering as a Building.
        // We act as a passive resource.
    }

    protected override void OnTick(int tick) { }
}