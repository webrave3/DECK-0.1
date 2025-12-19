using UnityEngine;

public class ModifierBuilding : BuildingBase
{
    public enum ModificationType { SetSuit, SetRank, SetColor }

    [Header("Modifier Configuration")]
    public ModificationType operation;

    // We use specific types so you see dropdowns in the Inspector
    [Tooltip("Used if Operation is SetSuit")]
    public CardSuit targetSuit = CardSuit.Heart;

    [Tooltip("Used if Operation is SetRank")]
    [Range(1, 13)]
    public int targetRank = 1;

    [Tooltip("Used if Operation is SetColor")]
    public CardColor targetColor = CardColor.Red;

    [Header("Processing")]
    public int processingTime = 5;
    private int processTimer = 0;

    protected override void OnTick(int tick)
    {
        if (internalCard != null)
        {
            processTimer++;
            // Optional: Log progress
            // if (showDebugLogs && processTimer % 5 == 0) Debug.Log($"Processing... {processTimer}");

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
            internalVisual.InitializeMovement(internalVisual.transform.position, targetPos);
        }
    }

    void ModifyCard(CardPayload card)
    {
        // 1. Logic Update
        switch (operation)
        {
            case ModificationType.SetRank:
                card.rank = targetRank;
                break;
            case ModificationType.SetSuit:
                card.suit = targetSuit;
                break;
            case ModificationType.SetColor:
                card.color = targetColor;
                break;
        }

        // 2. Visual Update (Refresh the look of the card)
        if (internalVisual != null)
        {
            // If you added the SetVisuals method to ItemVisualizer from previous steps:
            internalVisual.SetVisuals(card);
        }
    }

    void TryPushItem()
    {
        Vector2Int forwardPos = GetForwardGridPosition();
        BuildingBase target = CasinoGridManager.Instance.GetBuildingAt(forwardPos);

        if (target != null && target.CanAcceptItem(GridPosition))
        {
            // Apply the change BEFORE pushing
            ModifyCard(internalCard);

            target.ReceiveItem(internalCard, internalVisual);

            internalCard = null;
            internalVisual = null;
            processTimer = 0;
        }
    }

    public override bool CanAcceptItem(Vector2Int fromPos) => internalCard == null && incomingCard == null;
}