using UnityEngine;

public class ModifierBuilding : BuildingBase
{
    // Updated to match new Card Layers (Ink instead of Color)
    public enum ModificationType { SetSuit, SetRank, SetInk, SetMaterial }

    [Header("Modifier Configuration")]
    public ModificationType operation;

    [Tooltip("Used if Operation is SetSuit")]
    public CardSuit targetSuit = CardSuit.Heart;

    [Tooltip("Used if Operation is SetRank")]
    [Range(2, 14)] // Updated range for Ace High logic
    public int targetRank = 2;

    [Tooltip("Used if Operation is SetInk")]
    public CardInk targetInk = CardInk.Standard;

    [Tooltip("Used if Operation is SetMaterial")]
    public CardMaterial targetMaterial = CardMaterial.Cardstock;

    [Header("Processing")]
    public float processingTime = 1.0f;
    private float processTimer = 0f;

    protected override void OnTick(int tick)
    {
        if (internalItem != null)
        {
            processTimer += TickManager.Instance.tickRate;

            if (processTimer >= processingTime)
            {
                TryPushItem();
            }
        }
    }

    protected override void OnItemArrived()
    {
        if (internalVisual != null)
        {
            internalVisual.transform.SetParent(this.transform);
            // Move visual to center
            Vector3 targetPos = transform.position + Vector3.up * 0.5f;

            // Move over one tick duration
            float duration = TickManager.Instance.tickRate;
            internalVisual.InitializeMovement(targetPos, duration);
        }
    }

    void ModifyStack(ItemPayload item)
    {
        // Iterate through all cards in the stack and modify them
        for (int i = 0; i < item.contents.Count; i++)
        {
            CardData card = item.contents[i];

            // 1. Apply Logic based on new Layered System
            switch (operation)
            {
                case ModificationType.SetRank:
                    card.rank = targetRank;
                    break;
                case ModificationType.SetSuit:
                    card.suit = targetSuit;
                    break;
                case ModificationType.SetInk:
                    card.ink = targetInk;
                    break;
                case ModificationType.SetMaterial:
                    card.material = targetMaterial;
                    break;
            }

            // Write the modified struct back into the list
            item.contents[i] = card;
        }

        // 2. Visual Update
        if (internalVisual != null)
        {
            internalVisual.SetVisuals(item);
        }

        GameLogger.Log($"Modifier: Updated stack of {item.contents.Count}");
    }

    void TryPushItem()
    {
        Vector2Int forwardPos = GetForwardGridPosition();
        BuildingBase target = CasinoGridManager.Instance.GetBuildingAt(forwardPos);

        if (target != null && target.CanAcceptItem(GridPosition))
        {
            // Apply the change BEFORE pushing
            ModifyStack(internalItem);

            target.ReceiveItem(internalItem, internalVisual);

            internalItem = null;
            internalVisual = null;
            processTimer = 0f;
        }
    }

    public override bool CanAcceptItem(Vector2Int fromPos) => internalItem == null && incomingItem == null;
}