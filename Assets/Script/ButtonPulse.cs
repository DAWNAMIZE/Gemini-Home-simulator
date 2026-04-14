using UnityEngine;

public class ButtonPulse : MonoBehaviour
{
    public float pulseSpeed = 2.0f;
    public float pulseSize = 0.1f; // How much it grows (0.1 = 10%)

    private Vector3 originalScale;

    void Start()
    {
        originalScale = transform.localScale;
    }

    void Update()
    {
        // Calculate the scale using a Sine wave for a smooth heart-beat effect
        // We use Time.unscaledTime so it still pulses even when the game is PAUSED (Time.timeScale = 0)
        float scaleOffset = Mathf.Sin(Time.unscaledTime * pulseSpeed) * pulseSize;
        transform.localScale = originalScale + new Vector3(scaleOffset, scaleOffset, scaleOffset);
    }
}