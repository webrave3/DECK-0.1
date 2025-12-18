using UnityEngine;

public class BootLoader : MonoBehaviour
{
    void Start()
    {
        // Initialize any other services here (Analytics, Audio, etc.)
        SceneController.Instance.LoadMainMenu();
    }
}