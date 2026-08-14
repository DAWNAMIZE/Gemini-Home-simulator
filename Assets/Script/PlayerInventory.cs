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
        // THE FIX: Check if we already have 4 items!
        if (backpackItems.Count >= 4)
        {
            Debug.Log("Inventory is full! Cannot pick up " + itemPrefabToAdd.name);
            return; // Stop the code here
        }

        backpackItems.Add(itemPrefabToAdd);
        Debug.Log("Successfully added " + itemPrefabToAdd.name + " to the backpack!");
        
        // Auto-refresh the 4 boxes on screen
        if (BackpackUIManager.instance != null) BackpackUIManager.instance.RefreshUI();
    }

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

    public void HoldPhysicalItem(GameObject physicalObject)
    {
        if (currentlyHeldModel != null) DropSelectedItem();

        currentlyHeldModel = physicalObject;
        selectedSlotIndex = -1; 

        currentlyHeldModel.transform.SetParent(playerHand);
        currentlyHeldModel.transform.localPosition = Vector3.zero; 
        currentlyHeldModel.transform.localRotation = Quaternion.identity;

        Rigidbody rb = currentlyHeldModel.GetComponent<Rigidbody>();
        if (rb != null) rb.isKinematic = true;

        Collider col = currentlyHeldModel.GetComponent<Collider>();
        if (col != null) col.enabled = false;

        if (PhoneUIManager.instance != null && PhoneUIManager.instance.dropButton != null)
        {
            PhoneUIManager.instance.dropButton.SetActive(true);
        }
    }

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

            if (selectedSlotIndex >= 0 && selectedSlotIndex < backpackItems.Count)
            {
                backpackItems.RemoveAt(selectedSlotIndex);
                if (BackpackUIManager.instance != null) BackpackUIManager.instance.RefreshUI();
            }

            ClearHand();
            Debug.Log("Item dropped back into the world!");
        }
    }

    public void ClearHand()
    {
        currentlyHeldModel = null;
        selectedSlotIndex = -1;

        if (PhoneUIManager.instance != null && PhoneUIManager.instance.dropButton != null)
        {
            PhoneUIManager.instance.dropButton.SetActive(false);
        }
    }

    // THE FIX: This now accurately asks for a GameObject instead of a string!
    public bool ConsumeItemFromBackpack(GameObject prefabToFind)
    {
        if (prefabToFind == null) return false;

        for (int i = 0; i < backpackItems.Count; i++)
        {
            if (backpackItems[i] != null)
            {
                // Clean the name just in case Unity added "(Clone)"
                string cleanName = backpackItems[i].name.Replace("(Clone)", "").Trim();
                string targetName = prefabToFind.name.Replace("(Clone)", "").Trim();

                if (cleanName == targetName)
                {
                    backpackItems.RemoveAt(i); // Remove it from the bag
                    
                    // Force the UI to refresh so the crate visually disappears from the phone
                    if (BackpackUIManager.instance != null) BackpackUIManager.instance.RefreshUI();
                    
                    return true; 
                }
            }
        }
        return false; 
    }
}