using UnityEngine;

/// <summary>
/// A tray that sits on top of the prep table and snaps finished dishes in place.
/// It is created at runtime so no scene editing is required.
/// </summary>
public class FoodTray : MonoBehaviour, IInteractable
{
    public static FoodTray instance;

    [Header("Snap")]
    public Transform foodAnchor;
    public FoodDish currentFood;

    [Header("Table")]
    public string tableName = "rpgpp_lt_table_01 (4)";

    void Awake()
    {
        if (instance == null) instance = this;

        if (foodAnchor == null)
        {
            Transform found = transform.Find("FoodAnchor");
            if (found != null) foodAnchor = found;
        }
    }

    // ------------------------------------------------------------------
    // CREATION
    // ------------------------------------------------------------------

    public static FoodTray GetOrCreate()
    {
        if (instance != null) return instance;

        FoodTray existing = Object.FindFirstObjectByType<FoodTray>(FindObjectsInactive.Include);
        if (existing != null)
        {
            instance = existing;
            return existing;
        }

        return BuildOnTable();
    }

    private static FoodTray BuildOnTable()
    {
        GameObject table = GameObject.Find("rpgpp_lt_table_01 (4)");
        if (table == null)
            table = FindTableFallback();

        if (table == null)
        {
            Debug.LogWarning("FoodTray: could not find the prep table 'rpgpp_lt_table_01 (4)'.");
            return null;
        }

        Bounds bounds;
        if (!TryGetRendererBounds(table, out bounds))
        {
            bounds = new Bounds(table.transform.position + Vector3.up, new Vector3(1.5f, 0.1f, 1f));
        }

        float width = Mathf.Clamp(bounds.size.x * 0.55f, 0.45f, 2.4f);
        float depth = Mathf.Clamp(bounds.size.z * 0.55f, 0.35f, 1.8f);
        float thickness = Mathf.Clamp(bounds.size.y * 0.05f, 0.04f, 0.12f);

        GameObject tray = new GameObject("FoodTray");
        tray.transform.position = new Vector3(bounds.center.x, bounds.max.y + thickness * 0.5f, bounds.center.z);
        tray.transform.rotation = Quaternion.Euler(0f, table.transform.eulerAngles.y, 0f);

        GameObject surface = GameObject.CreatePrimitive(PrimitiveType.Cube);
        surface.name = "TraySurface";
        surface.transform.SetParent(tray.transform, false);
        surface.transform.localPosition = Vector3.zero;
        surface.transform.localRotation = Quaternion.identity;
        surface.transform.localScale = new Vector3(width, thickness, depth);

        Renderer renderer = surface.GetComponent<Renderer>();
        if (renderer != null)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) shader = Shader.Find("Standard");

            if (shader != null)
            {
                Material material = new Material(shader);
                material.color = new Color(0.93f, 0.93f, 0.89f, 1f);
                if (material.HasProperty("_BaseColor"))
                    material.SetColor("_BaseColor", new Color(0.93f, 0.93f, 0.89f, 1f));
                renderer.material = material;
            }
        }

        // Remove the primitive's default collider? No - keep it so the tray can
        // be tapped to pick a placed dish back up.

        GameObject anchor = new GameObject("FoodAnchor");
        anchor.transform.SetParent(tray.transform, false);
        anchor.transform.localPosition = new Vector3(0f, thickness * 0.5f + 0.03f, 0f);

        FoodTray trayComponent = tray.AddComponent<FoodTray>();
        trayComponent.foodAnchor = anchor.transform;
        instance = trayComponent;

        return trayComponent;
    }

    private static GameObject FindTableFallback()
    {
        Transform[] all = Object.FindObjectsByType<Transform>(FindObjectsSortMode.None);
        GameObject best = null;

        foreach (Transform t in all)
        {
            if (t == null) continue;
            if (t.name == "rpgpp_lt_table_01 (4)") return t.gameObject;
            if (best == null && t.name.StartsWith("rpgpp_lt_table_01")) best = t.gameObject;
        }

        return best;
    }

    private static bool TryGetRendererBounds(GameObject root, out Bounds bounds)
    {
        Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);
        bool found = false;
        bounds = new Bounds(root.transform.position, Vector3.one);

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

        return found;
    }

    // ------------------------------------------------------------------
    // PLACEMENT
    // ------------------------------------------------------------------

    public void PlaceFood(FoodDish dish)
    {
        if (dish == null) return;

        if (currentFood != null && currentFood != dish)
            currentFood.Drop();

        currentFood = dish;

        Transform anchor = foodAnchor != null ? foodAnchor : transform;
        dish.transform.SetParent(anchor, false);
        dish.transform.localPosition = Vector3.zero;
        dish.transform.localRotation = Quaternion.identity;
        dish.SnapToTray();
    }

    public void NotifyFoodRemoved(FoodDish dish)
    {
        if (currentFood == dish) currentFood = null;
    }

    public void Interact()
    {
        if (currentFood != null) currentFood.PickUp();
    }
}
