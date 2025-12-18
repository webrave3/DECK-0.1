using UnityEngine;

// This is a passive building that just exists on the map.
// The Unpacker will look for this underneath itself.
public class SupplyDrop : BuildingBase
{
    [Header("Resource Settings")]
    public CardPayload resourceType; // e.g., Blank Card

    protected override void HandleTick(int tick)
    {
        // Supply Drops do nothing on their own. 
        // They are just data containers for the Unpacker to read.
    }

    // Supply drops cannot accept items
    public override bool CanAcceptItem(Vector2Int fromPos) => false;
}