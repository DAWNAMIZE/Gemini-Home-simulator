using UnityEngine;

public class GasCooker : MonoBehaviour
{
    [Header("References")]
    public SnapPoint snapPoint;
    public ParticleSystem flameParticles;
    
    [Header("Indicator Settings")]
    public MeshRenderer burnerRingRenderer; // Drag the Ring model here
    public Color offColor = Color.gray;
    public Color onColor = Color.red;

    [Header("Settings")]
    public float consumptionRate = 0.5f;
    private bool isCookerOn = false;

    void Update()
    {
        // 1. Check if canister is docked
        if (snapPoint != null && snapPoint.IsOccupied && snapPoint.CurrentItem != null)
        {
            GasCanister canister = snapPoint.CurrentItem.GetComponent<GasCanister>();

            // 2. Check if we have gas and the knob is ON
            if (isCookerOn && canister != null && canister.currentGas > 0)
            {
                RunCooker(canister);
            }
            else
            {
                StopCooker();
            }
        }
        else
        {
            StopCooker();
        }
    }

    void RunCooker(GasCanister canister)
    {
        canister.currentGas -= consumptionRate * Time.deltaTime;
        
        // Visuals
        if (!flameParticles.isPlaying) flameParticles.Play();
        
        // Change the ring to RED
        burnerRingRenderer.material.color = onColor; 
        
        // If your shader uses Emission, use this line instead:
        // burnerRingRenderer.material.SetColor("_EmissionColor", onColor);
    }

    void StopCooker()
    {
        if (flameParticles.isPlaying) flameParticles.Stop();
        
        // Change the ring back to GRAY (or your default color)
        if(burnerRingRenderer != null)
            burnerRingRenderer.material.color = offColor;
    }

    public void ToggleCooker()
    {
        isCookerOn = !isCookerOn;
    }
}