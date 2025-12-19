using UnityEngine;
using System.Collections.Generic;
using System.Linq; // Needed for shuffling

public class TickManager : MonoBehaviour
{
    public static TickManager Instance { get; private set; }

    [Header("Settings")]
    public float tickRate = 0.2f; // Seconds per tick

    private float timer;
    private int currentTick = 0;

    // We use a List so we can shuffle it for fair merging
    private List<System.Action<int>> tickListeners = new List<System.Action<int>>();

    public event System.Action<int> OnTick
    {
        add { if (!tickListeners.Contains(value)) tickListeners.Add(value); }
        remove { tickListeners.Remove(value); }
    }

    private void Awake()
    {
        if (Instance != null && Instance != this) Destroy(this);
        else Instance = this;
    }

    private void Update()
    {
        timer += Time.deltaTime;
        if (timer >= tickRate)
        {
            timer -= tickRate;
            currentTick++;
            RunTick();
        }
    }

    private void RunTick()
    {
        // SHUFFLE the list to prevent "Priority Starvation"
        // This ensures Belt A and Belt B have equal chance to push to Belt C
        int n = tickListeners.Count;
        while (n > 1)
        {
            n--;
            int k = Random.Range(0, n + 1);
            var value = tickListeners[k];
            tickListeners[k] = tickListeners[n];
            tickListeners[n] = value;
        }

        // Execute all
        // Copy list to avoid errors if scripts unsubscribe during tick
        var listenersCopy = new List<System.Action<int>>(tickListeners);
        foreach (var listener in listenersCopy)
        {
            try { listener?.Invoke(currentTick); }
            catch (System.Exception e) { Debug.LogError($"Tick Error: {e}"); }
        }
    }

    public float GetInterpolationFactor()
    {
        return Mathf.Clamp01(timer / tickRate);
    }
}