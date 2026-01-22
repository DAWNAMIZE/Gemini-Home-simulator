using UnityEngine;

public class RefillSnapPoint : MonoBehaviour
{
    public SnapPoint snapPoint;      
    public float refillSpeed = 10.0f; 

    void Update()
    {
        // Safety check
        if (snapPoint == null) return;

        // If occupied, get the GasCanister component from the locked item
        if (snapPoint.IsOccupied && snapPoint.CurrentItem != null)
        {
            GasCanister canister = snapPoint.CurrentItem.GetComponent<GasCanister>();

            if (canister != null && canister.currentGas < canister.maxGas)
            {
                canister.currentGas += refillSpeed * Time.deltaTime;
                canister.currentGas = Mathf.Min(canister.currentGas, canister.maxGas);
            }
        }
    }
}