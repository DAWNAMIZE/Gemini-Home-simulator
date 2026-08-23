using UnityEngine;

[CreateAssetMenu(fileName = "New Recipe", menuName = "Survival Game/Recipe Data")]
public class Recipe : ScriptableObject
{
    [Header("UI Details")]
    public string recipeName = "Bitter Leaf Soup";
    
   
    
    [Tooltip("How many seconds the loading bar takes to fill up")]
    public float cookTime = 5f;

    [Header("The Reward")]
    [Tooltip("The 3D bowl of food that spawns when cooking is done")]
    public GameObject finishedDishPrefab; 
    
    public float hungerRestore = 50f;
}