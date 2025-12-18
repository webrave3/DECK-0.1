using UnityEngine;

public class ModifierBuilding : BuildingBase
{
    public enum ModificationType { AddSuit, AddRank, AddColor }

    [Header("Modifier Settings")]
    public ModificationType operation;
    public int valueToAdd = 1;
    public int processingTime = 5;

    private int processTimer = 0;

    protected override void OnTick(int tick)
    {
        if (internalCard != null)
        {
            processTimer++;
            if (showDebugLogs && processTimer % 5 == 0) Debug.Log($"[{name}] Processing... {processTimer}/{processingTime}");

            if (processTimer >= processingTime)
            {
                TryPushItem();
            }
        }
    }

    // THIS WAS MISSING BEFORE!
    // Without this, the visual card stayed on the previous belt while the data moved here.
    protected override void OnItemArrived()
    {
        if (internalVisual != null)
        {
            internalVisual.transform.SetParent(this.transform);

            // Move visual to center of machine (slightly higher)
            Vector3 targetPos = transform.position + Vector3.up * 0.5f;
            internalVisual.InitializeMovement(internalVisual.transform.position, targetPos);

            if (showDebugLogs) Debug.Log($"[{name}] Item Arrived visually.");
        }
    }

    void ModifyCard(CardPayload card)
    {
        // 1. Logic Update
        switch (operation)
        {
            case ModificationType.AddRank:
                card.rank = valueToAdd;
                break;
            case ModificationType.AddSuit:
                card.suit = (CardSuit)valueToAdd;
                break;
        }

        // 2. Visual Update
        if (internalVisual != null)
        {
            // Get the Renderer of the card
            Renderer r = internalVisual.GetComponent<Renderer>();
            if (r != null)
            {
                // Simple Color Change for MVP
                if (card.suit == CardSuit.Heart) r.material.color = Color.red;
                else if (card.suit == CardSuit.Spade) r.material.color = Color.black;
                else if (card.suit == CardSuit.Diamond) r.material.color = Color.blue; // Just for distinction
                else if (card.suit == CardSuit.Club) r.material.color = Color.green;
            }
        }
    }

    void TryPushItem()
    {
        Vector2Int forwardPos = GetForwardGridPosition();
        BuildingBase target = CasinoGridManager.Instance.GetBuildingAt(forwardPos);

        if (target != null && target.CanAcceptItem(GridPosition))
        {
            ModifyCard(internalCard);

            target.ReceiveItem(internalCard, internalVisual);

            internalCard = null;
            internalVisual = null;
            processTimer = 0;

            if (showDebugLogs) Debug.Log($"[{name}] Process Complete. Pushed item.");
        }
        else
        {
            if (showDebugLogs) Debug.Log($"[{name}] Output blocked.");
        }
    }

    // Only accept if empty
    public override bool CanAcceptItem(Vector2Int fromPos) => internalCard == null && incomingCard == null;
}