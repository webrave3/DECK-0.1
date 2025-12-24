using UnityEngine;
using TMPro;

public class ItemVisualizer : MonoBehaviour
{
    [Header("Visual References")]
    public MeshRenderer meshRenderer;
    public TextMeshPro rankText;

    [Header("Settings")]
    public Vector3 baseScale = new Vector3(0.4f, 0.03f, 0.5f);

    public ItemPayload cachedPayload;

    // Movement State
    private Vector3 targetPos;
    private float moveSpeed;
    private bool isMoving = false;

    private void Awake()
    {
        if (meshRenderer == null) meshRenderer = GetComponentInChildren<MeshRenderer>();

        // Physics Safety
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null) { rb.useGravity = false; rb.isKinematic = true; }
        Collider col = GetComponent<Collider>();
        if (col != null) col.enabled = false;
    }

    public void SetVisuals(ItemPayload item)
    {
        cachedPayload = item;
        if (item == null || item.contents.Count == 0) return;

        CardData topCard = item.contents[item.contents.Count - 1];

        // 1. Apply Background Color
        if (meshRenderer != null)
        {
            meshRenderer.material.color = topCard.GetDisplayColor();
        }

        // 2. Apply Legible Text
        if (rankText != null)
        {
            // Update: Check for "Empty" suit (0)
            if (topCard.suit == 0)
            {
                rankText.text = "";
            }
            else
            {
                string rankStr = GetRankString(topCard.rank);
                string suitStr = GetSuitSymbol(topCard.suit);

                Color txtColor = topCard.GetContrastingTextColor();
                string hexColor = "#" + ColorUtility.ToHtmlStringRGBA(txtColor);

                rankText.text = $"<color={hexColor}>{rankStr}\n<size=80%>{suitStr}</size></color>";
            }
        }

        // 3. Apply Scale (Stacks)
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

    public void InitializeMovement(Vector3 endPos, float duration)
    {
        targetPos = endPos;
        float distance = Vector3.Distance(transform.position, endPos);
        if (duration > 0 && distance > 0) moveSpeed = distance / duration;
        else moveSpeed = 50f;

        isMoving = true;
        gameObject.SetActive(true);
    }

    private void Update()
    {
        if (!isMoving) return;

        transform.position = Vector3.MoveTowards(transform.position, targetPos, moveSpeed * Time.deltaTime);

        if (Vector3.Distance(transform.position, targetPos) > 0.01f)
        {
            Vector3 dir = targetPos - transform.position;
            dir.y = 0;
            if (dir != Vector3.zero)
            {
                Quaternion targetRot = Quaternion.LookRotation(dir);
                transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRot, 720f * Time.deltaTime);
            }
        }

        if (Vector3.Distance(transform.position, targetPos) < 0.001f)
        {
            isMoving = false;
        }
    }

    // --- Helpers ---

    // UPDATED: Now accepts float
    string GetRankString(float rank)
    {
        // If it's a whole number, treat it like a classic card
        if (Mathf.Approximately(rank % 1, 0))
        {
            int r = Mathf.RoundToInt(rank);
            switch (r)
            {
                case 11: return "J";
                case 12: return "Q";
                case 13: return "K";
                case 14: return "A";
                default: return r.ToString();
            }
        }
        // Otherwise show the float (e.g. "3.5")
        return rank.ToString("0.0");
    }

    string GetSuitSymbol(CardSuit suit)
    {
        // Handle Composite Suits (e.g. Heart + Spade)
        string s = "";
        if (suit.HasFlag(CardSuit.Heart)) s += "♥";
        if (suit.HasFlag(CardSuit.Diamond)) s += "♦";
        if (suit.HasFlag(CardSuit.Club)) s += "♣";
        if (suit.HasFlag(CardSuit.Spade)) s += "♠";
        if (suit.HasFlag(CardSuit.Gear)) s += "⚙";
        return s;
    }
}