using UnityEngine;

public class CasinoGridManager : MonoBehaviour
{
    public static CasinoGridManager Instance { get; private set; }

    [Header("Map Settings")]
    public int mapWidth = 100;
    public int mapHeight = 100;
    public float cellSize = 1f;

    [Header("Debug")]
    public bool sandboxMode = true; // Set TRUE to ignore locked areas

    private BuildingBase[,] grid;
    private SupplyDrop[,] resourceGrid;
    [SerializeField, HideInInspector] private bool[,] unlockedPlots;

    private void Awake()
    {
        if (Instance != null && Instance != this) Destroy(this);
        else Instance = this;

        InitializeGridData();
    }

    private void InitializeGridData()
    {
        if (grid == null) grid = new BuildingBase[mapWidth, mapHeight];
        if (resourceGrid == null) resourceGrid = new SupplyDrop[mapWidth, mapHeight];

        if (unlockedPlots == null || unlockedPlots.GetLength(0) != mapWidth)
        {
            unlockedPlots = new bool[mapWidth, mapHeight];
            UnlockArea(45, 45, 10, 10);
        }
    }

    public void UnlockArea(int startX, int startY, int width, int height)
    {
        for (int x = startX; x < startX + width; x++)
        {
            for (int y = startY; y < startY + height; y++)
            {
                if (IsValidCoord(x, y)) unlockedPlots[x, y] = true;
            }
        }
    }

    public bool IsBuildable(Vector2Int pos)
    {
        if (!IsValidCoord(pos.x, pos.y)) return false;

        // Sandbox check
        if (!sandboxMode && !unlockedPlots[pos.x, pos.y]) return false;

        return grid[pos.x, pos.y] == null;
    }

    public void PlaceBuilding(BuildingBase building, Vector2Int pos)
    {
        if (!IsBuildable(pos)) return;
        grid[pos.x, pos.y] = building;
        building.Setup(pos);
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
            // Destroy the GameObject
            Destroy(grid[pos.x, pos.y].gameObject);

            // Clear the Grid Slot
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

    public Vector2Int WorldToGrid(Vector3 worldPos)
    {
        return new Vector2Int(Mathf.FloorToInt(worldPos.x / cellSize), Mathf.FloorToInt(worldPos.z / cellSize));
    }

    public Vector3 GridToWorld(Vector2Int gridPos)
    {
        return new Vector3(gridPos.x * cellSize + (cellSize / 2f), 0, gridPos.y * cellSize + (cellSize / 2f));
    }

    private bool IsValidCoord(int x, int y)
    {
        return x >= 0 && y >= 0 && x < mapWidth && y < mapHeight;
    }

    private void OnValidate()
    {
        if (Application.isPlaying) return;
        InitializeGridData();
    }

    private void OnDrawGizmos()
    {
        if (unlockedPlots == null) return;
        Gizmos.color = new Color(0, 1, 0, 0.3f);

        for (int x = 0; x < mapWidth; x++)
        {
            for (int y = 0; y < mapHeight; y++)
            {
                // Only draw if within bounds and unlocked
                if (x < unlockedPlots.GetLength(0) && y < unlockedPlots.GetLength(1) && unlockedPlots[x, y])
                {
                    Vector3 center = new Vector3(x * cellSize + (cellSize / 2f), 0.1f, y * cellSize + (cellSize / 2f));
                    Gizmos.DrawWireCube(center, Vector3.one * cellSize * 0.9f);
                }
            }
        }
    }
}