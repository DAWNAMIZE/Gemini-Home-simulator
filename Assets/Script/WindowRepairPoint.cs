using UnityEngine;

/// <summary>
/// Window-1 repair marker. While the player holds Hammer_01 nearby the repair
/// prompt appears; repairing spawns a Wooden-Bars barricade at this window.
/// The window can also be aimed at and boarded with the normal interact button.
/// </summary>
public class WindowRepairPoint : MonoBehaviour, IInteractable
{
    [Header("Window")]
    public string windowName = "Window-1";
    public float interactionRange = 3.2f;
    public Transform barricadeAnchor;

    [Header("Barricade")]
    [Tooltip("Prefab spawned when this window is boarded up. Leave empty to use the fallback Wooden-Bars prefab.")]
    public GameObject barricadePrefab;

    [Header("State")]
    public WoodenBarricade barricade;

    [Header("Audio (Optional)")]
    [Tooltip("Sound played when the window is boarded up.")]
    public AudioClip repairSound;

    [Tooltip("Optional AudioSource. One is added automatically when left empty.")]
    public AudioSource repairAudioSource;

    [Range(0f, 1f)] public float repairVolume = 1f;

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

    public void Interact()
    {
        if (barricade != null)
        {
            ToastUI.Show("This window is already boarded up.");
            return;
        }

        if (!HasHammer)
        {
            ToastUI.Show("You need the hammer equipped to board this window.");
            return;
        }

        Repair();
    }

    public void Repair()
    {
        if (barricade != null)
        {
            ToastUI.Show("This window is already boarded up.");
            return;
        }

        Transform anchor = barricadeAnchor != null ? barricadeAnchor : transform;
        barricade = WoodenBarricade.Spawn(anchor.position, anchor.rotation, this, barricadePrefab);
        PlayRepairSound();

        promptVisible = false;
        RepairUI.HideIf(this);
    }

    private void PlayRepairSound()
    {
        if (repairSound == null) return;

        if (repairAudioSource == null)
        {
            repairAudioSource = GetComponent<AudioSource>();
            if (repairAudioSource == null)
            {
                repairAudioSource = gameObject.AddComponent<AudioSource>();
                repairAudioSource.playOnAwake = false;
                repairAudioSource.spatialBlend = 1f;
            }
        }

        repairAudioSource.PlayOneShot(repairSound, repairVolume);
    }

    public void NotifyBarricadeDestroyed(WoodenBarricade destroyed)
    {
        if (barricade == destroyed) barricade = null;
    }
}
