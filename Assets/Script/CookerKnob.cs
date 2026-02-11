using UnityEngine;

public class CookerKnob : MonoBehaviour, IInteractable
{
    [Header("Settings")]
    public GasCooker targetCooker; // Drag the Burner Object here
    public float rotationAmount = 45f;
    private bool isOn = false;

    [Header("Audio")]
    public AudioSource audioSource; // Drag AudioSource here (Uncheck 'Play On Awake')
    public AudioClip clickSound;    // Drag 'Click' sound here

    public void Interact()
    {
        if (targetCooker != null)
        {
            // 1. Toggle the logic on the burner
            targetCooker.ToggleCooker();
            
            // 2. Rotate the Knob Visual
            isOn = !isOn;
            float targetRotation = isOn ? rotationAmount : 0f;
            transform.localRotation = Quaternion.Euler(0, targetRotation, 0);
            
            // 3. Play Click Sound
            if (audioSource != null && clickSound != null)
            {
                audioSource.pitch = Random.Range(0.95f, 1.05f); // Slight variation
                audioSource.PlayOneShot(clickSound);
            }

            Debug.Log("Knob Toggled: " + (isOn ? "ON" : "OFF"));
        }
        else
        {
            Debug.LogError("Knob is missing a reference to the GasCooker!");
        }
    }
}