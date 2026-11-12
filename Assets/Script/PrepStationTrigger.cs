using UnityEngine;

public class PrepStationTrigger : MonoBehaviour, IInteractable
{
    [Header("Oven Connection")]
    [Tooltip("Drag your Oven/Stove object here so the table knows if a pot is ready")]
    public StoveSnapPoint linkedStove;

    public void Interact()
    {
        if (PrepStationUI.instance == null)
        {
            Debug.LogWarning("PrepStationTrigger: no PrepStationUI found in the scene.");
            return;
        }

        PortablePot pot = null;

        if (linkedStove != null && linkedStove.currentPotOnStove != null)
            pot = linkedStove.currentPotOnStove.GetComponent<PortablePot>();

        // The station always opens so the player can choose a recipe. If a pot
        // is on the stove it cooks there; otherwise the station spawns the dish
        // at the finished-dish spawn point.
        PrepStationUI.instance.OpenPrepStation(pot);
    }
}
