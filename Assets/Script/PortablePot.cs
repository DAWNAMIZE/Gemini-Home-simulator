using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI; // NEW: Required for UI!

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
    public GameObject potUICanvas; // Drag your PotCanvas here
    public Image[] ingredientSlots; // Drag your 5 Slot images here
    public Color emptyColor = new Color(0.2f, 0.2f, 0.2f, 0.8f); // Dark Grey
    public Color filledColor = Color.green; // Turns green when added

    public enum CookingState { Empty, Prepping, Cooking, Cooked, Burnt }

    public void SetupPot(Recipe dish)
    {
        currentDish = dish;
        state = CookingState.Prepping;
        addedIngredients.Clear(); 
        
        if (foodMesh != null) foodMesh.enabled = true; 
        if (steamParticles) steamParticles.SetActive(false);
        if (potUICanvas != null) potUICanvas.SetActive(true);

        // Reset the UI slots!
        for (int i = 0; i < ingredientSlots.Length; i++)
        {
            // If the recipe needs this slot, show it and make it empty
            if (i < dish.requiredIngredients.Count)
            {
                ingredientSlots[i].gameObject.SetActive(true);
                ingredientSlots[i].color = emptyColor;
            }
            else // Hide extra slots if recipe only needs 3 items
            {
                ingredientSlots[i].gameObject.SetActive(false);
            }
        }

        Debug.Log("Pot is ready! Drop ingredients in for: " + dish.recipeName);
    }

    public void Interact()
    {
        if (state == CookingState.Cooked)
        {
            PlayerVitals player = FindFirstObjectByType<PlayerVitals>();
            if(player != null && currentDish != null) player.Eat(currentDish.hungerRestore);
            
            state = CookingState.Empty;
            currentDish = null;
            if (foodMesh != null) foodMesh.enabled = false;
            return;
        }

        if (state == CookingState.Prepping && PlayerInventory.instance != null && PlayerInventory.instance.currentlyHeldModel != null)
        {
            GameObject heldItem = PlayerInventory.instance.currentlyHeldModel;
            TryAddIngredient(heldItem);
            return; 
        }

        if (PlayerInventory.instance != null && PlayerInventory.instance.currentlyHeldModel == null)
        {
            if (potUICanvas != null) potUICanvas.SetActive(false); // Hide UI when picking up
            PlayerInventory.instance.HoldPhysicalItem(this.gameObject);
        }
    }

    private void TryAddIngredient(GameObject ingredientObject)
    {
        if (currentDish == null) return;

        string ingredientName = ingredientObject.name.Replace("(Clone)", "").Trim();

        if (currentDish.requiredIngredients.Contains(ingredientName))
        {
            if (addedIngredients.Contains(ingredientName)) return;

            // Fill the visual UI slot right BEFORE we add it to the list!
            int slotIndexToFill = addedIngredients.Count; 
            if (slotIndexToFill < ingredientSlots.Length)
            {
                ingredientSlots[slotIndexToFill].color = filledColor;
            }

            addedIngredients.Add(ingredientName);
            Debug.Log("Added " + ingredientName + "! (" + addedIngredients.Count + "/" + currentDish.requiredIngredients.Count + ")");

            PlayerInventory.instance.ClearHand();
            Destroy(ingredientObject);

            if (addedIngredients.Count >= currentDish.requiredIngredients.Count) StartCooking();
        }
        else
        {
            Debug.Log(ingredientName + " does NOT belong in " + currentDish.recipeName + "!");
        }
    }

    private void StartCooking()
    {
        state = CookingState.Cooking;
        if (steamParticles) steamParticles.SetActive(true);
        if (potUICanvas != null) potUICanvas.SetActive(false); // Hide slots while cooking!
        FinishCooking(); 
    }

    private void FinishCooking()
    {
        state = CookingState.Cooked;
        if (steamParticles) steamParticles.SetActive(false);
        if (potUICanvas != null) potUICanvas.SetActive(false);
        
        if (currentDish.finishedDishPrefab != null)
        {
            Instantiate(currentDish.finishedDishPrefab, transform.position, transform.rotation);
            Destroy(gameObject); 
        }
    }
}