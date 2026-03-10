using UnityEngine;
using UnityEngine.UI;

public class StoveBurner : MonoBehaviour, IInteractable
{
    [Header("Setup")]
    public Transform potSnapPoint; 
    public GasCanister gasSystem;
    
    [Header("UI Meter")]
    public GameObject timerCanvas; // Floating World Space Canvas
    public Image timerFill;

    private PortablePot placedPot;
    private bool isGasOn = false;

    void Update()
    {
        if (isGasOn && placedPot != null && gasSystem.currentGas > 0)
        {
            // Drain Gas
            gasSystem.currentGas -= Time.deltaTime;

            // Cook
            if (placedPot.state == PortablePot.CookingState.Raw || placedPot.state == PortablePot.CookingState.Cooking)
            {
                placedPot.state = PortablePot.CookingState.Cooking;
                placedPot.cookProgress += Time.deltaTime;
                placedPot.UpdateVisuals();

                // Update UI
                if (timerCanvas)
                {
                    timerCanvas.SetActive(true);
                    timerFill.fillAmount = placedPot.cookProgress / placedPot.currentDish.cookTime;
                }

                // Check Done
                if (placedPot.cookProgress >= placedPot.currentDish.cookTime)
                {
                    placedPot.state = PortablePot.CookingState.Cooked;
                    placedPot.UpdateVisuals();
                    isGasOn = false; // Auto-off when done
                }
            }
        }
        else
        {
            if (timerCanvas) timerCanvas.SetActive(false);
        }
    }

    public void Interact()
    {
        // 1. Place Pot
        if (PlayerInventory.instance.isHoldingPot && placedPot == null)
        {
            PlacePot();
            return;
        }

        // 2. Toggle Gas
        if (placedPot != null && gasSystem.currentGas > 0)
        {
            isGasOn = !isGasOn;
        }
    }

    void PlacePot()
    {
        PortablePot pot = PlayerInventory.instance.heldPot;
        
        // Reactivate Pot
        pot.gameObject.SetActive(true);
        pot.transform.position = potSnapPoint.position;
        pot.transform.rotation = potSnapPoint.rotation;
        
        placedPot = pot;
        PlayerInventory.instance.DropPot(); // Clear from hand
    }
}