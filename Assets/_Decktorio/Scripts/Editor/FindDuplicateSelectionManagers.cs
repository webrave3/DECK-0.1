using UnityEngine;
using UnityEditor;

public class FindDuplicateSelectionManagers : MonoBehaviour
{
    [MenuItem("Tools/Find Duplicate SelectionManagers")]
    public static void FindThem()
    {
        // Find ALL SelectionManagers, including inactive ones
        SelectionManager[] allManagers = Resources.FindObjectsOfTypeAll<SelectionManager>();

        int count = 0;
        foreach (var sm in allManagers)
        {
            // Filter out assets in the project folder (prefabs), we only want Scene objects
            if (EditorUtility.IsPersistent(sm.transform.root.gameObject)) continue;

            // Filter out HideAndDontSave objects (internal Unity stuff)
            if ((sm.gameObject.hideFlags & HideFlags.HideAndDontSave) != 0) continue;

            count++;
            Debug.LogError($"🚨 FOUND SelectionManager #{count} on GameObject: '{sm.gameObject.name}'", sm.gameObject);
        }

        if (count == 0)
        {
            Debug.Log("No SelectionManagers found in the scene.");
        }
        else if (count == 1)
        {
            Debug.Log($"✅ Only 1 SelectionManager found. (If you still have the bug, check if this object has TWO scripts attached!)");
        }
        else
        {
            Debug.LogError($"🚨 CRITICAL: Found {count} SelectionManagers. You must DELETE all but one.");
        }
    }
}