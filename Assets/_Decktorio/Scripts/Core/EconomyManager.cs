using UnityEngine;
using System;

public class EconomyManager : MonoBehaviour
{
    public static EconomyManager Instance { get; private set; }

    [Header("Heist Settings")]
    public float creditLimit = 5000f; // Game Over if Debt > this
    public float interestRate = 0.05f; // 5% Interest
    public float interestInterval = 10f; // Apply every 10s

    [Header("Current State")]
    public float currentDebt = 0f;
    public float liquidCash = 0f; // "Profit" you have earned after paying debt
    public bool isBusted = false;

    // Events for UI
    public event Action OnEconomyUpdated;

    private void Awake()
    {
        Instance = this;
        // Start the Interest Timer
        InvokeRepeating(nameof(ApplyInterest), interestInterval, interestInterval);
    }

    public bool CanBuild(float cost)
    {
        if (isBusted) return false;
        // You can build if adding the debt keeps you under the limit
        return (currentDebt + cost) <= creditLimit;
    }

    public void AddDebt(float cost)
    {
        currentDebt += cost;
        CheckBustState();
        OnEconomyUpdated?.Invoke();
    }

    public void ProcessPayout(float amount)
    {
        // 1. Pay off Debt first
        if (currentDebt > 0)
        {
            float payment = Mathf.Min(currentDebt, amount);
            currentDebt -= payment;
            amount -= payment; // Remaining cash
        }

        // 2. Keep the rest as Liquid Cash (Profit)
        if (amount > 0)
        {
            liquidCash += amount;
        }

        CheckBustState();
        OnEconomyUpdated?.Invoke();
    }

    private void ApplyInterest()
    {
        if (currentDebt > 0 && !isBusted)
        {
            // The Vig: Interest grows based on current debt
            float interest = currentDebt * interestRate;
            currentDebt += interest;

            GameLogger.Log($"[Economy] Interest Applied: +${interest:F0}");
            CheckBustState();
            OnEconomyUpdated?.Invoke();
        }
    }

    private void CheckBustState()
    {
        bool wasBusted = isBusted;
        isBusted = currentDebt >= creditLimit;

        if (isBusted && !wasBusted)
        {
            GameLogger.Log("BUSTED! Credit Limit Exceeded. Construction Halted.");
            // Trigger Game Over Warning UI here
        }
        else if (!isBusted && wasBusted)
        {
            GameLogger.Log("Credit Restored. Back in business.");
        }
    }
}