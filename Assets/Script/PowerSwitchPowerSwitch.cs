using UnityEngine;

public class PowerSwitch : MonoBehaviour, IInteractable
{
    [Header("Power Switch Settings")]
    public bool IsPowered = false;

    [Header("Optional Visual Indicator")]
    public Light indicatorLight;
    public Color onColor = Color.green;
    public Color offColor = Color.red;

    // Called by your interaction system
    public void Interact()
    {
        TogglePower();
    }

    public void TogglePower()
    {
        IsPowered = !IsPowered;

        Debug.Log("Cooker Power: " + (IsPowered ? "ON" : "OFF"));

        UpdateVisuals();
    }

    private void UpdateVisuals()
    {
        if (indicatorLight != null)
        {
            indicatorLight.enabled = true;
            indicatorLight.color = IsPowered ? onColor : offColor;
        }
    }
}
