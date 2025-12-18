using UnityEngine;
using UnityEngine.UI;

public class MainMenuUI : MonoBehaviour
{
    [Header("Buttons")]
    public Button playButton;
    public Button quitButton;

    private void Start()
    {
        playButton.onClick.AddListener(OnPlayClicked);
        quitButton.onClick.AddListener(OnQuitClicked);
    }

    void OnPlayClicked()
    {
        // Play the "Slot Machine Chime" sound effect here later
        SceneController.Instance.LoadGameplay();
    }

    void OnQuitClicked()
    {
        Debug.Log("Quitting Game...");
        Application.Quit();
    }
}