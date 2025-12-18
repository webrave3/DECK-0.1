using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class SimpleHotbar : MonoBehaviour
{
    [Header("UI References")]
    public GameObject buttonPrefab;
    public Transform buttonContainer;

    [Header("Data")]
    public List<BuildingDefinition> availableBuildings;

    private void Start()
    {
        InitializeButtons();
    }

    public void InitializeButtons()
    {
        foreach (Transform child in buttonContainer)
        {
            Destroy(child.gameObject);
        }

        foreach (var def in availableBuildings)
        {
            if (def == null) continue;

            GameObject btnObj = Instantiate(buttonPrefab, buttonContainer);
            Button btn = btnObj.GetComponent<Button>();
            TextMeshProUGUI txt = btnObj.GetComponentInChildren<TextMeshProUGUI>();

            if (txt) txt.text = def.name;

            btn.onClick.AddListener(() =>
            {
                if (BuildingSystem.Instance == null)
                {
                    Debug.LogError("CRITICAL: BuildingSystem is missing from the scene!");
                    return;
                }
                BuildingSystem.Instance.SelectBuilding(def);
            });
        }
    }
}