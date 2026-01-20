using UnityEngine;

public class GasCanister : MonoBehaviour
{
    [Header("Canister Stats")]
    public float maxGas = 20f;
    public float currentGas = 20f;

    public bool IsEmpty => currentGas <= 0;

    // Optional: Visual to show if it's empty (like a gauge or label)
    public string GetStatus() => $"{currentGas:0}/{maxGas} L";
}