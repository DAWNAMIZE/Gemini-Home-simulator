using UnityEngine;

public class SolarSystem : MonoBehaviour
{
    [Header("References")]
    public DayNightCycle timeSystem; // Reference to the script above

    [Header("Stats")]
    public float MaxCharge = 100f; //
    public float CurrentCharge = 75f; //
    public float ChargeRate = 5f; // How much it charges per second in full sun

    void Update()
    {
        if (timeSystem == null) return;

        // Only charge if it is Daytime
        if (timeSystem.IsDaytime && CurrentCharge < MaxCharge)
        {
            // Charge the battery
            CurrentCharge += ChargeRate * Time.deltaTime;
            CurrentCharge = Mathf.Min(CurrentCharge, MaxCharge);
        }
    }
}