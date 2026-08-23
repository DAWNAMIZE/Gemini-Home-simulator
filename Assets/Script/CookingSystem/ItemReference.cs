using UnityEngine;

// This allows you to right-click in the Project folder and create a new Item Reference file
[CreateAssetMenu(fileName = "NewItemReference", menuName = "Inventory/Item Reference")]
public class ItemReference : ScriptableObject
{
    [Header("Item Identity")]
    [Tooltip("The display name for the UI")]
    public string itemName;
    
    [Tooltip("The image shown in the backpack")]
    public Sprite itemIcon;

    [Header("Item Settings")]
    public string category;
    public int effectAmount;
}