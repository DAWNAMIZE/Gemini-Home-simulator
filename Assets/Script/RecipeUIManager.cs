using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class RecipeUIManager : MonoBehaviour
{
    // THIS IS THE FIX: The singleton instance so other scripts can find this Manager
    public static RecipeUIManager instance;

    [Header("UI Panels")]
    public GameObject recipeMenuPanel; 
    public Transform recipeButtonContainer; 
    public GameObject recipeButtonPrefab; 

    // The pot we are currently prepping food for on the oven
    private GameObject targetPot;

    void Awake()
    {
        // This links the instance variable to this exact script when the game starts
        if (instance == null) 
        {
            instance = this;
        }
    }

    void Start()
    {
        // Keep the menu closed until we interact with the table
        if (recipeMenuPanel != null) recipeMenuPanel.SetActive(false);
    }

    // Called by the Prep Station Table
    public void OpenMenu(GameObject potOnStove)
    {
        targetPot = potOnStove;
        recipeMenuPanel.SetActive(true);
        PopulateList();
    }

    public void CloseMenu()
    {
        recipeMenuPanel.SetActive(false);
        targetPot = null;
    }

    private void PopulateList()
    {
        // 1. Clear out any old buttons so they don't duplicate
        foreach (Transform child in recipeButtonContainer)
        {
            Destroy(child.gameObject);
        }

        // 2. Safety check: Does the player actually know any recipes?
        if (PlayerInventory.instance == null || PlayerInventory.instance.unlockedRecipes.Count == 0)
        {
            Debug.Log("No recipes learned yet! Go find some notebooks.");
            return;
        }

        // 3. Loop through the backpack and create a button for every recipe
        foreach (Recipe recipe in PlayerInventory.instance.unlockedRecipes)
        {
            // Spawn the button inside our container
            GameObject newBtn = Instantiate(recipeButtonPrefab, recipeButtonContainer);

            // Find the Text component and change it to the Recipe's name
            TextMeshProUGUI btnText = newBtn.GetComponentInChildren<TextMeshProUGUI>();
            if (btnText != null) 
            {
                btnText.text = recipe.recipeName;
            }

            // Wire up the button's click event via code!
            Button buttonComp = newBtn.GetComponent<Button>();
            if (buttonComp != null)
            {
                // When clicked, it passes this specific recipe to the OnRecipeSelected function
                buttonComp.onClick.AddListener(() => OnRecipeSelected(recipe));
            }
        }
    }

    private void OnRecipeSelected(Recipe selectedRecipe)
    {
        Debug.Log("You selected to cook: " + selectedRecipe.recipeName);

        // Tell the Pot on the fire what we are about to cook!
        if (targetPot != null)
        {
            PotState potState = targetPot.GetComponent<PotState>();
            if (potState != null)
            {
                potState.BeginAssemblyPhase(selectedRecipe);
            }
        }

        // Close the UI menu
        CloseMenu();
    }
}