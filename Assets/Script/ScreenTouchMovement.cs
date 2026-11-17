using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
#endif

/// <summary>
/// Replaces the on-screen movement joystick. Drag anywhere on the left side of
/// the screen to move; UI buttons and the look joystick are ignored.
/// Mouse drag works the same way in the editor for quick testing.
/// Uses the Input System package when active, with a legacy fallback.
/// </summary>
[DefaultExecutionOrder(-100)]
public class ScreenTouchMovement : MonoBehaviour
{
    [Header("Movement Drag")]
    [Tooltip("Pixels of drag needed for full movement speed.")]
    public float dragRadius = 130f;
    [Tooltip("Ignore very small drags so a tap does not nudge the player.")]
    public float deadZone = 8f;
    [Tooltip("How quickly the movement input follows the finger.")]
    public float smoothing = 22f;
    [Tooltip("Only start movement drags on the left half of the screen.")]
    public bool leftHalfOnly = true;

    [Header("References")]
    public MobileInputManager inputManager;

    public Vector2 MoveInput { get; private set; }

    private int moveFingerId = -1;
    private Vector2 moveOrigin;
    private Vector2 moveTarget;

    private bool mouseDragging;
    private Vector2 mouseOrigin;

    private readonly List<RaycastResult> uiHits = new List<RaycastResult>();
    private PointerEventData uiPointerData;

    void Awake()
    {
        if (inputManager == null) inputManager = GetComponent<MobileInputManager>();
        if (inputManager == null) inputManager = Object.FindFirstObjectByType<MobileInputManager>();
    }

    void Update()
    {
#if ENABLE_INPUT_SYSTEM
        ReadInputSystem();
#else
        ReadLegacyInput();
#endif

        MoveInput = Vector2.Lerp(MoveInput, moveTarget,
            1f - Mathf.Exp(-smoothing * Time.unscaledDeltaTime));
    }

#if ENABLE_INPUT_SYSTEM
    private void ReadInputSystem()
    {
        Touchscreen touchscreen = Touchscreen.current;
        if (touchscreen != null && HasActiveTouch(touchscreen))
        {
            mouseDragging = false;
            HandleTouches(touchscreen);
            return;
        }

        HandleMouse(Mouse.current);
    }

    private static bool HasActiveTouch(Touchscreen touchscreen)
    {
        foreach (TouchControl touch in touchscreen.touches)
        {
            if (touch.phase.ReadValue() != UnityEngine.InputSystem.TouchPhase.None)
                return true;
        }

        return false;
    }

    private void HandleTouches(Touchscreen touchscreen)
    {
        bool moveFingerSeen = false;

        foreach (TouchControl touch in touchscreen.touches)
        {
            UnityEngine.InputSystem.TouchPhase phase = touch.phase.ReadValue();
            if (phase == UnityEngine.InputSystem.TouchPhase.None) continue;

            int fingerId = touch.touchId.ReadValue();
            Vector2 position = touch.position.ReadValue();

            if (moveFingerId == -1)
            {
                if (phase == UnityEngine.InputSystem.TouchPhase.Began &&
                    !IsOverUI(position) &&
                    IsMovementSide(position))
                {
                    moveFingerId = fingerId;
                    moveOrigin = position;
                    moveTarget = Vector2.zero;
                    moveFingerSeen = true;
                }
                continue;
            }

            if (fingerId != moveFingerId) continue;

            if (phase == UnityEngine.InputSystem.TouchPhase.Ended ||
                phase == UnityEngine.InputSystem.TouchPhase.Canceled)
            {
                moveFingerId = -1;
                moveTarget = Vector2.zero;
            }
            else
            {
                moveTarget = CalculateMove(position, moveOrigin);
                moveFingerSeen = true;
            }
        }

        if (moveFingerId != -1 && !moveFingerSeen)
        {
            moveFingerId = -1;
            moveTarget = Vector2.zero;
        }
    }

    private void HandleMouse(Mouse mouse)
    {
        if (mouse == null)
        {
            mouseDragging = false;
            moveTarget = Vector2.zero;
            return;
        }

        Vector2 position = mouse.position.ReadValue();

        if (mouse.leftButton.wasPressedThisFrame &&
            !IsOverUI(position) &&
            IsMovementSide(position))
        {
            mouseDragging = true;
            mouseOrigin = position;
        }

        if (mouseDragging && mouse.leftButton.isPressed)
            moveTarget = CalculateMove(position, mouseOrigin);

        if (mouseDragging && mouse.leftButton.wasReleasedThisFrame)
        {
            mouseDragging = false;
            moveTarget = Vector2.zero;
        }
    }
#else
    private void ReadLegacyInput()
    {
        if (Input.touchCount > 0)
        {
            mouseDragging = false;
            HandleLegacyTouches();
            return;
        }

        HandleLegacyMouse();
    }

    private void HandleLegacyTouches()
    {
        bool moveFingerSeen = false;

        for (int i = 0; i < Input.touchCount; i++)
        {
            Touch touch = Input.GetTouch(i);

            if (moveFingerId == -1)
            {
                if (touch.phase == TouchPhase.Began &&
                    !IsOverUI(touch.position) &&
                    IsMovementSide(touch.position))
                {
                    moveFingerId = touch.fingerId;
                    moveOrigin = touch.position;
                    moveTarget = Vector2.zero;
                    moveFingerSeen = true;
                }
                continue;
            }

            if (touch.fingerId != moveFingerId) continue;

            if (touch.phase == TouchPhase.Ended || touch.phase == TouchPhase.Canceled)
            {
                moveFingerId = -1;
                moveTarget = Vector2.zero;
            }
            else
            {
                moveTarget = CalculateMove(touch.position, moveOrigin);
                moveFingerSeen = true;
            }
        }

        if (moveFingerId != -1 && !moveFingerSeen)
        {
            moveFingerId = -1;
            moveTarget = Vector2.zero;
        }
    }

    private void HandleLegacyMouse()
    {
        Vector2 position = Input.mousePosition;

        if (Input.GetMouseButtonDown(0) &&
            !IsOverUI(position) &&
            IsMovementSide(position))
        {
            mouseDragging = true;
            mouseOrigin = position;
        }

        if (mouseDragging && Input.GetMouseButton(0))
            moveTarget = CalculateMove(position, mouseOrigin);

        if (mouseDragging && Input.GetMouseButtonUp(0))
        {
            mouseDragging = false;
            moveTarget = Vector2.zero;
        }
    }
#endif

    private Vector2 CalculateMove(Vector2 position, Vector2 origin)
    {
        Vector2 delta = position - origin;
        if (delta.magnitude < deadZone) return Vector2.zero;

        return Vector2.ClampMagnitude(delta / Mathf.Max(1f, dragRadius), 1f);
    }

    private bool IsMovementSide(Vector2 screenPosition)
    {
        if (!leftHalfOnly) return true;
        return screenPosition.x <= Screen.width * 0.5f;
    }

    /// <summary>True when a UI element sits under the given screen point.</summary>
    private bool IsOverUI(Vector2 screenPosition)
    {
        EventSystem eventSystem = EventSystem.current;
        if (eventSystem == null) return false;

        if (uiPointerData == null) uiPointerData = new PointerEventData(eventSystem);
        uiPointerData.position = screenPosition;

        uiHits.Clear();
        eventSystem.RaycastAll(uiPointerData, uiHits);
        return uiHits.Count > 0;
    }

    private void OnDisable()
    {
        moveFingerId = -1;
        mouseDragging = false;
        moveTarget = Vector2.zero;
        MoveInput = Vector2.zero;
    }
}
