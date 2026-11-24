using System.Collections.Generic;
using UnityEngine;

public enum NigerianCuisine
{
    Igbo = 0,
    Yoruba = 1,
    Hausa = 2
}

[CreateAssetMenu(fileName = "New Recipe", menuName = "Cooking/Recipe")]
public class Recipe : ScriptableObject
{
    public string recipeID;
    public string recipeName;
    public Sprite recipeIcon;
    public bool isUnlocked;

    [Header("Cuisine")]
    [Tooltip("Which Nigerian cuisine this dish belongs to. Used by the phone grocery list.")]
    public NigerianCuisine cuisine = NigerianCuisine.Igbo;
    public GameObject finishedDishPrefab; // Matches PortablePot
    public float cookTime = 4f;           // Matches PortablePot

    [Header("Ingredients")]
    [Tooltip("Ingredient names required by this dish. Matched against ItemReference.itemName.")]
    public List<string> requiredIngredients = new List<string>();

    [Header("Economy")]
    [Tooltip("Money the customer pays for this dish.")]
    public int sellPrice = 25;

    [Tooltip("How much hunger eating this dish restores.")]
    public int hungerRestore = 70;
}
