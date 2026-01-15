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
            inputManager = FindObjectOfType<MobileInputManager>();
    }

    void Update()
    {
        if (inputManager != null && inputManager.InteractPressed)
        {
            Debug.Log("Interact button pressed!");

            Ray ray = new Ray(cam.transform.position, cam.transform.forward);

            if (Physics.Raycast(ray, out RaycastHit hit, interactRange))
            {
                Debug.Log("Raycast hit: " + hit.collider.name);

                Door door = hit.collider.GetComponentInParent<Door>();
                if (door != null)
                {
                    Debug.Log("Interacting with door: " + door.name);
                    door.Interact();
                    return;
                }

                IInteractable interactable = hit.collider.GetComponent<IInteractable>();
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
