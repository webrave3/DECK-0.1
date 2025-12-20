using UnityEngine;

public class ItemVisualizer : MonoBehaviour
{
    private Vector3 startPos;
    private Vector3 targetPos;
    private float moveProgress = 0f;
    private float moveDuration = 1f;
    private bool isMoving = false;

    // References
    private MeshRenderer meshRenderer;

    private void Awake()
    {
        meshRenderer = GetComponentInChildren<MeshRenderer>();
        if (meshRenderer == null) meshRenderer = GetComponent<MeshRenderer>();
    }

    public void SetVisuals(ItemPayload item)
    {
        if (meshRenderer == null || item.contents.Count == 0) return;

        // Get the top card of the stack for visual representation
        CardData topCard = item.contents[item.contents.Count - 1];

        // Simple color debugging
        switch (topCard.suit)
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

        // If it's a stack, maybe scale it slightly?
        if (item.contents.Count > 1)
        {
            transform.localScale = new Vector3(1, 1 + (item.contents.Count * 0.1f), 1);
        }
    }

    public void InitializeMovement(Vector3 end, float duration)
    {
        startPos = transform.position;
        targetPos = end;
        moveDuration = duration;
        moveProgress = 0f;
        isMoving = true;
        gameObject.SetActive(true);

        if (startPos != targetPos)
        {
            transform.rotation = Quaternion.LookRotation(targetPos - startPos);
        }
    }

    private void Update()
    {
        if (!isMoving || TickManager.Instance == null) return;

        float speedMultiplier = 1f / TickManager.Instance.tickRate;
        float dt = Time.deltaTime * speedMultiplier;

        moveProgress += dt;
        float t = Mathf.Clamp01(moveProgress);

        transform.position = Vector3.Lerp(startPos, targetPos, t);

        if (t >= 1.0f)
        {
            isMoving = false;
            transform.position = targetPos;
        }
    }
}