using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class SceneController : MonoBehaviour
{
    public static SceneController Instance { get; private set; }

    [Header("Configuration")]
    public float fadeDuration = 0.5f;

    // Use specific build indices or names for safety
    public const int BOOT_SCENE_INDEX = 0;
    public const int MENU_SCENE_INDEX = 1;
    public const int GAME_SCENE_INDEX = 2;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public void LoadMainMenu()
    {
        StartCoroutine(LoadSceneRoutine(MENU_SCENE_INDEX));
    }

    public void LoadGameplay()
    {
        StartCoroutine(LoadSceneRoutine(GAME_SCENE_INDEX));
    }

    private IEnumerator LoadSceneRoutine(int sceneIndex)
    {
        // Optional: Add Fade Out animation trigger here later
        // yield return new WaitForSeconds(fadeDuration);

        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(sceneIndex);

        // Wait until the asynchronous scene fully loads
        while (!asyncLoad.isDone)
        {
            yield return null;
        }

        // Optional: Add Fade In animation trigger here
    }
}