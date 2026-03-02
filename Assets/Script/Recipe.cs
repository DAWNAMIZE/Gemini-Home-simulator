using UnityEngine;

[CreateAssetMenu(fileName = "New Recipe", menuName = "Survival/Recipe")]
public class Recipe : ScriptableObject
{
    // The error happens if "public" is missing here:
    public string recipeName;      
    
    public float cookTime = 10f;   
    public float hungerRestore = 50f; 
    
    [Header("Visuals")]
    public Material rawMat;        
    public Material cookedMat;     
    public Material burntMat;      
}