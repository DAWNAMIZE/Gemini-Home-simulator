using UnityEngine;

public class CookingSystem : MonoBehaviour
{
    [Header("References")]
    public SnapPoint snapPoint;
    public PowerSwitch powerSwitch;

    [Header("Cooking Settings")]
    public float cookTime = 5f;   // seconds required to cook
    private float cookTimer;
    private bool isCooking;

    void Update()
    {
        if (snapPoint == null || powerSwitch == null) return;

        if (powerSwitch.IsPowered && snapPoint.IsOccupied)
        {
            StartCooking();
        }
        else
        {
            StopCooking();
        }

        if (isCooking)
        {
            cookTimer += Time.deltaTime;
            if (cookTimer >= cookTime)
            {
                FinishCooking();
            }
        }
    }

    void StartCooking()
    {
        if (!isCooking)
        {
            isCooking = true;
            cookTimer = 0f;
            Debug.Log("Cooking started!");
        }
    }

    void StopCooking()
    {
        if (isCooking)
        {
            isCooking = false;
            Debug.Log("Cooking stopped.");
        }
    }

    void FinishCooking()
    {
        isCooking = false;
        Debug.Log("Cooking complete!");
        // You can add effects or change item states here
    }
}
