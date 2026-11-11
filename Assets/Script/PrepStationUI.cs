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
    public GameObject uiPanel;          // Drag the visible panel here so it actually closes

    [Header("Active Cooking Data")]
    public PortablePot targetCookingPot;

    void Awake()
    {
        if (instance == null)
            instance = this;
        else
            Destroy(gameObject);
    }

    public void OpenPrepStation(PortablePot pot)
    {
        targetCookingPot = pot;

        if (uiPanel != null)
            uiPanel.SetActive(true);
        else
            gameObject.SetActive(true);

        RefreshRecipeList();
    }

    public void RefreshRecipeList()
    {
        if (recipeContainer == null || recipeSlotPrefab == null) return;

        // Clear old UI slots
        foreach (Transform child in recipeContainer)
            Destroy(child.gameObject);

        foreach (Recipe recipe in allGameRecipes)
        {
            if (recipe == null) continue;

            GameObject slotObj = Instantiate(recipeSlotPrefab, recipeContainer);
            Image icon = slotObj.transform.Find("Icon")?.GetComponent<Image>();
            Button selectBtn = slotObj.GetComponent<Button>();

            if (icon != null)
                icon.sprite = recipe.recipeIcon;

            bool isUnlocked = recipe.isUnlocked || IsRecipeLearned(recipe);

            if (isUnlocked && selectBtn != null)
            {
                selectBtn.interactable = true;
                if (icon != null) icon.color = Color.white;

                Recipe captured = recipe;
                selectBtn.onClick.AddListener(() => OnSelectRecipeToCook(captured));
            }
            else
            {
                if (selectBtn != null) selectBtn.interactable = false;
                if (icon != null) icon.color = new Color(1f, 1f, 1f, 0.4f);
            }
        }
    }

    private bool IsRecipeLearned(Recipe recipe)
    {
        if (PlayerInventory.instance == null || PlayerInventory.instance.unlockedRecipes == null)
            return false;

        return PlayerInventory.instance.unlockedRecipes.Contains(recipe);
    }

    private void OnSelectRecipeToCook(Recipe recipe)
    {
        if (targetCookingPot != null && recipe != null)
        {
            targetCookingPot.StartCooking(recipe);

            // Close the correct panel
            if (uiPanel != null)
                uiPanel.SetActive(false);
            else
                gameObject.SetActive(false);
        }
    }
}