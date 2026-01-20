using UnityEngine;

public class CookingSystem : MonoBehaviour
{
    [Header("References")]
    public SnapPoint snapPoint;
    public PowerSwitch powerSwitch;
    public GasSystem gasSystem; //

    [Header("Visuals")]
    public Renderer burnerRenderer; // The mesh that turns red when hot
    public Color coldColor = Color.gray;
    public Color hotColor = Color.red;
    public float heatSpeed = 2f;

    [Header("Cooking Settings")]
    public float cookTime = 10f; 
    public float gasConsumptionRate = 0.5f; 
    private float cookTimer;
    private bool isCooking;

    public bool IsCookingActive => isCooking;

    void Update()
    {
        if (snapPoint == null || powerSwitch == null || gasSystem == null) return;

        // Condition: Switch is ON, Pot is there, and there is Gas
        if (powerSwitch.IsPowered && snapPoint.IsOccupied && gasSystem.CurrentGas > 0)
        {
            StartCooking();
        }
        else
        {
            StopCooking();
        }

        HandleCookingLogic();
        UpdateBurnerVisuals();
    }

    void HandleCookingLogic()
    {
        if (isCooking)
        {
            // Consume Gas
            gasSystem.CurrentGas -= gasConsumptionRate * Time.deltaTime;
            gasSystem.CurrentGas = Mathf.Max(0, gasSystem.CurrentGas);

            // Progress the Pot
            Pot pot = snapPoint.CurrentItem.GetComponent<Pot>();
            if (pot != null)
            {
                cookTimer += Time.deltaTime;
                float progress = cookTimer / cookTime;
                pot.UpdateCookProgress(progress);

                if (cookTimer >= cookTime)
                    FinishCooking(pot);
            }
        }
    }

    void UpdateBurnerVisuals()
    {
        if (burnerRenderer != null)
        {
            Color targetColor = isCooking ? hotColor : coldColor;
            burnerRenderer.material.color = Color.Lerp(burnerRenderer.material.color, targetColor, Time.deltaTime * heatSpeed);
        }
    }

    void StartCooking()
    {
        if (!isCooking)
        {
            isCooking = true;
            Debug.Log("Gas ignition successful. Cooking...");
        }
    }

    void StopCooking()
    {
        if (isCooking)
        {
            isCooking = false;
            Debug.Log("Cooking stopped (Power off or out of gas).");
        }
    }

    void FinishCooking(Pot pot)
    {
        isCooking = false;
        if (pot != null) pot.SetCooked(); //
        powerSwitch.IsPowered = false; // Turn off the ignition automatically
        Debug.Log("Meal is ready!");
    }
}