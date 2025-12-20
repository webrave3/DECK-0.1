using UnityEngine;

public class WhoEnabledMe : MonoBehaviour
{
    private void OnEnable()
    {
        // This prints a "Stack Trace" showing exactly which script enabled this object
        Debug.LogError($"🚨 CAUGHT YOU! '{gameObject.name}' was just enabled by:");
        Debug.Log(System.Environment.StackTrace);
    }
}