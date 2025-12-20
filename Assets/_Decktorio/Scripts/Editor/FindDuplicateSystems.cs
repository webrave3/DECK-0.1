using UnityEngine;
using UnityEditor;

public class FindDuplicateSystems : MonoBehaviour
{
    [MenuItem("Tools/Find Duplicate Systems")]
    public static void FindThem()
    {
        var systems = FindObjectsByType<BuildingSystem>(FindObjectsSortMode.None);

        if (systems.Length == 0)
        {
            Debug.LogError("❌ No BuildingSystem found in the scene!");
        }
        else if (systems.Length == 1)
        {
            Debug.Log($"✅ GOOD: Only 1 BuildingSystem found on: '{systems[0].gameObject.name}'");
            EditorGUIUtility.PingObject(systems[0].gameObject);
        }
        else
        {
            Debug.LogError($"🚨 FOUND {systems.Length} BUILDING SYSTEMS! THIS IS THE BUG.");
            foreach (var s in systems)
            {
                Debug.LogError($" -> Found one on GameObject: '{s.gameObject.name}'", s.gameObject);
            }
        }
    }
}