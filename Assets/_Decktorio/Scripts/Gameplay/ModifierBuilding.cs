using UnityEngine;

public class ModifierBuilding : BuildingBase
{
    public enum ModificationType { AddSuit, AddRank, AddColor }

    [Header("Settings")]
    public ModificationType operation;
    public int valueToAdd = 1; // Rank 1-13 or Suit Index
    public int processingTime = 5; // Ticks to process

    private int processTimer = 0;

    protected override void OnTick(int tick)
    {
        // 1. If we have an item, process it
        if (internalCard != null)
        {
            processTimer++;
            if (processTimer >= processingTime)
            {
                TryPushItem();
            }
        }
    }

    void ModifyCard(CardPayload card)
    {
        switch (operation)
        {
            case ModificationType.AddRank:
                card.rank = valueToAdd;
                // Update Visuals here (e.g., change texture to "2")
                break;
            case ModificationType.AddSuit:
                card.suit = (CardSuit)valueToAdd;
                // Update Visuals here (e.g., change color/icon)
                break;
        }
    }

    void TryPushItem()
    {
        Vector2Int forwardPos = GetForwardGridPosition();
        BuildingBase target = CasinoGridManager.Instance.GetBuildingAt(forwardPos);

        if (target != null && target.CanAcceptItem(GridPosition))
        {
            // Apply Modification right before leaving
            ModifyCard(internalCard);

            target.ReceiveItem(internalCard, internalVisual);
            internalCard = null;
            internalVisual = null;
            processTimer = 0;
        }
    }

    // Only accept if empty
    public override bool CanAcceptItem(Vector2Int fromPos) => internalCard == null && incomingCard == null;
}