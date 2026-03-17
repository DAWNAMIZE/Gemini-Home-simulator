using UnityEngine;

public class PotSnapPoint : MonoBehaviour, IInteractable
{
    [Header("Configuration")]
    public Transform snapPosition; // The exact center of where the pot sits
    public GameObject ghostPreview; // The transparent hint

    [Header("Status")]
    public bool IsOccupied = false;
    public PortablePot CurrentPot; // Matches 'CurrentItem' from your SnapPoint script

    void Update()
    {
        // MEMORY FIX: If player snatches the pot back directly from the table
        if (IsOccupied && CurrentPot != null && PlayerInventory.instance.heldPot == CurrentPot)
        {
            ReleasePot();
        }

        // Handle the Ghost Cylinder highlight automatically
        bool playerHasPot = PlayerInventory.instance != null && PlayerInventory.instance.isHoldingPot;
        SetHighlight(playerHasPot);
    }

    public void Interact()
    {
        // 1. If holding a pot and the socket is empty -> Lock it in
        if (PlayerInventory.instance.isHoldingPot && !IsOccupied)
        {
            LockPot(PlayerInventory.instance.heldPot);
        }
        // 2. If the socket is occupied -> Let the player pick the pot back up
        else if (IsOccupied && CurrentPot != null)
        {
            CurrentPot.Interact(); // Triggers the pot's own pickup logic
        }
    }

    // --- MIRRORED FROM YOUR SNAPPOINT.CS ---

    public void SetHighlight(bool active)
    {
        if (IsOccupied) 
        {
            if (ghostPreview != null) ghostPreview.SetActive(false);
            return;
        }
        if (ghostPreview != null) ghostPreview.SetActive(active);
    }

    public void LockPot(PortablePot pot)
    {
        IsOccupied = true;
        CurrentPot = pot;

        // 1. Un-hide the pot so we can actually see it
        pot.gameObject.SetActive(true);

        // 2. Snap rigidly to the socket position
        pot.transform.SetParent(null);
        pot.transform.position = snapPosition.position;
        pot.transform.rotation = snapPosition.rotation;

        // 3. Re-enable the collider so the crosshair can interact with it again
        Collider col = pot.GetComponent<Collider>();
        if (col != null) col.enabled = true;

        // 4. Clear from player's hand
        PlayerInventory.instance.DropPot();

        Debug.Log("Mechanical Lock Engaged: " + pot.name);
    }

    public void ReleasePot()
    {
        IsOccupied = false;
        CurrentPot = null;
        Debug.Log("Pot Socket Released.");
    }
}