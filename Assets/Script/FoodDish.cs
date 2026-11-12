using UnityEngine;

/// <summary>
/// A finished dish that can be picked up, eaten, dropped, or snapped onto a tray.
/// The component can be attached at runtime with <see cref="Attach"/> so dish
/// prefabs do not need to be edited manually.
/// </summary>
public class FoodDish : MonoBehaviour, IInteractable
{
    [Header("Food Data")]
    public string dishName = "Dish";
    public float healAmount = 30f;

    [Header("State")]
    public bool isHeld;

    private int originalLayer = 0;
    private bool physicsReady;
    private FoodTray placedTray;

    void Awake()
    {
        EnsurePhysics();
    }

    // ------------------------------------------------------------------
    // ATTACH / SETUP
    // ------------------------------------------------------------------

    public static FoodDish Attach(GameObject dish, string displayName = null)
    {
        if (dish == null) return null;

        FoodDish foodDish = dish.GetComponent<FoodDish>();
        if (foodDish == null) foodDish = dish.AddComponent<FoodDish>();

        if (!string.IsNullOrEmpty(displayName)) foodDish.dishName = displayName;

        foodDish.EnsurePhysics();
        return foodDish;
    }

    private void EnsurePhysics()
    {
        if (physicsReady) return;
        physicsReady = true;

        originalLayer = gameObject.layer;

        if (GetComponentInChildren<Collider>(true) == null)
        {
            BoxCollider box = gameObject.AddComponent<BoxCollider>();

            Renderer[] renderers = GetComponentsInChildren<Renderer>(true);
            if (renderers.Length > 0)
            {
                Bounds bounds = renderers[0].bounds;
                for (int i = 1; i < renderers.Length; i++)
                    bounds.Encapsulate(renderers[i].bounds);

                Vector3 scale = transform.lossyScale;
                box.center = transform.InverseTransformPoint(bounds.center);
                box.size = new Vector3(
                    Mathf.Abs(scale.x) > 0.0001f ? bounds.size.x / Mathf.Abs(scale.x) : bounds.size.x,
                    Mathf.Abs(scale.y) > 0.0001f ? bounds.size.y / Mathf.Abs(scale.y) : bounds.size.y,
                    Mathf.Abs(scale.z) > 0.0001f ? bounds.size.z / Mathf.Abs(scale.z) : bounds.size.z);
            }
        }

        if (GetComponent<Rigidbody>() == null)
        {
            Rigidbody rb = gameObject.AddComponent<Rigidbody>();
            rb.mass = 1.5f;
            rb.useGravity = true;
            rb.interpolation = RigidbodyInterpolation.Interpolate;
        }
    }

    // ------------------------------------------------------------------
    // INTERACTION
    // ------------------------------------------------------------------

    public void Interact()
    {
        PickUp();
    }

    public void PickUp()
    {
        EnsurePhysics();

        if (PlayerInventory.instance == null)
        {
            Debug.LogWarning("FoodDish: PlayerInventory was not found.");
            return;
        }

        if (placedTray != null)
        {
            placedTray.NotifyFoodRemoved(this);
            placedTray = null;
        }

        transform.SetParent(null, true);
        PlayerInventory.instance.HoldPhysicalItem(gameObject);

        SetHeldPhysics(true);
        isHeld = true;

        FoodActionUI.Show(this);
    }

    public void Eat()
    {
        if (PlayerVitals.instance != null)
            PlayerVitals.instance.Heal(healAmount);
        else
            Debug.LogWarning("FoodDish: PlayerVitals was not found; health was not restored.");

        ReleaseFromPlayer();
        FoodActionUI.Hide();

        Destroy(gameObject);
    }

    public void Drop()
    {
        Vector3 dropPosition = transform.position;
        Quaternion dropRotation = transform.rotation;

        ReleaseFromPlayer();

        if (PlayerInventory.instance != null && PlayerInventory.instance.dropSpawnPoint != null)
        {
            dropPosition = PlayerInventory.instance.dropSpawnPoint.position;
        }

        transform.SetParent(null, true);
        transform.SetPositionAndRotation(dropPosition, dropRotation);

        placedTray = null;
        SetHeldPhysics(false);
        isHeld = false;

        FoodActionUI.Hide();
    }

    public void PlaceOnTray(FoodTray tray)
    {
        if (tray == null)
        {
            Drop();
            return;
        }

        ReleaseFromPlayer();

        placedTray = tray;
        isHeld = false;

        tray.PlaceFood(this);
        FoodActionUI.Hide();
    }

    /// <summary>
    /// Called by PlayerInventory when the phone's drop button was used.
    /// </summary>
    public void OnDroppedByInventory()
    {
        placedTray = null;
        isHeld = false;
        SetHeldPhysics(false);
        FoodActionUI.Hide();
    }

    public void RestoreWorldPhysics()
    {
        EnsurePhysics();
        SetHeldPhysics(false);
        isHeld = false;
    }

    /// <summary>
    /// Locks the dish into place on a tray. It keeps its collider enabled so it
    /// can be tapped again, but it no longer falls under gravity.
    /// </summary>
    public void SnapToTray()
    {
        EnsurePhysics();

        Collider[] colliders = GetComponentsInChildren<Collider>(true);
        foreach (Collider col in colliders)
            col.enabled = true;

        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = true;
            rb.useGravity = false;
        }

        gameObject.layer = originalLayer;
        isHeld = false;
    }

    // ------------------------------------------------------------------
    // PHYSICS
    // ------------------------------------------------------------------

    private void SetHeldPhysics(bool held)
    {
        Collider[] colliders = GetComponentsInChildren<Collider>(true);
        foreach (Collider col in colliders)
            col.enabled = !held;

        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = held;
            rb.useGravity = !held;
#if UNITY_6000_0_OR_NEWER
            if (held) rb.linearVelocity = Vector3.zero;
#else
            if (held) rb.velocity = Vector3.zero;
#endif
        }

        gameObject.layer = held ? 2 : originalLayer;
    }

    private void ReleaseFromPlayer()
    {
        if (PlayerInventory.instance != null &&
            PlayerInventory.instance.currentlyHeldModel == gameObject)
        {
            PlayerInventory.instance.ClearHand();
        }
    }
}
