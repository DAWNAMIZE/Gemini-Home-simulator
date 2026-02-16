using UnityEngine;

public class GasSystem : MonoBehaviour
{
    [Header("Gas Stats")]
    public float MaxGas = 100f;
    public float CurrentGas = 100f;

    // THIS WAS MISSING
    public void ConsumeGas(float amount)
    {
        CurrentGas -= amount;
        
        // Prevent negative gas
        if (CurrentGas < 0) CurrentGas = 0;
    }
}