using UnityEngine;

public class ConveyorBelt : BuildingBase
{
    [Header("Visuals")]
    public float itemHeightOffset = 0.2f;
    // FIXED: 0.40 leaves a 0.10 gap from the edge (0.50), creating padding.
    public float forwardVisualOffset = 1.0f;
    public float speedModifier = 1.0f;

    private Transform visualStraight;
    private Transform visualLeft;
    private Transform visualRight;

    protected override void Start()
    {
        base.Start();
        // Safety Register
        if (CasinoGridManager.Instance != null)
        {
            Vector2Int truePos = CasinoGridManager.Instance.WorldToGrid(transform.position);
            if (CasinoGridManager.Instance.GetBuildingAt(truePos) != this)
            {
                Setup(truePos);
                CasinoGridManager.Instance.RegisterBuilding(truePos, this);
            }
        }

        visualStraight = transform.Find("Visual_Straight");
        visualLeft = transform.Find("Visual_Corner_Left");
        visualRight = transform.Find("Visual_Corner_Right");
        UpdateVisuals();
    }

    public void UpdateVisuals()
    {
        bool inputLeft = false, inputRight = false, inputBack = false;
        CheckNeighbor(GetBackGridPosition(), ref inputBack);
        CheckNeighbor(GetLeftGridPosition(), ref inputLeft);
        CheckNeighbor(GetRightGridPosition(), ref inputRight);

        if (visualStraight) visualStraight.gameObject.SetActive(false);
        if (visualLeft) visualLeft.gameObject.SetActive(false);
        if (visualRight) visualRight.gameObject.SetActive(false);

        if (inputLeft && !inputBack) { if (visualLeft) visualLeft.gameObject.SetActive(true); else if (visualStraight) visualStraight.gameObject.SetActive(true); }
        else if (inputRight && !inputBack) { if (visualRight) visualRight.gameObject.SetActive(true); else if (visualStraight) visualStraight.gameObject.SetActive(true); }
        else { if (visualStraight) visualStraight.gameObject.SetActive(true); }
    }

    private void CheckNeighbor(Vector2Int pos, ref bool hasInput)
    {
        BuildingBase b = CasinoGridManager.Instance.GetBuildingAt(pos);
        if (b != null && b.GetForwardGridPosition() == GridPosition) hasInput = true;
    }

    protected override void OnTick(int tick)
    {
        if (internalItem != null)
        {
            TryPushItem();

            // Queue Logic: If blocked, ensure visual sits at the edge
            if (internalItem != null && internalVisual != null)
            {
                MoveVisualToEdge();
            }
        }
    }

    private void TryPushItem()
    {
        Vector2Int forwardPos = GetForwardGridPosition();
        BuildingBase target = CasinoGridManager.Instance.GetBuildingAt(forwardPos);

        if (target != null && target.CanAcceptItem(GridPosition))
        {
            internalItem.velocityBonus = this.speedModifier;
            target.ReceiveItem(internalItem, internalVisual);
            internalItem = null;
            internalVisual = null;
        }
    }

    private void MoveVisualToEdge()
    {
        Vector3 worldForward = (CasinoGridManager.Instance.GridToWorld(GetForwardGridPosition()) - transform.position).normalized;
        Vector3 edgePos = transform.position + (Vector3.up * itemHeightOffset) + (worldForward * forwardVisualOffset);

        // Pass duration based on tickrate for smooth visual queueing
        float duration = TickManager.Instance != null ? TickManager.Instance.tickRate : 0.5f;
        internalVisual.InitializeMovement(edgePos, duration);
    }

    public override bool CanAcceptItem(Vector2Int fromPos)
    {
        if (incomingItem != null || internalItem != null) return false;
        if (fromPos == GetForwardGridPosition()) return false;
        return true;
    }

    protected override void OnItemArrived()
    {
        if (internalVisual != null)
        {
            internalVisual.transform.SetParent(this.transform);
            // Immediately start moving to queue position
            MoveVisualToEdge();
        }
    }

    // Direction Helpers
    public Vector2Int GetLeftGridPosition() => GridPosition + GetDirFromIndex((RotationIndex + 3) % 4);
    public Vector2Int GetRightGridPosition() => GridPosition + GetDirFromIndex((RotationIndex + 1) % 4);
    public Vector2Int GetBackGridPosition() => GridPosition + GetDirFromIndex((RotationIndex + 2) % 4);
    public static Vector2Int GetDirFromIndex(int index)
    {
        switch (index) { case 0: return new Vector2Int(0, 1); case 1: return new Vector2Int(1, 0); case 2: return new Vector2Int(0, -1); case 3: return new Vector2Int(-1, 0); }
        return Vector2Int.zero;
    }

    private void OnDrawGizmos()
    {
        if (internalItem != null && internalVisual == null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position + Vector3.up, 0.5f);
        }
    }
}