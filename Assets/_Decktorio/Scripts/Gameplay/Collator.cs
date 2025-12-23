using UnityEngine;
using System.Collections.Generic;

public class Collator : BuildingBase, IConfigurable
{
    [Header("Settings")]
    public int maxCapacity = 5;
    public float processTime = 0.5f;
    public bool smartEject = true; // If true, ejects as soon as a valid hand is formed (Pair, etc)

    [Header("Output")]
    public GameObject stackVisualPrefab;

    // Logic
    private List<CardData> buffer = new List<CardData>();
    private List<GameObject> tempVisuals = new List<GameObject>();
    private float timer = 0f;
    private bool isProcessing = false;

    private ItemPayload outputProduct;
    private ItemVisualizer outputVisual;

    protected override void OnTick(int tick)
    {
        // 1. Output Logic
        if (outputProduct != null)
        {
            TryPushResult();
            return;
        }

        // 2. Processing Logic
        if (isProcessing)
        {
            timer += TickManager.Instance.tickRate;
            if (timer >= processTime)
            {
                FinishCollating();
            }
            return;
        }

        // 3. Smart Logic Check
        if (buffer.Count > 0)
        {
            CheckForCompletion();
        }
    }

    public override bool CanAcceptItem(Vector2Int fromPos)
    {
        if (isProcessing || outputProduct != null) return false;
        if (buffer.Count >= maxCapacity) return false;
        if (incomingItem != null) return false;
        return true;
    }

    public override void ReceiveItem(ItemPayload item, ItemVisualizer visual)
    {
        buffer.AddRange(item.contents);

        visual.transform.SetParent(this.transform);
        float stackHeight = 0.2f + (buffer.Count * 0.1f);
        Vector3 targetPos = transform.position + new Vector3(0, stackHeight, 0);
        visual.InitializeMovement(targetPos, 0.2f);

        tempVisuals.Add(visual.gameObject);
    }

    private void CheckForCompletion()
    {
        bool ready = false;

        // Condition A: Full
        if (buffer.Count >= maxCapacity) ready = true;

        // Condition B: Smart Hand Detection
        if (smartEject && buffer.Count >= 2)
        {
            PokerHandType currentHand = PokerEvaluator.Evaluate(buffer);
            // If we have anything better than a High Card, ship it!
            if (currentHand > PokerHandType.HighCard)
            {
                ready = true;
            }
        }

        if (ready)
        {
            isProcessing = true;
            timer = 0f;
        }
    }

    private void FinishCollating()
    {
        outputProduct = new ItemPayload(buffer);

        foreach (var g in tempVisuals) Destroy(g);
        tempVisuals.Clear();
        buffer.Clear();

        if (stackVisualPrefab != null)
        {
            GameObject g = Instantiate(stackVisualPrefab, transform.position + Vector3.up * 0.5f, Quaternion.identity);
            outputVisual = g.GetComponent<ItemVisualizer>();
            if (outputVisual == null) outputVisual = g.AddComponent<ItemVisualizer>();

            outputVisual.transform.SetParent(this.transform);
            outputVisual.SetVisuals(outputProduct);
        }
        else
        {
            Debug.LogError("[Collator] Missing StackVisualPrefab!");
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

    // --- IConfigurable Implementation ---

    public string GetInspectorTitle() => "Collator";

    public string GetInspectorStatus()
    {
        string s = $"Buffer: {buffer.Count} / {maxCapacity}";
        if (isProcessing) s += " (Packing...)";
        else if (outputProduct != null) s += " (Output Ready)";
        return s;
    }

    public List<BuildingSetting> GetSettings()
    {
        return new List<BuildingSetting>
        {
            new BuildingSetting
            {
                settingId = "smartEject",
                displayName = "Smart Eject",
                options = new List<string> { "Off (Wait Full)", "On (Detect Hands)" },
                currentIndex = smartEject ? 1 : 0
            }
        };
    }

    public void OnSettingChanged(string settingId, int newValue)
    {
        if (settingId == "smartEject") smartEject = (newValue == 1);
    }

    public Dictionary<string, int> GetConfigurationState()
    {
        return new Dictionary<string, int> { { "smartEject", smartEject ? 1 : 0 } };
    }

    public void SetConfigurationState(Dictionary<string, int> state)
    {
        if (state.ContainsKey("smartEject")) smartEject = (state["smartEject"] == 1);
    }
}