using UnityEngine;
using System.Collections.Generic;

public class PlayerInventory : MonoBehaviour
{
    public static PlayerInventory instance;

    [Header("Status")]
    public bool isHoldingPot = false;
    
    // THE MISSING LINE causing your error:
    public PortablePot heldPot; 

    [Header("Knowledge")]
    public List<Recipe> unlockedRecipes = new List<Recipe>();

    void Awake()
    {
        // Singleton pattern to access this from anywhere
        if (instance == null) instance = this;
        else Destroy(gameObject);
    }

    public void LearnRecipe(Recipe newRecipe)
    {
        if (!unlockedRecipes.Contains(newRecipe))
        {
            unlockedRecipes.Add(newRecipe);
            Debug.Log("Learned: " + newRecipe.recipeName);
        }
    }

    public void PickUpPot(PortablePot pot)
    {
        isHoldingPot = true;
        heldPot = pot;
        Debug.Log("Picked up Pot");
        // Optional: Enable a visual pot in the player's hand here
    }

    public void DropPot()
    {
        isHoldingPot = false;
        heldPot = null;
        Debug.Log("Dropped Pot");
    }
}