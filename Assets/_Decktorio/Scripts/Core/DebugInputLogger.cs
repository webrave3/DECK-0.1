using UnityEngine;
using UnityEngine.InputSystem;

public class DebugInputLogger : MonoBehaviour
{
    void Update()
    {
        if (Mouse.current.rightButton.wasPressedThisFrame)
        {
            Debug.Log($"[FRAME {Time.frameCount}] Right Click Detected.");

            if (BuildingSystem.Instance.IsBuildingOrPasteActive())
                Debug.Log($" -> BuildingSystem is ACTIVE.");
            else
                Debug.Log($" -> BuildingSystem is INACTIVE.");

            if (BuildingSystem.Instance.LastCancelFrame == Time.frameCount)
                Debug.Log($" -> BuildingSystem CANCELLED this frame.");
        }
    }
}