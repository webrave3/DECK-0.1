using UnityEngine;

public class ConveyorBelt : BuildingBase
{
    [Header("Visuals")]
    public float itemHeightOffset = 0.2f;
    public float forwardVisualOffset = 0.0f;

    [Header("Gameplay")]
    public float speedModifier = 1.0f;

    private Transform visualStraight;
    private Transform visualLeft;
    private Transform visualRight;

    protected override void Start()
    {
        base.Start();

        // --- SAFETY REGISTRATION ---
        Vector2Int truePos = CasinoGridManager.Instance.WorldToGrid(transform.position);
        if (CasinoGridManager.Instance.GetBuildingAt(truePos) != this)
        {
            Setup(truePos);
            // FIXED: Arguments swapped to (Position, Building)
            CasinoGridManager.Instance.RegisterBuilding(truePos, this);
        }
        // ---------------------------

        visualStraight = transform.Find("Visual_Straight");
        visualLeft = transform.Find("Visual_Corner_Left");
        visualRight = transform.Find("Visual_Corner_Right");
        UpdateVisuals();
    }

    public void UpdateVisuals()
    {
        bool inputLeft = false;
        bool inputRight = false;
        bool inputBack = false;

        CheckNeighbor(GetBackGridPosition(), ref inputBack);
        CheckNeighbor(GetLeftGridPosition(), ref inputLeft);
        CheckNeighbor(GetRightGridPosition(), ref inputRight);

        if (visualStraight) visualStraight.gameObject.SetActive(false);
        if (visualLeft) visualLeft.gameObject.SetActive(false);
        if (visualRight) visualRight.gameObject.SetActive(false);

        if (inputLeft && !inputBack)
        {
            if (visualLeft) visualLeft.gameObject.SetActive(true);
            else if (visualStraight) visualStraight.gameObject.SetActive(true);
        }
        else if (inputRight && !inputBack)
        {
            if (visualRight) visualRight.gameObject.SetActive(true);
            else if (visualStraight) visualStraight.gameObject.SetActive(true);
        }
        else
        {
            if (visualStraight) visualStraight.gameObject.SetActive(true);
        }
    }

    private void CheckNeighbor(Vector2Int pos, ref bool hasInput)
    {
        BuildingBase b = CasinoGridManager.Instance.GetBuildingAt(pos);
        if (b != null && b is ConveyorBelt && b.GetForwardGridPosition() == GridPosition)
            hasInput = true;
    }

    protected override void OnTick(int tick)
    {
        if (internalItem != null) TryPushItem();
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

    public override bool CanAcceptItem(Vector2Int fromPos)
    {
        if (incomingItem != null) return false;
        if (internalItem != null) return false;
        return true;
    }

    protected override void OnItemArrived()
    {
        if (internalVisual != null)
        {
            internalVisual.transform.SetParent(this.transform);
            Vector3 targetPos = transform.position + (Vector3.up * itemHeightOffset);
            if (forwardVisualOffset != 0)
            {
                Vector3 worldForward = (CasinoGridManager.Instance.GridToWorld(GetForwardGridPosition()) - transform.position).normalized;
                targetPos += worldForward * forwardVisualOffset;
            }
            internalVisual.InitializeMovement(targetPos, TickManager.Instance.tickRate);
        }
    }

    public Vector2Int GetLeftGridPosition() => GridPosition + GetDirFromIndex((RotationIndex + 3) % 4);
    public Vector2Int GetRightGridPosition() => GridPosition + GetDirFromIndex((RotationIndex + 1) % 4);
    public Vector2Int GetBackGridPosition() => GridPosition + GetDirFromIndex((RotationIndex + 2) % 4);

    public static Vector2Int GetDirFromIndex(int index)
    {
        switch (index)
        {
            case 0: return new Vector2Int(0, 1);
            case 1: return new Vector2Int(1, 0);
            case 2: return new Vector2Int(0, -1);
            case 3: return new Vector2Int(-1, 0);
        }
        return Vector2Int.zero;
    }
}