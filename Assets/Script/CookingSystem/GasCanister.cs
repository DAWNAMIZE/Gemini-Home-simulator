using UnityEngine;
using TMPro;

public class GasCanister : MonoBehaviour
{
    public float maxGas = 20f;
    public float currentGas = 0f; 

    public TextMeshProUGUI gasAmountText; 
    public Canvas canvasUI;

    void Update()
    {
        if (gasAmountText != null)
        {
            // Forces the text to update every frame
            gasAmountText.text = currentGas.ToString("F1") + " / " + maxGas.ToString("F1") + " L";
            
            // Visual Polish
            gasAmountText.color = (currentGas >= maxGas) ? Color.green : Color.cyan;
        }

        // Keep the UI facing the player
        if (canvasUI != null && Camera.main != null)
        {
            canvasUI.transform.LookAt(canvasUI.transform.position + Camera.main.transform.rotation * Vector3.forward,
                                     Camera.main.transform.rotation * Vector3.up);
        }
    }
}