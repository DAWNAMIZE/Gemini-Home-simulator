using UnityEngine;

public class SinkTap : MonoBehaviour, IInteractable
{
    [Header("Connections")]
    public WaterTankSystem waterTank; // Links directly to YOUR script

    [Header("Visuals")]
    public ParticleSystem waterParticles; 

    [Header("Audio")]
    public AudioSource tapAudio;    
    public AudioClip runningWaterSound;

    [Header("Settings")]
    public float flowRate = 5f; // Liters drained per second
    public bool isOpen = false;

    void Update()
    {
        if (isOpen && waterTank != null && waterTank.CurrentWater > 0)
        {
            RunTap();
        }
        else
        {
            StopTap();
        }
    }

    void RunTap()
    {
        // Notice we do NOT use Time.deltaTime here, because you already put it in the Tank script!
        waterTank.ConsumeWater(flowRate);

        // Visuals
        if (waterParticles && !waterParticles.isPlaying) waterParticles.Play();

        // Audio
        if (tapAudio && runningWaterSound && !tapAudio.isPlaying)
        {
            tapAudio.clip = runningWaterSound;
            tapAudio.loop = true;
            tapAudio.Play();
        }
    }

    void StopTap()
    {
        if (waterParticles) waterParticles.Stop();
        if (tapAudio) tapAudio.Stop();
        
        // Auto-close tap if the tank runs completely dry
        if (waterTank != null && waterTank.CurrentWater <= 0)
        {
            isOpen = false;
        }
    }

    public void Interact()
    {
        isOpen = !isOpen;
    }
}