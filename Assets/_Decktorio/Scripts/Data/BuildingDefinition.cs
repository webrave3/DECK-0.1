using UnityEngine;

[CreateAssetMenu(menuName = "Decktorio/Building Definition")]
public class BuildingDefinition : ScriptableObject
{
    [Header("Identity")]
    public string id;
    public string displayName;

    [Header("Visuals")]
    public GameObject prefab;      // The actual functional object
    public GameObject visualGhost; // Optional: A semi-transparent material version
    public Sprite icon;            // For the Hotbar UI

    [Header("Economy")]
    public int baseDebtCost = 10;  // How much Debt is generated to place this

    [Header("Grid Logic")]
    public Vector2Int size = Vector2Int.one; // 1x1, 2x2, etc.
}