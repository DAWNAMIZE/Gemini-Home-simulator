using UnityEngine;
using System.Collections;

public class Door : MonoBehaviour, IInteractable
{
    [Header("Door Settings")]
    public Transform hinge;            // The pivot/hinge point of the door
    public float openAngle = 90f;      // How far the door swings open (degrees)
    public float openSpeed = 2f;       // Speed of door animation
    public bool isOpen = false;        // Current open/closed state
    public float autoCloseDelay = 3f;  // Seconds before door auto-closes (0 = disable)

    private Quaternion closedRotation;
    private Quaternion openRotation;
    private bool isAnimating = false;

    void Start()
    {
        if (hinge == null)
            hinge = transform; // fallback — but better to assign manually

        closedRotation = hinge.localRotation;
        openRotation = hinge.localRotation * Quaternion.Euler(0f, openAngle, 0f);
    }

    public void Interact()
    {
        if (!isAnimating)
            StartCoroutine(ToggleDoor());
    }

    private IEnumerator ToggleDoor()
    {
        isAnimating = true;

        Quaternion startRot = hinge.localRotation;
        Quaternion targetRot = isOpen ? closedRotation : openRotation;
        float time = 0f;

        while (time < 1f)
        {
            time += Time.deltaTime * openSpeed;
            hinge.localRotation = Quaternion.Slerp(startRot, targetRot, time);
            yield return null;
        }

        hinge.localRotation = targetRot;
        isOpen = !isOpen;
        isAnimating = false;

        // Automatically close after a delay
        if (isOpen && autoCloseDelay > 0)
            StartCoroutine(AutoClose());
    }

    private IEnumerator AutoClose()
    {
        yield return new WaitForSeconds(autoCloseDelay);
        if (isOpen && !isAnimating)
            StartCoroutine(ToggleDoor());
    }

    public string GetNameForUI()
    {
        return "Door";
    }
}
