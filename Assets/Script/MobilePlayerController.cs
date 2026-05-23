using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class MobilePlayerController : MonoBehaviour
{
    [Header("Movement")]
    public float maxMoveSpeed = 4.5f;
    public float acceleration = 10f;
    public float deceleration = 14f;
    public float gravity = -20f;

    [Header("Look")]
    public float lookSensitivity = 0.12f;
    public float lookSmoothTime = 0.04f;
    public float maxLookAngle = 80f;

    [Header("PC Test Settings")]
    public float pcLookSensitivity = 2.2f;

    [Header("References")]
    public Transform cameraHolder;
    public MobileInputManager inputManager; // Automatically grabs your input manager!

    [Header("Interaction")]
    public KeyCode interactKey = KeyCode.F;
    public float interactDistance = 3f;

    private CharacterController controller;

    // Movement Variables
    private Vector3 currentVelocity;
    private float verticalVelocity;

    // Look Variables
    private float xRotation;
    private Vector2 lookVelocity;
    private Vector2 currentLookDelta;

    void Start()
    {
        controller = GetComponent<CharacterController>();
        
        // Find the input manager if it wasn't manually assigned in the inspector
        if (inputManager == null)
        {
            inputManager = Object.FindFirstObjectByType<MobileInputManager>();
        }
    }

    void Update()
    {
        HandleMovement();
        HandleLook();

        // PC keyboard interaction testing
        if (Input.GetKeyDown(interactKey))
        {
            DoInteract();
        }
    }

    void HandleMovement()
    {
        Vector2 input = Vector2.zero;
        
        // 1. Get Mobile Joystick Input
        if (inputManager != null)
        {
            input = inputManager.MoveInput;
        }

        // 2. PC WASD Fallback (Ensures your PC testing still works!)
        if (input.sqrMagnitude == 0)
        {
            input = new Vector2(Input.GetAxis("Horizontal"), Input.GetAxis("Vertical"));
        }

        // Calculate target velocity based on where the player is facing
        Vector3 moveDirection = transform.right * input.x + transform.forward * input.y;
        Vector3 targetVelocity = moveDirection * maxMoveSpeed;

        // Apply acceleration/deceleration smoothly
        float currentAccel = (input.magnitude > 0) ? acceleration : deceleration;
        currentVelocity = Vector3.Lerp(currentVelocity, targetVelocity, currentAccel * Time.deltaTime);

        // Apply Gravity
        if (controller.isGrounded)
        {
            verticalVelocity = -2f; // Slight downward force to stay grounded on slopes
        }
        else
        {
            verticalVelocity += gravity * Time.deltaTime;
        }

        // Apply final movement to the CharacterController
        Vector3 finalVelocity = currentVelocity + (Vector3.up * verticalVelocity);
        controller.Move(finalVelocity * Time.deltaTime);
    }

    void HandleLook()
    {
        if (cameraHolder == null) return;

        Vector2 lookInput = Vector2.zero;

        // 1. Get Mobile Joystick Input
        if (inputManager != null)
        {
            lookInput = inputManager.LookInput;
        }

        // 2. PC Mouse Fallback (Ensures PC testing still works!)
        if (lookInput.sqrMagnitude == 0)
        {
            lookInput = new Vector2(Input.GetAxis("Mouse X") * pcLookSensitivity, Input.GetAxis("Mouse Y") * pcLookSensitivity);
        }

        // Smooth the look input to avoid jittery camera movement
        currentLookDelta = Vector2.SmoothDamp(currentLookDelta, lookInput, ref lookVelocity, lookSmoothTime);

        // Vertical look (Rotates the Camera up and down)
        xRotation -= currentLookDelta.y * lookSensitivity * 100f * Time.deltaTime;
        xRotation = Mathf.Clamp(xRotation, -maxLookAngle, maxLookAngle);
        cameraHolder.localRotation = Quaternion.Euler(xRotation, 0f, 0f);

        // Horizontal look (Rotates the entire Player Body left and right)
        transform.Rotate(Vector3.up * (currentLookDelta.x * lookSensitivity * 100f * Time.deltaTime));
    }

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