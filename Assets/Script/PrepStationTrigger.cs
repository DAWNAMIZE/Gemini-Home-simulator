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
            // SUCCESS! Open the UI and tell it WHICH pot is on the fire
            RecipeUIManager.instance.OpenMenu(linkedStove.currentPotOnStove);
        }
        else
        {
            Debug.Log("You must place an empty pot on the fire before you can start prepping a recipe.");
        }
    }
}