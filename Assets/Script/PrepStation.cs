using UnityEngine;

public class PrepSnapPoint : MonoBehaviour
{
    [Header("Placement Settings")]
    public Transform potSnapPosition; 
    public GameObject currentPotOnStation; 
    
    [Header("Visual Feedback")]
    public GameObject ghostCylinder; 

    void Start() 
    { 
        if (ghostCylinder != null) ghostCylinder.SetActive(false); 
    }

    public void Interact()
    {
        // 1. If a pot is already snapped here, open the UI
        if (currentPotOnStation != null)
        {
            Debug.Log("Opening Prep Station UI..."); 
            return;
        }

        // 2. If the station is empty, snap the Pot from the player's hand to the Table
        if (PlayerInventory.instance != null && PlayerInventory.instance.currentlyHeldModel != null)
        {
            GameObject heldItem = PlayerInventory.instance.currentlyHeldModel;

            if (heldItem.CompareTag("Holdable"))
            {
                currentPotOnStation = heldItem;
                currentPotOnStation.transform.SetParent(potSnapPosition);
                currentPotOnStation.transform.localPosition = Vector3.zero;
                currentPotOnStation.transform.localRotation = Quaternion.identity;

                PlayerInventory.instance.ClearHand();
                if (ghostCylinder != null) ghostCylinder.SetActive(false);
            }
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player") && currentPotOnStation == null)
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