using UnityEngine;
using TMPro;

public class ItemVisualizer : MonoBehaviour
{
    [Header("Visual References")]
    public MeshRenderer meshRenderer;
    public TextMeshPro rankText;

    [Header("Settings")]
    public Vector3 baseScale = new Vector3(0.8f, 0.05f, 1.1f);
    public float jumpHeight = 0.2f; // Height of the little hop

    public ItemPayload cachedPayload;

    // Movement State
    private Vector3 startPos;
    private Vector3 targetPos;
    private bool isMoving = false;

    private void Awake()
    {
        if (meshRenderer == null) meshRenderer = GetComponentInChildren<MeshRenderer>();
    }

    public void SetVisuals(ItemPayload item)
    {
        cachedPayload = item;
        if (item == null || item.contents.Count == 0) return;

        CardData topCard = item.contents[item.contents.Count - 1];

        // 1. Apply Color
        if (meshRenderer != null)
        {
            meshRenderer.material.color = topCard.GetDisplayColor();
        }

        // 2. Apply Text
        if (rankText != null)
        {
            if (topCard.suit == CardSuit.None)
            {
                rankText.text = "";
            }
            else
            {
                string rankStr = GetRankString(topCard.rank);
                string suitStr = GetSuitSymbol(topCard.suit);

                // Rich Text Coloring
                string colorTag = (topCard.suit == CardSuit.Heart || topCard.suit == CardSuit.Diamond)
                    ? "<color=red>" : "<color=black>";

                rankText.text = $"{colorTag}{rankStr}\n<size=80%>{suitStr}</size></color>";
            }
        }

        // 3. Force Card Shape (Fixes "Too Large/Cube" issues)
        if (item.contents.Count > 1)
        {
            float stackHeight = 1f + (item.contents.Count * 0.1f);
            transform.localScale = new Vector3(baseScale.x, baseScale.y * stackHeight, baseScale.z);
        }
        else
        {
            transform.localScale = baseScale;
        }
    }

    public void InitializeMovement(Vector3 end, float duration)
    {
        // We ignore 'duration' now and rely on TickManager for perfect sync
        startPos = transform.position;
        targetPos = end;
        isMoving = true;
        gameObject.SetActive(true);

        // Snap rotation to look at destination
        if (Vector3.Distance(startPos, targetPos) > 0.01f)
        {
            transform.rotation = Quaternion.LookRotation(targetPos - startPos);
        }
    }

    private void Update()
    {
        if (!isMoving || TickManager.Instance == null) return;

        // --- THE FIX ---
        // Instead of calculating our own time, we ask the TickManager:
        // "How far along are we in the current tick (0.0 to 1.0)?"
        float t = TickManager.Instance.GetInterpolationFactor();

        // 1. Linear Move
        Vector3 currentPos = Vector3.Lerp(startPos, targetPos, t);

        // 2. Add Arc/Hop (Parabola)
        // Only hop if moving significant distance (e.g. between tiles)
        if (Vector3.Distance(startPos, targetPos) > 0.5f)
        {
            float height = Mathf.Sin(t * Mathf.PI) * jumpHeight;
            currentPos.y += height;
        }

        transform.position = currentPos;

        // Snap to finish if tick is done (factor loops back to 0, so we check near 1)
        // Actually, with interpolation factor, we just let it ride. 
        // When the next tick fires, InitializeMovement will be called again with new positions.
    }

    // --- Helpers ---
    string GetRankString(int rank)
    {
        switch (rank)
        {
            case 11: return "J";
            case 12: return "Q";
            case 13: return "K";
            case 14: return "A";
            default: return rank.ToString();
        }
    }

    string GetSuitSymbol(CardSuit suit)
    {
        switch (suit)
        {
            case CardSuit.Heart: return "♥";
            case CardSuit.Diamond: return "♦";
            case CardSuit.Club: return "♣";
            case CardSuit.Spade: return "♠";
            default: return "";
        }
    }
}