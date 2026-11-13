using UnityEngine;

public class PlayerInteract : MonoBehaviour
{
    [Header("Settings")]
    public float interactRange = 3f;

    [Header("References")]
    public MobileInputManager inputManager;
    public Camera cam;

    void Start()
    {
        if (cam == null)
            cam = GetComponentInChildren<Camera>();

        if (inputManager == null)
            inputManager = Object.FindFirstObjectByType<MobileInputManager>();
    }

    void Update()
    {
        if (inputManager != null && inputManager.InteractPressed)
        {
            if (cam == null) return;

            Debug.Log("Interact button pressed!");

            Ray ray = new Ray(cam.transform.position, cam.transform.forward);

            if (Physics.Raycast(ray, out RaycastHit hit, interactRange))
            {
                Debug.Log("Raycast hit: " + hit.collider.name);

                // Feeding the oven takes priority over all other oven interactions.
                OvenFuel oven = hit.collider.GetComponentInParent<OvenFuel>();
                if (oven != null && oven.TryInsertHeldWood()) return;

                Door door = hit.collider.GetComponentInParent<Door>();
                if (door != null)
                {
                    Debug.Log("Interacting with door: " + door.name);
                    door.Interact();
                    return;
                }

                IInteractable interactable = hit.collider.GetComponentInParent<IInteractable>();
                if (interactable != null)
                {
                    Debug.Log("Interacting with: " + interactable);
                    interactable.Interact();
                }
            }
            else
            {
                Debug.Log("Raycast found nothing.");
            }
        }
    }

    private void OnDrawGizmosSelected()
    {
        if (cam == null) return;

        Gizmos.color = Color.yellow;
        Gizmos.DrawRay(cam.transform.position, cam.transform.forward * interactRange);
    }
}
