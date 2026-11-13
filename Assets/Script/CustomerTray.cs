using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Turns one of the flat Customer-tray objects into a snap target for finished
/// plates, mirroring how the pot snaps onto the oven.
/// </summary>
public class CustomerTray : MonoBehaviour, IInteractable
{
    public static readonly List<CustomerTray> All = new List<CustomerTray>();

    [Header("Snap")]
    public FoodDish currentFood;
    public float snapOffset = 0.03f;

    void Awake()
    {
        if (!All.Contains(this)) All.Add(this);
    }

    void OnDestroy()
    {
        All.Remove(this);
    }

    public static void AttachToSceneTrays()
    {
        GameObject[] roots = UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects();

        foreach (GameObject root in roots)
        {
            Transform[] children = root.GetComponentsInChildren<Transform>(true);
            foreach (Transform t in children)
            {
                if (t == null) continue;
                if (!t.name.StartsWith("Customer-tray")) continue;

                if (t.GetComponent<CustomerTray>() == null)
                    t.gameObject.AddComponent<CustomerTray>();

                if (t.GetComponent<Collider>() == null)
                    t.gameObject.AddComponent<BoxCollider>();
            }
        }
    }

    public static CustomerTray FindNearest(Vector3 position, float maxDistance = 3f,
        bool requireEmpty = false)
    {
        CustomerTray best = null;
        float bestDistance = maxDistance;

        foreach (CustomerTray tray in All)
        {
            if (tray == null) continue;
            if (requireEmpty && tray.currentFood != null) continue;

            float distance = Vector3.Distance(position, tray.transform.position);
            if (distance <= bestDistance)
            {
                best = tray;
                bestDistance = distance;
            }
        }

        return best;
    }

    /// <summary>
    /// Snaps a dish to the top of this tray. Uses SetParent(worldPositionStays)
    /// so the tray's thin scale does not squash the plate.
    /// </summary>
    public void PlaceFood(FoodDish dish)
    {
        if (dish == null) return;

        if (currentFood != null && currentFood != dish)
        {
            FoodDish old = currentFood;
            currentFood = null;
            old.Drop();
        }

        currentFood = dish;

        if (PlayerInventory.instance != null)
            PlayerInventory.instance.ClearHandIfHolding(dish.gameObject);

        Bounds bounds = GetWorldBounds();
        Vector3 target = new Vector3(bounds.center.x, bounds.max.y + snapOffset, bounds.center.z);
        Quaternion rotation = Quaternion.Euler(0f, transform.eulerAngles.y, 0f);

        dish.transform.SetParent(transform, true);
        dish.transform.SetPositionAndRotation(target, rotation);

        dish.SnapToCustomerTray(this);
    }

    public void NotifyFoodRemoved(FoodDish dish)
    {
        if (currentFood == dish) currentFood = null;
    }

    public void Interact()
    {
        if (currentFood != null)
            currentFood.PickUp();
    }

    private Bounds GetWorldBounds()
    {
        Renderer[] renderers = GetComponentsInChildren<Renderer>(true);
        bool found = false;
        Bounds bounds = new Bounds(transform.position, Vector3.one * 0.5f);

        foreach (Renderer renderer in renderers)
        {
            if (renderer == null || !renderer.enabled) continue;

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

        return bounds;
    }
}
