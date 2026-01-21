using UnityEngine;
using UnityEngine.UI;

public class GasCanister : MonoBehaviour
{
    [Header("Gas Stats")]
    public float maxGas = 20f;
    public float currentGas = 0f; // Start at 0 to see it "load up"

    [Header("UI References")]
    public Image fillBar;
    public Canvas canvasUI;

    void Update()
    {
        // Update the bar visual
        if (fillBar != null)
        {
            fillBar.fillAmount = currentGas / maxGas;
            
            // Change color based on level
            if (currentGas / maxGas < 0.2f) fillBar.color = Color.red;
            else fillBar.color = Color.cyan;
        }

        // Optional: Make the UI always face the camera for readability
        if (canvasUI != null && Camera.main != null)
        {
            canvasUI.transform.LookAt(canvasUI.transform.position + Camera.main.transform.rotation * Vector3.forward,
                                     Camera.main.transform.rotation * Vector3.up);
        }
    }
}