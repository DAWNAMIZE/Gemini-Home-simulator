using UnityEngine;
using UnityEngine.UI;

public class MobileInputManager : MonoBehaviour
{
    [Header("UI References")]
    public MobileJoystick moveJoystick;
    public MobileJoystick lookJoystick;
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
        if (interactButton) interactButton.onClick.AddListener(OnInteractClicked);
        if (phoneButton) phoneButton.onClick.AddListener(OnPhoneClicked);
        if (jumpButton) jumpButton.onClick.AddListener(OnJumpClicked);
        if (crouchButton) crouchButton.onClick.AddListener(OnCrouchClicked);
    }

    void Update()
    {
        if (moveJoystick)
            MoveInput = new Vector2(moveJoystick.Horizontal(), moveJoystick.Vertical());

        if (lookJoystick)
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
        if (phoneUIManager != null)
        {
            phoneUIManager.ToggleBackpack();
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