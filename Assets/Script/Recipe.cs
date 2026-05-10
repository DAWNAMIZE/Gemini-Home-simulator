using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "New Recipe", menuName = "Survival Game/Recipe Data")]
public class Recipe : ScriptableObject
{
    [Header("UI Details")]
    public string recipeName = "New Recipe";
    
    [Header("Cooking Rules")]
    [Tooltip("Type the exact names of the prefabs needed for this dish")]
    public List<string> requiredIngredients; 
    
    [Header("The Reward")]
    [Tooltip("The 3D bowl of food that spawns when cooking is done")]
    public GameObject finishedDishPrefab; 
    
    [Tooltip("How much health/hunger the player gets when they eat it")]
    public float hungerRestore = 50f;
}