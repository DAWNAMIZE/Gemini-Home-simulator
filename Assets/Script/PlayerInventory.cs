using System.Collections.Generic;
using UnityEngine;

public class PlayerInventory : MonoBehaviour
{
    public static PlayerInventory instance;

    [Header("Recipes")]
    public List<Recipe> unlockedRecipes = new List<Recipe>();

    [Header("Backpack Items")]
    public List<GameObject> backpackItems = new List<GameObject>();
    
    [Header("Holding & Dropping")]
    public Transform playerHand;      
    public Transform dropSpawnPoint;  

    // These MUST be public so the Snap Points can access them!
    public int selectedSlotIndex = -1; 
    public GameObject currentlyHeldModel; 

    void Awake()
    {
        if (instance == null) instance = this;
        else Destroy(gameObject);
    }

    public void LearnRecipe(Recipe newRecipe)
    {
        if (!unlockedRecipes.Contains(newRecipe))
        {
            unlockedRecipes.Add(newRecipe);
            Debug.Log("Learned new recipe: " + newRecipe.name);
        }
    }

    public void AddItemToBackpack(GameObject itemPrefabToAdd)
    {
        backpackItems.Add(itemPrefabToAdd);
        Debug.Log("Successfully added " + itemPrefabToAdd.name + " to the backpack!");
    }

    // --- RESTORED: UI BAG SELECTION LOGIC ---
    public void SelectSlot(int index)
    {
        if (index >= 0 && index < backpackItems.Count)
        {
            selectedSlotIndex = index;
            if (PhoneUIManager.instance != null && PhoneUIManager.instance.dropButton != null) 
            {
                PhoneUIManager.instance.dropButton.SetActive(true);
            }
            EquipItemVisually(backpackItems[index]);
        }
    }

    private void EquipItemVisually(GameObject itemPrefab)
    {
        if (currentlyHeldModel != null) DropSelectedItem();

        if (playerHand != null && itemPrefab != null)
        {
            currentlyHeldModel = Instantiate(itemPrefab, playerHand.position, playerHand.rotation);
            currentlyHeldModel.transform.SetParent(playerHand); 
            currentlyHeldModel.SetActive(true);

            Rigidbody rb = currentlyHeldModel.GetComponent<Rigidbody>();
            if (rb != null) rb.isKinematic = true; 

            Collider col = currentlyHeldModel.GetComponent<Collider>();
            if (col != null) col.enabled = false;
        }
    }

    // --- DIRECT HOLD LOGIC (For Physical Tools like the Pot) ---
    public void HoldPhysicalItem(GameObject physicalObject)
    {
        // If we are already holding something, drop it first!
        if (currentlyHeldModel != null) DropSelectedItem();

        currentlyHeldModel = physicalObject;
        selectedSlotIndex = -1; // -1 means it is NOT from the bag

        // Snap it to the player's hand
        currentlyHeldModel.transform.SetParent(playerHand);
        currentlyHeldModel.transform.localPosition = Vector3.zero; 
        currentlyHeldModel.transform.localRotation = Quaternion.identity;

        // Turn off physics so you can carry it without it pushing you around
        Rigidbody rb = currentlyHeldModel.GetComponent<Rigidbody>();
        if (rb != null) rb.isKinematic = true;

        Collider col = currentlyHeldModel.GetComponent<Collider>();
        if (col != null) col.enabled = false;

        // Turn on the UI Drop Button
        if (PhoneUIManager.instance != null && PhoneUIManager.instance.dropButton != null)
        {
            PhoneUIManager.instance.dropButton.SetActive(true);
        }
    }

    // --- DROP LOGIC ---
    public void DropSelectedItem()
    {
        if (currentlyHeldModel != null)
        {
            // Unparent it from the hand
            currentlyHeldModel.transform.SetParent(null);
            
            // Move it to the drop point
            if (dropSpawnPoint != null) currentlyHeldModel.transform.position = dropSpawnPoint.position;

            // Turn physics back on so it falls to the ground
            Rigidbody rb = currentlyHeldModel.GetComponent<Rigidbody>();
            if (rb != null) rb.isKinematic = false;

            Collider col = currentlyHeldModel.GetComponent<Collider>();
            if (col != null) col.enabled = true;

            // ONLY remove from the list if it came from the UI Bag
            if (selectedSlotIndex >= 0 && selectedSlotIndex < backpackItems.Count)
            {
                backpackItems.RemoveAt(selectedSlotIndex);
            }

            // Tell the inventory the hand is empty
            ClearHand();

            Debug.Log("Item dropped back into the world!");
        }
    }

    // --- CLEAR HAND LOGIC (Called by Snap Points) ---
    public void ClearHand()
    {
        currentlyHeldModel = null;
        selectedSlotIndex = -1;

        if (PhoneUIManager.instance != null && PhoneUIManager.instance.dropButton != null)
        {
            PhoneUIManager.instance.dropButton.SetActive(false);
        }
    }
    // NEW: Searches the bag for a specific ingredient and consumes it!
    public bool ConsumeItemFromBackpack(string itemNameToFind)
    {
        for (int i = 0; i < backpackItems.Count; i++)
        {
            if (backpackItems[i] != null)
            {
                // Clean the name just in case Unity added "(Clone)"
                string cleanName = backpackItems[i].name.Replace("(Clone)", "").Trim();
                
                if (cleanName == itemNameToFind)
                {
                    backpackItems.RemoveAt(i); // Remove it from the bag
                    return true; // Successfully found and consumed!
                }
            }
        }
        return false; // The player doesn't have it
    }
}