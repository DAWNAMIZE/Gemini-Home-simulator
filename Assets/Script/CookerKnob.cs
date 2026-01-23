using UnityEngine;

public class CookerKnob : MonoBehaviour, IInteractable
{
    [Header("Settings")]
    public GasCooker targetCooker; // Drag the Burner_Socket here
    public float rotationAmount = 45f;
    private bool isOn = false;

    public void Interact()
    {
        if (targetCooker != null)
        {
            // 1. Tell the cooker to toggle on or off
            targetCooker.ToggleCooker();
            
            // 2. Visual feedback: Rotate the knob
            isOn = !isOn;
            float targetRotation = isOn ? rotationAmount : 0f;
            transform.localRotation = Quaternion.Euler(0, targetRotation, 0);
            
            Debug.Log("Knob Toggled: " + (isOn ? "ON" : "OFF"));
        }
        else
        {
            Debug.LogError("Knob is missing a reference to the GasCooker!");
        }
    }
}