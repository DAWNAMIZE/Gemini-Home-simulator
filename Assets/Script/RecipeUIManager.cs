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

    [Header("Universal Settings")]
    [Tooltip("Drag your new Universal Ration Crate PREFAB here from the Project folder")]
    public GameObject universalRationCratePrefab;

    private GameObject targetPot;

    void Awake()
    {
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
                // THE FIX: We now check the bag for the Universal Ration Crate!
                bool hasRation = PlayerInventory.instance.ConsumeItemFromBackpack(universalRationCratePrefab);
                
                if (hasRation)
                {
                    Debug.Log("Ration used! Cooking " + selectedRecipe.recipeName);
                    pot.StartCooking(selectedRecipe);
                }
                else
                {
                    Debug.Log("You do not have a Ration Crate in your bag!");
                }
            }
        }
        CloseMenu();
    }
}