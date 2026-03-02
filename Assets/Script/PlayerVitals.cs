using UnityEngine;
using UnityEngine.UI; // Required for the Screen Overlay

public class PlayerVitals : MonoBehaviour
{
    [Header("Health (The Master Stat)")]
    public float maxHealth = 100f;
    public float currentHealth;
    public bool isDead = false;

    [Header("Survival Stats (0 to 100)")]
    public float hunger = 100f;   
    public float thirst = 100f;   
    public float hygiene = 100f;  
    public float stress = 0f;     

    [Header("Decay Rates")]
    public float hungerRate = 0.3f;   
    public float thirstRate = 0.5f;   
    public float hygieneRate = 0.1f;
    public float stressRatePassive = 0.1f; 

    [Header("Screen Effects (The Feedback)")] // NEW SECTION
    public Image bloodOverlay;       // Drag a Red Panel here
    public AudioSource heartbeatSFX; // Drag a Heartbeat loop here
    public AudioSource coughSFX;     // Drag a Cough sound here
    public AudioSource stomachSFX;   // Drag a Growl sound here

    // Private timers for sounds
    private float nextCoughTime = 0f;
    private float nextGrowlTime = 0f;
    private DayNightCycle sunSystem;

    void Start()
    {
        currentHealth = maxHealth;
        sunSystem = DayNightCycle.instance;
        
        // Ensure overlay starts invisible
        if (bloodOverlay != null) 
            bloodOverlay.color = new Color(1, 0, 0, 0);
    }

    void Update()
    {
        if (isDead) return;

        HandleDecay();
        CheckHealthImpact();
        UpdateScreenEffects(); // NEW: Handle the visuals/audio
    }

    void HandleDecay()
    {
        hunger -= hungerRate * Time.deltaTime;
        thirst -= thirstRate * Time.deltaTime;
        hygiene -= hygieneRate * Time.deltaTime;

        // Stress Logic
        if (sunSystem != null && !sunSystem.IsDaytime)
            stress += (stressRatePassive * 2f) * Time.deltaTime; 
        else
            stress -= stressRatePassive * Time.deltaTime; 

        // Clamp values
        hunger = Mathf.Clamp(hunger, 0, 100);
        thirst = Mathf.Clamp(thirst, 0, 100);
        hygiene = Mathf.Clamp(hygiene, 0, 100);
        stress = Mathf.Clamp(stress, 0, 100);
    }

    void CheckHealthImpact()
    {
        // Damage Logic
        if (hunger <= 0) currentHealth -= 1f * Time.deltaTime;
        if (thirst <= 0) currentHealth -= 3f * Time.deltaTime;
        if (stress >= 100) currentHealth -= 0.5f * Time.deltaTime;

        currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);

        if (currentHealth <= 0) Die();
    }

    // --- NEW: THE SCREEN EFFECTS ---
    void UpdateScreenEffects()
    {
        // 1. BLOOD OVERLAY (Visual)
        // Only show if Health is below 40%
        if (bloodOverlay != null)
        {
            float alpha = 0f;
            if (currentHealth < 40f)
            {
                // The lower the health, the redder the screen (0 to 1)
                alpha = 1f - (currentHealth / 40f); 
            }
            
            // Apply the alpha
            bloodOverlay.color = new Color(1f, 0f, 0f, alpha);
        }

        // 2. HEARTBEAT (Audio)
        // Volume gets louder as you die
        if (heartbeatSFX != null)
        {
            if (currentHealth < 40f)
            {
                if (!heartbeatSFX.isPlaying) heartbeatSFX.Play();
                // Volume goes from 0.0 to 1.0 based on health
                heartbeatSFX.volume = 1f - (currentHealth / 40f);
            }
            else
            {
                if (heartbeatSFX.isPlaying) heartbeatSFX.Stop();
            }
        }

        // 3. THIRST COUGH (Audio)
        if (thirst < 15f && coughSFX != null && Time.time > nextCoughTime)
        {
            coughSFX.Play();
            nextCoughTime = Time.time + Random.Range(5f, 15f); // Cough every 5-15 seconds
        }

        // 4. HUNGER GROWL (Audio)
        if (hunger < 15f && stomachSFX != null && Time.time > nextGrowlTime)
        {
            stomachSFX.Play();
            nextGrowlTime = Time.time + Random.Range(10f, 20f); // Growl every 10-20 seconds
        }
    }

    public void Eat(float amount) => hunger = Mathf.Clamp(hunger + amount, 0, 100);
    public void Drink(float amount) => thirst = Mathf.Clamp(thirst + amount, 0, 100);
    public void Clean(float amount) => hygiene = Mathf.Clamp(hygiene + amount, 0, 100);

    void Die()
    {
        isDead = true;
        // Optional: Make screen fully black or red here
        if (bloodOverlay != null) bloodOverlay.color = Color.black; 
        Debug.Log("PLAYER DIED");
    }
}