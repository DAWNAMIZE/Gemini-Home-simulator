using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class RecipeUIManager : MonoBehaviour
{
    // The singleton instance so other scripts can find this Manager
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
        else 
        {
            Destroy(gameObject);
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
        
        if (recipeMenuPanel != null) 
        {
            recipeMenuPanel.SetActive(true);
        }
        
        PopulateList();
    }

    public void CloseMenu()
    {
        if (recipeMenuPanel != null) 
        {
            recipeMenuPanel.SetActive(false);
        }
        targetPot = null;
    }

    private void PopulateList()
    {
        // 1. Clear out any old buttons so they don't duplicate
        foreach (Transform child in recipeButtonContainer)
        {
            Destroy(child.gameObject);
        }

        if (PlayerInventory.instance == null || PlayerInventory.instance.unlockedRecipes == null)
        {
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

        if (targetPot != null)
        {
            // THE FIX: This now correctly looks for the new PortablePot script instead of the deleted PotState!
            PortablePot pot = targetPot.GetComponent<PortablePot>();
            if (pot != null)
            {
                pot.SetupPot(selectedRecipe);
            }
            else
            {
                Debug.LogWarning("The pot on the stove does not have a PortablePot script attached!");
            }
        }
        CloseMenu();
    }
}