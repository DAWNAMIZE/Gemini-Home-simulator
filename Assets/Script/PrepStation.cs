using UnityEngine;

public class PrepStation : MonoBehaviour, IInteractable
{
    [Header("Station Setup")]
    public Transform placementPoint;
    public bool isOccupied = false;
    public PortablePot currentPot;

    public void Interact()
    {
        // CASE 1: Player is holding a pot and the station is empty
        if (!isOccupied && PlayerHands.instance != null && PlayerHands.instance.isHoldingPot)
        {
            currentPot = PlayerHands.instance.heldPot;
            
            // Tell the hands to let go
            PlayerHands.instance.Release();

            // Lock the pot to the prep station
            currentPot.transform.SetParent(placementPoint != null ? placementPoint : transform);
            currentPot.transform.localPosition = Vector3.zero;
            currentPot.transform.localRotation = Quaternion.identity;
            
            // Turn the collider back on so it can be clicked again
            Collider col = currentPot.GetComponent<Collider>();
            if (col != null) col.enabled = true;

            isOccupied = true;
            Debug.Log("Pot placed on the Prep Station.");
            return;
        }

        // CASE 2: The station has a pot and the player's hands are empty
        if (isOccupied && currentPot != null && PlayerHands.instance != null && PlayerHands.instance.currentHoldType == PlayerHands.HoldType.None)
        {
            // Forward the click directly to the pot so the player picks it up
            currentPot.Interact();
            
            currentPot = null;
            isOccupied = false;
            return;
        }
        
        // CASE 3: Station is full and hands are full
        if (isOccupied && PlayerHands.instance != null && PlayerHands.instance.currentHoldType != PlayerHands.HoldType.None)
        {
            Debug.Log("The prep station is already occupied!");
        }
    }
}