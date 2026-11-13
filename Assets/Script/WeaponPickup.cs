using UnityEngine;

/// <summary>
/// Replaces the old ItemPickup behaviour on weapon scene objects. Weapons are
/// stored as whole scene instances, so equipping simply moves the same object
/// into the player's hand instead of spawning duplicates.
/// </summary>
public class WeaponPickup : MonoBehaviour, IInteractable
{
    public void Interact()
    {
        if (PlayerInventory.instance == null)
        {
            Debug.LogWarning("WeaponPickup: PlayerInventory was not found.");
            return;
        }

        PlayerInventory.instance.PickUpWeapon(gameObject);
    }
}
