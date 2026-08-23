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
        if (backpackItems.Count >= 4)
        {
            Debug.Log("Inventory is full! Cannot pick up " + itemPrefabToAdd.name);
            return; 
        }

        backpackItems.Add(itemPrefabToAdd);
        Debug.Log("Successfully added " + itemPrefabToAdd.name + " to the backpack!");
        
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

    public bool ConsumeItemFromBackpack(GameObject prefabToFind)
    {
        if (prefabToFind == null) return false;

        ItemDetails targetDetails = prefabToFind.GetComponent<ItemDetails>();
        if (targetDetails == null || targetDetails.itemRef == null) 
        {
            Debug.LogError("The target prefab is missing an ItemReference asset! Please assign one in the Inspector.");
            return false;
        }

        // Store the exact stable reference we are looking for
        ItemReference targetRef = targetDetails.itemRef;

        for (int i = 0; i < backpackItems.Count; i++)
        {
            if (backpackItems[i] != null)
            {
                ItemDetails bagItemDetails = backpackItems[i].GetComponent<ItemDetails>();
                
                // Compare the references directly! No string checking needed.
                if (bagItemDetails != null && bagItemDetails.itemRef == targetRef)
                {
                    Debug.Log("MATCH FOUND! Consuming [" + targetRef.itemName + "]");
                    backpackItems.RemoveAt(i); 
                    
                    if (BackpackUIManager.instance != null) BackpackUIManager.instance.RefreshUI();
                    
                    return true; 
                }
            }
        }
        
        Debug.Log("FAILED -> Could not find the stable reference for [" + targetRef.itemName + "] in the bag.");
        return false; 
    }
}