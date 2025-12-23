using UnityEngine;
using System;

public class HeistManager : MonoBehaviour
{
    public static HeistManager Instance { get; private set; }

    [Header("Heist Objective")]
    public float vaultMaxHP = 10000f;
    public float vaultCurrentHP;
    public bool isHeistComplete = false;

    // Events for UI and Game State
    public event Action OnHeistUpdated;
    public event Action OnLevelComplete;

    private void Awake()
    {
        if (Instance != null && Instance != this) Destroy(this);
        else Instance = this;

        // Initialize HP
        vaultCurrentHP = vaultMaxHP;
    }

    private void Start()
    {
        OnHeistUpdated?.Invoke();
    }

    /// <summary>
    /// Call this method from your Turrets, Hackers, or Item Sinks to damage the vault.
    /// </summary>
    /// <param name="damageAmount">How much progress to make</param>
    public void DamageVault(float damageAmount)
    {
        if (isHeistComplete) return;

        vaultCurrentHP -= damageAmount;
        if (vaultCurrentHP <= 0f)
        {
            vaultCurrentHP = 0f;
            CompleteHeist();
        }

        OnHeistUpdated?.Invoke();
    }

    private void CompleteHeist()
    {
        isHeistComplete = true;
        GameLogger.Log("HEIST COMPLETE! Vault destroyed. You win!");

        // Notify other systems (like EconomyManager) to stop the pressure
        OnLevelComplete?.Invoke();
    }
}