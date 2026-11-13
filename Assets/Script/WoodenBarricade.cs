using UnityEngine;

/// <summary>
/// Boarded-up window. Takes ten hits before it breaks. Uses the Wooden-Bars
/// prefab when it can be found under Resources, otherwise builds simple planks.
/// </summary>
public class WoodenBarricade : MonoBehaviour, IInteractable
{
    public int maxHits = 10;
    public int hitsRemaining = 10;

    public WindowRepairPoint owner;

    public static WoodenBarricade Spawn(Vector3 position, Quaternion rotation,
        WindowRepairPoint ownerPoint)
    {
        GameObject barricade = null;

        GameObject prefab = Resources.Load<GameObject>("Wooden-Bars");
        if (prefab != null)
        {
            barricade = Instantiate(prefab, position, rotation);
        }
        else
        {
            barricade = BuildProcedural(position, rotation);
        }

        barricade.name = "Wooden-Bars";
        barricade.transform.SetPositionAndRotation(position, rotation);

        if (barricade.GetComponentInChildren<Collider>(true) == null)
        {
            BoxCollider box = barricade.AddComponent<BoxCollider>();
            Bounds bounds = CalculateBounds(barricade);
            box.center = barricade.transform.InverseTransformPoint(bounds.center);
            Vector3 scale = barricade.transform.lossyScale;
            box.size = new Vector3(
                Mathf.Abs(scale.x) > 0.0001f ? bounds.size.x / Mathf.Abs(scale.x) : bounds.size.x,
                Mathf.Abs(scale.y) > 0.0001f ? bounds.size.y / Mathf.Abs(scale.y) : bounds.size.y,
                Mathf.Abs(scale.z) > 0.0001f ? bounds.size.z / Mathf.Abs(scale.z) : bounds.size.z);
        }

        WoodenBarricade result = barricade.GetComponent<WoodenBarricade>();
        if (result == null) result = barricade.AddComponent<WoodenBarricade>();

        result.owner = ownerPoint;
        result.hitsRemaining = result.maxHits;

        ToastUI.Show("Wooden bars placed. Hit them 10 times to break them.");
        return result;
    }

    public void Interact()
    {
        Hit(1);
    }

    public void Hit(int amount)
    {
        if (amount <= 0) return;

        hitsRemaining -= amount;

        if (hitsRemaining > 0)
        {
            ToastUI.Show("Wooden bars: " + hitsRemaining + " hit" + (hitsRemaining == 1 ? "" : "s") + " left.");
            return;
        }

        if (owner != null) owner.NotifyBarricadeDestroyed(this);
        ToastUI.Show("Wooden bars destroyed.");
        Destroy(gameObject);
    }

    private static GameObject BuildProcedural(Vector3 position, Quaternion rotation)
    {
        GameObject root = new GameObject("Wooden-Bars");
        root.transform.SetPositionAndRotation(position, rotation);

        Material wood = CreateWoodMaterial();

        for (int i = 0; i < 6; i++)
        {
            GameObject plank = GameObject.CreatePrimitive(PrimitiveType.Cube);
            plank.name = "Plank " + (i + 1);
            plank.transform.SetParent(root.transform, false);
            plank.transform.localPosition = new Vector3(-0.95f + i * 0.38f, 0f, 0f);
            plank.transform.localRotation = Quaternion.Euler(0f, 0f, (i % 2 == 0) ? 4f : -4f);
            plank.transform.localScale = new Vector3(0.30f, 2.05f, 0.12f);
            ApplyWood(plank, wood);
        }

        GameObject brace = GameObject.CreatePrimitive(PrimitiveType.Cube);
        brace.name = "Brace";
        brace.transform.SetParent(root.transform, false);
        brace.transform.localPosition = new Vector3(0f, 0.1f, -0.06f);
        brace.transform.localRotation = Quaternion.Euler(0f, 0f, 8f);
        brace.transform.localScale = new Vector3(2.15f, 0.22f, 0.10f);
        ApplyWood(brace, wood);

        return root;
    }

    private static Material CreateWoodMaterial()
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null) shader = Shader.Find("Standard");

        Material material = shader != null ? new Material(shader) : null;
        if (material != null)
        {
            Color color = new Color(0.36f, 0.23f, 0.12f, 1f);
            if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", color);
            material.color = color;
        }

        return material;
    }

    private static void ApplyWood(GameObject target, Material material)
    {
        if (material == null) return;
        Renderer renderer = target.GetComponent<Renderer>();
        if (renderer != null) renderer.material = material;
    }

    private static Bounds CalculateBounds(GameObject root)
    {
        Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);
        bool found = false;
        Bounds bounds = new Bounds(root.transform.position, new Vector3(2f, 2f, 0.2f));

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
