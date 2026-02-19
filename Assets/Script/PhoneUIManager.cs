using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PhoneUIManager : MonoBehaviour
{
    [Header("Phone UI Panel")]
    public GameObject phonePanel; 

    [Header("System References")]
    public SolarSystem solarSystem;     
    public GasCanister gasCanister;     // CHANGED: Now looks directly at the Canister
    public GeneratorSystem generator;   
    public DayNightCycle dayNightSystem; 
    public WaterTankSystem waterSystem; 

    [Header("Global UI")]
    public TMP_Text clockText;
    public Image notificationBanner; 
    public TMP_Text notificationText;    

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

    void Update()
    {
        UpdateClock(); 
        
        if (isOpen)
        {
            UpdateSolarUI();
            UpdateGasUI();
            UpdateGeneratorUI();
            UpdateWaterUI();
            UpdateNotifications();
        }
    }

    void UpdateSolarUI()
    {
        if (solarSystem == null || solarSlider == null) return;
        
        solarSlider.maxValue = solarSystem.MaxCharge;
        solarSlider.value = solarSystem.CurrentCharge;
        
        float percent = (solarSystem.CurrentCharge / solarSystem.MaxCharge) * 100f;
        if (solarText) solarText.text = $"{percent:0}%";
        if (solarFill) solarFill.color = percent <= 20 ? colorCritical : colorNormal;
    }

    // CHANGED: This whole method now reads from GasCanister instead of GasSystem
    void UpdateGasUI()
    {
        if (gasCanister == null || gasSlider == null) return;

        gasSlider.maxValue = gasCanister.maxGas;
        gasSlider.value = gasCanister.currentGas;
        
        float percent = 0f;
        if (gasCanister.maxGas > 0)
            percent = (gasCanister.currentGas / gasCanister.maxGas) * 100f;

        if (gasText) gasText.text = $"{percent:0}%";
        if (gasFill) gasFill.color = percent <= 20 ? colorCritical : colorNormal;
    }

    void UpdateGeneratorUI()
    {
        if (generator == null || fuelSlider == null) return;

        fuelSlider.maxValue = generator.MaxFuel;
        fuelSlider.value = generator.CurrentFuel;

        float percent = (generator.CurrentFuel / generator.MaxFuel) * 100f;
        if (fuelText) fuelText.text = $"{percent:0}%";
    }

    void UpdateWaterUI()
    {
        if (waterSystem == null || waterSlider == null) return;

        waterSlider.maxValue = waterSystem.MaxWater;
        waterSlider.value = waterSystem.CurrentWater;

        float percent = 0f;
        if (waterSystem.MaxWater > 0)
        {
            percent = (waterSystem.CurrentWater / waterSystem.MaxWater) * 100f;
        }
        
        if (waterText) waterText.text = $"{percent:0}%";
        if (waterFill) waterFill.color = percent <= 20 ? colorCritical : colorNormal;
    }

    void UpdateClock()
    {
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

        // CHANGED: Now calculates gas safety based on the canister directly
        float gasPercent = (gasCanister != null && gasCanister.maxGas > 0) 
            ? gasCanister.currentGas / gasCanister.maxGas : 0f;

        float powerPercent = (solarSystem != null && solarSystem.MaxCharge > 0) 
            ? solarSystem.CurrentCharge / solarSystem.MaxCharge : 0f;

        float waterPercent = (waterSystem != null && waterSystem.MaxWater > 0) 
            ? waterSystem.CurrentWater / waterSystem.MaxWater : 0f; 

        bool lowGas = gasPercent <= 0.2f;   
        bool lowPower = powerPercent <= 0.2f;
        bool lowWater = waterPercent <= 0.2f; 

        if ((lowGas && lowPower) || (lowGas && lowWater) || (lowPower && lowWater))
            SetNotification("CRITICAL: MULTIPLE FAILURES", colorCritical);
        else if (lowWater)
            SetNotification("WARNING: LOW WATER", colorWarning); 
        else if (lowGas)
            SetNotification("WARNING: LOW GAS", colorWarning);
        else if (lowPower)
            SetNotification("WARNING: LOW BATTERY", colorWarning);
        else
            SetNotification("SYSTEM NORMAL", colorNormal);
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