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
    public float lookSensitivity = 120f;
    public float lookSmoothTime = 0.04f;
    public float maxLookAngle = 80f;

    [Header("References")]
    public Transform cameraHolder;
    public MobileInputManager inputManager;

    [Header("Interaction")]
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
        
        if (inputManager == null)
        {
            inputManager = Object.FindFirstObjectByType<MobileInputManager>();
        }
    }

    void Update()
    {
        HandleMovement();
        HandleLook();
    }

    void HandleMovement()
    {
        // Reads ONLY from the Left UI Joystick
        Vector2 input = Vector2.zero;
        if (inputManager != null)
        {
            input = inputManager.MoveInput;
        }

        // Calculate movement relative to player orientation
        Vector3 moveDirection = transform.right * input.x + transform.forward * input.y;
        Vector3 targetVelocity = moveDirection * maxMoveSpeed;

        // Apply acceleration/deceleration
        float currentAccel = (input.sqrMagnitude > 0.01f) ? acceleration : deceleration;
        currentVelocity = Vector3.Lerp(currentVelocity, targetVelocity, currentAccel * Time.deltaTime);

        // Apply Gravity
        if (controller.isGrounded)
        {
            verticalVelocity = -2f;
        }
        else
        {
            verticalVelocity += gravity * Time.deltaTime;
        }

        Vector3 finalVelocity = currentVelocity + (Vector3.up * verticalVelocity);
        controller.Move(finalVelocity * Time.deltaTime);
    }

    void HandleLook()
    {
        if (cameraHolder == null) return;

        // Reads ONLY from the Right UI Joystick
        Vector2 lookInput = Vector2.zero;
        if (inputManager != null)
        {
            lookInput = inputManager.LookInput;
        }

        // Smooth joystick look input
        currentLookDelta = Vector2.SmoothDamp(currentLookDelta, lookInput, ref lookVelocity, lookSmoothTime);

        // Vertical look (Camera pitch)
        xRotation -= currentLookDelta.y * lookSensitivity * Time.deltaTime;
        xRotation = Mathf.Clamp(xRotation, -maxLookAngle, maxLookAngle);
        cameraHolder.localRotation = Quaternion.Euler(xRotation, 0f, 0f);

        // Horizontal look (Player body yaw)
        transform.Rotate(Vector3.up * (currentLookDelta.x * lookSensitivity * Time.deltaTime));
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

            interactedObject.SendMessage("Interact", SendMessageOptions.DontRequireReceiver);

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