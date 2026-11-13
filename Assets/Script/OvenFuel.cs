using UnityEngine;

/// <summary>
/// Wood fuel for the survival oven. One load of wood allows four finished
/// plates; when the counter reaches zero cooking is blocked until the player
/// walks up with more wood and interacts.
/// </summary>
public class OvenFuel : MonoBehaviour, IInteractable
{
    public static OvenFuel instance;

    [Header("Fuel")]
    [Tooltip("How many plates a single piece of wood can cook.")]
    public int platesPerLoad = 4;

    [Tooltip("Plates left in the current wood load.")]
    public int platesRemaining = 4;

    public bool HasFuel => platesRemaining > 0;
    public int PlatesRemaining => Mathf.Max(0, platesRemaining);

    void Awake()
    {
        if (instance == null) instance = this;
    }

    void OnDestroy()
    {
        if (instance == this) instance = null;
    }

    /// <summary>
    /// Finds the fuel component that owns a pot. Pots parented under the stove
    /// resolve their own oven; loose pots fall back to the scene's oven.
    /// </summary>
    public static OvenFuel FindFor(PortablePot pot)
    {
        if (pot != null)
        {
            OvenFuel parent = pot.GetComponentInParent<OvenFuel>();
            if (parent != null) return parent;
        }

        if (instance != null) return instance;

        instance = Object.FindFirstObjectByType<OvenFuel>(FindObjectsInactive.Include);
        return instance;
    }

    public bool ConsumePlate()
    {
        if (platesRemaining <= 0) return false;

        platesRemaining--;

        if (platesRemaining == 0)
            ToastUI.Show("The oven is out of fuel. Bring wood to keep cooking.");

        return true;
    }

    /// <summary>
    /// Called when the player interacts with the oven (or the pot) while
    /// holding wood. Consumes the held wood and refills the fuel counter.
    /// </summary>
    public bool TryInsertHeldWood()
    {
        PlayerInventory inventory = PlayerInventory.instance;
        if (inventory == null || inventory.currentlyHeldModel == null) return false;

        GameObject held = inventory.currentlyHeldModel;
        if (held == null) return false;

        bool isWood = held.GetComponentInParent<WoodPickup>() != null ||
                      held.GetComponentInChildren<WoodPickup>(true) != null ||
                      held.name.IndexOf("wood", System.StringComparison.OrdinalIgnoreCase) >= 0;

        if (!isWood) return false;

        platesRemaining = Mathf.Max(1, platesPerLoad);
        inventory.ClearHand();
        Destroy(held);

        ToastUI.Show("Wood added - " + platesRemaining + " more plates can be cooked.");
        return true;
    }

    /// <summary>
    /// Tapping the oven without wood reports the current fuel state.
    /// </summary>
    public void Interact()
    {
        if (TryInsertHeldWood()) return;

        if (HasFuel)
            ToastUI.Show("Oven fuel: " + platesRemaining + " / " + platesPerLoad + " plates left.");
        else
            ToastUI.Show("The oven is cold. Pick up wood and interact with the oven.");
    }

    public string GetFuelSummary()
    {
        return platesRemaining + " / " + Mathf.Max(1, platesPerLoad) + " plates";
    }
}
