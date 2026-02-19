using UnityEngine;

public class Appliance : MonoBehaviour, IInteractable
{
    [Header("System Connection")]
    public SolarSystem solarSystem; // Drag your Grey Inverter here

    [Header("Appliance Settings")]
    public string applianceName = "New Appliance";
    public float powerDrawRate = 0.1f; // Set this based on the list above!
    public bool isOn = false;

    [Header("Feedback (Optional)")]
    public GameObject activeVisuals; // Drag a light object, PC screen, or fan blades here
    public AudioSource activeAudio;  // Drag an AudioSource here (Check 'Loop')

    void Update()
    {
        if (isOn)
        {
            // Check if we have enough power to run
            if (solarSystem != null && solarSystem.CurrentCharge > 0)
            {
                RunAppliance();
            }
            else
            {
                // Power cut! Battery is dead, turn off automatically
                TurnOff();
            }
        }
    }

    // This allows the player to click the object to turn it on/off
    public void Interact()
    {
        if (isOn) TurnOff();
        else TurnOn();
    }

    void TurnOn()
    {
        // Only allow turning on if the house actually has power
        if (solarSystem != null && solarSystem.CurrentCharge > 0)
        {
            isOn = true;
            
            // Turn on visuals and sound
            if (activeVisuals != null) activeVisuals.SetActive(true);
            if (activeAudio != null && !activeAudio.isPlaying) activeAudio.Play();
        }
    }

    void TurnOff()
    {
        isOn = false;
        
        // Turn off visuals and sound
        if (activeVisuals != null) activeVisuals.SetActive(false);
        if (activeAudio != null) activeAudio.Stop();
    }

    void RunAppliance()
    {
        // Drain the battery from the inverter
        solarSystem.CurrentCharge -= powerDrawRate * Time.deltaTime;
        
        // Prevent the battery from dropping below zero
        if (solarSystem.CurrentCharge < 0) 
        {
            solarSystem.CurrentCharge = 0;
        }
    }
}