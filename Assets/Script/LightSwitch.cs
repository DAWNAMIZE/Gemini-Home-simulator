using UnityEngine;

public class LightSwitch : MonoBehaviour, IInteractable
{
    public Light controlledLight;

    public void Interact()
    {
        if (controlledLight != null)
            controlledLight.enabled = !controlledLight.enabled;
    }
}
