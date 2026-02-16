using UnityEngine;

public class CookerKnob : MonoBehaviour, IInteractable
{
    [Header("Settings")]
    public GasCooker targetCooker; // Drag the Stove Object here
    public float rotationAmount = 45f;
    private bool isOn = false;

    [Header("Audio")]
    public AudioSource audioSource; // Drag AudioSource here (Uncheck 'Loop')
    public AudioClip clickSound;    // Drag 'Click' sound here

    public void Interact()
    {
        if (targetCooker != null)
        {
            // 1. Toggle Stove
            targetCooker.ToggleCooker();

            // 2. Rotate Knob
            isOn = !isOn;
            float targetRotation = isOn ? rotationAmount : 0f;
            transform.localRotation = Quaternion.Euler(0, targetRotation, 0);

            // 3. Play Click Sound
            if (audioSource != null && clickSound != null)
            {
                audioSource.pitch = Random.Range(0.9f, 1.1f);
                audioSource.PlayOneShot(clickSound);
            }
        }
    }
}