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
    
    // ==========================================
    // ( KEEP ALL YOUR AWESOME UPDATE, FIXEDUPDATE, 
    //   AND TOUCH MOVEMENT LOGIC EXACTLY AS IT IS! )
    // ==========================================

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
            GameObject interactedObject = hit.collider.gameObject;

            // This tells doors, ovens, AND your ItemPickup.cs script to run!
            interactedObject.SendMessage("Interact", SendMessageOptions.DontRequireReceiver);

            // FORK 2: We ONLY need this for holding tools like the Pot!
            if (interactedObject.CompareTag("Holdable"))
            {
                if (PlayerInventory.instance != null)
                {
                    PlayerInventory.instance.HoldPhysicalItem(interactedObject);
                }
                Debug.Log("Physically holding the " + interactedObject.name);
            }
        }
    }
}