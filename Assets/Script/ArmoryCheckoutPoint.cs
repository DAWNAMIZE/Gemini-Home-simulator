using UnityEngine;

/// <summary>
/// Physical checkout counter for the armory. Interacting pays for the current
/// order if one exists, otherwise it opens the catalogue so the player can
/// build an order.
/// </summary>
public class ArmoryCheckoutPoint : MonoBehaviour, IInteractable
{
    [Header("Checkout")]
    public string counterName = "Armory Checkout";

    public void Interact()
    {
        ArmoryUI ui = ArmoryUI.GetOrCreate();
        if (ui == null) return;

        if (ui.HasCart)
        {
            ui.CheckoutFromCounter();
            return;
        }

        ToastUI.Show("Add gear to your order, then return to the checkout.");
        ui.ShowCatalogue();
    }
}
