using UnityEngine;

public class ModifierBuilding : BuildingBase
{
    public enum ModificationType { SetSuit, SetRank, SetColor }

    [Header("Modifier Configuration")]
    public ModificationType operation;

    [Tooltip("Used if Operation is SetSuit")]
    public CardSuit targetSuit = CardSuit.Heart;

    [Tooltip("Used if Operation is SetRank")]
    [Range(1, 13)]
    public int targetRank = 1;

    [Tooltip("Used if Operation is SetColor")]
    public CardColor targetColor = CardColor.Red;

    [Header("Processing")]
    public float processingTime = 1.0f; // Changed to float for TickManager
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

            // 1. Apply Logic
            switch (operation)
            {
                case ModificationType.SetRank:
                    card.rank = targetRank;
                    break;
                case ModificationType.SetSuit:
                    card.suit = targetSuit;
                    // Auto-update color to match suit (Standard Deck rules)
                    if (targetSuit == CardSuit.Heart || targetSuit == CardSuit.Diamond)
                        card.color = CardColor.Red;
                    else
                        card.color = CardColor.Black;
                    break;
                case ModificationType.SetColor:
                    card.color = targetColor;
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