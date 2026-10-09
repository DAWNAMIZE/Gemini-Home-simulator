using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class PrepStationUI : MonoBehaviour
{
    public static PrepStationUI instance;

    [Header("Recipe Database")]
    public List<Recipe> allGameRecipes = new List<Recipe>(); 
    
    [Header("UI References")]
    public Transform recipeContainer; 
    public GameObject recipeSlotPrefab; 

    [Header("Active Cooking Data")]
    public PortablePot targetCookingPot; 

    void Awake()
    {
        if (instance == null) 
        {
            instance = this;
        }
        else 
        {
            Destroy(gameObject);
        }
    }

    public void OpenPrepStation(PortablePot pot)
    {
        targetCookingPot = pot;
        gameObject.SetActive(true);
        RefreshRecipeList();
    }

    public void RefreshRecipeList()
    {
        // Clear old UI slots to prevent duplicates
        foreach (Transform child in recipeContainer)
        {
            Destroy(child.gameObject);
        }

        // Generate UI slots dynamically based on the Scriptable Objects
        foreach (Recipe recipe in allGameRecipes)
        {
            GameObject slotObj = Instantiate(recipeSlotPrefab, recipeContainer);
            
            Image icon = slotObj.transform.Find("Icon").GetComponent<Image>();
            Button selectBtn = slotObj.GetComponent<Button>();

            if (icon != null) icon.sprite = recipe.recipeIcon;

            if (recipe.isUnlocked)
            {
                selectBtn.interactable = true;
                icon.color = new Color(1, 1, 1, 1f); // Full opacity
                
                Recipe targetRecipe = recipe; 
                selectBtn.onClick.AddListener(() => OnSelectRecipeToCook(targetRecipe));
            }
            else
            {
                selectBtn.interactable = false;
                icon.color = new Color(1, 1, 1, 0.4f); // Greyed out / semi-transparent for locked items
            }
        }
    }

    private void OnSelectRecipeToCook(Recipe recipe)
    {
        if (targetCookingPot != null)
        {
            // Calls the exact method name from your PortablePot.cs
            targetCookingPot.StartCooking(recipe);
            gameObject.SetActive(false); 
        }
    }
}