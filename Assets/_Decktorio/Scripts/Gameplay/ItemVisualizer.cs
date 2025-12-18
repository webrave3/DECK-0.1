using UnityEngine;

public class ItemVisualizer : MonoBehaviour
{
    private Vector3 startPos;
    private Vector3 endPos;
    private bool isMoving = false;

    // Called by the Belt when an item enters it
    public void InitializeMovement(Vector3 start, Vector3 end)
    {
        startPos = start;
        endPos = end;
        isMoving = true;
        transform.position = start;
    }

    private void Update()
    {
        if (!isMoving) return;

        // Ask the TickManager "How far are we between ticks?" (0.0 to 1.0)
        float percent = TickManager.Instance.GetInterpolationFactor();

        // Smoothly slide to the destination
        transform.position = Vector3.Lerp(startPos, endPos, percent);

        // Optional: Add a tiny arc or "wobble" here later for juice
    }
}