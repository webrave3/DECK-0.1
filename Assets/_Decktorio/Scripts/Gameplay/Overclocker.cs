using UnityEngine;

public class Overclocker : BuildingBase
{
    [Header("Settings")]
    public float processingTime = 0.5f;
    [Range(0, 100)] public int explosionChance = 10; // 10% chance to fail

    private float processTimer = 0f;

    protected override void OnTick(int tick)
    {
        if (internalItem != null)
        {
            processTimer += TickManager.Instance.tickRate;
            if (processTimer >= processingTime)
            {
                Overclock();
                processTimer = 0f;
            }
        }
    }

    private void Overclock()
    {
        // RISK CHECK
        if (Random.Range(0, 100) < explosionChance)
        {
            // BOOM!
            // Destroy item and visual
            Destroy(internalVisual.gameObject);
            internalItem = null;
            internalVisual = null;

            // Optional: Play particle effect here
            Debug.Log("Overclocker EXPLODED a card!");
            return;
        }

        // SUCCESS
        for (int i = 0; i < internalItem.contents.Count; i++)
        {
            CardData c = internalItem.contents[i];

            c.rank *= 2.0f; // Double Value
            c.heat += 50.0f; // Massive Heat spike

            // Mark as Neon if not already (Visual cue of overclocking)
            c.ink |= CardInk.Neon;

            internalItem.contents[i] = c;
        }

        if (internalVisual != null) internalVisual.SetVisuals(internalItem);
        TryPushItem();
    }

    private void TryPushItem()
    {
        Vector2Int fwd = GetForwardGridPosition();
        BuildingBase target = CasinoGridManager.Instance.GetBuildingAt(fwd);

        if (target != null && target.CanAcceptItem(GridPosition))
        {
            target.ReceiveItem(internalItem, internalVisual);
            internalItem = null;
            internalVisual = null;
        }
    }
}