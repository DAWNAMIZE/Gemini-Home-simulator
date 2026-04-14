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

    private int selectedSlotIndex = -1; 
    private GameObject currentlyHeldModel; 

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
            Debug.Log("Learned new recipe!");
        }
    }

    public void AddItemToBackpack(GameObject itemPrefabToAdd)
    {
        backpackItems.Add(itemPrefabToAdd);
        Debug.Log("Successfully added " + itemPrefabToAdd.name + " to the backpack!");
    }

    // --- SELECTION & EQUIP LOGIC (For Bag Items) ---
    public void SelectSlot(int index)
    {
        if (index >= 0 && index < backpackItems.Count)
        {
            selectedSlotIndex = index;
            if (PhoneUIManager.instance.dropButton != null) PhoneUIManager.instance.dropButton.SetActive(true);
            EquipItemVisually(backpackItems[index]);
        }
    }

    private void EquipItemVisually(GameObject itemPrefab)
    {
        if (currentlyHeldModel != null) Destroy(currentlyHeldModel);

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

    // --- NEW: DIRECT HOLD LOGIC (For Physical Tools like the Pot) ---
    public void HoldPhysicalItem(GameObject physicalObject)
    {
        // If we are already holding something from the bag, drop it first!
        if (currentlyHeldModel != null) DropSelectedItem();

        currentlyHeldModel = physicalObject;
        selectedSlotIndex = -1; // -1 means it is NOT from the bag

        // Snap it to the hand
        currentlyHeldModel.transform.SetParent(playerHand);
        currentlyHeldModel.transform.localPosition = Vector3.zero; // Centers it on the hand
        currentlyHeldModel.transform.localRotation = Quaternion.identity;

        // Turn off physics so you can carry it
        Rigidbody rb = currentlyHeldModel.GetComponent<Rigidbody>();
        if (rb != null) rb.isKinematic = true;

        Collider col = currentlyHeldModel.GetComponent<Collider>();
        if (col != null) col.enabled = false;

        // Turn on the UI Drop Button
        if (PhoneUIManager.instance.dropButton != null)
        {
            PhoneUIManager.instance.dropButton.SetActive(true);
        }
    }

    // --- DROP LOGIC (Updated to handle both Bag and Physical items) ---
    public void DropSelectedItem()
    {
        if (currentlyHeldModel != null)
        {
            currentlyHeldModel.transform.SetParent(null);
            
            if (dropSpawnPoint != null) currentlyHeldModel.transform.position = dropSpawnPoint.position;

            Rigidbody rb = currentlyHeldModel.GetComponent<Rigidbody>();
            if (rb != null) rb.isKinematic = false;

            Collider col = currentlyHeldModel.GetComponent<Collider>();
            if (col != null) col.enabled = true;

            // ONLY remove from the list if it came from the UI Bag
            if (selectedSlotIndex >= 0 && selectedSlotIndex < backpackItems.Count)
            {
                backpackItems.RemoveAt(selectedSlotIndex);
            }

            currentlyHeldModel = null;
            selectedSlotIndex = -1;

            if (PhoneUIManager.instance.dropButton != null) PhoneUIManager.instance.dropButton.SetActive(false);

            Debug.Log("Item dropped back into the world!");
        }
    }
}