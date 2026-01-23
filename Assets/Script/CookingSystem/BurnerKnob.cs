using UnityEngine;

public class BurnerKnob : MonoBehaviour, IInteractable
{
    public GasCooker cooker; // Drag the Stove Base here

    public void Interact()
    {
        if (cooker != null)
        {
            cooker.ToggleCooker();
            // Visual feedback: Rotate the knob 45 degrees
            transform.localEulerAngles += new Vector3(0, 45, 0);
        }
    }
}