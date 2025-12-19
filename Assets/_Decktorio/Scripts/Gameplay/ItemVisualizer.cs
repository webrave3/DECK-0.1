using UnityEngine;

public class ItemVisualizer : MonoBehaviour
{
    private Vector3 startPos;
    private Vector3 targetPos;
    private float moveProgress = 0f;
    private float moveDuration = 1f; // Will be set by belt speed
    private bool isMoving = false;

    // References
    private MeshRenderer meshRenderer;

    private void Awake()
    {
        meshRenderer = GetComponentInChildren<MeshRenderer>();
        if (meshRenderer == null) meshRenderer = GetComponent<MeshRenderer>();
    }

    public void SetVisuals(CardPayload card)
    {
        if (meshRenderer == null) return;

        // Visual debug colors based on suit
        switch (card.suit)
        {
            case CardSuit.Heart:
            case CardSuit.Diamond:
                meshRenderer.material.color = Color.red;
                break;
            case CardSuit.Spade:
            case CardSuit.Club:
                meshRenderer.material.color = Color.black;
                break;
            default:
                meshRenderer.material.color = Color.white;
                break;
        }
    }

    /// <summary>
    /// Starts moving the item from its CURRENT position to the new target.
    /// </summary>
    public void InitializeMovement(Vector3 end, float duration)
    {
        // Start from wherever we are right now to prevent teleporting
        startPos = transform.position;
        targetPos = end;
        moveDuration = duration;
        moveProgress = 0f;
        isMoving = true;
        gameObject.SetActive(true);

        // Face the direction of travel
        if (startPos != targetPos)
        {
            transform.rotation = Quaternion.LookRotation(targetPos - startPos);
        }
    }

    private void Update()
    {
        if (!isMoving || TickManager.Instance == null) return;

        // Calculate speed based on tick rate so it matches the game loop
        float speedMultiplier = 1f / TickManager.Instance.tickRate;
        float dt = Time.deltaTime * speedMultiplier;

        moveProgress += dt;
        float t = Mathf.Clamp01(moveProgress);

        // Smooth Lerp
        transform.position = Vector3.Lerp(startPos, targetPos, t);

        if (t >= 1.0f)
        {
            isMoving = false;
            transform.position = targetPos; // Snap to exact end to avoid drift
        }
    }
}