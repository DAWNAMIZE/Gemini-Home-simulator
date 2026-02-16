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
    public float MaxCharge = 100f;     // The Phone needs to read this!
    public float CurrentCharge = 75f;  // The Phone needs to read this!
    public float ChargeRate = 5f; 

    void Update()
    {
        if (timeSystem == null) return;

        UpdateClock();
        UpdateChargeDisplay();

        // Daytime is roughly 0.2 to 0.8
        bool isDay = timeSystem.timeOfDay > 0.2f && timeSystem.timeOfDay < 0.8f;

        if (isDay && CurrentCharge < MaxCharge)
        {
            CurrentCharge += ChargeRate * Time.deltaTime;
            CurrentCharge = Mathf.Min(CurrentCharge, MaxCharge);
            UpdateVisuals(true);
        }
        else
        {
            UpdateVisuals(false);
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

    void UpdateVisuals(bool isCharging)
    {
        if (indicatorLight != null)
            indicatorLight.material.color = isCharging ? chargingColor : idleColor;
    }
}