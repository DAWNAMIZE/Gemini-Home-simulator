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

    // Touch tracking
    private int moveFingerId = -1;
    private int lookFingerId = -1;

    private Vector2 moveTouchStart;
    private Vector2 moveInput;
    private Vector2 lookInput;

    void Start()
    {
        controller = GetComponent<CharacterController>();

        if (cameraHolder == null && Camera.main != null)
            cameraHolder = Camera.main.transform.parent;
    }

    void Update()
    {
#if UNITY_EDITOR || UNITY_STANDALONE
        HandlePCInput();
#else
        HandleTouchInput();
#endif
        HandleMovement();
        HandleLook();
        HandleInteraction();
    }

    // =========================
    // PC INPUT
    // =========================
    void HandlePCInput()
    {
        moveInput = new Vector2(
            Input.GetAxisRaw("Horizontal"),
            Input.GetAxisRaw("Vertical")
        );
        moveInput = Vector2.ClampMagnitude(moveInput, 1f);

        lookInput = new Vector2(
            Input.GetAxis("Mouse X"),
            Input.GetAxis("Mouse Y")
        ) * pcLookSensitivity;
    }

    // =========================
    // MOBILE INPUT
    // =========================
    void HandleTouchInput()
    {
        moveInput = Vector2.zero;
        lookInput = Vector2.zero;

        foreach (Touch touch in Input.touches)
        {
            if (touch.phase == TouchPhase.Began)
            {
                if (touch.position.x < Screen.width * 0.5f && moveFingerId == -1)
                {
                    moveFingerId = touch.fingerId;
                    moveTouchStart = touch.position;
                }
                else if (touch.position.x >= Screen.width * 0.5f && lookFingerId == -1)
                {
                    lookFingerId = touch.fingerId;
                }
            }

            if (touch.fingerId == moveFingerId)
            {
                if (touch.phase == TouchPhase.Moved || touch.phase == TouchPhase.Stationary)
                {
                    Vector2 delta = touch.position - moveTouchStart;
                    float dist = delta.magnitude;

                    if (dist > stickDeadzone)
                    {
                        float strength = Mathf.Clamp01(dist / maxStickDistance);
                        moveInput = delta.normalized * strength;
                    }
                }

                if (touch.phase == TouchPhase.Ended || touch.phase == TouchPhase.Canceled)
                    moveFingerId = -1;
            }

            if (touch.fingerId == lookFingerId)
            {
                if (touch.phase == TouchPhase.Moved)
                    lookInput = touch.deltaPosition;

                if (touch.phase == TouchPhase.Ended || touch.phase == TouchPhase.Canceled)
                    lookFingerId = -1;
            }
        }
    }

    // =========================
    // MOVEMENT
    // =========================
    void HandleMovement()
    {
        Vector3 inputDir =
            transform.forward * moveInput.y +
            transform.right * moveInput.x;

        Vector3 targetVelocity = inputDir * maxMoveSpeed;

        currentVelocity = Vector3.MoveTowards(
            currentVelocity,
            targetVelocity,
            (inputDir.magnitude > 0.01f ? acceleration : deceleration) * Time.deltaTime
        );

        if (controller.isGrounded)
        {
            if (verticalVelocity < 0)
                verticalVelocity = -2f;
        }
        else
        {
            verticalVelocity += gravity * Time.deltaTime;
        }

        Vector3 finalMove = currentVelocity;
        finalMove.y = verticalVelocity;

        controller.Move(finalMove * Time.deltaTime);
    }

    // =========================
    // LOOK
    // =========================
    void HandleLook()
    {
        Vector2 targetLook = lookInput * lookSensitivity;

        currentLookDelta = Vector2.SmoothDamp(
            currentLookDelta,
            targetLook,
            ref lookVelocity,
            lookSmoothTime
        );

        xRotation -= currentLookDelta.y;
        xRotation = Mathf.Clamp(xRotation, -maxLookAngle, maxLookAngle);

        cameraHolder.localRotation = Quaternion.Euler(xRotation, 0f, 0f);
        transform.Rotate(Vector3.up * currentLookDelta.x);
    }

    // =========================
    // INTERACTION
    // =========================
    void HandleInteraction()
    {
#if UNITY_EDITOR || UNITY_STANDALONE
        if (Input.GetMouseButtonDown(0) || Input.GetKeyDown(interactKey))
        {
            DoInteract();
        }
#endif
    }

    // Called from UI button on mobile
    public void Interact()
    {
        DoInteract();
    }

    void DoInteract()
    {
        if (cameraHolder == null)
            return;

        Ray ray = new Ray(cameraHolder.position, cameraHolder.forward);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, interactDistance))
        {
            hit.collider.gameObject.SendMessage(
                "Interact",
                SendMessageOptions.DontRequireReceiver
            );

            Debug.Log("Interacted with: " + hit.collider.name);
        }
    }
}
