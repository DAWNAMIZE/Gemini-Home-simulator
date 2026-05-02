using UnityEngine;

public class StoveSnapPoint : MonoBehaviour, IInteractable
{
    [Header("Placement Settings")]
    public Transform potSnapPosition; 
    public GameObject currentPotOnStove; 

    [Header("Visual Feedback")]
    public GameObject ghostCylinder; 

    void Start() 
    { 
        if (ghostCylinder != null) ghostCylinder.SetActive(false); 
    }

    public void Interact()
    {
        // 1. If the stove is empty, grab the Pot from the player's hand
        if (currentPotOnStove == null)
        {
            if (PlayerInventory.instance != null && PlayerInventory.instance.currentlyHeldModel != null)
            {
                GameObject heldItem = PlayerInventory.instance.currentlyHeldModel;

                if (heldItem.CompareTag("Holdable"))
                {
                    currentPotOnStove = heldItem;
                    currentPotOnStove.transform.SetParent(potSnapPosition);
                    currentPotOnStove.transform.localPosition = Vector3.zero;
                    currentPotOnStove.transform.localRotation = Quaternion.identity;

                    PlayerInventory.instance.ClearHand();
                    if (ghostCylinder != null) ghostCylinder.SetActive(false);
                    
                    Debug.Log("Pot placed! Go to the Prep Station to select your recipe.");
                }
            }
        }
        else
        {
            // The pot is already here! (We will add the logic to drop ingredients in here later)
            Debug.Log("The pot is on the fire. Go select a recipe at the table!");
        }
    }

    private void OnTriggerEnter(Collider other)
    {
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
        if (other.CompareTag("Player") && ghostCylinder != null)
        {
            ghostCylinder.SetActive(false);
        }
    }
}