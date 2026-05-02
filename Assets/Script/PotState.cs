using UnityEngine;
using System.Collections.Generic;

public class PotState : MonoBehaviour
{
    [Header("Pot Status")]
    public bool isLoaded = false; // Becomes true ONLY when all ingredients are added
    public Recipe activeRecipe; 
    public GameObject finishedDishPrefab; 

    [Header("Physical Assembly")]
    // Tracks the exact names of the ingredients the player has thrown in
    public List<string> ingredientsInside = new List<string>();

    // --- STEP 1: UI TELLS THE POT WHAT WE ARE MAKING ---
    public void BeginAssemblyPhase(Recipe selectedRecipe)
    {
        activeRecipe = selectedRecipe;
        finishedDishPrefab = selectedRecipe.finishedDishPrefab;
        ingredientsInside.Clear(); // Empty out any old ingredients
        isLoaded = false;
        
        Debug.Log("POT READY: Waiting for " + selectedRecipe.requiredIngredients.Count + " ingredients to make " + selectedRecipe.recipeName);
        
        // (Next we will add the code here to turn on the floating Holographic UI slots!)
    }

    // --- STEP 2: PLAYER PHYSICALLY DROPS AN ITEM IN ---
    public bool TryAddIngredient(ItemDetails ingredientItem)
    {
        // 1. Did they select a recipe first?
        if (activeRecipe == null)
        {
            Debug.Log("You need to select a recipe at the Prep Station first!");
            return false;
        }

        // 2. Is this ingredient actually part of the recipe?
        // (This checks if the name, like "Palm Oil", is in the Recipe's required list)
        if (activeRecipe.requiredIngredients.Contains(ingredientItem.itemName))
        {
            // 3. Have we already added this specific item?
            if (!ingredientsInside.Contains(ingredientItem.itemName))
            {
                ingredientsInside.Add(ingredientItem.itemName);
                Debug.Log("Success! Dropped " + ingredientItem.itemName + " into the pot.");

                // 4. CHECK FOR COMPLETION! Did we just add the final ingredient?
                if (ingredientsInside.Count >= activeRecipe.requiredIngredients.Count)
                {
                    Debug.Log("SUCCESS! All ingredients added. Fire is hot, ready to cook!");
                    isLoaded = true; // The oven will now recognize it's ready to cook
                }

                return true; // Item was successfully accepted
            }
            else
            {
                Debug.Log("You already added " + ingredientItem.itemName + " to the pot!");
                return false;
            }
        }
        else
        {
            Debug.Log("Wrong ingredient! " + ingredientItem.itemName + " does not belong in " + activeRecipe.recipeName + "!");
            return false;
        }
    }

    // --- STEP 3: RESET AFTER COOKING ---
    public void EmptyPot()
    {
        isLoaded = false;
        activeRecipe = null;
        finishedDishPrefab = null;
        ingredientsInside.Clear();
    }
}