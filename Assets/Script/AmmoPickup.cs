using UnityEngine;

/// <summary>
/// Simple bullet pickup. Adds rounds to the shared pistol ammo pool.
/// </summary>
public class AmmoPickup : MonoBehaviour, IInteractable
{
    public int amount = 5;

    public void Interact()
    {
        WeaponInstance.AddAmmo(amount);
        Destroy(gameObject);
    }
}
