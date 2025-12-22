using UnityEngine;
using TMPro;

public class ItemVisualizer : MonoBehaviour
{
    [Header("Visual References")]
    public MeshRenderer meshRenderer;
    public TextMeshPro rankText;

    [Header("Settings")]
    // FIXED: Smaller scale for proper belt margins (Padding)
    public Vector3 baseScale = new Vector3(0.4f, 0.03f, 0.5f);

    public ItemPayload cachedPayload;

    // Movement State
    private Vector3 targetPos;
    private float moveSpeed;
    private bool isMoving = false;

    private void Awake()
    {
        if (meshRenderer == null) meshRenderer = GetComponentInChildren<MeshRenderer>();

        // --- PHYSICS SAFETY LOCK ---
        // Prevents cards from falling through the floor (fixing the Unpacker bug)
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.useGravity = false;
            rb.isKinematic = true;
        }
        Collider col = GetComponent<Collider>();
        if (col != null) col.enabled = false;
        // ---------------------------
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
            if (topCard.suit == CardSuit.None)
            {
                rankText.text = "";
            }
            else
            {
                string rankStr = GetRankString(topCard.rank);
                string suitStr = GetSuitSymbol(topCard.suit);

                // FIX: Get high-contrast text color (White on Black/Red, Black on Gold/White)
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

        // Calculate constant speed
        float distance = Vector3.Distance(transform.position, endPos);
        if (duration > 0 && distance > 0) moveSpeed = distance / duration;
        else moveSpeed = 50f; // Fast snap if 0 duration

        isMoving = true;
        gameObject.SetActive(true);
    }

    private void Update()
    {
        if (!isMoving) return;

        // 1. Move Linear (Smooth Slide)
        transform.position = Vector3.MoveTowards(transform.position, targetPos, moveSpeed * Time.deltaTime);

        // 2. Rotate Snappy (Fixes diagonal floating issues)
        if (Vector3.Distance(transform.position, targetPos) > 0.01f)
        {
            Vector3 dir = targetPos - transform.position;
            dir.y = 0;
            if (dir != Vector3.zero)
            {
                Quaternion targetRot = Quaternion.LookRotation(dir);
                // Fast rotation (720 deg/s) looks snappy and responsive
                transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRot, 720f * Time.deltaTime);
            }
        }

        // 3. Stop check
        if (Vector3.Distance(transform.position, targetPos) < 0.001f)
        {
            isMoving = false;
        }
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