using UnityEngine;

public class DayNightCycle : MonoBehaviour
{
    // THIS is the missing line that caused the error!
    public static DayNightCycle instance; 

    [Header("Time Settings")]
    public float dayLengthInSeconds = 120f; 
    [Range(0, 1)] public float timeOfDay = 0.25f; 
    
    [Header("Light Settings")]
    public Light sunLight; 
    public AnimationCurve sunIntensity; 

    public bool IsDaytime => timeOfDay > 0.2f && timeOfDay < 0.8f;

    void Awake()
    {
        // Singleton Logic: This allows other scripts (like the Phone) to find this script easily
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject); // This makes the time persist between scenes!
        }
        else
        {
            Destroy(gameObject); // Prevents duplicates if you go back to the menu
        }
    }

    void Update()
    {
        // Advance time
        timeOfDay += Time.deltaTime / dayLengthInSeconds;
        if (timeOfDay >= 1) timeOfDay = 0;

        // Visuals: Rotate the sun
        if (sunLight != null)
        {
            sunLight.transform.localRotation = Quaternion.Euler((timeOfDay * 360f) - 90f, 170f, 0f);
            
            if (sunIntensity != null)
                sunLight.intensity = sunIntensity.Evaluate(timeOfDay);
        }
        else
        {
            // If the sun is missing (because we changed scenes), find the new one!
            FindSunInScene();
        }
    }

    void FindSunInScene()
    {
        // This looks for the light tagged "Sun" in the new scene
        GameObject sunObj = GameObject.FindGameObjectWithTag("Sun");
        if (sunObj != null)
        {
            sunLight = sunObj.GetComponent<Light>();
        }
    }
}