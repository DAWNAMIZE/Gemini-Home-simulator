using UnityEngine;

public class ItemPickup : MonoBehaviour, IInteractable
{
    [Header("Inventory Item")]
    [Tooltip("Drag the Prefab of this item from your Project folder here")]
    public GameObject itemPrefab; 

    public void Interact()
    {
        if (itemPrefab == null)
        {
            Debug.LogWarning("You forgot to attach the item prefab to this pickup!");
            return;
        }

        if (PlayerInventory.instance != null)
        {
            // 1. Add the prefab to your Backpack list
            PlayerInventory.instance.AddItemToBackpack(itemPrefab);
            
            // 2. Destroy the 3D model from the floor
            Destroy(gameObject);
        }
    }
}