using UnityEngine;

public class ItemPickup : MonoBehaviour, IInteractable
{
    public GameObject itemPrefab;

    public void Interact()
    {
        if (PlayerInventory.instance != null && itemPrefab != null)
        {
            PlayerInventory.instance.AddItemToBackpack(itemPrefab);
            Debug.Log("Stashed " + gameObject.name + " into the backpack.");
            Destroy(gameObject);
        }
    }
}