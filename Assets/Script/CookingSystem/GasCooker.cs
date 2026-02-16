using UnityEngine;

public class GasCooker : MonoBehaviour
{
    [Header("Connections")]
    public SnapPoint snapPoint;        // DRAG YOUR SNAP POINT HERE (The socket)
    public GasSystem gasSystem;        // DRAG YOUR WALL "GasSystem" HERE (The bridge to the phone)

    [Header("Visuals")]
    public ParticleSystem flameParticles;
    public MeshRenderer burnerRingRenderer;
    public Color offColor = Color.gray;
    public Color onColor = Color.red;

    [Header("Audio")]
    public AudioSource burnerAudio;    // Drag AudioSource here (CHECK 'Loop' in Inspector)
    public AudioClip burningLoopSound; // Drag Hissing Sound

    [Header("Settings")]
    public float consumptionRate = 2f;
    public bool isCookerOn = false;

    void Update()
    {
        // 1. Find the Canister (The Source of Truth)
        GasCanister canister = null;
        if (snapPoint != null && snapPoint.IsOccupied && snapPoint.CurrentItem != null)
        {
            canister = snapPoint.CurrentItem.GetComponent<GasCanister>();
        }

        // 2. THE BRIDGE: Sync Canister Data to Wall System (so Phone sees it)
        if (gasSystem != null)
        {
            if (canister != null)
            {
                // Copy the exact values from Canister to Wall System
                gasSystem.CurrentGas = canister.currentGas;
                gasSystem.MaxGas = canister.maxGas;
            }
            else
            {
                // No canister connected? Phone should show 0.
                gasSystem.CurrentGas = 0;
            }
        }

        // 3. Cooking Logic: Is Switch ON? + Is Canister attached? + Is there Gas?
        if (isCookerOn && canister != null && canister.currentGas > 0)
        {
            RunCooker(canister);
        }
        else
        {
            StopCooker();
        }
    }

    void RunCooker(GasCanister canister)
    {
        // CONSUME: Take gas directly from the Canister
        canister.currentGas -= consumptionRate * Time.deltaTime;
        
        // Safety: Don't let it go below 0
        if (canister.currentGas < 0) canister.currentGas = 0;

        // VISUALS: Fire & Red Ring
        if (flameParticles != null && !flameParticles.isPlaying) flameParticles.Play();
        if (burnerRingRenderer != null) burnerRingRenderer.material.color = onColor;

        // AUDIO: Play Hiss Loop
        if (burnerAudio != null && burningLoopSound != null)
        {
            if (!burnerAudio.isPlaying)
            {
                burnerAudio.clip = burningLoopSound;
                burnerAudio.loop = true; 
                burnerAudio.Play();
            }
        }
    }

    void StopCooker()
    {
        // Stop Visuals
        if (flameParticles != null) flameParticles.Stop();
        if (burnerRingRenderer != null) burnerRingRenderer.material.color = offColor;
        
        // Stop Audio
        if (burnerAudio != null) burnerAudio.Stop();
    }

    // This is called by the Knob
    public void ToggleCooker()
    {
        isCookerOn = !isCookerOn;
        if (!isCookerOn) StopCooker();
    }
}