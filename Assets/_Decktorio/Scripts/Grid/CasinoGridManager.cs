using UnityEngine;
using System.Collections.Generic;

public class CasinoGridManager : MonoBehaviour
{
    public static CasinoGridManager Instance { get; private set; }

    [Header("Map Settings")]
    public int mapWidth = 100;
    public int mapHeight = 100;
    public float cellSize = 1f;

    [Header("Alignment")]
    [Tooltip("Drag your Ground/Floor Plane here. The grid will automatically align to its bottom-left corner.")]
    public Transform floorPlane;
    private Vector3 gridOrigin; // Calculated automatically

    [Header("Debug")]
    public bool sandboxMode = true;

    private BuildingBase[,] grid;
    private SupplyDrop[,] resourceGrid;
    [SerializeField, HideInInspector] private bool[,] unlockedPlots;

    private void Awake()
    {
        if (Instance != null && Instance != this) Destroy(this);
        else Instance = this;

        RecalculateOrigin();
        InitializeGridData();
    }

    // --- NEW: Calculates Grid Origin from the Floor Plane ---
    public void RecalculateOrigin()
    {
        if (floorPlane != null)
        {
            Renderer r = floorPlane.GetComponent<Renderer>();
            if (r != null)
            {
                // bounds.min gives us the Bottom-Left-Back corner of the object
                gridOrigin = r.bounds.min;
                // Force Y to 0 just in case the plane is floating slightly
                gridOrigin.y = 0;
            }
            else
            {
                // Fallback if no renderer (e.g. just a transform)
                gridOrigin = floorPlane.position;
            }
        }
        else
        {
            gridOrigin = Vector3.zero;
        }
    }

    private void InitializeGridData()
    {
        if (grid == null) grid = new BuildingBase[mapWidth, mapHeight];
        if (resourceGrid == null) resourceGrid = new SupplyDrop[mapWidth, mapHeight];

        if (unlockedPlots == null || unlockedPlots.GetLength(0) != mapWidth)
        {
            unlockedPlots = new bool[mapWidth, mapHeight];
            UnlockArea(0, 0, mapWidth, mapHeight); // Unlock everything for testing
        }
    }

    public void UnlockArea(int startX, int startY, int width, int height)
    {
        for (int x = startX; x < startX + width; x++)
            for (int y = startY; y < startY + height; y++)
                if (IsValidCoord(x, y)) unlockedPlots[x, y] = true;
    }

    // --- CHECKS ---

    public bool IsValidCoord(int x, int y)
    {
        return x >= 0 && y >= 0 && x < mapWidth && y < mapHeight;
    }

    public bool IsUnlocked(Vector2Int pos)
    {
        if (!IsValidCoord(pos.x, pos.y)) return false;
        return sandboxMode || unlockedPlots[pos.x, pos.y];
    }

    public bool IsOccupied(Vector2Int pos)
    {
        if (!IsValidCoord(pos.x, pos.y)) return false;
        return grid[pos.x, pos.y] != null;
    }

    public bool IsCellEmpty(Vector2Int pos) => !IsOccupied(pos);

    public bool IsBuildable(Vector2Int pos)
    {
        return IsUnlocked(pos) && !IsOccupied(pos);
    }

    // --- MANAGEMENT ---

    public void PlaceBuilding(BuildingBase building, Vector2Int pos)
    {
        if (!IsValidCoord(pos.x, pos.y)) return;
        if (grid[pos.x, pos.y] != null) RemoveBuilding(pos);

        grid[pos.x, pos.y] = building;
        building.Setup(pos);
    }

    public void RegisterBuilding(Vector2Int pos, BuildingBase building)
    {
        PlaceBuilding(building, pos);
    }

    public BuildingBase GetBuildingAt(Vector2Int pos)
    {
        if (!IsValidCoord(pos.x, pos.y)) return null;
        return grid[pos.x, pos.y];
    }

    public void RemoveBuilding(Vector2Int pos)
    {
        if (IsValidCoord(pos.x, pos.y) && grid[pos.x, pos.y] != null)
        {
            Destroy(grid[pos.x, pos.y].gameObject);
            grid[pos.x, pos.y] = null;
        }
    }

    public void RegisterResource(SupplyDrop drop, Vector2Int pos)
    {
        if (IsValidCoord(pos.x, pos.y))
        {
            resourceGrid[pos.x, pos.y] = drop;
            drop.Setup(pos);
        }
    }

    public SupplyDrop GetResourceAt(Vector2Int pos)
    {
        if (!IsValidCoord(pos.x, pos.y)) return null;
        return resourceGrid[pos.x, pos.y];
    }

    // --- CONVERSION (UPDATED) ---

    public Vector2Int WorldToGrid(Vector3 worldPos)
    {
        // Subtract origin first to make calculations local to the grid start
        Vector3 localPos = worldPos - gridOrigin;
        return new Vector2Int(Mathf.FloorToInt(localPos.x / cellSize), Mathf.FloorToInt(localPos.z / cellSize));
    }

    public Vector3 GridToWorld(Vector2Int gridPos)
    {
        // Add origin back to get world space
        float x = gridOrigin.x + (gridPos.x * cellSize) + (cellSize / 2f);
        float z = gridOrigin.z + (gridPos.y * cellSize) + (cellSize / 2f);
        return new Vector3(x, 0, z);
    }

    private void OnValidate() { if (!Application.isPlaying) InitializeGridData(); }

    private void OnDrawGizmos()
    {
        // Visualise the grid so you can see if it matches your floor
        if (floorPlane == null) return;

        // Quick recalculate for editor view
        RecalculateOrigin();

        Gizmos.color = new Color(0, 1, 0, 0.2f);
        // Draw outline of grid area
        Vector3 size = new Vector3(mapWidth * cellSize, 0.1f, mapHeight * cellSize);
        Vector3 center = gridOrigin + (size / 2f);
        Gizmos.DrawWireCube(center, size);

        // Draw Origin Tile
        Gizmos.color = Color.red;
        Gizmos.DrawWireCube(GridToWorld(Vector2Int.zero), new Vector3(cellSize, 0.1f, cellSize));
    }
}