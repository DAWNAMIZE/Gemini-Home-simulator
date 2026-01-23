using UnityEngine;

public class CookerSwitch : MonoBehaviour, IInteractable
{
    public GasCooker parentCooker;

    public void Interact()
    {
        if (parentCooker != null)
        {
            parentCooker.ToggleCooker();
        }
    }
}