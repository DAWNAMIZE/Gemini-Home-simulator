using UnityEngine;

public class RecipePickup : MonoBehaviour, IInteractable
{
    [Header("What does this teach?")]
    public Recipe recipeToLearn; 
    
    public void Interact()
    {
        if (recipeToLearn == null) return;

        if (PlayerInventory.instance != null)
        {
            // Teach the player
            PlayerInventory.instance.LearnRecipe(recipeToLearn);
            
            // Destroy this object so you can't pick it up twice
            Destroy(gameObject);
        }
    }
}