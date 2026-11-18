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
    public Transform assignedSpawnPoint;

    [Header("Held Pose")]
    public Vector3 heldLocalPosition = new Vector3(0f, -0.03f, 0.16f);
    public Vector3 heldLocalEuler = new Vector3(0f, 180f, 0f);
    public float heldMaxSize = 0.5f;

    private int originalLayer = 0;
    private bool physicsReady;
    private FoodTray placedTray;
    private CustomerTray placedCustomerTray;
    private Vector3 originalLocalScale = Vector3.one;
    private bool scaleCaptured;

    void Awake()
    {
        CaptureOriginalScale();
        EnsurePhysics();
    }

    void OnDestroy()
    {
        ReleaseSpawnSlot();
        NotifyCustomerTrayRemoved();
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

        // Never allow a second pickup while the hand already holds something.
        if (!PlayerInventory.instance.IsHandEmpty)
        {
            ToastUI.Show("Your hands are full - drop what you are holding first.");
            return;
        }

        if (isHeld) return;

        if (placedCustomerTray != null &&
            placedCustomerTray.assignedCustomer != null &&
            placedCustomerTray.assignedCustomer.IsEating)
        {
            ToastUI.Show("Wait for the customer to finish eating.");
            return;
        }

        if (placedTray != null)
        {
            placedTray.NotifyFoodRemoved(this);
            placedTray = null;
        }

        NotifyCustomerTrayRemoved();
        ReleaseSpawnSlot();

        transform.SetParent(null, true);

        if (!PlayerInventory.instance.TryHoldPhysicalItem(gameObject))
            return;

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

        NotifyCustomerTrayRemoved();
        ReleaseSpawnSlot();
        ReleaseFromPlayer();
        FoodActionUI.Hide();

        Destroy(gameObject);
    }

    /// <summary>
    /// Called by a customer when their eating timer completes. Removes the
    /// dish from its tray and destroys the plate.
    /// </summary>
    public void ConsumeByCustomer()
    {
        NotifyCustomerTrayRemoved();
        ReleaseSpawnSlot();
        ReleaseFromPlayer();
        FoodActionUI.Hide();

        Destroy(gameObject);
    }

    public void Drop()
    {
        // Dropping a plate close to a customer tray snaps it into place, just
        // like the pot snaps onto the oven.
        if (isHeld)
        {
            Vector3 checkPosition = PlayerInventory.instance != null &&
                                    PlayerInventory.instance.dropSpawnPoint != null
                ? PlayerInventory.instance.dropSpawnPoint.position
                : transform.position;

            CustomerTray nearestTray = CustomerTray.FindNearest(checkPosition, 2.2f, true);
            if (nearestTray != null && nearestTray.CanAcceptDish(this))
            {
                PlaceOnCustomerTray(nearestTray);
                return;
            }
        }

        Vector3 dropPosition = transform.position;
        Quaternion dropRotation = transform.rotation;

        ReleaseFromPlayer();

        if (PlayerInventory.instance != null && PlayerInventory.instance.dropSpawnPoint != null)
        {
            dropPosition = PlayerInventory.instance.dropSpawnPoint.position;
        }

        NotifyCustomerTrayRemoved();
        ReleaseSpawnSlot();

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
        NotifyCustomerTrayRemoved();
        ReleaseSpawnSlot();

        placedTray = tray;
        isHeld = false;

        tray.PlaceFood(this);
        FoodActionUI.Hide();
    }

    /// <summary>
    /// Snaps the plate onto one of the flat Customer-tray objects.
    /// </summary>
    public void PlaceOnCustomerTray(CustomerTray tray)
    {
        if (tray == null)
        {
            Drop();
            return;
        }

        ReleaseFromPlayer();
        ReleaseSpawnSlot();

        if (placedTray != null)
        {
            placedTray.NotifyFoodRemoved(this);
            placedTray = null;
        }

        tray.PlaceFood(this);
        FoodActionUI.Hide();
    }

    /// <summary>
    /// Called by <see cref="CustomerTray.PlaceFood"/> once the tray has already
    /// repositioned and parented this plate. Does not ask the tray to place the
    /// dish again, which would recurse.
    /// </summary>
    public void SnapToCustomerTray(CustomerTray tray)
    {
        placedCustomerTray = tray;
        isHeld = false;
        SnapToTray();

        if (tray != null)
        {
            Bounds trayBounds = tray.GetWorldBounds();
            AlignRenderedPlateTo(new Vector3(trayBounds.center.x,
                trayBounds.max.y + tray.snapOffset, trayBounds.center.z));
        }
    }

    /// <summary>
    /// Called by PlayerInventory when the phone's drop button was used.
    /// </summary>
    public void OnDroppedByInventory()
    {
        placedTray = null;
        ReleaseSpawnSlot();
        isHeld = false;
        RestoreHeldPose();
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

        // Keep the visible plate centred on the tray anchor even when the dish
        // prefab's renderer is offset from its root.
        if (transform.parent != null)
            AlignRenderedPlateTo(transform.parent.position);

        gameObject.layer = originalLayer;
        isHeld = false;
    }

    /// <summary>
    /// Moves the dish so the rendered plate (not the prefab root) is centred on
    /// a world anchor and its lowest point rests at the anchor's height.
    /// </summary>
    public void AlignRenderedPlateTo(Vector3 worldAnchor)
    {
        if (!TryGetRendererBounds(out Bounds bounds)) return;

        Vector3 position = transform.position;
        position.x += worldAnchor.x - bounds.center.x;
        position.z += worldAnchor.z - bounds.center.z;
        position.y += worldAnchor.y - bounds.min.y;
        transform.position = position;
    }

    /// <summary>
    /// Drops a freshly spawned dish onto one of the oven's finished-dish
    /// points. The dish is aligned to the plate/marker surface and frozen so
    /// it never gets bounced off the table by its own collider.
    /// </summary>
    public void PlaceAtSpawnPoint(Transform spawnPoint)
    {
        EnsurePhysics();

        if (spawnPoint == null) return;

        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.constraints = RigidbodyConstraints.None;
#if UNITY_6000_0_OR_NEWER
            // Unity logs a warning if velocity is assigned while kinematic.
            if (!rb.isKinematic)
            {
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
            }
#else
            if (!rb.isKinematic)
            {
                rb.velocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
            }
#endif
            rb.isKinematic = true;
            rb.useGravity = false;
        }

        transform.SetParent(spawnPoint, true);
        transform.rotation = Quaternion.Euler(0f, spawnPoint.eulerAngles.y, 0f);

        float surfaceY = ResolveSpawnSurface(spawnPoint);

        if (!TryGetRendererBounds(out _))
        {
            transform.position = new Vector3(spawnPoint.position.x, surfaceY, spawnPoint.position.z);
        }
        else
        {
            // Some dish prefabs keep their visual mesh a couple of metres away
            // from the prefab root. Align the rendered plate itself with the
            // marker, not the root, or the food hangs off the edge of the table.
            AlignRenderedPlateTo(new Vector3(spawnPoint.position.x, surfaceY + 0.01f,
                spawnPoint.position.z));
        }

        isHeld = false;
        gameObject.layer = originalLayer;

        Collider[] colliders = GetComponentsInChildren<Collider>(true);
        foreach (Collider col in colliders)
            col.enabled = true;
    }

    private static float ResolveSpawnSurface(Transform spawnPoint)
    {
        // The oven spawn points have a thin Cylinder marker that shows where
        // the plate should rest. Use its top surface when it exists.
        Transform marker = FindDeepChild(spawnPoint, "Cylinder");
        if (marker != null)
        {
            Renderer markerRenderer = marker.GetComponent<Renderer>();
            if (markerRenderer != null) return markerRenderer.bounds.max.y;
            if (markerRenderer == null)
            {
                markerRenderer = marker.GetComponentInChildren<Renderer>(true);
                if (markerRenderer != null) return markerRenderer.bounds.max.y;
            }
        }

        // Fall back to the highest surface under the spawn point.
        Vector3 origin = spawnPoint.position + Vector3.up * 2f;
        if (Physics.Raycast(origin, Vector3.down, out RaycastHit hit, 8f, ~0,
                QueryTriggerInteraction.Ignore))
        {
            return hit.point.y;
        }

        return spawnPoint.position.y;
    }

    private static Transform FindDeepChild(Transform parent, string childName)
    {
        if (parent == null) return null;
        if (parent.name == childName) return parent;

        for (int i = 0; i < parent.childCount; i++)
        {
            Transform found = FindDeepChild(parent.GetChild(i), childName);
            if (found != null) return found;
        }

        return null;
    }

    // ------------------------------------------------------------------
    // HELD POSE
    // ------------------------------------------------------------------

    /// <summary>
    /// Parents the plate into the player's hand with a readable size and a
    /// position in front of the camera. The original scale is restored before
    /// the dish is put back into the world.
    /// </summary>
    public void ApplyHeldPose()
    {
        CaptureOriginalScale();

        transform.localPosition = Vector3.zero;
        transform.localRotation = Quaternion.Euler(heldLocalEuler);
        transform.localScale = originalLocalScale;

        if (TryGetRendererBounds(out Bounds bounds))
        {
            float largest = Mathf.Max(bounds.size.x, bounds.size.y, bounds.size.z);
            if (heldMaxSize > 0.01f && largest > 0.0001f)
            {
                float factor = Mathf.Clamp(heldMaxSize / largest, 0.02f, 20f);
                transform.localScale = originalLocalScale * factor;
            }

            if (transform.parent != null && TryGetRendererBounds(out bounds))
            {
                Vector3 target = transform.parent.TransformPoint(heldLocalPosition);
                transform.position += target - bounds.center;
                return;
            }
        }

        transform.localPosition = heldLocalPosition;
    }

    public void RestoreHeldPose()
    {
        if (scaleCaptured) transform.localScale = originalLocalScale;
    }

    private void CaptureOriginalScale()
    {
        if (scaleCaptured) return;
        originalLocalScale = transform.localScale;
        scaleCaptured = true;
    }

    private bool TryGetRendererBounds(out Bounds bounds)
    {
        Renderer[] renderers = GetComponentsInChildren<Renderer>(true);
        bounds = new Bounds(transform.position, Vector3.zero);
        bool found = false;

        foreach (Renderer renderer in renderers)
        {
            if (renderer == null || !renderer.enabled || !renderer.gameObject.activeInHierarchy)
                continue;

            if (!found)
            {
                bounds = renderer.bounds;
                found = true;
            }
            else
            {
                bounds.Encapsulate(renderer.bounds);
            }
        }

        return found;
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
            rb.constraints = RigidbodyConstraints.None;
#if UNITY_6000_0_OR_NEWER
            if (held && !rb.isKinematic)
            {
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
            }
#else
            if (held && !rb.isKinematic)
            {
                rb.velocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
            }
#endif
            rb.isKinematic = held;
            rb.useGravity = !held;
        }

        gameObject.layer = held ? 2 : originalLayer;
    }

    private void ReleaseFromPlayer()
    {
        RestoreHeldPose();

        if (PlayerInventory.instance != null &&
            PlayerInventory.instance.currentlyHeldModel == gameObject)
        {
            PlayerInventory.instance.ClearHand();
        }
    }

    /// <summary>
    /// Frees the oven spawn point this dish occupied so cooking can continue.
    /// </summary>
    private void ReleaseSpawnSlot()
    {
        if (assignedSpawnPoint == null) return;

        if (DishSpawnManager.instance != null)
            DishSpawnManager.instance.ReleaseSlot(assignedSpawnPoint);

        assignedSpawnPoint = null;
    }

    private void NotifyCustomerTrayRemoved()
    {
        if (placedCustomerTray == null) return;

        placedCustomerTray.NotifyFoodRemoved(this);
        placedCustomerTray = null;
    }
}
