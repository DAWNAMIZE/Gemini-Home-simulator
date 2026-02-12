using UnityEngine;
using TMPro;

public class SolarSystem : MonoBehaviour
{
    [Header("References")]
    public DayNightCycle timeSystem;
    public TMP_Text clockDisplay;
    public TMP_Text chargeDisplay;

    [Header("Visuals")]
    public MeshRenderer indicatorLight;
    public Color chargingColor = Color.green;
    public Color idleColor = Color.red;

    [Header("Stats")]
    public float MaxCharge = 100f;
    public float CurrentCharge = 75f;
    public float ChargeRate = 5f; // Default is 5

    void Start()
    {
        // Safety Check: If ChargeRate is 0, fix it automatically
        if (ChargeRate <= 0)
        {
            ChargeRate = 5f;
            Debug.LogWarning("Charge Rate was 0! Resetting to 5.");
        }
    }

    void Update()
    {
        if (timeSystem == null) return;

        UpdateClock();
        UpdateChargeDisplay();

        // Check Daytime (0.2 to 0.8 is roughly 5 AM to 7 PM)
        bool isDay = timeSystem.timeOfDay > 0.2f && timeSystem.timeOfDay < 0.8f;

        if (isDay && CurrentCharge < MaxCharge)
        {
            ChargeBattery();
            UpdateVisuals(true); // Turn Green
        }
        else
        {
            UpdateVisuals(false); // Turn Red/Grey
        }
    }

    void UpdateClock()
    {
        if (clockDisplay == null) return;

        float hours24 = timeSystem.timeOfDay * 24f;
        int hours = Mathf.FloorToInt(hours24);
        int minutes = Mathf.FloorToInt((hours24 - hours) * 60);

        string suffix = (hours >= 12) ? "PM" : "AM";
        int hours12 = hours % 12;
        if (hours12 == 0) hours12 = 12;

        clockDisplay.text = $"{hours12:00}:{minutes:00} <size=60%>{suffix}</size>";
    }

    void UpdateChargeDisplay()
    {
        if (chargeDisplay == null) return;
        float percent = (CurrentCharge / MaxCharge) * 100f;
        chargeDisplay.text = $"Charge:\n{percent:0}%";
    }

    void ChargeBattery()
    {
        // Increase charge
        CurrentCharge += ChargeRate * Time.deltaTime;
        
        // Clamp to Max
        CurrentCharge = Mathf.Min(CurrentCharge, MaxCharge);
    }

    void UpdateVisuals(bool isCharging)
    {
        if (indicatorLight != null)
        {
            indicatorLight.material.color = isCharging ? chargingColor : idleColor;
        }
    }
}