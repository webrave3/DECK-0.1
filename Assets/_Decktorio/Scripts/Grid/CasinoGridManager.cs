using UnityEngine;

public class CasinoGridManager : MonoBehaviour
{
    public static CasinoGridManager Instance { get; private set; }

    [Header("Map Settings")]
    public int mapWidth = 100;
    public int mapHeight = 100;
    public float cellSize = 1f;

    // The actual data storage for buildings
    private BuildingBase[,] grid;

    // "Plots" mask: true = unlocked/buildable, false = locked area
    // SerializedField ensures it saves between editor sessions if modified in OnValidate
    [SerializeField, HideInInspector] private bool[,] unlockedPlots;

    private void Awake()
    {
        // Singleton setup
        if (Instance != null && Instance != this) Destroy(this);
        else Instance = this;

        InitializeGridData();
    }

    // Called by Awake AND OnValidate to ensure data exists
    private void InitializeGridData()
    {
        if (grid == null) grid = new BuildingBase[mapWidth, mapHeight];

        // If unlockedPlots is null or the wrong size (e.g. changed map size in inspector), recreate it
        if (unlockedPlots == null || unlockedPlots.GetLength(0) != mapWidth || unlockedPlots.GetLength(1) != mapHeight)
        {
            unlockedPlots = new bool[mapWidth, mapHeight];
            // Initial "Rat Maze" Unlock (10x10 center area)
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
        // Must be unlocked AND empty
        if (grid == null) return false; // Safety check
        return unlockedPlots[pos.x, pos.y] && grid[pos.x, pos.y] == null;
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
        if (grid == null) return null;
        return grid[pos.x, pos.y];
    }

    public Vector2Int WorldToGrid(Vector3 worldPos)
    {
        // Use FloorToInt for more reliable grid alignment than RoundToInt
        return new Vector2Int(Mathf.FloorToInt(worldPos.x / cellSize), Mathf.FloorToInt(worldPos.z / cellSize));
    }

    public Vector3 GridToWorld(Vector2Int gridPos)
    {
        // Center the object in the cell (+0.5f)
        return new Vector3(gridPos.x * cellSize + (cellSize / 2f), 0, gridPos.y * cellSize + (cellSize / 2f));
    }

    private bool IsValidCoord(int x, int y)
    {
        return x >= 0 && y >= 0 && x < mapWidth && y < mapHeight;
    }

    // This runs in the EDITOR when you change values or load the scene
    private void OnValidate()
    {
        // Do not run during Play mode to avoid overwriting runtime data
        if (Application.isPlaying) return;
        InitializeGridData();
    }

    private void OnDrawGizmos()
    {
        // Only draw if we have data
        if (unlockedPlots == null) return;

        Gizmos.color = new Color(0, 1, 0, 0.3f); // Semi-transparent Green

        for (int x = 0; x < mapWidth; x++)
        {
            for (int y = 0; y < mapHeight; y++)
            {
                // Safety check against out of bounds during live resizing
                if (x < unlockedPlots.GetLength(0) && y < unlockedPlots.GetLength(1) && unlockedPlots[x, y])
                {
                    // Draw the gizmo centered in the tile
                    Vector3 center = new Vector3(x * cellSize + (cellSize / 2f), 0.1f, y * cellSize + (cellSize / 2f));
                    Gizmos.DrawWireCube(center, Vector3.one * cellSize * 0.9f);
                }
            }
        }
    }
}