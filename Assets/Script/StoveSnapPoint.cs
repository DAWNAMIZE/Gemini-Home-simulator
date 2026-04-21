using UnityEngine;
using System.Collections;

public class StoveSnapPoint : MonoBehaviour
{
    [Header("Placement Settings")]
    public Transform potSnapPosition; 
    public GameObject currentPotOnStove; 

    [Header("Visual Feedback")]
    public GameObject ghostCylinder; 
    
    [Header("Cooking Output")]
    public Transform finishedDishSpawnPoint; 

    void Start() 
    { 
        if (ghostCylinder != null) ghostCylinder.SetActive(false); 
    }

    public void Interact()
    {
        // 1. Is a pot here? Check if it's loaded and ready to cook!
        if (currentPotOnStove != null)
        {
            PotState state = currentPotOnStove.GetComponent<PotState>();
            if (state != null && state.isLoaded)
            {
                StartCoroutine(CookFoodRoutine(state));
            }
            else
            {
                Debug.Log("The pot is empty! Go to the Prep Station first.");
            }
            return; // Stop here so we don't try to snap a second pot
        }

        // 2. If stove is empty, snap the Pot from the player's hand to the Stove
        if (PlayerInventory.instance != null && PlayerInventory.instance.currentlyHeldModel != null)
        {
            GameObject heldItem = PlayerInventory.instance.currentlyHeldModel;

            if (heldItem.CompareTag("Holdable"))
            {
                // Transfer ownership
                currentPotOnStove = heldItem;
                
                // Snap position
                currentPotOnStove.transform.SetParent(potSnapPosition);
                currentPotOnStove.transform.localPosition = Vector3.zero;
                currentPotOnStove.transform.localRotation = Quaternion.identity;

                // Clear the player's hand
                PlayerInventory.instance.ClearHand();
                
                // Hide the green hover preview
                if (ghostCylinder != null) ghostCylinder.SetActive(false);
            }
        }
    }

    private IEnumerator CookFoodRoutine(PotState potState)
    {
        // Prevent double-clicking while it's already cooking
        potState.isLoaded = false; 
        Debug.Log("Cooking for 3 seconds...");

        yield return new WaitForSeconds(3f);

        Debug.Log("Dish is ready!");
        
        // Hide the cooking pot
        currentPotOnStove.SetActive(false);

        // Spawn the finished dish
        if (potState.finishedDishPrefab != null && finishedDishSpawnPoint != null)
        {
            Instantiate(potState.finishedDishPrefab, finishedDishSpawnPoint.position, finishedDishSpawnPoint.rotation);
        }

        // Clear the stove so the player can place a new pot later
        currentPotOnStove = null; 
    }

    private void OnTriggerEnter(Collider other)
    {
        // Show the ghost cylinder if the player walks up holding a pot
        if (other.CompareTag("Player") && currentPotOnStove == null)
        {
            if (PlayerInventory.instance?.currentlyHeldModel?.CompareTag("Holdable") == true)
            {
                if (ghostCylinder != null) ghostCylinder.SetActive(true);
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        // Hide the ghost cylinder when the player walks away
        if (other.CompareTag("Player") && ghostCylinder != null)
        {
            ghostCylinder.SetActive(false);
        }
    }
}