using UnityEngine;

public class RoomLightSwitch : MonoBehaviour, IInteractable
{
    [Header("Power Connection")]
    public SolarSystem solarSystem;   // Drag your Inverter here
    public float powerCost = 0.1f;    // Cost for the whole room per second

    [Header("The Lights")]
    public Light[] roomLights;        // Drag all actual Light components here
    public MeshRenderer[] bulbModels; // Drag the 3D models of the bulbs here
    
    [Header("Materials (Optional)")]
    public Material bulbOnMat;        // Assign a material with Emission (Glow)
    public Material bulbOffMat;       // Assign a plain white/grey material

    [Header("State")]
    public bool isOn = false;
    public AudioSource clickSound;    

    void Start()
    {
        // Ensure lights start in the correct state
        UpdateLights();
    }

    void Update()
    {
        // If the switch is ON, we must check for power
        if (isOn)
        {
            if (solarSystem != null && solarSystem.CurrentCharge > 0)
            {
                // We have power, drain it
                solarSystem.CurrentCharge -= powerCost * Time.deltaTime;
            }
            else
            {
                // Battery died! Force lights off.
                // (We don't toggle 'isOn' to false, so they come back when power returns)
                SetPhysicalLights(false);
            }
        }
    }

    public void Interact()
    {
        // Toggle the switch
        isOn = !isOn;
        
        if (clickSound) clickSound.Play();
        UpdateLights();
    }

    void UpdateLights()
    {
        // Only turn on if Switch is ON AND Battery has charge
        bool hasPower = (solarSystem != null && solarSystem.CurrentCharge > 0);
        bool finalState = isOn && hasPower;

        SetPhysicalLights(finalState);
    }

    void SetPhysicalLights(bool state)
    {
        // 1. Turn the actual Unity Lights On/Off
        foreach (Light l in roomLights)
        {
            if (l != null) l.enabled = state;
        }

        // 2. Make the Bulbs Glow (Material Swap)
        if (bulbOnMat != null && bulbOffMat != null)
        {
            foreach (MeshRenderer bulb in bulbModels)
            {
                if (bulb != null) 
                    bulb.material = state ? bulbOnMat : bulbOffMat;
            }
        }
    }
}