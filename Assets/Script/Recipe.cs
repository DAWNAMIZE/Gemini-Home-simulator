using UnityEngine;
using System.Collections.Generic; // Required so Unity understands what a "List" is

[CreateAssetMenu(fileName = "NewRecipe", menuName = "Survival Game/Recipe")]
public class Recipe : ScriptableObject
{
    [Header("Recipe Details")]
    public string recipeName = "New Dish";
    
    [Tooltip("The exact names of the raw ingredients needed to cook this dish")]
    public List<string> requiredIngredients = new List<string>();

    [Header("The Result")]
    [Tooltip("The 3D model of the finished, cooked meal")]
    public GameObject finishedDishPrefab; 

    [Header("Legacy Stats")]
    [Tooltip("How much hunger this dish restores. (Note: Eventually ItemDetails will handle this!)")]
    public float hungerRestore = 70f; 
}