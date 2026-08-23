using UnityEngine;

public class ItemDetails : MonoBehaviour
{
    [Header("Stable Identity Definition")]
    [Tooltip("Drag the ItemReference ScriptableObject here to define this item.")]
    public ItemReference itemRef;

    // These act as bridges so your other scripts can still safely read the data
    public string itemName => itemRef != null ? itemRef.itemName : "Unknown Item";
    public Sprite itemIcon => itemRef != null ? itemRef.itemIcon : null;
    public string category => itemRef != null ? itemRef.category : "None";
    public int effectAmount => itemRef != null ? itemRef.effectAmount : 0;
}