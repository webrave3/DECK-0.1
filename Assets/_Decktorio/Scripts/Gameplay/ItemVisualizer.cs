using UnityEngine;
using TMPro;

public class ItemVisualizer : MonoBehaviour
{
    [Header("Visual References")]
    public MeshRenderer meshRenderer;
    public TextMeshPro rankText;

    [Header("Settings")]
    public Vector3 baseScale = new Vector3(0.8f, 0.05f, 1.1f);

    // Removed jumpHeight - we want smooth conveyor sliding!

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

        if (meshRenderer != null)
            meshRenderer.material.color = topCard.GetDisplayColor();

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
                string colorTag = (topCard.suit == CardSuit.Heart || topCard.suit == CardSuit.Diamond)
                    ? "<color=red>" : "<color=black>";

                rankText.text = $"{colorTag}{rankStr}\n<size=80%>{suitStr}</size></color>";
            }
        }

        // Shape scaling
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
        startPos = transform.position;
        targetPos = end;
        isMoving = true;
        gameObject.SetActive(true);

        // Instant Rotation look-at for cleaner turns
        if (Vector3.Distance(startPos, targetPos) > 0.01f)
        {
            Vector3 dir = targetPos - startPos;
            dir.y = 0; // Keep flat
            if (dir != Vector3.zero) transform.rotation = Quaternion.LookRotation(dir);
        }
    }

    private void Update()
    {
        if (!isMoving || TickManager.Instance == null) return;

        // Smooth Linear Interpolation (Sliding)
        float t = TickManager.Instance.GetInterpolationFactor();

        // Use Lerp for smooth sliding. 
        // No jumping, no arcs. Just slide.
        Vector3 currentPos = Vector3.Lerp(startPos, targetPos, t);

        transform.position = currentPos;
    }

    // --- Helpers ---
    string GetRankString(int rank)
    {
        switch (rank) { case 11: return "J"; case 12: return "Q"; case 13: return "K"; case 14: return "A"; default: return rank.ToString(); }
    }

    string GetSuitSymbol(CardSuit suit)
    {
        switch (suit) { case CardSuit.Heart: return "♥"; case CardSuit.Diamond: return "♦"; case CardSuit.Club: return "♣"; case CardSuit.Spade: return "♠"; default: return ""; }
    }
}