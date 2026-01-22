using UnityEngine;

public class SnapPoint : MonoBehaviour
{
    [Header("Configuration")]
    public Transform snapPosition;  
    public GameObject ghostPreview; 
    
    [Header("Status")]
    public bool IsOccupied = false;
    public PickupItem CurrentItem; // This stores the canister

    public void SetHighlight(bool active)
    {
        if (IsOccupied) 
        {
            if (ghostPreview != null) ghostPreview.SetActive(false);
            return;
        }
        if (ghostPreview != null) ghostPreview.SetActive(active);
    }

    // UPDATED: Added '(PickupItem item)' so it can receive the canister
    public void LockItem(PickupItem item)
    {
        IsOccupied = true;
        CurrentItem = item; // This is the 'Handshake' that allows refilling
        Debug.Log("Mechanical Lock Engaged: " + item.name);
    }

    public void ReleaseItem()
    {
        IsOccupied = false;
        CurrentItem = null;
        Debug.Log("Socket Released.");
    }
}