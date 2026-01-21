using UnityEngine;

public class RefillSnapPoint : MonoBehaviour
{
    [Header("References")]
    public SnapPoint snapPoint;      
    public float refillSpeed = 5.0f; // Units per second

    [Header("Source Settings")]
    public bool isInfiniteSource = true; // Is this a gas pump?

    void Update()
    {
        // Check if a canister is snapped in the socket
        if (snapPoint.IsOccupied && snapPoint.CurrentItem != null)
        {
            GasCanister canister = snapPoint.CurrentItem.GetComponent<GasCanister>();

            if (canister != null && canister.currentGas < canister.maxGas)
            {
                // Transfer gas into the canister
                float amount = refillSpeed * Time.deltaTime;
                canister.currentGas += amount;

                // Clamp to max
                canister.currentGas = Mathf.Min(canister.currentGas, canister.maxGas);

                // Optional: add a "hissing" sound while filling
            }
        }
    }
}