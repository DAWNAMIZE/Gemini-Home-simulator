using UnityEngine;
using UnityEngine.UI; // Still needed for Images/Sliders
using TMPro;          // ADDED: Needed for TextMeshPro

public class PhoneUIManager : MonoBehaviour
{
    [Header("Phone UI Panel")]
    public GameObject phonePanel; 

    [Header("System References")]
    public SolarSystem solarSystem;
    public WaterTankSystem waterTankSystem; // Optional if you have it
    public GasSystem gasSystem;             // Drag the Wall GasSystem here
    public DayNightCycle dayNightSystem;    // Drag DayNightController here

    [Header("Notification UI")]
    public Image notificationBanner; 
    public TMP_Text notificationText;    // CHANGED to TMP_Text
    public TMP_Text clockText;           // CHANGED to TMP_Text

    [Header("Sliders & Fills")]
    public Slider solarSlider;
    public Image solarFill; 
    public Slider waterSlider; // Optional
    public Slider gasSlider;
    public Image gasFill;   

    [Header("Percentage Text")]
    public TMP_Text solarText; // CHANGED to TMP_Text
    public TMP_Text waterText; // CHANGED to TMP_Text
    public TMP_Text gasText;   // CHANGED to TMP_Text

    [Header("Style Settings")]
    public Color colorNormal = new Color32(0, 255, 100, 255);   // Green
    public Color colorWarning = new Color32(255, 215, 0, 255);  // Yellow
    public Color colorCritical = new Color32(255, 50, 50, 255); // Red

    private bool isOpen = false;

    void Start()
    {
        if (phonePanel != null) phonePanel.SetActive(false);
        InitializeSliders();
    }

    void InitializeSliders()
    {
        if (solarSystem != null && solarSlider != null) solarSlider.maxValue = solarSystem.MaxCharge;
        // if (waterTankSystem != null && waterSlider != null) waterSlider.maxValue = waterTankSystem.MaxWater;
        if (gasSystem != null && gasSlider != null) gasSlider.maxValue = gasSystem.MaxGas;
    }

    void Update()
    {
        UpdateClock(); 
        
        if (!isOpen) return;

        UpdateSolarUI();
        UpdateGasUI();
        UpdateNotifications();
    }

    void UpdateSolarUI()
    {
        if (solarSystem == null || solarSlider == null) return;
        
        solarSlider.value = solarSystem.CurrentCharge;
        float percent = (solarSystem.CurrentCharge / solarSystem.MaxCharge) * 100f;
        
        if (solarText) solarText.text = $"{percent:0}%";
        if (solarFill) solarFill.color = percent <= 20 ? colorCritical : colorNormal;
    }

    void UpdateGasUI()
    {
        if (gasSystem == null || gasSlider == null) return;

        gasSlider.value = gasSystem.CurrentGas;
        float percent = (gasSystem.CurrentGas / gasSystem.MaxGas) * 100f;

        if (gasText) gasText.text = $"{percent:0}%";
        if (gasFill) gasFill.color = percent <= 20 ? colorCritical : colorNormal;
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

        // Calculate Percentages (Safe Math)
        float gasPercent = (gasSystem != null && gasSystem.MaxGas > 0) 
            ? gasSystem.CurrentGas / gasSystem.MaxGas : 1f;

        float powerPercent = (solarSystem != null && solarSystem.MaxCharge > 0) 
            ? solarSystem.CurrentCharge / solarSystem.MaxCharge : 1f;

        // Logic
        bool lowGas = gasPercent <= 0.2f;
        bool lowPower = powerPercent <= 0.2f;

        if (lowGas && lowPower)
            SetNotification("CRITICAL: MULTIPLE FAILURES", colorCritical);
        else if (lowGas)
            SetNotification("WARNING: LOW GAS LEVEL", colorWarning);
        else if (lowPower)
            SetNotification("WARNING: LOW POWER", colorWarning);
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