using UnityEngine;

public class WaterTankSystem : MonoBehaviour
{
    [Header("Tank Statistics")]
    public float MaxWater = 1000f; // Total capacity in Liters
    public float CurrentWater = 750f; // Starting amount

    [Header("Visuals (Optional)")]
    // Assign a blue cylinder or plane inside your tank model to visualize the level
    public Transform waterLevelMesh; 
    public Vector3 emptyScale = new Vector3(1, 0.1f, 1); // Size when empty
    public Vector3 fullScale = new Vector3(1, 1f, 1);    // Size when full

    void Update()
    {
        // Optional: Update the visual water level every frame
        UpdateVisuals();
    }

    // --- PUBLIC METHODS (For other scripts to use) ---

    // Call this from a Tap, Shower, or Toilet script
    // Returns TRUE if water was successfully taken
    public bool ConsumeWater(float amount)
    {
        if (CurrentWater >= amount)
        {
            CurrentWater -= amount;
            return true; 
        }
        else
        {
            Debug.Log("Water Tank Empty!");
            return false;
        }
    }

    // Call this from a Rain Collector or Refill Truck script
    public void AddWater(float amount)
    {
        CurrentWater += amount;
        CurrentWater = Mathf.Min(CurrentWater, MaxWater); // Clamp to max
    }

    // --- INTERNAL VISUALS ---

    private void UpdateVisuals()
    {
        if (waterLevelMesh != null)
        {
            // Calculate percentage (0 to 1)
            float percentage = CurrentWater / MaxWater;

            // Smoothly scale the water mesh up/down
            waterLevelMesh.localScale = Vector3.Lerp(emptyScale, fullScale, percentage);
        }
    }
}
