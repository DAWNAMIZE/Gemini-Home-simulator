using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Runtime wiring for the survival house. Attaches the cooking, weapon, save,
/// sleep and repair components without editing the scene or prefab YAML.
/// Runs on every scene load and skips scenes without a player.
/// </summary>
public class GameBootstrap : MonoBehaviour
{
    private static GameBootstrap runner;

    private static readonly string[] KnownWeaponNames =
    {
        "Pistol_I", "Pistol_H", "Pistol_G", "Pistol_F", "Pistol_J",
        "Ax_01", "Dagger_01", "Sword_01", "Hammer_01"
    };

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Initialize()
    {
        if (runner != null) return;

        GameObject go = new GameObject("GameBootstrap");
        DontDestroyOnLoad(go);
        runner = go.AddComponent<GameBootstrap>();

        SceneManager.sceneLoaded -= HandleSceneLoaded;
        SceneManager.sceneLoaded += HandleSceneLoaded;

        runner.StartCoroutine(runner.SetupScene());
    }

    private static void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (runner == null) return;
        runner.StartCoroutine(runner.SetupScene());
    }

    private IEnumerator SetupScene()
    {
        // Let every scene object finish Awake/Start before we wire into them.
        yield return null;

        PlayerInventory inventory = PlayerInventory.instance;
        if (inventory == null) inventory = Object.FindFirstObjectByType<PlayerInventory>();
        if (inventory == null) yield break; // main menu / empty scene

        RuntimeUI.EnsureEventSystem();

        CustomerTray.AttachToSceneTrays();
        DishSpawnManager spawnManager = DishSpawnManager.GetOrCreate();
        if (spawnManager != null) spawnManager.DiscoverSpawnPoints();

        ConfigureWeaponPickups();
        EnsureAmmoPickups(inventory);
        EnsureBedSavePoint(inventory);
        EnsureOvenFuel();
        EnsureWoodPickups(inventory);
        EnsureWindowRepairPoint(inventory);

        WeaponHUD.GetOrCreate();
        SlotActionUI.GetOrCreate();

        if (SaveGameManager.pendingContinue)
        {
            SaveGameManager.pendingContinue = false;
            SaveGameManager.LoadAndApply();
        }

        if (BackpackUIManager.instance != null) BackpackUIManager.instance.RefreshUI();
    }

    // ------------------------------------------------------------------
    // WEAPONS
    // ------------------------------------------------------------------

    private static void ConfigureWeaponPickups()
    {
        HashSet<Transform> roots = new HashSet<Transform>();

        foreach (string weaponName in KnownWeaponNames)
        {
            GameObject found = GameObject.Find(weaponName);
            if (found != null) roots.Add(found.transform);
        }

        // Fallback: anything that looks like a weapon root in the scene.
        Transform[] all = Object.FindObjectsByType<Transform>(
            FindObjectsInactive.Include, FindObjectsSortMode.None);

        foreach (Transform t in all)
        {
            if (t == null || !t.gameObject.scene.IsValid()) continue;
            if (!LooksLikeWeaponName(t.name)) continue;
            if (HasWeaponAncestor(t)) continue;
            roots.Add(t);
        }

        foreach (Transform weapon in roots)
        {
            if (weapon == null) continue;

            // Weapon pickups can be dropped later, so they must not be static.
            weapon.gameObject.isStatic = false;

            foreach (ItemPickup pickup in weapon.GetComponentsInChildren<ItemPickup>(true))
            {
                if (pickup != null) Destroy(pickup);
            }

            if (weapon.GetComponent<WeaponPickup>() == null)
                weapon.gameObject.AddComponent<WeaponPickup>();

            WeaponInstance.Attach(weapon.gameObject);
        }
    }

    private static bool LooksLikeWeaponName(string name)
    {
        if (string.IsNullOrEmpty(name)) return false;

        string lower = name.ToLowerInvariant();
        return lower.StartsWith("pistol") ||
               lower.StartsWith("ax_") ||
               lower.StartsWith("dagger") ||
               lower.StartsWith("sword") ||
               lower.StartsWith("hammer");
    }

    private static bool HasWeaponAncestor(Transform transform)
    {
        Transform parent = transform.parent;
        while (parent != null)
        {
            if (LooksLikeWeaponName(parent.name)) return true;
            parent = parent.parent;
        }

        return false;
    }

    // ------------------------------------------------------------------
    // AMMO
    // ------------------------------------------------------------------

    private static void EnsureAmmoPickups(PlayerInventory inventory)
    {
        AmmoPickup[] existing = Object.FindObjectsByType<AmmoPickup>(
            FindObjectsInactive.Include, FindObjectsSortMode.None);
        if (existing != null && existing.Length > 0) return;

        Vector3 forward = inventory.transform.forward;
        forward.y = 0f;
        if (forward.sqrMagnitude < 0.001f) forward = Vector3.forward;
        forward.Normalize();

        Vector3 right = Vector3.Cross(Vector3.up, forward).normalized;
        Vector3[] offsets =
        {
            right * 1.6f + forward * 1.4f,
            -right * 1.8f + forward * 1.1f,
            forward * 3.2f + right * 0.6f
        };

        for (int i = 0; i < offsets.Length; i++)
            CreateAmmoBox("AmmoBox_" + (i + 1), inventory.transform.position + offsets[i]);
    }

    private static void CreateAmmoBox(string name, Vector3 hint)
    {
        Vector3 position = hint + Vector3.up * 0.45f;

        if (Physics.Raycast(hint + Vector3.up * 1.5f, Vector3.down, out RaycastHit hit, 6f))
            position = hit.point + Vector3.up * 0.45f;

        GameObject box = GameObject.CreatePrimitive(PrimitiveType.Cube);
        box.name = name;
        box.transform.position = position;
        box.transform.localScale = new Vector3(0.34f, 0.22f, 0.24f);

        Renderer renderer = box.GetComponent<Renderer>();
        if (renderer != null)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) shader = Shader.Find("Standard");

            if (shader != null)
            {
                Material material = new Material(shader);
                Color color = new Color(0.85f, 0.68f, 0.16f, 1f);
                if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", color);
                material.color = color;
                renderer.material = material;
            }
        }

        box.AddComponent<AmmoPickup>().amount = 5;
        box.AddComponent<PickupRotator>();
    }

    // ------------------------------------------------------------------
    // OVEN FUEL / WOOD
    // ------------------------------------------------------------------

    private static void EnsureOvenFuel()
    {
        if (OvenFuel.instance != null) return;

        Transform oven = FindSceneTransform("Oven");
        if (oven == null) return;

        if (oven.GetComponent<OvenFuel>() == null)
            oven.gameObject.AddComponent<OvenFuel>();
    }

    private static void EnsureWoodPickups(PlayerInventory inventory)
    {
        Transform kitchen = FindSceneTransform("Kitcen_Item");
        if (kitchen == null) kitchen = FindSceneTransform("Kitchen_Item");

        bool foundWood = false;

        if (kitchen != null)
        {
            foreach (Transform child in kitchen.GetComponentsInChildren<Transform>(true))
            {
                if (child == null) continue;
                if (child.name.IndexOf("wood", System.StringComparison.OrdinalIgnoreCase) < 0)
                    continue;

                foundWood = true;
                if (child.GetComponent<WoodPickup>() == null)
                    child.gameObject.AddComponent<WoodPickup>();
            }
        }

        if (foundWood) return;

        // Fallback for scenes without the kitchen wood props: spawn a small
        // pile of logs near the player so the fuel loop still works.
        Vector3 forward = HorizontalForward(inventory.transform);
        Vector3 right = Vector3.Cross(Vector3.up, forward).normalized;

        for (int i = 0; i < 3; i++)
        {
            Vector3 hint = inventory.transform.position + right * (1.2f + i * 0.35f) - forward * 1.4f;
            if (Physics.Raycast(hint + Vector3.up * 2f, Vector3.down, out RaycastHit hit, 7f))
                hint = hit.point;

            GameObject log = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            log.name = "Wood_" + (i + 1);
            log.transform.position = hint + Vector3.up * 0.12f;
            log.transform.rotation = Quaternion.Euler(0f, i * 30f, 90f);
            log.transform.localScale = new Vector3(0.16f, 0.5f, 0.16f);
            log.isStatic = false;
            log.AddComponent<WoodPickup>();
        }
    }

    // ------------------------------------------------------------------
    // BED / SAVE POINT
    // ------------------------------------------------------------------

    private static void EnsureBedSavePoint(PlayerInventory inventory)
    {
        Transform existing = FindSceneTransform("Bed_Save");
        if (existing != null)
        {
            if (existing.GetComponent<BedSavePoint>() == null)
                existing.gameObject.AddComponent<BedSavePoint>();
            return;
        }

        Vector3 forward = HorizontalForward(inventory.transform);
        Vector3 right = Vector3.Cross(Vector3.up, forward).normalized;
        Vector3 hint = inventory.transform.position + right * 2.4f - forward * 0.4f;

        if (Physics.Raycast(hint + Vector3.up * 2f, Vector3.down, out RaycastHit hit, 7f))
            hint = hit.point;

        GameObject bed = new GameObject("Bed_Save");
        bed.transform.position = hint;
        bed.transform.rotation = Quaternion.LookRotation(forward, Vector3.up);

        CreatePart(bed.transform, "Frame", new Vector3(0f, 0.20f, 0f),
            new Vector3(1.95f, 0.30f, 1.05f), new Color(0.32f, 0.20f, 0.11f));
        CreatePart(bed.transform, "Mattress", new Vector3(0f, 0.42f, 0f),
            new Vector3(1.80f, 0.18f, 0.95f), new Color(0.82f, 0.80f, 0.72f));
        CreatePart(bed.transform, "Pillow", new Vector3(0f, 0.55f, 0.32f),
            new Vector3(0.62f, 0.14f, 0.30f), new Color(0.94f, 0.94f, 0.90f));

        BedSavePoint point = bed.AddComponent<BedSavePoint>();
        point.bedName = "Bed";
    }

    // ------------------------------------------------------------------
    // WINDOW / REPAIR POINT
    // ------------------------------------------------------------------

    private static void EnsureWindowRepairPoint(PlayerInventory inventory)
    {
        Transform existing = FindSceneTransform("Window-1");
        if (existing == null) existing = FindSceneTransformStartingWith("Window-1");

        if (existing != null)
        {
            WindowRepairPoint existingRepair = existing.GetComponent<WindowRepairPoint>();
            if (existingRepair == null)
                existingRepair = existing.gameObject.AddComponent<WindowRepairPoint>();

            if (existingRepair.barricadeAnchor == null)
            {
                Transform existingAnchor = existing.Find("BarricadeAnchor");
                if (existingAnchor == null)
                {
                    GameObject created = new GameObject("BarricadeAnchor");
                    created.transform.SetParent(existing, false);
                    created.transform.localPosition = new Vector3(0f, 1.30f, -0.10f);
                    existingAnchor = created.transform;
                }

                existingRepair.barricadeAnchor = existingAnchor;
            }

            return;
        }

        Vector3 forward = HorizontalForward(inventory.transform);
        Vector3 hint = inventory.transform.position + forward * 3.6f;

        float groundY = inventory.transform.position.y;
        if (Physics.Raycast(hint + Vector3.up * 2f, Vector3.down, out RaycastHit hit, 7f))
            groundY = hit.point.y;

        GameObject window = new GameObject("Window-1");
        window.transform.position = new Vector3(hint.x, groundY, hint.z);
        window.transform.rotation = Quaternion.LookRotation(-forward, Vector3.up);

        Color frameColor = new Color(0.45f, 0.30f, 0.17f);
        CreatePart(window.transform, "Bottom", new Vector3(0f, 0.62f, 0f),
            new Vector3(1.50f, 0.12f, 0.14f), frameColor);
        CreatePart(window.transform, "Top", new Vector3(0f, 1.98f, 0f),
            new Vector3(1.50f, 0.12f, 0.14f), frameColor);
        CreatePart(window.transform, "Left", new Vector3(-0.69f, 1.30f, 0f),
            new Vector3(0.12f, 1.42f, 0.14f), frameColor);
        CreatePart(window.transform, "Right", new Vector3(0.69f, 1.30f, 0f),
            new Vector3(0.12f, 1.42f, 0.14f), frameColor);
        CreatePart(window.transform, "Glass", new Vector3(0f, 1.30f, 0f),
            new Vector3(1.26f, 1.20f, 0.04f), new Color(0.55f, 0.75f, 0.88f, 0.55f));

        GameObject anchor = new GameObject("BarricadeAnchor");
        anchor.transform.SetParent(window.transform, false);
        anchor.transform.localPosition = new Vector3(0f, 1.30f, -0.10f);

        WindowRepairPoint repair = window.AddComponent<WindowRepairPoint>();
        repair.windowName = "Window-1";
        repair.barricadeAnchor = anchor.transform;
    }

    // ------------------------------------------------------------------
    // HELPERS
    // ------------------------------------------------------------------

    private static Transform FindSceneTransform(string name)
    {
        Transform[] all = Object.FindObjectsByType<Transform>(
            FindObjectsInactive.Include, FindObjectsSortMode.None);

        foreach (Transform t in all)
        {
            if (t == null || !t.gameObject.scene.IsValid()) continue;
            if (t.name == name) return t;
        }

        return null;
    }

    private static Transform FindSceneTransformStartingWith(string prefix)
    {
        Transform[] all = Object.FindObjectsByType<Transform>(
            FindObjectsInactive.Include, FindObjectsSortMode.None);

        foreach (Transform t in all)
        {
            if (t == null || !t.gameObject.scene.IsValid()) continue;
            if (t.name.StartsWith(prefix)) return t;
        }

        return null;
    }

    private static Vector3 HorizontalForward(Transform player)
    {
        Vector3 forward = player.forward;
        forward.y = 0f;
        if (forward.sqrMagnitude < 0.001f) forward = Vector3.forward;
        return forward.normalized;
    }

    private static GameObject CreatePart(Transform parent, string name, Vector3 localPosition,
        Vector3 localScale, Color color)
    {
        GameObject part = GameObject.CreatePrimitive(PrimitiveType.Cube);
        part.name = name;
        part.transform.SetParent(parent, false);
        part.transform.localPosition = localPosition;
        part.transform.localRotation = Quaternion.identity;
        part.transform.localScale = localScale;

        Renderer renderer = part.GetComponent<Renderer>();
        if (renderer == null) return part;

        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null) shader = Shader.Find("Standard");
        if (shader == null) return part;

        Material material = new Material(shader);
        if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", color);
        material.color = color;
        if (color.a < 0.99f && material.HasProperty("_Surface"))
            material.SetFloat("_Surface", 1f);
        renderer.material = material;

        return part;
    }
}
