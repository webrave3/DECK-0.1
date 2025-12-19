using UnityEngine;

public class ItemVisualizer : MonoBehaviour
{
    private Vector3 startPos;
    private Vector3 endPos;
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

        // Temporary Debug Coloring
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

    public void InitializeMovement(Vector3 start, Vector3 end)
    {
        startPos = start;
        endPos = end;
        isMoving = true;
        gameObject.SetActive(true);

        if (start != end)
        {
            transform.rotation = Quaternion.LookRotation(end - start);
        }
    }

    private void Update()
    {
        if (TickManager.Instance == null) return;

        if (isMoving)
        {
            float percent = TickManager.Instance.GetInterpolationFactor();
            transform.position = Vector3.Lerp(startPos, endPos, percent);

            if (percent >= 1.0f)
            {
                isMoving = false;
                transform.position = endPos;
            }
        }
    }
}