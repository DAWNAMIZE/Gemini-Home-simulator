using UnityEngine;
using System.Collections;

public class TraditionalOven : MonoBehaviour // Ensure you implement your IInteractable interface if you use one
{
    [Header("Oven Setup")]
    public Transform potPlacementPoint; 
    public bool isOccupied = false;
    public PortablePot currentPot;

    [Header("Instant Cook Settings")]
    public float cookTime = 2.0f; // Rapid cooking for the MVP loop

    public void Interact()
    {
        // CASE 1: Player is holding a pot and the oven is empty
        if (!isOccupied && PlayerHands.instance != null && PlayerHands.instance.isHoldingPot)
        {
            currentPot = PlayerHands.instance.heldPot;
            
            // Tell the player's hands to let go
            PlayerHands.instance.Release();

            // Snap the pot to the top of the oven
            currentPot.transform.SetParent(potPlacementPoint);
            currentPot.transform.localPosition = Vector3.zero;
            currentPot.transform.localRotation = Quaternion.identity;
            
            // Anti-Shrink fix for the oven snap point
            currentPot.transform.localScale = Vector3.one; 
            
            // Re-enable physics and colliders now that it has left the hands
            Collider col = currentPot.GetComponent<Collider>();
            if (col != null) col.enabled = true;
            
            Rigidbody rb = currentPot.GetComponent<Rigidbody>();
            if (rb != null) rb.isKinematic = true; // Keep it frozen so it doesn't roll off the oven
            
            // Put it back on the Default layer (Layer 0) so the player can grab it again
            currentPot.gameObject.layer = 0; 

            isOccupied = true;
            Debug.Log("Pot placed on the fire.");

            // Instantly start cooking!
            StartCoroutine(CookFood());
            return;
        }

        // CASE 2: The oven has a pot and the player's hands are empty
        if (isOccupied && currentPot != null && PlayerHands.instance != null && PlayerHands.instance.currentHoldType == PlayerHands.HoldType.None)
        {
            // Forward the click directly to the pot so the player picks it up
            // (Assuming PortablePot has its own Interact method to trigger GrabPot)
            currentPot.SendMessage("Interact", SendMessageOptions.DontRequireReceiver);
            
            currentPot = null;
            isOccupied = false;
            return;
        }
    }

    private IEnumerator CookFood()
    {
        // Check state before cooking (assuming PortablePot has these states defined)
        if (currentPot != null /* && currentPot.state == PortablePot.CookingState.Raw */) 
        {
            // currentPot.state = PortablePot.CookingState.Cooking;
            // currentPot.UpdateVisuals(); 

            yield return new WaitForSeconds(cookTime);

            // Make sure the player didn't pick the pot up early
            if (currentPot != null && currentPot.transform.parent == potPlacementPoint) 
            {
                // currentPot.state = PortablePot.CookingState.Cooked;
                // currentPot.UpdateVisuals(); 
                Debug.Log("Food is ready! XP Granted.");
            }
        }
    }
}