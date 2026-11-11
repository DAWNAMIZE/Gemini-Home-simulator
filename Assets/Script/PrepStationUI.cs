using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class PrepStationUI : MonoBehaviour
{
    public static PrepStationUI instance;

    [Header("Recipe Database")]
    public List<Recipe> allGameRecipes = new List<Recipe>();

    [Header("UI References")]
    public Transform recipeContainer;
    public GameObject recipeSlotPrefab;
    public GameObject uiPanel;          // Drag the white square panel here

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

        GameObject panel = uiPanel != null ? uiPanel : gameObject;
        panel.SetActive(true);

        RefreshRecipeList();
    }

    public void Close()
    {
        GameObject panel = uiPanel != null ? uiPanel : gameObject;
        panel.SetActive(false);
    }

    public void RefreshRecipeList()
    {
        if (recipeContainer == null || recipeSlotPrefab == null) return;

        foreach (Transform child in recipeContainer)
            Destroy(child.gameObject);

        foreach (Recipe recipe in allGameRecipes)
        {
            if (recipe == null) continue;

            GameObject slotObj = Instantiate(recipeSlotPrefab, recipeContainer);

            Image icon = slotObj.transform.Find("Icon")?.GetComponent<Image>();
            TMP_Text nameText = slotObj.transform.Find("Name")?.GetComponent<TMP_Text>();
            Button selectBtn = slotObj.GetComponent<Button>();

            if (icon != null)
                icon.sprite = recipe.recipeIcon;

            if (nameText != null)
                nameText.text = recipe.recipeName;

            bool isUnlocked = recipe.isUnlocked || IsRecipeLearned(recipe);

            if (isUnlocked && selectBtn != null)
            {
                selectBtn.interactable = true;
                if (icon != null) icon.color = Color.white;
                if (nameText != null) nameText.color = Color.white;

                Recipe captured = recipe;
                selectBtn.onClick.AddListener(() => OnSelectRecipeToCook(captured));
            }
            else
            {
                if (selectBtn != null) selectBtn.interactable = false;
                if (icon != null) icon.color = new Color(1f, 1f, 1f, 0.4f);
                if (nameText != null) nameText.color = new Color(1f, 1f, 1f, 0.4f);
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
            Close();
        }
    }
}