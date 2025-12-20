using UnityEngine;
using System;

public class EconomyManager : MonoBehaviour
{
    public static EconomyManager Instance { get; private set; }

    [Header("Heist Settings")]
    public float creditLimit = 5000f; // Max Debt before Game Over
    public float interestRate = 0.05f; // 5% Interest per MINUTE

    [Header("Current State")]
    public float currentDebt = 0f;
    public float availableCash = 0f; // Liquid profit
    public bool isBusted = false;

    public event Action OnEconomyUpdated;

    private float logTimer = 0f;

    private void Awake()
    {
        Instance = this;
    }

    private void Update()
    {
        // Real-time Debt Accumulation
        if (currentDebt > 0 && !isBusted)
        {
            // Interest Rate is per minute, so we divide by 60
            float interestPerSecond = (currentDebt * interestRate) / 60f;
            currentDebt += interestPerSecond * Time.deltaTime;

            CheckBustState();
            OnEconomyUpdated?.Invoke();

            // Log throttling (Every 10 seconds)
            logTimer += Time.deltaTime;
            if (logTimer >= 10f)
            {
                logTimer = 0f;
                GameLogger.Log($"[Economy] Debt: ${currentDebt:F0} (Growing by ${interestPerSecond * 10f:F1}/10s)");
            }
        }
    }

    // Check if we have enough Buying Power (Cash + Remaining Credit)
    public bool CanBuild(float cost)
    {
        if (isBusted) return false;
        float purchasingPower = availableCash + (creditLimit - currentDebt);
        return cost <= purchasingPower;
    }

    // Spending Logic: Use Cash first, then Debt
    public void SpendMoney(float amount)
    {
        if (availableCash >= amount)
        {
            availableCash -= amount;
        }
        else
        {
            float remainder = amount - availableCash;
            availableCash = 0;
            currentDebt += remainder;
        }
        CheckBustState();
        OnEconomyUpdated?.Invoke();
    }

    // Earning Logic: Pay Debt first, then store Cash
    public void EarnMoney(float amount)
    {
        if (currentDebt > 0)
        {
            float payment = Mathf.Min(currentDebt, amount);
            currentDebt -= payment;
            amount -= payment;
        }

        if (amount > 0)
        {
            availableCash += amount;
        }

        CheckBustState();
        OnEconomyUpdated?.Invoke();
    }

    public void Refund(float amount)
    {
        EarnMoney(amount); // Refunds behave exactly like earning money
    }

    private void CheckBustState()
    {
        bool wasBusted = isBusted;
        isBusted = currentDebt >= creditLimit;

        if (isBusted != wasBusted)
        {
            if (isBusted) GameLogger.Log("BUSTED! Construction Halted.");
            else GameLogger.Log("Credit Restored.");
        }
    }
}