using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class EconomyUI : MonoBehaviour
{
    [Header("Display References")]
    public Slider rentTimerSlider;
    public Image sliderFillImage;
    public TextMeshProUGUI rentText;
    public TextMeshProUGUI cashText;

    [Header("Action Buttons")]
    // Make sure to drag the BUTTON object here
    public Button borrowButton;
    // Make sure to drag the TEXT object (child of the button) here
    public TextMeshProUGUI borrowText;

    public Button repayButton;
    public TextMeshProUGUI repayText;

    private void Start()
    {
        if (EconomyManager.Instance != null)
        {
            EconomyManager.Instance.OnEconomyUpdated += UpdateUI;
            UpdateUI();
        }
    }

    private void OnDestroy()
    {
        if (EconomyManager.Instance != null)
            EconomyManager.Instance.OnEconomyUpdated -= UpdateUI;
    }

    private void UpdateUI()
    {
        if (EconomyManager.Instance == null) return;
        var econ = EconomyManager.Instance;

        // 1. Basic Stats
        if (cashText != null)
            cashText.text = $"${econ.availableCash:N0}";

        if (rentText != null)
        {
            if (econ.isBusted) rentText.text = "BUSTED";
            else if (econ.isLevelWon) rentText.text = "SECURED";
            else rentText.text = $"Rent: ${econ.currentRent:N0}\nDue: {econ.timeUntilRentDue:F1}s";
        }

        if (rentTimerSlider != null)
        {
            float pct = econ.timeBetweenRent > 0 ? econ.timeUntilRentDue / econ.timeBetweenRent : 0;
            rentTimerSlider.value = pct;

            if (sliderFillImage != null)
                sliderFillImage.color = Color.Lerp(Color.red, Color.green, pct);
        }

        // 2. Dynamic Loan Buttons

        // --- BORROW BUTTON ---
        float loanAmt = econ.GetNextLoanAmount();
        float rentHit = econ.GetNextRentPenalty();

        // Null Check added here to prevent crash
        if (borrowText != null)
        {
            borrowText.text = $"Loan #{econ.activeLoans + 1}\n<color=#00FF00>+${loanAmt:N0}</color>\n<size=70%><color=#FF0000>(Rent +${rentHit:N0})</color></size>";
        }

        // --- REPAY BUTTON ---
        bool canRepay = econ.activeLoans > 0;

        if (canRepay)
        {
            float repayCost = econ.GetRepayCost();
            float rentRelief = econ.GetRepayRentRelief();
            bool hasCash = econ.availableCash >= repayCost;

            // Null Check added to prevent crash at line 86
            if (repayButton != null)
            {
                repayButton.interactable = hasCash;
            }

            if (repayText != null)
            {
                string color = hasCash ? "#FFFFFF" : "#555555";
                repayText.text = $"Repay #{econ.activeLoans}\n<color={color}>-${repayCost:N0}</color>\n<size=70%><color=#00FF00>(Rent -${rentRelief:N0})</color></size>";
            }
        }
        else
        {
            // Disable button if no debt
            if (repayButton != null) repayButton.interactable = false;
            if (repayText != null) repayText.text = "No Debt";
        }
    }

    // Connect these to the buttons in Inspector
    public void OnClick_Borrow() => EconomyManager.Instance?.TakeLoan();
    public void OnClick_Repay() => EconomyManager.Instance?.RepayLoan();
}