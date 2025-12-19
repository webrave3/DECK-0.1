using UnityEngine;

[RequireComponent(typeof(ConveyorBelt))]
public class BeltAutotiler : MonoBehaviour
{
    [Header("Visual Models")]
    public GameObject straightModel;
    public GameObject cornerLeftModel;
    public GameObject cornerRightModel;

    private ConveyorBelt belt;
    private bool isInitialized = false;

    // Debugging state
    private bool debugInputLeft;
    private bool debugInputRight;
    private bool debugInputBack;

    private void Awake()
    {
        belt = GetComponent<ConveyorBelt>();
    }

    // We removed Start() because it runs too early!
    // We will call this manually from BuildingSystem.
    public void Initialize()
    {
        isInitialized = true;
        UpdateVisuals();
        UpdateNeighbors();
    }

    public void UpdateVisuals()
    {
        if (belt == null) return;

        // 1. Get Neighbors
        // We use the belt's data to find coordinates relative to its rotation
        Vector2Int backPos = belt.GetBackGridPosition();
        Vector2Int leftPos = belt.GetLeftGridPosition();
        Vector2Int rightPos = belt.GetRightGridPosition();

        // 2. Check connections
        debugInputBack = IsBeltPointingAtMe(backPos);
        debugInputLeft = IsBeltPointingAtMe(leftPos);
        debugInputRight = IsBeltPointingAtMe(rightPos);

        // 3. Reset Models
        if (straightModel) straightModel.SetActive(false);
        if (cornerLeftModel) cornerLeftModel.SetActive(false);
        if (cornerRightModel) cornerRightModel.SetActive(false);

        // 4. Apply Logic
        // CORNER LEFT: Input ONLY from Left
        if (debugInputLeft && !debugInputBack && !debugInputRight)
        {
            if (cornerLeftModel) cornerLeftModel.SetActive(true);
        }
        // CORNER RIGHT: Input ONLY from Right
        else if (debugInputRight && !debugInputBack && !debugInputLeft)
        {
            if (cornerRightModel) cornerRightModel.SetActive(true);
        }
        // STRAIGHT: Default
        else
        {
            if (straightModel) straightModel.SetActive(true);
        }
    }

    private void UpdateNeighbors()
    {
        // Force neighbors to re-check their visuals now that I exist
        NotifyNeighbor(belt.GetForwardGridPosition());
        NotifyNeighbor(belt.GetBackGridPosition());
        NotifyNeighbor(belt.GetLeftGridPosition());
        NotifyNeighbor(belt.GetRightGridPosition());
    }

    private void NotifyNeighbor(Vector2Int pos)
    {
        BuildingBase b = CasinoGridManager.Instance.GetBuildingAt(pos);
        if (b != null)
        {
            BeltAutotiler tiler = b.GetComponent<BeltAutotiler>();
            if (tiler != null) tiler.UpdateVisuals();
        }
    }

    private bool IsBeltPointingAtMe(Vector2Int neighborPos)
    {
        BuildingBase b = CasinoGridManager.Instance.GetBuildingAt(neighborPos);
        if (b == null) return false;

        // Does that belt point to my grid coordinate?
        return b.GetForwardGridPosition() == belt.GridPosition;
    }

    private void OnDrawGizmosSelected()
    {
        if (!isInitialized) return;

        // Visual Debugging in Scene View
        Gizmos.color = Color.green;
        Vector3 center = transform.position + Vector3.up * 0.5f;

        if (debugInputLeft) DrawArrow(transform.position - transform.right, center);
        if (debugInputRight) DrawArrow(transform.position + transform.right, center);
        if (debugInputBack) DrawArrow(transform.position - transform.forward, center);
    }

    private void DrawArrow(Vector3 from, Vector3 to)
    {
        Gizmos.DrawLine(from, to);
        Gizmos.DrawSphere(from, 0.1f);
    }
}