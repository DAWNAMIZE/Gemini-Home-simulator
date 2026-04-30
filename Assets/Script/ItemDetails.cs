using UnityEngine;

public enum ItemCategory 
{ 
    RawIngredient, // Cannot be eaten directly
    Edible,        // Restores Hunger
    Medical        // Restores Health/Cures Status
}

public class ItemDetails : MonoBehaviour
{
    [Header("Item Identity")]
    public string itemName = "New Item";
    public Sprite itemIcon; 
    public ItemCategory category;

    [Header("Item Effects")]
    public float effectAmount = 0f; 
}