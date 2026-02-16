using UnityEngine;

public class GeneratorSystem : MonoBehaviour, IInteractable
{
    [Header("Connections")]
    public SolarSystem solarSystem; // Drag your Inverter here
    
    [Header("Fuel Stats")]
    public float MaxFuel = 20f;     // e.g., 20 Liters
    public float CurrentFuel = 20f; 
    public float ConsumptionRate = 0.5f; // Liters burned per second

    [Header("Power Stats")]
    public float ChargeRate = 15f;  // Generator charges much faster than the sun!

    [Header("Visuals & Audio")]
    public AudioSource generatorAudio; // Drag AudioSource here (Check 'Loop')
    public AudioClip runSound;         // Drag loud engine sound here
    public ParticleSystem smokeParticles;

    public bool IsRunning = false;

    // This allows you to click the generator to Start/Stop it
    public void Interact()
    {
        ToggleGenerator();
    }

    void Update()
    {
        if (IsRunning && CurrentFuel > 0)
        {
            RunGenerator();
        }
        else
        {
            StopGenerator();
        }
    }

    void RunGenerator()
    {
        // 1. Burn Fuel
        CurrentFuel -= ConsumptionRate * Time.deltaTime;

        // 2. Charge the House Battery
        if (solarSystem != null)
        {
            solarSystem.CurrentCharge += ChargeRate * Time.deltaTime;
            // Clamp so we don't overcharge
            if (solarSystem.CurrentCharge > solarSystem.MaxCharge)
                solarSystem.CurrentCharge = solarSystem.MaxCharge;
        }

        // 3. Audio & Visuals
        if (smokeParticles && !smokeParticles.isPlaying) smokeParticles.Play();
        
        if (generatorAudio && runSound && !generatorAudio.isPlaying)
        {
            generatorAudio.clip = runSound;
            generatorAudio.loop = true;
            generatorAudio.Play();
        }
    }

    void StopGenerator()
    {
        IsRunning = false; // Force state to off
        
        if (smokeParticles) smokeParticles.Stop();
        if (generatorAudio) generatorAudio.Stop();
    }

    public void ToggleGenerator()
    {
        IsRunning = !IsRunning;
        
        // Safety: Can't turn on if empty
        if (CurrentFuel <= 0) IsRunning = false; 
    }

    // Call this later when you implement Jerry Cans
    public void RefillFuel(float amount)
    {
        CurrentFuel += amount;
        if (CurrentFuel > MaxFuel) CurrentFuel = MaxFuel;
    }
}