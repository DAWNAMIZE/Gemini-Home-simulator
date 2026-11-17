using UnityEngine;
using UnityEngine.UI;

public class MobileInputManager : MonoBehaviour
{
    [Header("UI References")]
    [Tooltip("Legacy movement joystick. Leave empty when using ScreenTouchMovement.")]
    public MobileJoystick moveJoystick;
    [Tooltip("Legacy look joystick. Leave empty when using ScreenTouchLook.")]
    public MobileJoystick lookJoystick;
    [Tooltip("Drag-anywhere screen touch movement. Replaces moveJoystick.")]
    public ScreenTouchMovement screenTouch;
    [Tooltip("Drag-anywhere screen touch look. Replaces lookJoystick.")]
    public ScreenTouchLook screenTouchLook;
    public Button interactButton;
    public HoldButton holdButton;
    public Button phoneButton;
    public Button jumpButton;
    public Button crouchButton;

    [Header("System Connections")]
    public PhoneUIManager phoneUIManager; // DRAG YOUR PHONE MANAGER HERE!

    [Header("Runtime Values")]
    [HideInInspector] public Vector2 MoveInput;
    [HideInInspector] public Vector2 LookInput;
    [HideInInspector] public bool InteractPressed;
    [HideInInspector] public bool InteractHeld;
    [HideInInspector] public bool JumpPressed;
    [HideInInspector] public bool CrouchToggled;

    private bool isCrouching = false;

    void Start()
    {
        if (screenTouch == null)
            screenTouch = Object.FindFirstObjectByType<ScreenTouchMovement>(FindObjectsInactive.Include);
        if (screenTouchLook == null)
            screenTouchLook = Object.FindFirstObjectByType<ScreenTouchLook>(FindObjectsInactive.Include);

        // The interact button in the scene may already be wired to
        // MobilePlayerController.Interact. Only add the legacy polled listener
        // when nothing else handles the click, otherwise one tap interacts twice.
        if (interactButton && interactButton.onClick.GetPersistentEventCount() == 0)
            interactButton.onClick.AddListener(OnInteractClicked);
        if (phoneButton) phoneButton.onClick.AddListener(OnPhoneClicked);
        if (jumpButton) jumpButton.onClick.AddListener(OnJumpClicked);
        if (crouchButton) crouchButton.onClick.AddListener(OnCrouchClicked);
    }

    void Update()
    {
        if (screenTouch != null)
            MoveInput = screenTouch.MoveInput;
        else if (moveJoystick)
            MoveInput = new Vector2(moveJoystick.Horizontal(), moveJoystick.Vertical());

        if (screenTouchLook != null)
            LookInput = screenTouchLook.LookInput;
        else if (lookJoystick)
            LookInput = new Vector2(lookJoystick.Horizontal(), lookJoystick.Vertical());

        if (holdButton)
            InteractHeld = holdButton.IsHeld;
    }

    void LateUpdate()
    {
        InteractPressed = false;
        JumpPressed = false;
    }

    void OnInteractClicked() => InteractPressed = true;

    // THIS IS THE FIX: It now calls the Toggle function!
    void OnPhoneClicked()
    {
        if (phoneUIManager == null) phoneUIManager = PhoneUIManager.instance;
        if (phoneUIManager == null) phoneUIManager = Object.FindFirstObjectByType<PhoneUIManager>();

        if (phoneUIManager != null)
        {
            phoneUIManager.TogglePlayerPhone();
        }
        else
        {
            Debug.LogError("Phone UI Manager is not assigned in the Inspector!");
        }
    }

    void OnJumpClicked() => JumpPressed = true;

    void OnCrouchClicked()
    {
        isCrouching = !isCrouching;
        CrouchToggled = isCrouching;
    }
}