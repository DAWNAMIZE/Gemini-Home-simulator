using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
#endif

/// <summary>
/// Replaces the on-screen look joystick. Drag anywhere on the right side of the
/// screen to look around; UI buttons are ignored. Mouse drag works the same way
/// in the editor for quick testing.
/// Uses the Input System package when active, with a legacy fallback.
/// </summary>
[DefaultExecutionOrder(-100)]
public class ScreenTouchLook : MonoBehaviour
{
    [Header("Look Drag")]
    [Tooltip("Pixels of drag needed for full look speed.")]
    public float dragRadius = 130f;
    [Tooltip("Ignore very small drags so a tap does not nudge the camera.")]
    public float deadZone = 8f;
    [Tooltip("How quickly the look input follows the finger.")]
    public float smoothing = 22f;
    [Tooltip("Only start look drags on the right half of the screen.")]
    public bool rightHalfOnly = true;

    [Header("References")]
    public MobileInputManager inputManager;

    public Vector2 LookInput { get; private set; }

    private int lookFingerId = -1;
    private Vector2 lookOrigin;
    private Vector2 lookTarget;

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

        LookInput = Vector2.Lerp(LookInput, lookTarget,
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
        bool lookFingerSeen = false;

        foreach (TouchControl touch in touchscreen.touches)
        {
            UnityEngine.InputSystem.TouchPhase phase = touch.phase.ReadValue();
            if (phase == UnityEngine.InputSystem.TouchPhase.None) continue;

            int fingerId = touch.touchId.ReadValue();
            Vector2 position = touch.position.ReadValue();

            if (lookFingerId == -1)
            {
                if (phase == UnityEngine.InputSystem.TouchPhase.Began &&
                    !IsOverUI(position) &&
                    IsLookSide(position))
                {
                    lookFingerId = fingerId;
                    lookOrigin = position;
                    lookTarget = Vector2.zero;
                    lookFingerSeen = true;
                }
                continue;
            }

            if (fingerId != lookFingerId) continue;

            if (phase == UnityEngine.InputSystem.TouchPhase.Ended ||
                phase == UnityEngine.InputSystem.TouchPhase.Canceled)
            {
                lookFingerId = -1;
                lookTarget = Vector2.zero;
            }
            else
            {
                lookTarget = CalculateLook(position, lookOrigin);
                lookFingerSeen = true;
            }
        }

        if (lookFingerId != -1 && !lookFingerSeen)
        {
            lookFingerId = -1;
            lookTarget = Vector2.zero;
        }
    }

    private void HandleMouse(Mouse mouse)
    {
        if (mouse == null)
        {
            mouseDragging = false;
            lookTarget = Vector2.zero;
            return;
        }

        Vector2 position = mouse.position.ReadValue();

        if (mouse.leftButton.wasPressedThisFrame &&
            !IsOverUI(position) &&
            IsLookSide(position))
        {
            mouseDragging = true;
            mouseOrigin = position;
        }

        if (mouseDragging && mouse.leftButton.isPressed)
            lookTarget = CalculateLook(position, mouseOrigin);

        if (mouseDragging && mouse.leftButton.wasReleasedThisFrame)
        {
            mouseDragging = false;
            lookTarget = Vector2.zero;
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
        bool lookFingerSeen = false;

        for (int i = 0; i < Input.touchCount; i++)
        {
            Touch touch = Input.GetTouch(i);

            if (lookFingerId == -1)
            {
                if (touch.phase == TouchPhase.Began &&
                    !IsOverUI(touch.position) &&
                    IsLookSide(touch.position))
                {
                    lookFingerId = touch.fingerId;
                    lookOrigin = touch.position;
                    lookTarget = Vector2.zero;
                    lookFingerSeen = true;
                }
                continue;
            }

            if (touch.fingerId != lookFingerId) continue;

            if (touch.phase == TouchPhase.Ended || touch.phase == TouchPhase.Canceled)
            {
                lookFingerId = -1;
                lookTarget = Vector2.zero;
            }
            else
            {
                lookTarget = CalculateLook(touch.position, lookOrigin);
                lookFingerSeen = true;
            }
        }

        if (lookFingerId != -1 && !lookFingerSeen)
        {
            lookFingerId = -1;
            lookTarget = Vector2.zero;
        }
    }

    private void HandleLegacyMouse()
    {
        Vector2 position = Input.mousePosition;

        if (Input.GetMouseButtonDown(0) &&
            !IsOverUI(position) &&
            IsLookSide(position))
        {
            mouseDragging = true;
            mouseOrigin = position;
        }

        if (mouseDragging && Input.GetMouseButton(0))
            lookTarget = CalculateLook(position, mouseOrigin);

        if (mouseDragging && Input.GetMouseButtonUp(0))
        {
            mouseDragging = false;
            lookTarget = Vector2.zero;
        }
    }
#endif

    private Vector2 CalculateLook(Vector2 position, Vector2 origin)
    {
        Vector2 delta = position - origin;
        if (delta.magnitude < deadZone) return Vector2.zero;

        return Vector2.ClampMagnitude(delta / Mathf.Max(1f, dragRadius), 1f);
    }

    private bool IsLookSide(Vector2 screenPosition)
    {
        if (!rightHalfOnly) return true;
        return screenPosition.x >= Screen.width * 0.5f;
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
        lookFingerId = -1;
        mouseDragging = false;
        lookTarget = Vector2.zero;
        LookInput = Vector2.zero;
    }
}
