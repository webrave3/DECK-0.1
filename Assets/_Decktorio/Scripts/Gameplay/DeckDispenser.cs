using UnityEngine;
using System.Collections.Generic; // Required for List

public class DeckDispenser : BuildingBase
{
    [Header("Visual Settings")]
    public GameObject itemPrefab;
    public Transform outputAnchor;

    [Header("Dispenser Settings")]
    public float dispenseInterval = 2.0f;
    public CardSuit outputSuit = CardSuit.Heart;
    public int outputRank = 2;

    private float timer = 0f;

    // Status for Tooltip
    public string lastStatus = "Initializing";

    protected override void Start()
    {
        base.Start();
    }

    protected override void OnTick(int tick)
    {
        // 1. Spawning Logic
        if (internalItem == null)
        {
            timer += TickManager.Instance.tickRate;
            if (timer >= dispenseInterval)
            {
                SpawnCard();
                timer = 0f;
            }
            else
            {
                lastStatus = $"Charging: {(int)((timer / dispenseInterval) * 100)}%";
            }
        }
        // 2. Pushing Logic (Now Omni-Directional)
        else
        {
            TryPushSmart();
        }
    }

    void SpawnCard()
    {
        CardData newCard = new CardData(outputRank, outputSuit, CardMaterial.Cardstock, CardInk.Standard);
        internalItem = new ItemPayload(newCard);

        if (itemPrefab != null)
        {
            Vector3 spawnPos = outputAnchor != null ? outputAnchor.position : transform.position + Vector3.up * 0.6f;
            GameObject visualObj = Instantiate(itemPrefab, spawnPos, Quaternion.identity);

            internalVisual = visualObj.GetComponent<ItemVisualizer>();
            if (internalVisual == null) internalVisual = visualObj.AddComponent<ItemVisualizer>();

            internalVisual.transform.SetParent(this.transform);
            internalVisual.SetVisuals(internalItem);
        }

        lastStatus = "Card Ready";
        GameLogger.Log($"Dispenser: Created {outputRank} of {outputSuit}");
    }

    // Tries all 4 directions instead of just one
    void TryPushSmart()
    {
        // Define all 4 cardinal directions
        Vector2Int[] directions = new Vector2Int[]
        {
            new Vector2Int(0, 1),  // North
            new Vector2Int(1, 0),  // East
            new Vector2Int(0, -1), // South
            new Vector2Int(-1, 0)  // West
        };

        bool foundTarget = false;

        foreach (Vector2Int dir in directions)
        {
            Vector2Int targetPos = GridPosition + dir;
            BuildingBase target = CasinoGridManager.Instance.GetBuildingAt(targetPos);

            // VISUAL DEBUG: Draw a line to where we are checking
            // Red = Checking/Blocked, Green = Success (drawn inside block)
            Vector3 worldDir = new Vector3(dir.x, 0, dir.y);
            Debug.DrawRay(transform.position + Vector3.up, worldDir, Color.red, TickManager.Instance.tickRate);

            if (target != null)
            {
                if (target.CanAcceptItem(GridPosition))
                {
                    // SUCCESS!
                    target.ReceiveItem(internalItem, internalVisual);

                    internalItem = null;
                    internalVisual = null;
                    lastStatus = "Pushed";
                    foundTarget = true;

                    // Draw Green success line
                    Debug.DrawRay(transform.position + Vector3.up, worldDir, Color.green, 0.5f);
                    break; // Stop looking, we pushed it.
                }
                else
                {
                    // Found a building, but it refused us (probably full or wrong direction)
                    lastStatus = "Target Refused/Full";
                }
            }
        }

        if (!foundTarget && lastStatus != "Target Refused/Full")
        {
            lastStatus = "No Connection";

            // Visual Flair: Rotate the stuck card
            if (internalVisual != null)
                internalVisual.transform.Rotate(Vector3.up * 180 * TickManager.Instance.tickRate);
        }
    }
}