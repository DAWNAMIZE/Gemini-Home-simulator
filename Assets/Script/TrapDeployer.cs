using UnityEngine;

/// <summary>
/// Trap kit held in the player's hand. Firing while it is equipped places the
/// matching world trap on the ground ahead and consumes the kit.
/// </summary>
public class TrapDeployer : MonoBehaviour
{
    [Header("Trap Kit")]
    public GameObject trapPrefab;
    public string trapName = "Trap";
    public float placeDistance = 1.65f;

    public bool TryDeploy()
    {
        PlayerInventory inventory = PlayerInventory.instance;
        if (inventory == null || inventory.currentlyHeldModel != gameObject)
            return false;

        if (trapPrefab == null)
        {
            ToastUI.Show("This trap kit is missing its trap prefab.");
            return false;
        }

        Vector3 forward = inventory.transform.forward;
        forward.y = 0f;
        if (forward.sqrMagnitude < 0.001f) forward = Vector3.forward;
        forward.Normalize();

        Vector3 position = inventory.transform.position + forward * placeDistance;
        Vector3 rayOrigin = position + Vector3.up * 2f;

        if (Physics.Raycast(rayOrigin, Vector3.down, out RaycastHit hit, 5f))
            position = hit.point;
        else
            position.y = inventory.transform.position.y;

        GameObject trap = Instantiate(trapPrefab, position + Vector3.up * 0.02f,
            Quaternion.LookRotation(forward, Vector3.up));
        trap.name = trapPrefab.name;
        trap.SetActive(true);

        inventory.ConsumeEquippedItem();
        ToastUI.Show(trapName + " placed. Lure enemies onto it.");
        return true;
    }
}
