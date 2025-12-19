using UnityEngine;
using System.Collections.Generic;

public class Assembler : BuildingBase
{
    [Header("Settings")]
    public int handSize = 5;
    public float craftingTime = 2.0f;

    [Header("Output")]
    public GameObject moneyBagPrefab; // Visual for the completed hand

    // Internal State
    private List<CardPayload> buffer = new List<CardPayload>();
    private List<ItemVisualizer> visualBuffer = new List<ItemVisualizer>();
    private float craftTimer = 0f;
    private bool isCrafting = false;

    // Output State
    private CardPayload resultProduct;
    private ItemVisualizer resultVisual;

    protected override void OnTick(int tick)
    {
        // 1. Try to push existing output (if we finished crafting)
        if (resultProduct != null)
        {
            TryPushResult();
            return; // Busy pushing, can't craft
        }

        // 2. Crafting Process
        if (isCrafting)
        {
            craftTimer += TickManager.Instance.tickRate; // Approximate time
            if (craftTimer >= craftingTime)
            {
                CompleteCraft();
            }
            return; // Busy crafting
        }

        // 3. Logic: Should we start crafting?
        if (buffer.Count >= handSize)
        {
            EvaluateAndStart();
        }
    }

    public override bool CanAcceptItem(Vector2Int fromPos)
    {
        // Only accept if we have room and aren't busy
        if (isCrafting || resultProduct != null) return false;
        if (buffer.Count >= handSize) return false;

        return true;
    }

    public override void ReceiveItem(CardPayload item, ItemVisualizer visual)
    {
        // Add to internal storage
        buffer.Add(item);
        visualBuffer.Add(visual);

        // Visuals: Stack them on the table
        visual.transform.SetParent(this.transform);

        // Simple visual arrangement: Spread them out slightly
        float offset = (buffer.Count - 1) * 0.2f;
        Vector3 targetPos = transform.position + Vector3.up * 0.5f + Vector3.right * (offset - 0.4f);

        // Move over one tick duration
        float duration = TickManager.Instance.tickRate;
        visual.InitializeMovement(targetPos, duration);
    }

    private void EvaluateAndStart()
    {
        PokerHandType type = PokerEvaluator.Evaluate(buffer);

        if (type > PokerHandType.HighCard) // Require at least a Pair?
        {
            GameLogger.Log($"[Assembler] Valid Hand: {type}");
            isCrafting = true;
            craftTimer = 0f;
        }
        else
        {
            // BAD HAND LOGIC
            GameLogger.Log("[Assembler] Garbage Hand! Discarding...");
            ClearBuffer(); // Auto-trash for now to keep game flowing
        }
    }

    private void CompleteCraft()
    {
        // Calculate value
        int value = PokerEvaluator.GetHandValue(PokerEvaluator.Evaluate(buffer));

        // Create the "Output" item
        // We reuse the CardPayload class but maybe treat Rank as Value?
        resultProduct = new CardPayload(value, CardSuit.None, 1);
        GameLogger.Log($"[Assembler] Crafted value: {value}");

        // Clear inputs
        ClearBuffer();

        // Spawn visual for output (Money Bag)
        if (moneyBagPrefab != null)
        {
            GameObject g = Instantiate(moneyBagPrefab, transform.position + Vector3.up * 0.5f, Quaternion.identity);
            resultVisual = g.GetComponent<ItemVisualizer>();
            if (resultVisual == null) resultVisual = g.AddComponent<ItemVisualizer>();
            resultVisual.transform.SetParent(transform);
        }

        isCrafting = false;
    }

    private void TryPushResult()
    {
        // Push result to forward belt
        Vector2Int forward = GetForwardGridPosition();
        BuildingBase target = CasinoGridManager.Instance.GetBuildingAt(forward);

        if (target != null && target.CanAcceptItem(GridPosition))
        {
            target.ReceiveItem(resultProduct, resultVisual);
            resultProduct = null;
            resultVisual = null;
        }
    }

    private void ClearBuffer()
    {
        foreach (var v in visualBuffer)
        {
            if (v != null) Destroy(v.gameObject);
        }
        buffer.Clear();
        visualBuffer.Clear();
    }
}