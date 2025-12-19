using UnityEngine;
using System.Collections.Generic;

public class GameLogger : MonoBehaviour
{
    public static GameLogger Instance { get; private set; }

    private List<string> logs = new List<string>();
    private const int MaxLogs = 15;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        Log("GameLogger Initialized.");
    }

    public static void Log(string message)
    {
        if (Instance == null)
        {
            Debug.Log($"[GameLogger (Fallback)] {message}");
            return;
        }

        Instance.logs.Add($"[{Time.time:F1}] {message}");
        if (Instance.logs.Count > MaxLogs) Instance.logs.RemoveAt(0);
    }

    private void OnGUI()
    {
        // Ensure GUI depth is high to render on top
        GUI.depth = 0;
        GUI.skin.label.fontSize = 20;

        float y = 10;
        foreach (var log in logs)
        {
            // Shadow
            GUI.color = Color.black;
            GUI.Label(new Rect(12, y + 2, 800, 30), log);

            // Text
            GUI.color = Color.yellow;
            GUI.Label(new Rect(10, y, 800, 30), log);

            y += 25;
        }
    }
}