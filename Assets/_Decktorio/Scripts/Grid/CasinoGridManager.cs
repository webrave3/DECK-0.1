using UnityEngine;
using System.Collections.Generic;

public class CasinoGridManager : MonoBehaviour
{
    public static CasinoGridManager Instance { get; private set; }

    [Header("Map Settings")]
    public int mapWidth = 20;
    public int mapHeight = 20;
    public float cellSize = 1f;

    [Header("Generation Settings")]
    public GameObject dispenserPrefab;
    public ResourceNode resourceNodePrefab;
    [Range(0f, 1f)] public float resourceDensity = 0.1f;

    [Header("Alignment")]
    public Transform floorPlane;
    private Vector3 gridOrigin;

    [Header("Debug")]
    public bool sandboxMode = true;

    private BuildingBase[,] buildingGrid;
    private ResourceNode[,] resourceGrid;

    // Re-added for BuildingSystem compatibility
    private bool[,] unlockedPlots;

    private void Awake()
    {
        if (Instance != null && Instance != this) Destroy(this);
        else Instance = this;

        RecalculateOrigin();
        InitializeGridData();
    }

    private void Start()
    {
        GenerateWorld();
    }

    public void RecalculateOrigin()
    {
        if (floorPlane != null)
        {
            Renderer r = floorPlane.GetComponent<Renderer>();
            if (r != null)
            {
                gridOrigin = r.bounds.min;
                gridOrigin.y = 0;
            }
            else gridOrigin = floorPlane.position;
        }
        else gridOrigin = Vector3.zero;
    }

    private void InitializeGridData()
    {
        if (buildingGrid == null) buildingGrid = new BuildingBase[mapWidth, mapHeight];
        if (resourceGrid == null) resourceGrid = new ResourceNode[mapWidth, mapHeight];

        // Initialize and unlock all plots for Phase 1/2
        if (unlockedPlots == null || unlockedPlots.GetLength(0) != mapWidth)
        {
            unlockedPlots = new bool[mapWidth, mapHeight];
            UnlockArea(0, 0, mapWidth, mapHeight);
        }
    }

    public void UnlockArea(int startX, int startY, int width, int height)
    {
        for (int x = startX; x < startX + width; x++)
            for (int y = startY; y < startY + height; y++)
                if (IsValidCoord(x, y)) unlockedPlots[x, y] = true;
    }

    public void GenerateWorld()
    {
        ClearWorld();
        if (dispenserPrefab != null)
        {
            for (int y = 2; y < mapHeight - 2; y += 3) SpawnDispenser(0, y);
        }
        if (resourceNodePrefab != null)
        {
            for (int x = 2; x < mapWidth; x++)
            {
                for (int y = 0; y < mapHeight; y++)
                {
                    if (Random.value < resourceDensity) SpawnRandomResource(x, y);
                }
            }
        }
    }

    private void SpawnDispenser(int x, int y)
    {
        GameObject obj = Instantiate(dispenserPrefab, transform);
        BuildingBase building = obj.GetComponent<BuildingBase>();
        if (building != null)
        {
            PlaceBuilding(building, new Vector2Int(x, y));
            building.SetRotation(1);
        }
    }

    private void SpawnRandomResource(int x, int y)
    {
        GameObject obj = Instantiate(resourceNodePrefab.gameObject, transform);
        ResourceNode node = obj.GetComponent<ResourceNode>();

        bool isInk = Random.value > 0.5f;
        node.type = isInk ? ResourceType.InkSource : ResourceType.SuitMold;

        if (isInk)
        {
            node.inkGiven = (Random.value > 0.5f) ? CardInk.Red : CardInk.Black;
            node.nodeColor = (node.inkGiven == CardInk.Red) ? Color.red : Color.gray;
        }
        else
        {
            int suitIndex = Random.Range(0, 4);
            switch (suitIndex)
            {
                case 0: node.suitGiven = CardSuit.Heart; node.nodeColor = new Color(1, 0.5f, 0.5f); break;
                case 1: node.suitGiven = CardSuit.Diamond; node.nodeColor = new Color(1, 0.2f, 0.2f); break;
                case 2: node.suitGiven = CardSuit.Club; node.nodeColor = new Color(0.2f, 0.2f, 0.2f); break;
                case 3: node.suitGiven = CardSuit.Spade; node.nodeColor = Color.black; break;
            }
        }
        resourceGrid[x, y] = node;
        node.Setup(new Vector2Int(x, y));
    }

    private void ClearWorld()
    {
        foreach (Transform child in transform) Destroy(child.gameObject);
        buildingGrid = new BuildingBase[mapWidth, mapHeight];
        resourceGrid = new ResourceNode[mapWidth, mapHeight];
    }

    // --- CHECKS ---

    public bool IsValidCoord(int x, int y) => x >= 0 && y >= 0 && x < mapWidth && y < mapHeight;
    public bool IsOccupied(Vector2Int pos) => IsValidCoord(pos.x, pos.y) && buildingGrid[pos.x, pos.y] != null;

    // Re-added IsUnlocked
    public bool IsUnlocked(Vector2Int pos)
    {
        if (!IsValidCoord(pos.x, pos.y)) return false;
        return sandboxMode || unlockedPlots[pos.x, pos.y];
    }

    public bool IsBuildable(Vector2Int pos) => IsUnlocked(pos) && !IsOccupied(pos);

    // --- MANAGEMENT ---

    public void PlaceBuilding(BuildingBase building, Vector2Int pos)
    {
        if (!IsValidCoord(pos.x, pos.y)) return;
        if (buildingGrid[pos.x, pos.y] != null) RemoveBuilding(pos);
        buildingGrid[pos.x, pos.y] = building;
        building.Setup(pos);
    }

    public void RegisterBuilding(Vector2Int pos, BuildingBase building) => PlaceBuilding(building, pos);

    public BuildingBase GetBuildingAt(Vector2Int pos)
    {
        if (!IsValidCoord(pos.x, pos.y)) return null;
        return buildingGrid[pos.x, pos.y];
    }

    public void RemoveBuilding(Vector2Int pos)
    {
        if (IsValidCoord(pos.x, pos.y) && buildingGrid[pos.x, pos.y] != null)
        {
            BuildingBase b = buildingGrid[pos.x, pos.y];
            buildingGrid[pos.x, pos.y] = null;
            if (b != null) Destroy(b.gameObject);
        }
    }

    public void RegisterResource(ResourceNode node, Vector2Int pos)
    {
        if (IsValidCoord(pos.x, pos.y))
        {
            resourceGrid[pos.x, pos.y] = node;
            node.Setup(pos);
        }
    }

    public ResourceNode GetResourceAt(Vector2Int pos)
    {
        if (!IsValidCoord(pos.x, pos.y)) return null;
        return resourceGrid[pos.x, pos.y];
    }

    // --- CONVERSION ---

    public Vector2Int WorldToGrid(Vector3 worldPos)
    {
        Vector3 localPos = worldPos - gridOrigin;
        return new Vector2Int(Mathf.FloorToInt(localPos.x / cellSize), Mathf.FloorToInt(localPos.z / cellSize));
    }

    public Vector3 GridToWorld(Vector2Int gridPos)
    {
        float x = gridOrigin.x + (gridPos.x * cellSize) + (cellSize / 2f);
        float z = gridOrigin.z + (gridPos.y * cellSize) + (cellSize / 2f);
        return new Vector3(x, 0, z);
    }
}