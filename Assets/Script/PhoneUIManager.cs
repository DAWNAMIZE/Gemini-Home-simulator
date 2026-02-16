using UnityEngine;
using UnityEngine.UI;
using TMPro; // Required for TextMeshPro

public class PhoneUIManager : MonoBehaviour
{
    [Header("Phone UI Panel")]
    public GameObject phonePanel; 

    [Header("System References")]
    public SolarSystem solarSystem;     // Drag the Grey Inverter here
    public GasSystem gasSystem;         // Drag the Green Wall Box here
    public GeneratorSystem generator;   // Drag the Generator Object here
    public DayNightCycle dayNightSystem; // Drag the Time Controller here

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

    [Header("Style Settings")]
    public Color colorNormal = new Color32(0, 255, 100, 255);   // Green
    public Color colorWarning = new Color32(255, 215, 0, 255);  // Yellow
    public Color colorCritical = new Color32(255, 50, 50, 255); // Red

    private bool isOpen = false;

    void Update()
    {
        // Clock always updates so it's ready when you open phone
        UpdateClock(); 
        
        if (isOpen)
        {
            UpdateSolarUI();
            UpdateGasUI();
            UpdateGeneratorUI();
            UpdateNotifications();
        }
    }

    void UpdateSolarUI()
    {
        if (solarSystem == null || solarSlider == null) return;
        
        // 1. Sync Slider Logic
        solarSlider.maxValue = solarSystem.MaxCharge;
        solarSlider.value = solarSystem.CurrentCharge;
        
        // 2. Text Logic
        float percent = (solarSystem.CurrentCharge / solarSystem.MaxCharge) * 100f;
        if (solarText) solarText.text = $"{percent:0}%";
        
        // 3. Color Logic
        if (solarFill) solarFill.color = percent <= 20 ? colorCritical : colorNormal;
    }

    void UpdateGasUI()
    {
        if (gasSystem == null || gasSlider == null) return;

        // 1. Sync Slider Logic
        gasSlider.maxValue = gasSystem.MaxGas;
        gasSlider.value = gasSystem.CurrentGas;
        
        // 2. Text Logic
        float percent = 0f;
        if (gasSystem.MaxGas > 0)
            percent = (gasSystem.CurrentGas / gasSystem.MaxGas) * 100f;

        if (gasText) gasText.text = $"{percent:0}%";
        
        // 3. Color Logic
        if (gasFill) gasFill.color = percent <= 20 ? colorCritical : colorNormal;
    }

    void UpdateGeneratorUI()
    {
        // Safety check in case you haven't built the generator yet
        if (generator == null || fuelSlider == null) return;

        fuelSlider.maxValue = generator.MaxFuel;
        fuelSlider.value = generator.CurrentFuel;

        float percent = (generator.CurrentFuel / generator.MaxFuel) * 100f;
        if (fuelText) fuelText.text = $"{percent:0}%";
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

        // Calculate Safety Percentages
        float gasPercent = (gasSystem != null && gasSystem.MaxGas > 0) 
            ? gasSystem.CurrentGas / gasSystem.MaxGas : 0f;

        float powerPercent = (solarSystem != null && solarSystem.MaxCharge > 0) 
            ? solarSystem.CurrentCharge / solarSystem.MaxCharge : 0f;

        // Logic Check
        bool lowGas = gasPercent <= 0.2f;   // Low if under 20%
        bool lowPower = powerPercent <= 0.2f;

        if (lowGas && lowPower)
            SetNotification("CRITICAL: SYSTEM FAILURE", colorCritical);
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
        
        // Make text white on Red background, Black on Green/Yellow background
        notificationText.color = (bgColor == colorCritical) ? Color.white : Color.black;
    }

    public void TogglePhone()
    {
        isOpen = !isOpen;
        if (phonePanel != null) phonePanel.SetActive(isOpen);
    }
}