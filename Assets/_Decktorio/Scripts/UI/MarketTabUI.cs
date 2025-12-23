using System;
using System.Collections.Generic;
using UnityEngine;

public class MarketTabUI : MonoBehaviour
{
    [Header("Configuration")]
    public GameObject rowPrefab;
    public Transform listContainer; // The "Content" object of the ScrollView

    private List<MarketRowUI> _spawnedRows = new List<MarketRowUI>();
    private bool _initialized = false;

    private void OnEnable()
    {
        if (!_initialized) InitList();
        RefreshList();
    }

    private void Update()
    {
        // Live update every frame
        RefreshList();
    }

    private void InitList()
    {
        // 1. Clean up placeholder children
        if (listContainer != null)
        {
            foreach (Transform child in listContainer)
            {
                Destroy(child.gameObject);
            }
        }
        _spawnedRows.Clear();

        // 2. Loop through all PokerHandTypes
        foreach (PokerHandType type in Enum.GetValues(typeof(PokerHandType)))
        {
            // Skip types we don't want to sell
            if (type == PokerHandType.IllegalTech) continue;

            // Optional: Skip HighCard
            // if (type == PokerHandType.HighCard) continue;

            if (rowPrefab != null && listContainer != null)
            {
                GameObject obj = Instantiate(rowPrefab, listContainer);
                MarketRowUI row = obj.GetComponent<MarketRowUI>();
                if (row != null)
                {
                    row.Setup(type);
                    _spawnedRows.Add(row);
                }
            }
        }

        _initialized = true;
    }

    private void RefreshList()
    {
        foreach (var row in _spawnedRows)
        {
            if (row != null) row.UpdateRow();
        }
    }
}