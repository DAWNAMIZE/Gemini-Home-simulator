using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PortablePot : MonoBehaviour, IInteractable 
{
    [Header("Data")]
    public Recipe currentDish;
    public CookingState state = CookingState.Empty;
    public List<string> addedIngredients = new List<string>();

    [Header("Visuals")]
    public MeshRenderer foodMesh;
    public GameObject steamParticles;

    [Header("Floating UI")]
    public GameObject potUICanvas; 
    public Image[] ingredientSlots; 
    
    // NEW: We need references to your buttons!
    public Button[] addButtons; 
    public Button cookButton;   
    
    public Color emptyColor = new Color(0.2f, 0.2f, 0.2f, 0.8f); // Dark Grey

    public enum CookingState { Empty, Prepping, Cooking, Cooked, Burnt }

    public void SetupPot(Recipe dish)
    {
        currentDish = dish;
        state = CookingState.Prepping;
        addedIngredients.Clear(); 
        
        // Turn on visuals and UI
        if (foodMesh != null) foodMesh.enabled = true; 
        if (steamParticles) steamParticles.SetActive(false);
        if (potUICanvas != null) potUICanvas.SetActive(true);

        // Hide the cook button until all ingredients are added
        if (cookButton != null) cookButton.gameObject.SetActive(false);

        // Set up the slots based on the recipe
        for (int i = 0; i < ingredientSlots.Length; i++)
        {
            if (i < dish.requiredIngredients.Count)
            {
                ingredientSlots[i].gameObject.SetActive(true);

                // --- NEW: Grab the picture from the recipe's icon list! ---
                if (dish.ingredientIcons != null && i < dish.ingredientIcons.Count && dish.ingredientIcons[i] != null)
                {
                    ingredientSlots[i].sprite = dish.ingredientIcons[i];
                    // Make it semi-transparent white so the player knows they still need to add it
                    ingredientSlots[i].color = new Color(1f, 1f, 1f, 0.4f); 
                }
                else
                {
                    // Fallback if you forgot to add a picture to the list
                    ingredientSlots[i].sprite = null;
                    ingredientSlots[i].color = emptyColor; 
                }

                // Setup the Add Button
                string requiredIngredientName = dish.requiredIngredients[i];
                int slotIndex = i; 

                if (addButtons != null && i < addButtons.Length && addButtons[i] != null)
                {
                    addButtons[i].gameObject.SetActive(true);
                    addButtons[i].onClick.RemoveAllListeners();
                    
                    // Notice we pass the name and the slot index to the button!
                    addButtons[i].onClick.AddListener(() => TryAddFromUI(requiredIngredientName, slotIndex));
                }
            }
            else 
            {
                // Hide extra slots if the recipe doesn't need them
                ingredientSlots[i].gameObject.SetActive(false);
                if (addButtons != null && i < addButtons.Length && addButtons[i] != null)
                {
                    addButtons[i].gameObject.SetActive(false);
                }
            }
        }

        Debug.Log("Pot is ready! Click the Add buttons to prep: " + dish.recipeName);
    }

    // TRIGGERED BY THE "ADD" UI BUTTONS
    public void TryAddFromUI(string ingredientNeeded, int slotIndex)
    {
        // Ask the inventory if we have the item. If yes, it deletes it from the bag!
        if (PlayerInventory.instance.ConsumeItemFromBackpack(ingredientNeeded))
        {
            addedIngredients.Add(ingredientNeeded);
            
            // --- THE VISUAL MAGIC ---
            // Remove the transparency so the picture looks solid/collected!
            ingredientSlots[slotIndex].color = Color.white; 
            // ------------------------
            
            // Hide this Add button so the player can't click it again
            if (addButtons != null && slotIndex < addButtons.Length && addButtons[slotIndex] != null)
            {
                addButtons[slotIndex].gameObject.SetActive(false);
            }
            
            Debug.Log("Success! Added " + ingredientNeeded + " to the pot!");

            // Check if we have added everything
            if (addedIngredients.Count >= currentDish.requiredIngredients.Count)
            {
                Debug.Log("All ingredients added! Ready to cook!");
                if (cookButton != null) cookButton.gameObject.SetActive(true); 
            }
        }
        else
        {
            Debug.Log("You do not have " + ingredientNeeded + " in your backpack!");
        }
    }

    // TRIGGERED BY THE "COOK" UI BUTTON
    public void OnCookButtonClicked()
    {
        state = CookingState.Cooking;
        
        if (steamParticles) steamParticles.SetActive(true);
        if (potUICanvas != null) potUICanvas.SetActive(false); // Hide the whole UI while cooking
        
        // For testing, we finish cooking instantly. Later you can add a timer here!
        FinishCooking(); 
    }

    public void Interact()
    {
        // 1. EAT LOGIC
        if (state == CookingState.Cooked)
        {
            PlayerVitals player = FindFirstObjectByType<PlayerVitals>();
            if(player != null && currentDish != null) player.Eat(currentDish.hungerRestore);
            
            state = CookingState.Empty;
            currentDish = null;
            if (foodMesh != null) foodMesh.enabled = false;
            return;
        }

        // 2. PICK UP EMPTY POT LOGIC
        if (PlayerInventory.instance != null && PlayerInventory.instance.currentlyHeldModel == null && state == CookingState.Empty)
        {
            PlayerInventory.instance.HoldPhysicalItem(this.gameObject);
        }
    }

    private void FinishCooking()
    {
        state = CookingState.Cooked;
        if (steamParticles) steamParticles.SetActive(false);
        
        if (currentDish.finishedDishPrefab != null)
        {
            // Spawn the final bowl of soup!
            Instantiate(currentDish.finishedDishPrefab, transform.position, transform.rotation);
            
            // Destroy the empty cooking pot
            Destroy(gameObject); 
        }
    }
}