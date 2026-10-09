using UnityEngine;

[CreateAssetMenu(fileName = "New Recipe", menuName = "Cooking/Recipe")]
public class Recipe : ScriptableObject
{
    public string recipeID;
    public string recipeName;
    public Sprite recipeIcon;
    public bool isUnlocked; 
    public GameObject finishedDishPrefab; // Matches PortablePot
    public float cookTime = 4f;           // Matches PortablePot
}