using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PhoneUIManager : MonoBehaviour
{
    [Header("Phone UI Panel")]
    public GameObject phonePanel; 

    [Header("System References")]
    public PlayerVitals playerStats; // NEW: Drag your Player here!
    public SolarSystem solarSystem;     
    public GasCanister gasCanister;     
    public GeneratorSystem generator;   
    public DayNightCycle dayNightSystem; 
    public WaterTankSystem waterSystem; 

    [Header("Global UI")]
    public TMP_Text clockText;
    public Image notificationBanner; 
    public TMP_Text notificationText;    

    [Header("Vitals App (Health & Survival)")] // NEW SECTION
    public Slider healthSlider;
    public Image healthFill; // Optional: To change color to red
    public Slider hungerSlider;
    public Slider thirstSlider;   // NEW
    public Slider hygieneSlider;  // NEW
    public Slider stressSlider;

    [Header("Solar App (Battery)")]
    public Slider solarSlider;
    public TMP_Text solarText; 
    public Image solarFill; 

    [Header("Gas App (Cooking)")]
    public Slider gasSlider;
    public TMP_Text gasText;   
    public Image gasFill;   

    [Header("Generator App (Fuel)")]
    public Slider fuelSlider;
    public TMP_Text fuelText;

    [Header("Water App (Plumbing)")]
    public Slider waterSlider;
    public TMP_Text waterText;
    public Image waterFill;

    [Header("Style Settings")]
    public Color colorNormal = new Color32(0, 255, 100, 255);   
    public Color colorWarning = new Color32(255, 215, 0, 255);  
    public Color colorCritical = new Color32(255, 50, 50, 255); 

    private bool isOpen = false;

    void Start()
    {
        // Auto-connect to time system
        if (dayNightSystem == null) dayNightSystem = DayNightCycle.instance;
        
        // Auto-find player stats if missing
        if (playerStats == null) playerStats = FindFirstObjectByType<PlayerVitals>();
    }

    void Update()
    {
        UpdateClock(); 
        
        if (isOpen)
        {
            UpdateVitalsUI(); // NEW
            UpdateSolarUI();
            UpdateGasUI();
            UpdateGeneratorUI();
            UpdateWaterUI();
            UpdateNotifications();
        }
    }

    void UpdateVitalsUI()
    {
        if (playerStats == null) return;

        // 1. Health
        if (healthSlider) 
        {
            healthSlider.maxValue = playerStats.maxHealth;
            healthSlider.value = playerStats.currentHealth;
            // Turn Red if dying
            if (healthFill) healthFill.color = (playerStats.currentHealth < 30) ? colorCritical : colorNormal;
        }

        // 2. Hunger
        if (hungerSlider) { hungerSlider.maxValue = 100f; hungerSlider.value = playerStats.hunger; }

        // 3. Thirst (NEW)
        if (thirstSlider) { thirstSlider.maxValue = 100f; thirstSlider.value = playerStats.thirst; }

        // 4. Hygiene (NEW)
        if (hygieneSlider) { hygieneSlider.maxValue = 100f; hygieneSlider.value = playerStats.hygiene; }

        // 5. Stress
        if (stressSlider) { stressSlider.maxValue = 100f; stressSlider.value = playerStats.stress; }
    }

    // ... (Your existing resource update methods below remain unchanged) ...

    void UpdateSolarUI()
    {
        if (solarSystem == null || solarSlider == null) return;
        solarSlider.maxValue = solarSystem.MaxCharge;
        solarSlider.value = solarSystem.CurrentCharge;
        float percent = (solarSystem.CurrentCharge / solarSystem.MaxCharge) * 100f;
        if (solarText) solarText.text = $"{percent:0}%";
        if (solarFill) solarFill.color = percent <= 20 ? colorCritical : colorNormal;
    }

    void UpdateGasUI()
    {
        if (gasCanister == null || gasSlider == null) return;
        gasSlider.maxValue = gasCanister.maxGas;
        gasSlider.value = gasCanister.currentGas;
        float percent = (gasCanister.maxGas > 0) ? (gasCanister.currentGas / gasCanister.maxGas) * 100f : 0;
        if (gasText) gasText.text = $"{percent:0}%";
        if (gasFill) gasFill.color = percent <= 20 ? colorCritical : colorNormal;
    }

    void UpdateGeneratorUI()
    {
        if (generator == null || fuelSlider == null) return;
        fuelSlider.maxValue = generator.MaxFuel;
        fuelSlider.value = generator.CurrentFuel;
        float percent = (generator.MaxFuel > 0) ? (generator.CurrentFuel / generator.MaxFuel) * 100f : 0;
        if (fuelText) fuelText.text = $"{percent:0}%";
    }

    void UpdateWaterUI()
    {
        if (waterSystem == null || waterSlider == null) return;
        waterSlider.maxValue = waterSystem.MaxWater;
        waterSlider.value = waterSystem.CurrentWater;
        float percent = (waterSystem.MaxWater > 0) ? (waterSystem.CurrentWater / waterSystem.MaxWater) * 100f : 0;
        if (waterText) waterText.text = $"{percent:0}%";
        if (waterFill) waterFill.color = percent <= 20 ? colorCritical : colorNormal;
    }

    void UpdateClock()
    {
        if (dayNightSystem == null) dayNightSystem = DayNightCycle.instance;

        if (dayNightSystem != null && clockText != null)
        {
            float hours24 = dayNightSystem.timeOfDay * 24f;
            int hours = Mathf.FloorToInt(hours24);
            int minutes = Mathf.FloorToInt((hours24 - hours) * 60);
            string suffix = (hours >= 12) ? "PM" : "AM";
            int displayHour = hours % 12;
            if (displayHour == 0) displayHour = 12;
            clockText.text = $"{displayHour:00}:{minutes:00} <size=60%>{suffix}</size>";
        }
    }

    void UpdateNotifications()
    {
        if (notificationBanner == null || notificationText == null) return;

        // Vitals Warnings take priority!
        if (playerStats != null)
        {
            if (playerStats.currentHealth < 30) { SetNotification("CRITICAL HEALTH", colorCritical); return; }
            if (playerStats.thirst < 10) { SetNotification("SEVERE DEHYDRATION", colorCritical); return; }
            if (playerStats.hunger < 10) { SetNotification("STARVATION IMMINENT", colorCritical); return; }
        }

        // Resource Warnings
        float gasPercent = (gasCanister != null && gasCanister.maxGas > 0) ? gasCanister.currentGas / gasCanister.maxGas : 0f;
        float powerPercent = (solarSystem != null && solarSystem.MaxCharge > 0) ? solarSystem.CurrentCharge / solarSystem.MaxCharge : 0f;
        
        if (gasPercent <= 0.2f && powerPercent <= 0.2f) SetNotification("MULTIPLE SYSTEMS FAILING", colorCritical);
        else if (gasPercent <= 0.2f) SetNotification("WARNING: LOW GAS", colorWarning);
        else if (powerPercent <= 0.2f) SetNotification("WARNING: LOW BATTERY", colorWarning);
        else SetNotification("SYSTEM NORMAL", colorNormal);
    }

    void SetNotification(string message, Color bgColor)
    {
        notificationText.text = message;
        notificationBanner.color = bgColor;
        notificationText.color = (bgColor == colorCritical) ? Color.white : Color.black;
    }

    public void TogglePhone()
    {
        isOpen = !isOpen;
        if (phonePanel != null) phonePanel.SetActive(isOpen);
    }
}