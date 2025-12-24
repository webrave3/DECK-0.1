using UnityEngine;

public enum ResourceType
{
    InkSource,  // Provides Ink (Red, Black, Neon)
    SuitMold    // Provides Shape (Heart, Spade, etc.)
}

public class ResourceNode : MonoBehaviour
{
    [Header("Settings")]
    public ResourceType type;

    [Tooltip("If type is InkSource, this is the ink provided.")]
    public CardInk inkGiven;

    [Tooltip("If type is SuitMold, this is the suit provided.")]
    public CardSuit suitGiven;

    [Header("Visuals")]
    public SpriteRenderer iconRenderer;
    public Color nodeColor = Color.white;

    public Vector2Int GridPosition { get; private set; }

    public void Setup(Vector2Int pos)
    {
        GridPosition = pos;

        // Align to grid, but sit slightly below buildings (y = 0.01)
        // Assuming buildings sit at y = 0 or y = 0.5 depending on origin
        if (CasinoGridManager.Instance != null)
        {
            Vector3 worldPos = CasinoGridManager.Instance.GridToWorld(pos);
            worldPos.y = 0.01f; // Just above the floor plane
            transform.position = worldPos;
        }

        UpdateVisuals();
    }

    private void UpdateVisuals()
    {
        if (iconRenderer != null)
        {
            iconRenderer.color = nodeColor;
        }
    }
}