using UnityEngine;
using System;

public class TickManager : MonoBehaviour
{
    public static TickManager Instance { get; private set; }

    [Header("Simulation Settings")]
    [Tooltip("Time in seconds between ticks (e.g., 0.1 = 10 ticks/sec)")]
    public float tickRate = 0.1f;
    public bool isPaused = false;

    // The event that all buildings listen to
    public event Action<int> OnTick;

    private float timer;
    private int currentTick = 0;

    private void Awake()
    {
        if (Instance != null && Instance != this) Destroy(this);
        else Instance = this;
    }

    private void Update()
    {
        if (isPaused) return;

        timer += Time.deltaTime;
        if (timer >= tickRate)
        {
            timer -= tickRate;
            currentTick++;
            OnTick?.Invoke(currentTick);
        }
    }

    // Used by ItemVisualizers to move smoothly between A and B
    public float GetInterpolationFactor()
    {
        return Mathf.Clamp01(timer / tickRate);
    }
}