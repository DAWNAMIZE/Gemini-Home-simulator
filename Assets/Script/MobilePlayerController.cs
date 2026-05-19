using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class MobilePlayerController : MonoBehaviour
{
    [Header("Movement")]
    public float maxMoveSpeed = 4.5f;
    public float acceleration = 10f;
    public float deceleration = 14f;
    public float gravity = -20f;

    [Header("Virtual Stick")]
    public float maxStickDistance = 120f;
    public float stickDeadzone = 20f;

    [Header("Look")]
    public float lookSensitivity = 0.12f;
    public float lookSmoothTime = 0.04f;
    public float maxLookAngle = 80f;

    [Header("PC Test Settings")]
    public float pcLookSensitivity = 2.2f;

    [Header("References")]
    public Transform cameraHolder;

    [Header("Interaction")]
    public KeyCode interactKey = KeyCode.F;
    public float interactDistance = 3f;

    private CharacterController controller;

    // Movement
    private Vector3 currentVelocity;
    private float verticalVelocity;

    // Look
    private float xRotation;
    private Vector2 lookVelocity;
    private Vector2 currentLookDelta;

    // We do NOT need the old touch finger ID variables anymore, saving memory!
    
    void Start()
    {
        controller = GetComponent<CharacterController>();
    }

    // (KEEP YOUR UPDATE, FIXEDUPDATE, AND MOVEMENT LOGIC INTACT HERE IF YOU HAVE ANY OTHER CUSTOM CODE)

    public void Interact()
    {
        DoInteract();
    }

    void DoInteract()
    {
        if (cameraHolder == null) return;

        Ray ray = new Ray(cameraHolder.position, cameraHolder.forward);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, interactDistance))
        {
            GameObject interactedObject = hit.collider.gameObject;

            // NEW FIX: Prevent the Raycast from clicking the item we are already holding!
            if (PlayerInventory.instance != null && PlayerInventory.instance.currentlyHeldModel != null)
            {
                if (interactedObject == PlayerInventory.instance.currentlyHeldModel || interactedObject.transform.IsChildOf(PlayerInventory.instance.currentlyHeldModel.transform))
                {
                    return; // Stop right here, ignore it!
                }
            }

            // Trigger standard interactions (Doors, Appliances, Items)
            interactedObject.SendMessage("Interact", SendMessageOptions.DontRequireReceiver);

            // Special handling for physically held items (like the Pot)
            if (interactedObject.CompareTag("Holdable"))
            {
                if (PlayerInventory.instance != null)
                {
                    PlayerInventory.instance.HoldPhysicalItem(interactedObject);
                }
                Debug.Log("Physically holding the item: " + interactedObject.name);
            }
        }
    }
}