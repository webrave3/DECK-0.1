using UnityEngine;

[CreateAssetMenu(fileName = "New Building", menuName = "Decktorio/Building Definition")]
public class BuildingDefinition : ScriptableObject
{
    public string buildingName;
    public GameObject prefab;
    public int width = 1;
    public int height = 1;
    public int cost = 10;

    [TextArea] public string description;
}