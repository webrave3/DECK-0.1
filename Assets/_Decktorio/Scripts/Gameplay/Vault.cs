using UnityEngine;

public class Vault : BuildingBase
{
    protected override void OnTick(int tick)
    {
        // The Vault consumes items immediately
        if (internalItem != null)
        {
            ProcessDamage();
        }
    }

    private void ProcessDamage()
    {
        float totalDamage = 0f;
        float totalHeat = 0f;

        foreach (var card in internalItem.contents)
        {
            float cardValue = card.rank;

            // Multiplier for Composite Suits (More flags = Higher Multiplier)
            int suitCount = CountSetBits((int)card.suit);
            if (suitCount > 1) cardValue *= (suitCount * 1.5f);

            // Multiplier for Materials
            cardValue *= card.GetValueMultiplier();

            totalDamage += cardValue;
            totalHeat += card.heat;
        }

        // LOGGING (Later this will link to HeistManager)
        Debug.Log($"<color=red>VAULT HIT!</color> Damage: {totalDamage} | Heat Absorbed: {totalHeat}");

        // If HeistManager exists, report it
        if (HeistManager.Instance != null)
        {
            // HeistManager.Instance.DealDamage(totalDamage);
            // HeistManager.Instance.AddHeat(totalHeat);
        }

        // Destroy
        Destroy(internalVisual.gameObject);
        internalItem = null;
        internalVisual = null;
    }

    // Helper to count bits (How many suits?)
    int CountSetBits(int n)
    {
        int count = 0;
        while (n > 0)
        {
            n &= (n - 1);
            count++;
        }
        return count;
    }
}