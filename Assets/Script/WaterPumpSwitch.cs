using UnityEngine;

public class WaterPumpSwitch : MonoBehaviour, IInteractable
{
    [Header("Connections")]
    public WaterTankSystem waterTank; // Drag your GP Tank here
    public SolarSystem solarSystem;   // Drag your Inverter here

    [Header("Pump Settings")]
    public float fillRate = 5f;       // Adds 5 Liters per second
    public float powerCost = 2.0f;    // Drains 2.0 Battery per second (Heavy!)
    public bool isOn = false;

    [Header("Feedback")]
    public MeshRenderer indicatorLight; // UPDATED: Now a MeshRenderer to swap colors!
    public Material offMaterial;        // Drag Red Material here
    public Material onMaterial;         // Drag Green Material here
    public AudioSource pumpSound;     

    void Start()
    {
        // Ensure the light starts on the Red "Off" material
        UpdateVisuals();
    }

    void Update()
    {
        if (isOn)
        {
            OperatePump();
        }
    }

    public void Interact()
    {
        // Toggle the switch
        isOn = !isOn;
        UpdateVisuals();
    }

    void OperatePump()
    {
        // 1. Check if we have Power
        if (solarSystem != null && solarSystem.CurrentCharge > 0)
        {
            // 2. Check if Tank is full
            if (waterTank.CurrentWater >= waterTank.MaxWater)
            {
                // Auto-shutoff if full
                TurnOff(); 
                return;
            }

            // 3. Drain Power & Add Water
            solarSystem.CurrentCharge -= powerCost * Time.deltaTime;
            waterTank.CurrentWater += fillRate * Time.deltaTime;

            // Clamp values so they don't exceed the max
            if (waterTank.CurrentWater > waterTank.MaxWater) 
                waterTank.CurrentWater = waterTank.MaxWater;
        }
        else
        {
            // No power? Trip the breaker (Turn off)
            TurnOff();
        }
    }

    void TurnOff()
    {
        isOn = false;
        UpdateVisuals();
    }

    void UpdateVisuals()
    {
        // Swap materials based on the 'isOn' state
        if (indicatorLight != null && onMaterial != null && offMaterial != null) 
        {
            indicatorLight.material = isOn ? onMaterial : offMaterial;
        }

        // Turn sound on/off
        if (pumpSound != null)
        {
            if (isOn && !pumpSound.isPlaying) pumpSound.Play();
            else if (!isOn && pumpSound.isPlaying) pumpSound.Stop();
        }
    }
}