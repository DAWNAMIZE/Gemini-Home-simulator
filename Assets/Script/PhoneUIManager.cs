using UnityEngine;
using UnityEngine.UI;

public class PhoneUIManager : MonoBehaviour
{
    [Header("Phone UI Panel")]
    public GameObject phonePanel; 

    [Header("System References")]
    public SolarSystem solarSystem;
    public WaterTankSystem waterTankSystem;
    public GasSystem gasSystem;
    public DayNightCycle dayNightSystem; // Required for the phone clock

    [Header("Notification UI")]
    public Image notificationBanner; // Top bar background
    public Text notificationText;    // Top bar text
    public Text clockText;           // Phone digital clock

    [Header("Sliders")]
    public Slider solarSlider;
    public Image solarFill; // Drag the 'Fill' object from the Solar Slider here
    public Slider waterSlider;
    public Slider gasSlider;
    public Image gasFill;   // Drag the 'Fill' object from the Gas Slider here

    [Header("UI Text (Optional)")]
    public Text solarText;
    public Text waterText;
    public Text gasText;

    [Header("Style Settings")]
    public Color colorNormal = new Color32(0, 255, 100, 255);  // Green
    public Color colorWarning = new Color32(255, 215, 0, 255); // Yellow
    public Color colorCritical = new Color32(255, 50, 50, 255); // Red

    private bool isOpen = false;

    void Start()
    {
        if (phonePanel != null) phonePanel.SetActive(false);

        // Initialize slider max values
        if (solarSystem != null && solarSlider != null) solarSlider.maxValue = solarSystem.MaxCharge;
        if (waterTankSystem != null && waterSlider != null) waterSlider.maxValue = waterTankSystem.MaxWater;
        if (gasSystem != null && gasSlider != null) gasSlider.maxValue = gasSystem.MaxGas;
    }

    void Update()
    {
        UpdateClock(); // Time updates even if the panel is closed for sync
        
        if (!isOpen) return;

        UpdateSolarUI();
        UpdateGasUI();
        UpdateWaterUI();
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

    void UpdateWaterUI()
    {
        if (waterTankSystem != null && waterSlider != null)
        {
            waterSlider.value = waterTankSystem.CurrentWater;
            if (waterText) waterText.text = $"{waterTankSystem.CurrentWater:0}/{waterTankSystem.MaxWater}";
        }
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

            clockText.text = $"{displayHour:00}:{minutes:00} {suffix}";
        }
    }

    void UpdateNotifications()
    {
        if (notificationBanner == null || notificationText == null) return;

        bool lowGas = gasSystem != null && (gasSystem.CurrentGas / gasSystem.MaxGas) <= 0.2f;
        bool lowPower = solarSystem != null && (solarSystem.CurrentCharge / solarSystem.MaxCharge) <= 0.2f;

        if (lowGas && lowPower)
        {
            SetNotification("CRITICAL: MULTIPLE FAILURES", colorCritical);
        }
        else if (lowGas)
        {
            SetNotification("WARNING: LOW GAS", colorWarning);
        }
        else if (lowPower)
        {
            SetNotification("WARNING: LOW POWER", colorWarning);
        }
        else
        {
            SetNotification("SYSTEM NORMAL", colorNormal);
        }
    }

    void SetNotification(string message, Color bgColor)
    {
        notificationText.text = message;
        notificationBanner.color = bgColor;
    }

    public void TogglePhone()
    {
        isOpen = !isOpen;
        if (phonePanel != null) phonePanel.SetActive(isOpen);
    }
}