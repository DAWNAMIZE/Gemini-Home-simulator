using UnityEngine;
using TMPro;

public class SolarSystem : MonoBehaviour
{
    [Header("References")]
    public DayNightCycle timeSystem; 
    public TMP_Text clockDisplay; 
    public TMP_Text chargeDisplay; // NEW: Drag your percentage text here

    [Header("Visuals")]
    public MeshRenderer indicatorLight; 
    public Color chargingColor = Color.green;
    public Color idleColor = Color.red;

    [Header("Stats")]
    public float MaxCharge = 100f; 
    public float CurrentCharge = 75f; 
    public float ChargeRate = 5f; 

    void Update()
    {
        if (timeSystem == null) return;

        UpdateClock();
        UpdateChargeDisplay(); // NEW: Refresh the battery numbers

        if (timeSystem.IsDaytime && CurrentCharge < MaxCharge)
        {
            ChargeBattery();
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

        clockDisplay.text = string.Format("{0:00}:{1:00}", hours, minutes);
    }

    void UpdateChargeDisplay()
    {
        if (chargeDisplay == null) return;

        // Calculate percentage (Current / Max * 100)
        float chargePercent = (CurrentCharge / MaxCharge) * 100f;

        // Display as "Charge: 75%"
        chargeDisplay.text = string.Format("Charge: {0:0}%", Mathf.Round(chargePercent));
    }

    void ChargeBattery()
    {
        CurrentCharge += ChargeRate * Time.deltaTime;
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