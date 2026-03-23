using UnityEngine;

public class StoveBurner : MonoBehaviour, IInteractable
{
    [Header("Connections")]
    public SnapPoint burnerSocket;       // UPDATED: Now uses your universal SnapPoint
    public SnapPoint gasSocket;          // UPDATED: Now uses your universal SnapPoint
    public ParticleSystem cookingSteam;     

    [Header("Burner State")]
    public bool isGasOn = false;
    public float fuelDrainRate = 1f;        

    void Update()
    {
        // 1. Safely find the Gas Canister attached to the universal socket
        GasCanister connectedCanister = null;
        if (gasSocket != null && gasSocket.IsOccupied && gasSocket.CurrentItem != null)
        {
            connectedCanister = gasSocket.CurrentItem.GetComponent<GasCanister>();
        }

        // 2. Check if we have a connected canister that STILL HAS FUEL
        bool hasFuel = connectedCanister != null && !connectedCanister.IsEmpty();

        // 3. Auto-shutoff if we run out of gas while cooking
        if (isGasOn && !hasFuel)
        {
            isGasOn = false;
            Debug.Log("The gas canister is empty! The stove sputtered out.");
        }

        // 4. Drain the fuel if the stove is on and a canister is plugged in
        if (isGasOn && hasFuel)
        {
            connectedCanister.currentGas -= fuelDrainRate * Time.deltaTime;
            
            // Don't let fuel drop below zero
            if (connectedCanister.currentGas < 0) 
            {
                connectedCanister.currentGas = 0;
            }
        }

        // 5. Handle the visual steam (Only smoke if gas is on, we have fuel, and a pot is present)
        bool shouldSmoke = isGasOn && hasFuel && burnerSocket != null && burnerSocket.IsOccupied;

        if (cookingSteam != null)
        {
            if (shouldSmoke && !cookingSteam.isPlaying) 
                cookingSteam.Play();
            else if (!shouldSmoke && cookingSteam.isPlaying) 
                cookingSteam.Stop();
        }
    }

    public void Interact()
    {
        GasCanister connectedCanister = null;
        if (gasSocket != null && gasSocket.IsOccupied && gasSocket.CurrentItem != null)
        {
            connectedCanister = gasSocket.CurrentItem.GetComponent<GasCanister>();
        }

        // Safety lock: You cannot turn the stove on without a canister
        if (connectedCanister == null)
        {
            Debug.Log("Cannot turn on stove: No gas canister connected!");
            isGasOn = false;
            return;
        }

        // Safety lock: You cannot turn the stove on if the canister is completely empty
        if (connectedCanister.IsEmpty())
        {
            Debug.Log("Cannot turn on stove: The connected canister is empty!");
            isGasOn = false;
            return;
        }

        // Toggle the gas
        isGasOn = !isGasOn;
        Debug.Log(isGasOn ? "Stove ignited." : "Stove turned off.");
    }
}