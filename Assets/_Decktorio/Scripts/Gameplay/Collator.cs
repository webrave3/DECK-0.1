using UnityEngine;
using System.Collections.Generic;

public class Collator : BuildingBase
{
    [Header("Settings")]
    public int targetStackSize = 5; // e.g. Wait for 5 cards
    public float processingTime = 0.5f; // Time to "Pack" the stack

    [Header("Output")]
    public GameObject stackVisualPrefab; // Prefab for the stack visual (box/deck)

    // Internal Logic
    private List<CardData> buffer = new List<CardData>();
    private List<GameObject> garbageVisuals = new List<GameObject>(); // To destroy inputs
    private float timer = 0f;
    private bool isProcessing = false;

    // Output Buffer
    private ItemPayload outputProduct;
    private ItemVisualizer outputVisual;

    protected override void OnTick(int tick)
    {
        // 1. Output Logic: If we have a product, try to push it
        if (outputProduct != null)
        {
            TryPushResult();
            return; // Can't work while output is clogged
        }

        // 2. Processing Logic
        if (isProcessing)
        {
            timer += TickManager.Instance.tickRate;
            if (timer >= processingTime)
            {
                FinishCollating();
            }
            return;
        }

        // 3. Input Logic: If we have enough items, start working
        if (buffer.Count >= targetStackSize)
        {
            isProcessing = true;
            timer = 0f;
        }
    }

    public override bool CanAcceptItem(Vector2Int fromPos)
    {
        // Reject if busy, full, or output clogged
        if (isProcessing || outputProduct != null) return false;
        if (buffer.Count >= targetStackSize) return false;

        // Also reject if we have an item pending in mailbox
        if (incomingItem != null) return false;

        return true;
    }

    // Override ReceiveItem because we don't store inputs in 'internalItem'
    // We strip them and store them in 'buffer'
    public override void ReceiveItem(ItemPayload item, ItemVisualizer visual)
    {
        // Add data to buffer
        buffer.AddRange(item.contents);

        // Visual handling: Stack them on the machine
        visual.transform.SetParent(this.transform);

        // Visual stack height
        float stackHeight = 0.5f + (buffer.Count * 0.1f);
        Vector3 targetPos = transform.position + new Vector3(0, stackHeight, 0);

        // Move quickly to stack position
        visual.InitializeMovement(targetPos, 0.2f);

        // Track visual to destroy later
        garbageVisuals.Add(visual.gameObject);
    }

    private void FinishCollating()
    {
        // Create new ItemPayload containing ALL buffered cards
        outputProduct = new ItemPayload(buffer);

        // Clear inputs
        buffer.Clear();
        foreach (var g in garbageVisuals) Destroy(g);
        garbageVisuals.Clear();

        // Instantiate Output Visual
        if (stackVisualPrefab != null)
        {
            GameObject g = Instantiate(stackVisualPrefab, transform.position + Vector3.up * 0.5f, Quaternion.identity);
            outputVisual = g.GetComponent<ItemVisualizer>();
            if (outputVisual == null) outputVisual = g.AddComponent<ItemVisualizer>();

            outputVisual.transform.SetParent(this.transform);
            outputVisual.SetVisuals(outputProduct);
        }

        isProcessing = false;
    }

    private void TryPushResult()
    {
        BuildingBase target = CasinoGridManager.Instance.GetBuildingAt(GetForwardGridPosition());

        if (target != null && target.CanAcceptItem(GridPosition))
        {
            target.ReceiveItem(outputProduct, outputVisual);
            outputProduct = null;
            outputVisual = null;
        }
    }
}