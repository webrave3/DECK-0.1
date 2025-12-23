using UnityEngine;
using System;

public class EconomyManager : MonoBehaviour
{
    public static EconomyManager Instance { get; private set; }

    [Header("Resources")]
    public float availableCash = 500f;

    [Header("The Blind (Rent)")]
    public float currentRent = 100f;
    public float timeBetweenRent = 60f;
    public float timeUntilRentDue;
    public int rentCyclesPaid = 0;

    [Header("Rent Scaling")]
    [Tooltip("Rent grows by this multiplier every cycle (1.15 = 15%)")]
    public float rentMultiplier = 1.15f;
    [Tooltip("Flat rent added every cycle")]
    public float rentBaseIncrease = 50f;

    [Header("The Loan Shark (Settings)")]
    [Tooltip("Base Cash for the first loan")]
    public float baseLoanPrincipal = 1000f;
    [Tooltip("How much the loan size grows per ACTIVE loan (1.5 = +50% bigger each stack)")]
    public float loanStackingMultiplier = 1.5f;
    [Tooltip("Base Rent Penalty for the first loan")]
    public float baseRentPenalty = 150f;
    [Tooltip("Multiplier for Rent Penalty per ACTIVE loan")]
    public float penaltyStackingMultiplier = 1.5f;

    [Header("Repayment Scars")]
    [Tooltip("How much interest you pay instantly (1.2 = pay back 120%)")]
    public float repaymentInterest = 1.25f;
    [Tooltip("How much of the Rent Penalty is removed? (0.8 = 20% scar remains forever)")]
    public float rentReliefRatio = 0.8f;

    [Header("State")]
    public int activeLoans = 0;
    public bool isBusted = false;
    public bool isLevelWon = false;

    public event Action OnEconomyUpdated;

    private void Awake()
    {
        if (Instance != null && Instance != this) Destroy(this);
        else Instance = this;

        timeUntilRentDue = timeBetweenRent;
    }

    private void Start()
    {
        if (HeistManager.Instance != null)
            HeistManager.Instance.OnLevelComplete += HandleLevelWon;

        OnEconomyUpdated?.Invoke();
    }

    private void OnDestroy()
    {
        if (HeistManager.Instance != null)
            HeistManager.Instance.OnLevelComplete -= HandleLevelWon;
    }

    private void Update()
    {
        if (!isBusted && !isLevelWon)
        {
            timeUntilRentDue -= Time.deltaTime;
            if (timeUntilRentDue <= 0f) PayRentOrBust();
            OnEconomyUpdated?.Invoke();
        }
    }

    // --- RENT LOGIC ---
    private void PayRentOrBust()
    {
        if (availableCash >= currentRent)
        {
            availableCash -= currentRent;
            rentCyclesPaid++;
            GameLogger.Log($"Rent Paid: ${currentRent:N0}. The price of business goes up...");

            // Apply Exponential Inflation to the Rent
            currentRent = (currentRent + rentBaseIncrease) * rentMultiplier;
            timeUntilRentDue = timeBetweenRent;
        }
        else
        {
            isBusted = true;
            availableCash = 0;
            GameLogger.Log("BUSTED! The House always wins.");
        }
        OnEconomyUpdated?.Invoke();
    }

    private void HandleLevelWon()
    {
        isLevelWon = true;
        OnEconomyUpdated?.Invoke();
    }

    // --- DYNAMIC LOAN LOGIC (The Shark) ---

    // 1. Calculate the CASH size of the NEXT loan (Index = activeLoans)
    public float GetNextLoanAmount()
    {
        // Formula: Base * (1.5 ^ ActiveLoans) * (1.1 ^ CyclesPaid)
        // This ensures loans get bigger if you stack them, AND get bigger as the game goes on.
        float stackMult = Mathf.Pow(loanStackingMultiplier, activeLoans);
        float inflationMult = Mathf.Pow(1.1f, rentCyclesPaid);

        return baseLoanPrincipal * stackMult * inflationMult;
    }

    // 2. Calculate the RENT PENALTY of the NEXT loan
    public float GetNextRentPenalty()
    {
        float stackMult = Mathf.Pow(penaltyStackingMultiplier, activeLoans);
        float inflationMult = Mathf.Pow(1.1f, rentCyclesPaid);

        return baseRentPenalty * stackMult * inflationMult;
    }

    // 3. Calculate Cost to Repay the LATEST loan (LIFO)
    public float GetRepayCost()
    {
        if (activeLoans == 0) return 0;

        // We simulate the loan we are about to pay off (index = activeLoans - 1)
        float previousIndex = activeLoans - 1;
        float stackMult = Mathf.Pow(loanStackingMultiplier, previousIndex);
        float inflationMult = Mathf.Pow(1.1f, rentCyclesPaid);
        float principal = baseLoanPrincipal * stackMult * inflationMult;

        return principal * repaymentInterest;
    }

    // 4. Calculate how much Rent goes down (The Scar Logic)
    public float GetRepayRentRelief()
    {
        if (activeLoans == 0) return 0;

        float previousIndex = activeLoans - 1;
        float stackMult = Mathf.Pow(penaltyStackingMultiplier, previousIndex);
        float inflationMult = Mathf.Pow(1.1f, rentCyclesPaid);
        float originalPenalty = baseRentPenalty * stackMult * inflationMult;

        // CRITICAL: We only return a percentage. The rest stays as a permanent scar.
        return originalPenalty * rentReliefRatio;
    }

    // --- ACTIONS ---

    public void TakeLoan()
    {
        if (isBusted || isLevelWon) return;

        float cash = GetNextLoanAmount();
        float penalty = GetNextRentPenalty();

        availableCash += cash;
        currentRent += penalty;
        activeLoans++;

        GameLogger.Log($"Loan #{activeLoans} Taken: +${cash:N0}. Rent spiked by +${penalty:N0}.");
        OnEconomyUpdated?.Invoke();
    }

    public void RepayLoan()
    {
        if (isBusted || isLevelWon) return;
        if (activeLoans <= 0) return;

        float cost = GetRepayCost();

        if (availableCash >= cost)
        {
            float relief = GetRepayRentRelief();

            availableCash -= cost;
            currentRent -= relief;
            if (currentRent < 0) currentRent = 0;

            activeLoans--;

            GameLogger.Log($"Loan Repaid: -${cost:N0}. Rent lowered by ${relief:N0}. (Scar left behind)");
            OnEconomyUpdated?.Invoke();
        }
        else
        {
            GameLogger.Log($"Not enough cash. Need ${cost:N0} to repay Loan #{activeLoans}.");
        }
    }

    // --- STANDARD ECONOMY ---
    public bool CanBuild(float cost) => !isBusted && !isLevelWon && availableCash >= cost;
    public void SpendMoney(float amount) { if (availableCash >= amount) { availableCash -= amount; OnEconomyUpdated?.Invoke(); } }
    public void EarnMoney(float amount) { if (!isBusted) { availableCash += amount; OnEconomyUpdated?.Invoke(); } }
    public void Refund(float amount) => EarnMoney(amount);
}