using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class MobilePlayerController : MonoBehaviour
{
    public static MobilePlayerController instance;

    [Header("Movement")]
    public float maxMoveSpeed = 4.5f;
    public float acceleration = 10f;
    public float deceleration = 14f;
    public float gravity = -20f;

    [Header("Look")]
    public float lookSensitivity = 120f;
    public float lookSmoothTime = 0.04f;
    public float maxLookAngle = 80f;

    [Header("Recoil")]
    [Tooltip("How quickly the camera settles back after a shot.")]
    public float recoilReturnSpeed = 11f;
    [Tooltip("Maximum accumulated upward recoil in degrees.")]
    public float maxRecoilPitch = 12f;
    [Tooltip("Maximum accumulated sideways recoil in degrees.")]
    public float maxRecoilYaw = 5f;
    [Tooltip("Field-of-view punch added per shot, in degrees.")]
    public float recoilFovPunch = 2.2f;
    [Tooltip("How quickly the FOV punch settles back.")]
    public float recoilFovReturnSpeed = 9f;

    [Header("References")]
    public Transform cameraHolder;
    public MobileInputManager inputManager;

    [Header("Interaction")]
    public float interactDistance = 3f;

    private CharacterController controller;
    private Camera playerCamera;
    private float baseFieldOfView = 60f;
    private float recoilPitch;
    private float recoilYaw;
    private float recoilFov;

    // Movement Variables
    private Vector3 currentVelocity;
    private float verticalVelocity;

    // Look Variables
    private float xRotation;
    private Vector2 lookVelocity;
    private Vector2 currentLookDelta;

    void Awake()
    {
        if (instance == null) instance = this;
    }

    void OnDestroy()
    {
        if (instance == this) instance = null;
    }

    void Start()
    {
        controller = GetComponent<CharacterController>();
        
        if (inputManager == null)
        {
            inputManager = Object.FindFirstObjectByType<MobileInputManager>();
        }

        if (playerCamera == null)
            playerCamera = cameraHolder != null ? cameraHolder.GetComponent<Camera>() : GetComponentInChildren<Camera>();

        if (playerCamera != null)
            baseFieldOfView = playerCamera.fieldOfView;
    }

    void Update()
    {
        // The game-over screen freezes time, but keep the controller quiet too.
        if (PlayerVitals.instance != null && PlayerVitals.instance.isDead) return;

        HandleMovement();
        UpdateRecoil(Time.deltaTime);
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
        cameraHolder.localRotation = Quaternion.Euler(
            xRotation + recoilPitch, recoilYaw, 0f);

        // Horizontal look (Player body yaw)
        transform.Rotate(Vector3.up * (currentLookDelta.x * lookSensitivity * Time.deltaTime));
    }

    /// <summary>
    /// Adds a camera kick. The pitch kicks up immediately and springs back, so
    /// each shot is felt without permanently stealing the player's aim.
    /// </summary>
    public void AddRecoil(float pitchKick, float yawKick)
    {
        recoilPitch = Mathf.Clamp(recoilPitch + pitchKick, -maxRecoilPitch, maxRecoilPitch);
        recoilYaw = Mathf.Clamp(recoilYaw + yawKick, -maxRecoilYaw, maxRecoilYaw);
        recoilFov = Mathf.Clamp(recoilFov + recoilFovPunch, 0f, 8f);
    }

    private void UpdateRecoil(float deltaTime)
    {
        if (deltaTime <= 0f) return;

        float returnLerp = 1f - Mathf.Exp(-recoilReturnSpeed * deltaTime);
        recoilPitch = Mathf.Lerp(recoilPitch, 0f, returnLerp);
        recoilYaw = Mathf.Lerp(recoilYaw, 0f, returnLerp);

        float fovLerp = 1f - Mathf.Exp(-recoilFovReturnSpeed * deltaTime);
        recoilFov = Mathf.Lerp(recoilFov, 0f, fovLerp);

        if (playerCamera != null)
            playerCamera.fieldOfView = baseFieldOfView - recoilFov;
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