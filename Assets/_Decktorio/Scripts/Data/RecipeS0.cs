using UnityEngine;

[CreateAssetMenu(fileName = "NewRecipe", menuName = "Decktorio/Recipe")]
public class RecipeSO : ScriptableObject
{
    [Header("Input Requirements")]
    // If true, the input card must match this suit. If 'None', matches any.
    public CardSuit requiredSuit = CardSuit.None;
    // If > 0, the input card must match this rank.
    public int requiredRank = 0;
    public CardMaterial requiredMaterial = CardMaterial.Cardstock;

    [Header("Operation Settings")]
    public float processingTime = 2.0f;

    // Cost to process (Future-proofing for Economy)
    public int moneyCost = 0;

    [Header("Output Result")]
    // What changes? 
    public bool changeSuit;
    public CardSuit targetSuit;

    public bool changeRank;
    public int targetRank;

    public bool changeMaterial;
    public CardMaterial targetMaterial;

    public bool changeInk;
    public CardInk targetInk;

    // Helper: Checks if a card matches this recipe's input requirements
    public bool IsMatch(CardData inputCard)
    {
        if (requiredSuit != CardSuit.None && inputCard.suit != requiredSuit) return false;
        if (requiredRank > 0 && inputCard.rank != requiredRank) return false;
        if (inputCard.material != requiredMaterial) return false;

        return true;
    }

    // Helper: Produces the new card data based on the input
    public CardData Process(CardData inputCard)
    {
        CardData result = inputCard;

        if (changeSuit) result.suit = targetSuit;
        if (changeRank) result.rank = targetRank;
        if (changeMaterial) result.material = targetMaterial;
        if (changeInk) result.ink = targetInk;

        return result;
    }
}