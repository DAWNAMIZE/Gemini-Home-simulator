using UnityEngine;

public class SnapPoint : MonoBehaviour, IInteractable
{
    public enum SocketType { Universal, PotOnly, CanisterOnly }

    [Header("Configuration")]
    public SocketType acceptsItem = SocketType.Universal; // Allows you to restrict what fits here
    public Transform snapPosition;  
    public GameObject ghostPreview; 
    
    [Header("Status")]
    public bool IsOccupied = false;
    public GameObject CurrentItem; // Upgraded to hold ANY game object, not just PickupItems

    public void SetHighlight(bool active)
    {
        if (IsOccupied) 
        {
            if (ghostPreview != null) ghostPreview.SetActive(false);
            return;
        }
        if (ghostPreview != null) ghostPreview.SetActive(active);
    }

    public void Interact()
    {
        // CASE 1: Socket is empty, and player is holding something
        if (!IsOccupied && PlayerHands.instance != null && PlayerHands.instance.currentHoldType != PlayerHands.HoldType.None)
        {
            // Safety Check: Does this socket accept what the player is holding?
            if (acceptsItem == SocketType.PotOnly && !PlayerHands.instance.isHoldingPot) return;
            if (acceptsItem == SocketType.CanisterOnly && !PlayerHands.instance.isHoldingCanister) return;

            // 1. Identify what we are holding
            GameObject itemToSnap = null;
            if (PlayerHands.instance.isHoldingPot) itemToSnap = PlayerHands.instance.heldPot.gameObject;
            else if (PlayerHands.instance.isHoldingCanister) itemToSnap = PlayerHands.instance.heldCanister.gameObject;

            // 2. Tell the hands to let go
            PlayerHands.instance.Release();

            // 3. Snap it to the socket
            itemToSnap.transform.SetParent(snapPosition != null ? snapPosition : transform);
            itemToSnap.transform.localPosition = Vector3.zero;
            itemToSnap.transform.localRotation = Quaternion.identity;
            itemToSnap.transform.localScale = Vector3.one;

            // 4. Turn collider back on so it can be picked up later
            Collider col = itemToSnap.GetComponent<Collider>();
            if (col != null) col.enabled = true;

            LockItem(itemToSnap);
        }
        // CASE 2: Socket has an item, and player's hands are empty
        else if (IsOccupied && CurrentItem != null && PlayerHands.instance != null && PlayerHands.instance.currentHoldType == PlayerHands.HoldType.None)
        {
            // Forward the click to the item itself so the player picks it up!
            IInteractable interactable = CurrentItem.GetComponent<IInteractable>();
            if (interactable != null) interactable.Interact();
            
            ReleaseItem();
        }
    }

    // Upgraded these to accept universal GameObjects to avoid breaking other scripts
    public void LockItem(GameObject item)
    {
        IsOccupied = true;
        CurrentItem = item; 
        if (ghostPreview != null) ghostPreview.SetActive(false);
        Debug.Log("Mechanical Lock Engaged: " + item.name);
    }

    public void ReleaseItem()
    {
        IsOccupied = false;
        CurrentItem = null;
        Debug.Log("Socket Released.");
    }
}