using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class RecipeUIManager : MonoBehaviour
{
    public static RecipeUIManager instance;

    [Header("UI Panels")]
    public GameObject recipeMenuPanel; 
    public Transform recipeButtonContainer; 
    public GameObject recipeButtonPrefab; 

    private GameObject targetPot;

    void Awake()
    {
        // FIX 1: Always overwrite the instance so it never points to an old, destroyed play session!
        instance = this;
    }

    void Start()
    {
        if (recipeMenuPanel != null) recipeMenuPanel.SetActive(false);
    }

    public void OpenMenu(GameObject potOnStove)
    {
        targetPot = potOnStove;
        if (recipeMenuPanel != null) recipeMenuPanel.SetActive(true);
        PopulateList();
    }

    public void CloseMenu()
    {
        if (recipeMenuPanel != null) recipeMenuPanel.SetActive(false);
        targetPot = null;
    }

    private void PopulateList()
    {
        if (recipeButtonContainer == null) return;

        // FIX 2: A bulletproof backwards loop to safely destroy UI buttons without crashing the count
        for (int i = recipeButtonContainer.childCount - 1; i >= 0; i--)
        {
            Destroy(recipeButtonContainer.GetChild(i).gameObject);
        }

        if (PlayerInventory.instance == null || PlayerInventory.instance.unlockedRecipes == null) return;

        foreach (Recipe recipe in PlayerInventory.instance.unlockedRecipes)
        {
            GameObject newBtn = Instantiate(recipeButtonPrefab, recipeButtonContainer);

            TextMeshProUGUI btnText = newBtn.GetComponentInChildren<TextMeshProUGUI>();
            if (btnText != null) btnText.text = recipe.recipeName;

            Button buttonComp = newBtn.GetComponent<Button>();
            if (buttonComp != null)
            {
                buttonComp.onClick.AddListener(() => OnRecipeSelected(recipe));
            }
        }
    }

    private void OnRecipeSelected(Recipe selectedRecipe)
    {
        if (targetPot != null)
        {
            PortablePot pot = targetPot.GetComponent<PortablePot>();
            
            if (pot != null && pot.state == PortablePot.CookingState.Empty)
            {
                // Check the bag for the required Ration Crate
                bool hasRation = PlayerInventory.instance.ConsumeItemFromBackpack(selectedRecipe.requiredRation);
                
                if (hasRation)
                {
                    Debug.Log("Ration used! Cooking " + selectedRecipe.recipeName);
                    pot.StartCooking(selectedRecipe);
                }
                else
                {
                    Debug.Log("You do not have the required ration: " + selectedRecipe.requiredRation.name);
                }
            }
        }
        CloseMenu();
    }
}