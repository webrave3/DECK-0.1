using UnityEngine;
using System.Collections.Generic;

public class UniversalProcessor : BuildingBase
{
    [Header("Machine Configuration")]
    public RecipeSO activeRecipe;

    [Header("Visuals")]
    public Transform animationPart;
    public float animationSpeed = 360f;

    private float processTimer = 0f;
    private bool isProcessing = false;

    protected override void OnTick(int tick)
    {
        // 1. Check Input
        if (internalItem != null && !isProcessing)
        {
            if (activeRecipe != null && internalItem.contents.Count > 0)
            {
                // Check if the FIRST card matches requirements
                if (activeRecipe.IsMatch(internalItem.contents[0]))
                {
                    isProcessing = true;
                    processTimer = 0f;
                    // Debug Log
                    GameLogger.Log($"Processor: Started job on {internalItem.GetDebugLabel()}");
                }
            }
            else
            {
                // No recipe or empty? Pass it through.
                TryPushItem();
            }
        }

        // 2. Process Logic
        if (isProcessing)
        {
            processTimer += TickManager.Instance.tickRate;

            if (animationPart != null)
                animationPart.Rotate(Vector3.up * animationSpeed * TickManager.Instance.tickRate);

            if (processTimer >= activeRecipe.processingTime)
            {
                CompleteProcessing();
            }
        }
    }

    void CompleteProcessing()
    {
        if (internalItem == null || activeRecipe == null) return;

        // CRITICAL: Structs are value types. 
        // We must pull the struct out, modify it, and put it back in.
        for (int i = 0; i < internalItem.contents.Count; i++)
        {
            CardData original = internalItem.contents[i];

            // Log before
            // GameLogger.Log($"Before: {original.rank} of {original.suit}");

            // Process creates a NEW struct with changes
            CardData modified = activeRecipe.Process(original);

            // Log after
            // GameLogger.Log($"After: {modified.rank} of {modified.suit}");

            // Assign back to list (This updates the memory in the list)
            internalItem.contents[i] = modified;
        }

        // Force Visual Update immediately
        if (internalVisual != null)
        {
            internalVisual.SetVisuals(internalItem);
        }

        isProcessing = false;
        TryPushItem();
    }

    void TryPushItem()
    {
        Vector2Int forwardPos = GetForwardGridPosition();
        BuildingBase target = CasinoGridManager.Instance.GetBuildingAt(forwardPos);

        if (target != null && target.CanAcceptItem(GridPosition))
        {
            target.ReceiveItem(internalItem, internalVisual);
            internalItem = null;
            internalVisual = null;
        }
    }
}