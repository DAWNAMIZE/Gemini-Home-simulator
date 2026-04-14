using UnityEngine;

public class PotSnapPoint : MonoBehaviour, IInteractable
{
    [Header("Snap Settings")]
    public Transform snapTransform; // The exact invisible point the pot will snap to

    [Header("State")]
    public bool IsOccupied = false;
    public PortablePot CurrentPot;

    public void Interact()
    {
        // CASE 1: The player is holding a pot, and this socket is EMPTY
        if (!IsOccupied && PlayerHands.instance != null && PlayerHands.instance.isHoldingPot)
        {
            // 1. Identify the pot in the player's hands
            PortablePot potToSnap = PlayerHands.instance.heldPot;

            // 2. Tell the hands to officially let go of it
            PlayerHands.instance.Release();

            // 3. Move the pot to this socket
            potToSnap.transform.SetParent(snapTransform != null ? snapTransform : transform);
            potToSnap.transform.localPosition = Vector3.zero;
            potToSnap.transform.localRotation = Quaternion.identity;
            potToSnap.transform.localScale = Vector3.one; 
            
            // 4. Turn the pot's collider back ON so the player can click it again later
            Collider col = potToSnap.GetComponent<Collider>();
            if (col != null) col.enabled = true;

            // 5. Update this socket's memory
            CurrentPot = potToSnap;
            IsOccupied = true;

            Debug.Log("Pot snapped to the surface!");
            return;
        }

        // CASE 2: The socket already has a pot, and the player's hands are EMPTY
        if (IsOccupied && CurrentPot != null && PlayerHands.instance != null && PlayerHands.instance.currentHoldType == PlayerHands.HoldType.None)
        {
            // If the player clicks the table instead of the pot, we forward the click directly to the pot
            CurrentPot.Interact();
            
            // Clear the socket's memory because the player just picked it up
            CurrentPot = null;
            IsOccupied = false;
            return;
        }

        // CASE 3: The player has a pot, but there is ALREADY a pot here
        if (IsOccupied && PlayerHands.instance != null && PlayerHands.instance.isHoldingPot)
        {
            Debug.Log("There is already a pot occupying this space!");
        }
    }
}