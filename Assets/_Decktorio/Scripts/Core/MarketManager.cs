using System;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

[Serializable]
public class HandMarketData
{
    public PokerHandType handType;
    public string label;

    [Header("Market State")]
    public float currentMultiplier = 1.0f;
    public float timeSinceLastSale = 0f; // NEW: Tracks scarcity

    [Header("Settings")]
    public float minMultiplier = 0.1f;
    public float maxMultiplier = 2.0f;    // Expanded range to 200%
    public float crashPerSale = 0.05f;
    public float baseRecoveryPerSec = 0.01f;

    [Tooltip("How much faster price recovers if not sold for a while.")]
    public float scarcityScaling = 0.1f; // +10% recovery speed per second of waiting
}

public class MarketManager : MonoBehaviour
{
    public static MarketManager Instance { get; private set; }

    [Header("Global Settings")]
    public bool autoPopulateList = true;
    public float volatility = 0.005f; // Random +/- 0.5% fluctuation
    public float volatilityInterval = 2.0f; // Noise updates every 2s

    [SerializeField]
    private List<HandMarketData> marketDataList = new List<HandMarketData>();
    private Dictionary<PokerHandType, HandMarketData> marketLookup = new Dictionary<PokerHandType, HandMarketData>();

    private float _timerVolatility;

    public event Action OnMarketUpdated;

    private void Awake()
    {
        if (Instance != null && Instance != this) Destroy(this);
        else Instance = this;
    }

    private void Start()
    {
        InitializeMarket();
    }

    private void InitializeMarket()
    {
        foreach (var data in marketDataList)
        {
            if (!marketLookup.ContainsKey(data.handType))
                marketLookup.Add(data.handType, data);
        }

        if (autoPopulateList)
        {
            foreach (PokerHandType type in Enum.GetValues(typeof(PokerHandType)))
            {
                if (!marketLookup.ContainsKey(type))
                {
                    HandMarketData newData = new HandMarketData
                    {
                        handType = type,
                        label = type.ToString(),
                        currentMultiplier = 1.0f,
                        minMultiplier = 0.1f,
                        maxMultiplier = 1.5f,
                        crashPerSale = 0.05f,
                        baseRecoveryPerSec = 0.02f,
                        scarcityScaling = 0.05f // Default scarcity bonus
                    };

                    // Tuning: High Tier hands crash hard but reward scarcity
                    if (type >= PokerHandType.FullHouse)
                    {
                        newData.crashPerSale = 0.15f;
                        newData.baseRecoveryPerSec = 0.01f;
                        newData.maxMultiplier = 3.0f; // Can go up to 300% if starved!
                        newData.scarcityScaling = 0.2f; // Recovers fast if you wait
                    }

                    marketDataList.Add(newData);
                    marketLookup.Add(type, newData);
                }
            }
        }
    }

    private void Update()
    {
        float dt = Time.deltaTime;

        // 1. Recover Markets
        RecoverMarket(dt);

        // 2. Apply Volatility (Noise)
        _timerVolatility -= dt;
        if (_timerVolatility <= 0)
        {
            ApplyVolatility();
            _timerVolatility = volatilityInterval;
        }
    }

    private void RecoverMarket(float delta)
    {
        bool changed = false;

        foreach (var data in marketDataList)
        {
            // Increase time since last sale
            data.timeSinceLastSale += delta;

            // Calculate Dynamic Recovery Rate
            // Rate = Base + (TimeWaited * ScarcityFactor * Base)
            // Example: Wait 10s, Rate becomes 1.5x or 2x faster.
            float dynamicRate = data.baseRecoveryPerSec * (1f + (data.timeSinceLastSale * data.scarcityScaling));

            if (data.currentMultiplier < data.maxMultiplier)
            {
                data.currentMultiplier += dynamicRate * delta;

                if (data.currentMultiplier > data.maxMultiplier)
                    data.currentMultiplier = data.maxMultiplier;

                changed = true;
            }
        }

        if (changed) OnMarketUpdated?.Invoke();
    }

    private void ApplyVolatility()
    {
        // Randomly nudge prices up or down slightly
        foreach (var data in marketDataList)
        {
            float noise = Random.Range(-volatility, volatility);
            data.currentMultiplier += noise;

            // Safety Clamp
            if (data.currentMultiplier < data.minMultiplier) data.currentMultiplier = data.minMultiplier;
        }
        OnMarketUpdated?.Invoke();
    }

    // --- API ---

    public float GetMultiplier(PokerHandType type)
    {
        if (marketLookup.TryGetValue(type, out HandMarketData data))
        {
            return data.currentMultiplier;
        }
        return 1.0f;
    }

    public void RegisterSale(PokerHandType type)
    {
        if (marketLookup.TryGetValue(type, out HandMarketData data))
        {
            // Crash the price
            data.currentMultiplier -= data.crashPerSale;
            if (data.currentMultiplier < data.minMultiplier)
                data.currentMultiplier = data.minMultiplier;

            // Reset Scarcity Timer
            data.timeSinceLastSale = 0f;

            OnMarketUpdated?.Invoke();
        }
    }
}