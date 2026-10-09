using UnityEngine;

public class PrepStationTrigger : MonoBehaviour, IInteractable
{
    [Header("Oven Connection")]
    [Tooltip("Drag your Oven/Stove object here so the table knows if a pot is ready")]
    public StoveSnapPoint linkedStove;

    public void Interact()
    {
        if (linkedStove == null) return;

        // Check if the oven actually has a pot sitting on it
        if (linkedStove.currentPotOnStove != null)
        {
            // Grab the PortablePot script from the 3D GameObject
            PortablePot potComponent = linkedStove.currentPotOnStove.GetComponent<PortablePot>();

            // SUCCESS! Open the Prep Station UI and pass the active pot component
            if (PrepStationUI.instance != null && potComponent != null)
            {
                PrepStationUI.instance.OpenPrepStation(potComponent);
            }
        }
        else
        {
            Debug.Log("You must place an empty pot on the fire before you can start prepping a recipe.");
        }
    }
}