using UnityEngine;

public class RecipePickup : MonoBehaviour, IInteractable
{
    [Header("What does this teach?")]
    public Recipe recipeToLearn; // Drag the "Jollof Rice" asset here
    
    public void Interact()
    {
        // 1. Teach the player
        PlayerInventory.instance.LearnRecipe(recipeToLearn);

        // 2. Feedback
        Debug.Log("You found the recipe for " + recipeToLearn.recipeName + "!");
        
        // 3. Destroy this object (so you can't pick it up twice)
        Destroy(gameObject);
    }
}