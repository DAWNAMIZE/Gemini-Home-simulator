using UnityEngine;
using TMPro;

public class GasCanister : MonoBehaviour, IInteractable
{
    [Header("Fuel Settings")]
    public float maxGas = 20f;
    public float currentGas = 20f; 

    [Header("UI Visuals")]
    public TextMeshProUGUI gasAmountText; 
    public Canvas canvasUI;

    void Update()
    {
        if (gasAmountText != null)
        {
            gasAmountText.text = currentGas.ToString("F1") + " / " + maxGas.ToString("F1") + " L";
            gasAmountText.color = (currentGas >= maxGas) ? Color.green : Color.cyan;
        }

        if (canvasUI != null && Camera.main != null)
        {
            canvasUI.transform.LookAt(canvasUI.transform.position + Camera.main.transform.rotation * Vector3.forward,
                                     Camera.main.transform.rotation * Vector3.up);
        }
    }

    public void Interact()
    {
        if (PlayerHands.instance != null && PlayerHands.instance.currentHoldType != PlayerHands.HoldType.None) 
        {
            Debug.Log("Hands are full!");
            return;
        }

        PlayerHands.instance.GrabCanister(this);
    }

    public bool IsEmpty()
    {
        return currentGas <= 0f;
    }
}