using UnityEngine;
using System.Collections.Generic;

public class DeckDispenser : BuildingBase, IConfigurable
{
    [Header("Visual Settings")]
    public GameObject itemPrefab;
    public Transform outputAnchor;

    [Header("Dispenser Settings")]
    public float dispenseInterval = 2.0f;
    // UPDATED: Use CardInk.None for blank/standard
    public CardInk outputInk = CardInk.None;
    public CardSuit outputSuit = CardSuit.None; // Default to blank suit
    // UPDATED: Float rank
    public float outputRank = 1f; // Default to "1" (Ace low) or "0" (Blank)

    [Header("I/O Configuration")]
    [Range(0, 3)]
    public int outputDirectionOffset = 0;

    private float timer = 0f;
    public string lastStatus = "Initializing";

    protected override void OnTick(int tick)
    {
        if (internalItem == null)
        {
            timer += TickManager.Instance.tickRate;
            if (timer >= dispenseInterval)
            {
                SpawnCard();
                timer = 0f;
            }
            else lastStatus = $"Charging: {(int)((timer / dispenseInterval) * 100)}%";
        }
        else
        {
            TryPushDirectional();
        }
    }

    void SpawnCard()
    {
        if (itemPrefab == null) { lastStatus = "Error: No Prefab"; return; }

        // Create card with new Float Rank and Flag Enums
        CardData newCard = new CardData(outputRank, outputSuit, CardMaterial.Cardstock, outputInk);
        internalItem = new ItemPayload(newCard);

        Vector3 spawnPos = outputAnchor != null ? outputAnchor.position : transform.position + Vector3.up * 0.6f;
        GameObject visualObj = Instantiate(itemPrefab, spawnPos, Quaternion.identity);

        internalVisual = visualObj.GetComponent<ItemVisualizer>();
        if (internalVisual == null) internalVisual = visualObj.AddComponent<ItemVisualizer>();

        internalVisual.transform.SetParent(this.transform);
        internalVisual.SetVisuals(internalItem);
        lastStatus = "Card Ready";
    }

    private Vector2Int GetOutputGridPosition()
    {
        int currentRot = Application.isPlaying ? RotationIndex : Mathf.RoundToInt(transform.eulerAngles.y / 90f) % 4;
        int finalIndex = (currentRot + outputDirectionOffset) % 4;
        Vector2Int dir = Vector2Int.zero;
        switch (finalIndex) { case 0: dir = Vector2Int.up; break; case 1: dir = Vector2Int.right; break; case 2: dir = Vector2Int.down; break; case 3: dir = Vector2Int.left; break; }
        return GridPosition + dir;
    }

    void TryPushDirectional()
    {
        Vector2Int targetPos = GetOutputGridPosition();
        if (CasinoGridManager.Instance == null) return;
        BuildingBase target = CasinoGridManager.Instance.GetBuildingAt(targetPos);

        if (target == null) { lastStatus = "Blocked: No Target"; return; }

        if (target.CanAcceptItem(GridPosition))
        {
            target.ReceiveItem(internalItem, internalVisual);
            internalItem = null;
            internalVisual = null;
            lastStatus = "Pushed Success";
        }
        else lastStatus = "Blocked: Target Full";
    }

    public string GetInspectorTitle() => "Deck Dispenser";
    public string GetInspectorStatus() => lastStatus;

    public List<BuildingSetting> GetSettings()
    {
        return new List<BuildingSetting>(); // Phase 1: No settings needed for static dispensers
    }

    public void OnSettingChanged(string settingId, int newValue) { }
    public Dictionary<string, int> GetConfigurationState() => new Dictionary<string, int>();
    public void SetConfigurationState(Dictionary<string, int> state) { }
}