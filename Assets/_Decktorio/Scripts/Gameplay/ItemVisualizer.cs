using UnityEngine;

public class ItemVisualizer : MonoBehaviour
{
    private Vector3 startPos;
    private Vector3 endPos;
    private bool isMoving = false;

    public void InitializeMovement(Vector3 start, Vector3 end)
    {
        startPos = start;
        endPos = end;
        isMoving = true;
        transform.position = start;

        // Instant rotation to face destination (prevents sideways sliding)
        if (start != end)
        {
            transform.rotation = Quaternion.LookRotation(end - start);
        }
    }

    private void Update()
    {
        if (!isMoving) return;

        // Get interpolation (0.0 to 1.0)
        float percent = TickManager.Instance.GetInterpolationFactor();

        // Smooth slide
        transform.position = Vector3.Lerp(startPos, endPos, percent);
    }
}