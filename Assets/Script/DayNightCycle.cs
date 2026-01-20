using UnityEngine;

public class DayNightCycle : MonoBehaviour
{
    [Header("Time Settings")]
    public float dayLengthInSeconds = 120f; // 2 minutes for a full day
    [Range(0, 1)] public float timeOfDay = 0.25f; // 0 is midnight, 0.5 is noon
    
    [Header("Light Settings")]
    public Light sunLight; // Drag your Directional Light here
    public AnimationCurve sunIntensity; // Curve to make sun brighter at noon

    public bool IsDaytime => timeOfDay > 0.2f && timeOfDay < 0.8f;

    void Update()
    {
        // Advance time
        timeOfDay += Time.deltaTime / dayLengthInSeconds;
        if (timeOfDay >= 1) timeOfDay = 0;

        // Visuals: Rotate the sun based on time
        if (sunLight != null)
        {
            sunLight.transform.localRotation = Quaternion.Euler((timeOfDay * 360f) - 90f, 170f, 0f);
            
            // Adjust intensity based on the curve (0 at night, 1 at noon)
            if (sunIntensity != null)
                sunLight.intensity = sunIntensity.Evaluate(timeOfDay);
        }
    }
}