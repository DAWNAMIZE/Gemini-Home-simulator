using UnityEngine;

/// <summary>
/// Window-1 repair marker. While the player holds Hammer_01 nearby the repair
/// prompt appears; repairing spawns a Wooden-Bars barricade at this window.
/// </summary>
public class WindowRepairPoint : MonoBehaviour
{
    [Header("Window")]
    public string windowName = "Window-1";
    public float interactionRange = 3.2f;
    public Transform barricadeAnchor;

    [Header("State")]
    public WoodenBarricade barricade;

    private bool promptVisible;

    public bool HasHammer
    {
        get
        {
            if (PlayerInventory.instance == null) return false;
            GameObject held = PlayerInventory.instance.currentlyHeldModel;
            if (held == null) return false;

            return held.name.IndexOf("Hammer", System.StringComparison.OrdinalIgnoreCase) >= 0;
        }
    }

    void Update()
    {
        bool canShow = false;

        if (PlayerInventory.instance != null && barricade == null)
        {
            float distance = Vector3.Distance(
                PlayerInventory.instance.transform.position, transform.position);
            canShow = distance <= interactionRange && HasHammer;
        }

        if (canShow && !promptVisible)
        {
            promptVisible = true;
            RepairUI.Show(this);
        }
        else if (!canShow && promptVisible)
        {
            promptVisible = false;
            RepairUI.HideIf(this);
        }
    }

    public void Repair()
    {
        if (barricade != null)
        {
            ToastUI.Show("This window is already boarded up.");
            return;
        }

        Transform anchor = barricadeAnchor != null ? barricadeAnchor : transform;
        barricade = WoodenBarricade.Spawn(anchor.position, anchor.rotation, this);

        promptVisible = false;
        RepairUI.HideIf(this);
    }

    public void NotifyBarricadeDestroyed(WoodenBarricade destroyed)
    {
        if (barricade == destroyed) barricade = null;
    }
}
