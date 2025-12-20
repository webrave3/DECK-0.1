using UnityEngine;

public class ConveyorBelt : BuildingBase
{
    [Header("Visuals")]
    public float itemHeightOffset = 0.15f;
    public float forwardVisualOffset = 0.5f;

    [Header("Gameplay")]
    public float speedModifier = 1.0f;

    private Transform visualStraight;
    private Transform visualLeft;
    private Transform visualRight;

    protected override void Start()
    {
        base.Start();
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

        // Reset
        if (visualStraight) visualStraight.gameObject.SetActive(false);
        if (visualLeft) visualLeft.gameObject.SetActive(false);
        if (visualRight) visualRight.gameObject.SetActive(false);

        // Simple Corner Logic (Straight is default fallback)
        if (inputLeft)
        {
            if (visualLeft) visualLeft.gameObject.SetActive(true);
        }
        else if (inputRight)
        {
            if (visualRight) visualRight.gameObject.SetActive(true);
        }
        else
        {
            if (visualStraight) visualStraight.gameObject.SetActive(true);
        }
    }

    private void CheckNeighbor(Vector2Int pos, ref bool hasInput)
    {
        BuildingBase b = CasinoGridManager.Instance.GetBuildingAt(pos);
        if (b != null && b is ConveyorBelt && b.GetForwardGridPosition() == GridPosition) hasInput = true;
    }

    protected override void OnTick(int tick)
    {
        if (internalItem != null) TryPushItem();
    }

    private void TryPushItem()
    {
        if (AttemptPush(GetForwardGridPosition())) return;
    }

    private bool AttemptPush(Vector2Int targetPos)
    {
        BuildingBase target = CasinoGridManager.Instance.GetBuildingAt(targetPos);
        if (target != null && target.CanAcceptItem(GridPosition))
        {
            internalItem.velocityBonus = this.speedModifier;
            target.ReceiveItem(internalItem, internalVisual);
            internalItem = null;
            internalVisual = null;
            return true;
        }
        return false;
    }

    public override bool CanAcceptItem(Vector2Int fromPos)
    {
        if (internalItem != null || incomingItem != null) return false;
        if (fromPos == GetForwardGridPosition()) return false;
        return true;
    }

    public override bool CanBePlacedAt(Vector2Int gridPos)
    {
        if (CasinoGridManager.Instance.GetResourceAt(gridPos) != null) return false;
        return base.CanBePlacedAt(gridPos);
    }

    protected override void OnItemArrived()
    {
        if (internalVisual != null)
        {
            internalVisual.transform.SetParent(this.transform);
            Vector3 worldForward = (CasinoGridManager.Instance.GridToWorld(GetForwardGridPosition()) - transform.position).normalized;
            Vector3 targetPos = transform.position + (Vector3.up * itemHeightOffset) + (worldForward * forwardVisualOffset);
            float duration = TickManager.Instance.tickRate;
            internalVisual.InitializeMovement(targetPos, duration);
        }
    }

    // Helpers
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