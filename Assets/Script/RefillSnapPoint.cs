using UnityEngine;

public class RefillSnapPoint : MonoBehaviour
{
    [Header("References")]
    public SnapPoint snapPoint;      // Drag the SnapPoint component here
    public GasSystem houseGasSystem; // Drag the House GasSystem here

    [Header("Refill Settings")]
    public float refillSpeed = 2f;   // How much gas transfers per second

    void Update()
    {
        // 1. Check if an item is snapped
        if (snapPoint.IsOccupied && snapPoint.CurrentItem != null)
        {
            // 2. Try to get the GasCanister component from the snapped item
            GasCanister canister = snapPoint.CurrentItem.GetComponent<GasCanister>();

            if (canister != null && !canister.IsEmpty)
            {
                TransferGas(canister);
            }
        }
    }

    void TransferGas(GasCanister canister)
    {
        // Don't overfill the house
        if (houseGasSystem.CurrentGas >= houseGasSystem.MaxGas) return;

        float amountToTransfer = refillSpeed * Time.deltaTime;

        // Ensure we don't take more than the canister has
        amountToTransfer = Mathf.Min(amountToTransfer, canister.currentGas);

        // Move the gas
        canister.currentGas -= amountToTransfer;
        houseGasSystem.CurrentGas += amountToTransfer;

        // Clamp values to be safe
        houseGasSystem.CurrentGas = Mathf.Min(houseGasSystem.CurrentGas, houseGasSystem.MaxGas);
        
        Debug.Log($"Refilling... House: {houseGasSystem.CurrentGas:0} | Canister: {canister.currentGas:0}");
    }
}