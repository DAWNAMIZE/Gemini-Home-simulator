using UnityEngine;

/// <summary>
/// Pickable firewood. Interacting puts the log bundle in the player's hand;
/// carrying it to the oven and interacting feeds the oven's four-plate fuel
/// counter. The Drop button puts it back on the ground.
/// </summary>
public class WoodPickup : MonoBehaviour, IInteractable
{
    [Header("Display")]
    public string displayName = "Wood";

    [Header("Held Pose")]
    [Tooltip("Scale multiplier applied while the wood is in the player's hand.")]
    public float heldScaleMultiplier = 0.55f;
    public Vector3 heldLocalPosition = new Vector3(0f, -0.12f, 0.18f);
    public Vector3 heldLocalEuler = new Vector3(0f, 0f, 12f);

    void Awake()
    {
        gameObject.isStatic = false;
        EnsureCollider();
        ConfigureHeldPose();
    }

    private void EnsureCollider()
    {
        BoxCollider box = GetComponent<BoxCollider>();
        if (box == null) box = gameObject.AddComponent<BoxCollider>();

        Renderer renderer = GetComponent<Renderer>();
        if (renderer == null) renderer = GetComponentInChildren<Renderer>(true);

        if (renderer != null)
        {
            Bounds bounds = renderer.localBounds;
            if (bounds.size.sqrMagnitude > 0.000001f)
            {
                box.center = bounds.center;
                box.size = bounds.size;
                return;
            }
        }

        // Fallback for renderer-less placeholders.
        box.center = Vector3.zero;
        box.size = new Vector3(0.25f, 0.25f, 0.8f);
    }

    private void ConfigureHeldPose()
    {
        HeldItemPose pose = GetComponent<HeldItemPose>();
        if (pose == null) pose = gameObject.AddComponent<HeldItemPose>();

        pose.Capture();
        pose.localPosition = heldLocalPosition;
        pose.localEuler = heldLocalEuler;
        pose.localScale = transform.localScale * Mathf.Max(0.05f, heldScaleMultiplier);
    }

    public void Interact()
    {
        PlayerInventory inventory = PlayerInventory.instance;
        if (inventory == null)
        {
            Debug.LogWarning("WoodPickup: no PlayerInventory found.");
            return;
        }

        if (!inventory.IsHandEmpty)
        {
            ToastUI.Show("Your hands are full - drop what you are holding first.");
            return;
        }

        if (inventory.TryHoldPhysicalItem(gameObject))
            ToastUI.Show("Picked up wood - carry it to the oven.");
    }
}
