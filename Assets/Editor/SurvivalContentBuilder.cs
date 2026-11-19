#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// Builds the market shopkeeper prefab, the water well, the bathroom shower and
/// a market produce table containing one prefab instance of every ingredient.
/// Run from Tools > Gemini Home > Survival > Build World Content.
/// </summary>
public static class SurvivalContentBuilder
{
    private const string PrefabFolder = "Assets/Prefabs/Survival";
    private const string MaterialFolder = PrefabFolder + "/Materials";
    private const string WellPrefabPath = PrefabFolder + "/Water_Well.prefab";
    private const string ShowerPrefabPath = PrefabFolder + "/Bathroom_Shower.prefab";
    private const string ShopkeeperPrefabPath = PrefabFolder + "/Market_Shopkeeper.prefab";

    [MenuItem("Tools/Gemini Home/Survival/Build World Content")]
    public static void BuildAll()
    {
        if (EditorApplication.isPlaying)
        {
            EditorUtility.DisplayDialog("Survival Content",
                "Exit play mode before building world content.", "OK");
            return;
        }

        EnsureFolder("Assets/Prefabs");
        EnsureFolder(PrefabFolder);
        EnsureFolder(MaterialFolder);

        Material stone = CreateMaterial("Well_Stone",
            new Color(0.43f, 0.41f, 0.37f), 0.15f, 0f);
        Material wood = CreateMaterial("Survival_Wood",
            new Color(0.36f, 0.23f, 0.12f), 0.18f, 0f);
        Material water = CreateMaterial("Well_Water",
            new Color(0.12f, 0.48f, 0.78f), 0.78f, 0.05f);
        Material metal = CreateMaterial("Shower_Metal",
            new Color(0.58f, 0.61f, 0.65f), 0.65f, 0.75f);
        Material porcelain = CreateMaterial("Shower_Porcelain",
            new Color(0.90f, 0.92f, 0.91f), 0.55f, 0f);
        Material tile = CreateMaterial("Shower_Tile",
            new Color(0.52f, 0.72f, 0.78f), 0.42f, 0f);
        Material cloth = CreateMaterial("Market_Cloth",
            new Color(0.20f, 0.45f, 0.28f), 0.10f, 0f);
        Material skin = CreateMaterial("Shopkeeper_Skin",
            new Color(0.42f, 0.26f, 0.16f), 0.22f, 0f);
        Material shirt = CreateMaterial("Shopkeeper_Shirt",
            new Color(0.78f, 0.56f, 0.18f), 0.16f, 0f);
        Material trousers = CreateMaterial("Shopkeeper_Trousers",
            new Color(0.17f, 0.20f, 0.30f), 0.12f, 0f);
        Material cap = CreateMaterial("Shopkeeper_Cap",
            new Color(0.28f, 0.16f, 0.09f), 0.10f, 0f);
        Material eyes = CreateMaterial("Shopkeeper_Eyes",
            new Color(0.05f, 0.04f, 0.03f), 0.65f, 0f);

        BuildWaterWell(stone, wood, water);
        BuildBathroomShower(porcelain, tile, metal);
        BuildShopkeeperPrefab(skin, shirt, trousers, cloth, cap, eyes);

        PlaceWaterWell();
        PlaceBathroomShower();
        BuildWorldIngredients(wood, cloth);
        EnsureFloorNavMeshLink();

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log("[SurvivalContentBuilder] Built shopkeeper, well, shower, " +
                  "world ingredients and the floor NavMesh link.");
    }

    // ------------------------------------------------------------------
    // PREFABS
    // ------------------------------------------------------------------

    private static void BuildWaterWell(Material stone, Material wood, Material water)
    {
        GameObject root = new GameObject("Water_Well");
        BoxCollider box = root.AddComponent<BoxCollider>();
        box.center = new Vector3(0f, 1.25f, 0f);
        box.size = new Vector3(2f, 2.5f, 2f);

        WaterWell well = root.AddComponent<WaterWell>();
        well.drinkAmount = 30f;
        well.drinkVolume = 0.85f;

        AudioSource audio = root.AddComponent<AudioSource>();
        audio.playOnAwake = false;
        audio.spatialBlend = 1f;
        well.drinkAudio = audio;

        AddPart(root.transform, "StoneBase", PrimitiveType.Cylinder,
            new Vector3(0f, 0.35f, 0f), new Vector3(1.8f, 0.35f, 1.8f), stone);
        AddPart(root.transform, "Water", PrimitiveType.Cylinder,
            new Vector3(0f, 0.72f, 0f), new Vector3(1.42f, 0.02f, 1.42f), water);

        for (int i = 0; i < 8; i++)
        {
            float angle = i * 45f;
            float radians = angle * Mathf.Deg2Rad;
            AddPart(root.transform, "Rim_" + i, PrimitiveType.Cube,
                new Vector3(Mathf.Cos(radians) * 0.84f, 0.78f, Mathf.Sin(radians) * 0.84f),
                new Vector3(0.36f, 0.16f, 0.20f), stone,
                new Vector3(0f, -angle, 0f));
        }

        AddPart(root.transform, "PostLeft", PrimitiveType.Cube,
            new Vector3(-0.78f, 1.55f, 0f), new Vector3(0.12f, 1.75f, 0.12f), wood);
        AddPart(root.transform, "PostRight", PrimitiveType.Cube,
            new Vector3(0.78f, 1.55f, 0f), new Vector3(0.12f, 1.75f, 0.12f), wood);
        AddPart(root.transform, "Crossbar", PrimitiveType.Cube,
            new Vector3(0f, 2.38f, 0f), new Vector3(1.68f, 0.12f, 0.12f), wood);
        AddPart(root.transform, "RoofLeft", PrimitiveType.Cube,
            new Vector3(-0.44f, 2.66f, 0f), new Vector3(1.12f, 0.08f, 1.55f), wood,
            new Vector3(0f, 0f, -32f));
        AddPart(root.transform, "RoofRight", PrimitiveType.Cube,
            new Vector3(0.44f, 2.66f, 0f), new Vector3(1.12f, 0.08f, 1.55f), wood,
            new Vector3(0f, 0f, 32f));

        AddPart(root.transform, "Rope", PrimitiveType.Cube,
            new Vector3(0.55f, 1.76f, 0f), new Vector3(0.035f, 1.1f, 0.035f), wood);
        AddPart(root.transform, "Bucket", PrimitiveType.Cylinder,
            new Vector3(0.55f, 1.03f, 0f), new Vector3(0.2f, 0.2f, 0.2f), wood);

        PrefabUtility.SaveAsPrefabAsset(root, WellPrefabPath);
        Object.DestroyImmediate(root);
    }

    private static void BuildBathroomShower(Material porcelain, Material tile, Material metal)
    {
        GameObject root = new GameObject("Bathroom_Shower");
        BoxCollider box = root.AddComponent<BoxCollider>();
        box.center = new Vector3(0f, 1.15f, 0f);
        box.size = new Vector3(1.6f, 2.3f, 1.6f);

        BathroomShower shower = root.AddComponent<BathroomShower>();
        shower.hygieneAmount = 30f;
        shower.showerVolume = 0.8f;

        AudioSource audio = root.AddComponent<AudioSource>();
        audio.playOnAwake = false;
        audio.spatialBlend = 1f;
        shower.showerAudio = audio;

        AddPart(root.transform, "Tray", PrimitiveType.Cube,
            new Vector3(0f, 0.06f, 0f), new Vector3(1.44f, 0.12f, 1.44f), porcelain);
        AddPart(root.transform, "Drain", PrimitiveType.Cylinder,
            new Vector3(0f, 0.125f, 0f), new Vector3(0.18f, 0.006f, 0.18f), metal);

        AddPart(root.transform, "BackWall", PrimitiveType.Cube,
            new Vector3(0f, 1.16f, 0.7f), new Vector3(1.46f, 2.2f, 0.08f), tile);
        AddPart(root.transform, "SideWallLeft", PrimitiveType.Cube,
            new Vector3(-0.7f, 1.16f, 0f), new Vector3(0.08f, 2.2f, 1.4f), tile);
        AddPart(root.transform, "SideWallRight", PrimitiveType.Cube,
            new Vector3(0.7f, 1.16f, 0f), new Vector3(0.08f, 2.2f, 1.4f), tile);

        AddPart(root.transform, "Pipe", PrimitiveType.Cylinder,
            new Vector3(0.54f, 1.66f, 0.6f), new Vector3(0.07f, 0.95f, 0.07f), metal);
        AddPart(root.transform, "ShowerArm", PrimitiveType.Cube,
            new Vector3(0.33f, 2.12f, 0.48f), new Vector3(0.48f, 0.055f, 0.055f), metal,
            new Vector3(0f, -30f, 0f));
        AddPart(root.transform, "ShowerHead", PrimitiveType.Cylinder,
            new Vector3(0.11f, 2.09f, 0.36f), new Vector3(0.26f, 0.035f, 0.26f), metal);

        PrefabUtility.SaveAsPrefabAsset(root, ShowerPrefabPath);
        Object.DestroyImmediate(root);
    }

    private static void BuildShopkeeperPrefab(Material skin, Material shirt,
        Material trousers, Material apron, Material cap, Material eyes)
    {
        GameObject old = GameObject.Find("Market_Shopkeeper");
        if (old == null) old = GameObject.Find("Market_Man");

        Vector3 spawn = old != null
            ? old.transform.position
            : new Vector3(89.29f, 0f, 89.57f);

        if (old != null) Object.DestroyImmediate(old);

        GameObject root = new GameObject("Market_Shopkeeper");

        // Legs and feet.
        AddPart(root.transform, "LegLeft", PrimitiveType.Cylinder,
            new Vector3(-0.16f, 0.45f, 0f), new Vector3(0.17f, 0.45f, 0.17f), trousers);
        AddPart(root.transform, "LegRight", PrimitiveType.Cylinder,
            new Vector3(0.16f, 0.45f, 0f), new Vector3(0.17f, 0.45f, 0.17f), trousers);
        AddPart(root.transform, "FootLeft", PrimitiveType.Cube,
            new Vector3(-0.16f, 0.06f, 0.08f), new Vector3(0.22f, 0.12f, 0.34f), cap);
        AddPart(root.transform, "FootRight", PrimitiveType.Cube,
            new Vector3(0.16f, 0.06f, 0.08f), new Vector3(0.22f, 0.12f, 0.34f), cap);

        // Torso, apron, neck and head.
        AddPart(root.transform, "Torso", PrimitiveType.Capsule,
            new Vector3(0f, 1.24f, 0f), new Vector3(0.46f, 0.38f, 0.34f), shirt);
        AddPart(root.transform, "Apron", PrimitiveType.Cube,
            new Vector3(0f, 1.02f, 0.185f), new Vector3(0.44f, 0.76f, 0.035f), apron);
        AddPart(root.transform, "Neck", PrimitiveType.Cylinder,
            new Vector3(0f, 1.66f, 0f), new Vector3(0.14f, 0.08f, 0.14f), skin);
        AddPart(root.transform, "Head", PrimitiveType.Sphere,
            new Vector3(0f, 1.9f, 0f), new Vector3(0.36f, 0.4f, 0.34f), skin);

        // Face (the character faces local +Z).
        AddPart(root.transform, "EyeLeft", PrimitiveType.Sphere,
            new Vector3(-0.075f, 1.94f, 0.16f), new Vector3(0.045f, 0.045f, 0.03f), eyes);
        AddPart(root.transform, "EyeRight", PrimitiveType.Sphere,
            new Vector3(0.075f, 1.94f, 0.16f), new Vector3(0.045f, 0.045f, 0.03f), eyes);
        AddPart(root.transform, "Nose", PrimitiveType.Sphere,
            new Vector3(0f, 1.86f, 0.18f), new Vector3(0.07f, 0.09f, 0.08f), skin);
        AddPart(root.transform, "Mouth", PrimitiveType.Cube,
            new Vector3(0f, 1.8f, 0.165f), new Vector3(0.1f, 0.02f, 0.02f), eyes);

        // Flat cap.
        AddPart(root.transform, "CapBrim", PrimitiveType.Cylinder,
            new Vector3(0f, 2.1f, 0.03f), new Vector3(0.44f, 0.015f, 0.44f), cap);
        AddPart(root.transform, "CapTop", PrimitiveType.Cylinder,
            new Vector3(0f, 2.15f, 0f), new Vector3(0.32f, 0.075f, 0.32f), cap);

        // Arms angled down to the sides.
        AddPart(root.transform, "ArmLeft", PrimitiveType.Cylinder,
            new Vector3(-0.35f, 1.24f, 0f), new Vector3(0.11f, 0.35f, 0.11f), shirt,
            new Vector3(0f, 0f, 12f));
        AddPart(root.transform, "ArmRight", PrimitiveType.Cylinder,
            new Vector3(0.35f, 1.24f, 0f), new Vector3(0.11f, 0.35f, 0.11f), shirt,
            new Vector3(0f, 0f, -12f));
        AddPart(root.transform, "HandLeft", PrimitiveType.Sphere,
            new Vector3(-0.43f, 0.9f, 0f), new Vector3(0.12f, 0.14f, 0.12f), skin);
        AddPart(root.transform, "HandRight", PrimitiveType.Sphere,
            new Vector3(0.43f, 0.9f, 0f), new Vector3(0.12f, 0.14f, 0.12f), skin);

        BoxCollider box = root.AddComponent<BoxCollider>();
        box.center = new Vector3(0f, 1.05f, 0f);
        box.size = new Vector3(1f, 2.1f, 0.62f);

        root.AddComponent<MarketMan>();

        PrefabUtility.SaveAsPrefabAsset(root, ShopkeeperPrefabPath);
        Object.DestroyImmediate(root);

        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(ShopkeeperPrefabPath);
        if (prefab == null)
        {
            Debug.LogWarning("[SurvivalContentBuilder] Could not reload " +
                             ShopkeeperPrefabPath);
            return;
        }

        GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        instance.name = "Market_Shopkeeper";
        Vector3 ground = GroundPoint(new Vector3(spawn.x, 0f, spawn.z));
        instance.transform.position = ground;
        // Turn him toward the street so the player can walk up and talk.
        instance.transform.rotation = Quaternion.Euler(0f, 180f, 0f);
        Undo.RegisterCreatedObjectUndo(instance, "Add Market_Shopkeeper");
    }

    // ------------------------------------------------------------------
    // SCENE CONTENT
    // ------------------------------------------------------------------

    private static void PlaceWaterWell()
    {
        PlacePrefab(WellPrefabPath, "Water_Well", new Vector3(96f, 0f, 80f), 18f);
    }

    private static void PlaceBathroomShower()
    {
        // Standalone stall on the open floor north-east of the dining table.
        // The back wall faces north and the open front faces the room.
        PlacePrefab(ShowerPrefabPath, "Bathroom_Shower", new Vector3(97f, 0f, 58f), 0f);
    }

    private static GameObject PlacePrefab(string path, string name, Vector3 xzHint, float yaw)
    {
        GameObject existing = GameObject.Find(name);
        if (existing != null) return existing;

        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        if (prefab == null)
        {
            Debug.LogWarning("[SurvivalContentBuilder] Missing prefab: " + path);
            return null;
        }

        Vector3 point = GroundPoint(xzHint);
        GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        instance.name = name;
        instance.transform.position = point;
        instance.transform.rotation = Quaternion.Euler(0f, yaw, 0f);
        Undo.RegisterCreatedObjectUndo(instance, "Add " + name);
        return instance;
    }

    private static void BuildWorldIngredients(Material wood, Material cloth)
    {
        GameObject existing = GameObject.Find("WorldIngredients");
        if (existing != null) return;

        Vector3 anchor = GroundPoint(new Vector3(93.2f, 0f, 87.4f));
        GameObject root = new GameObject("WorldIngredients");
        Undo.RegisterCreatedObjectUndo(root, "Add WorldIngredients");
        root.transform.position = anchor;

        AddPart(root.transform, "TableTop", PrimitiveType.Cube,
            new Vector3(0f, 0.82f, 0f), new Vector3(3.6f, 0.12f, 1.6f), wood,
            Vector3.zero, true);
        AddPart(root.transform, "TableCloth", PrimitiveType.Cube,
            new Vector3(0f, 0.89f, 0f), new Vector3(3.35f, 0.02f, 1.36f), cloth);

        for (int i = 0; i < 4; i++)
        {
            float x = i < 2 ? -1.48f : 1.48f;
            float z = i % 2 == 0 ? -0.55f : 0.55f;
            AddPart(root.transform, "Leg_" + i, PrimitiveType.Cube,
                new Vector3(x, 0.38f, z), new Vector3(0.12f, 0.76f, 0.12f), wood);
        }

        int index = 0;
        foreach (IngredientCatalog.Entry entry in IngredientCatalog.All)
        {
            GameObject prefab = IngredientCatalog.LoadPrefab(entry.Name);
            if (prefab == null)
            {
                Debug.LogWarning("[SurvivalContentBuilder] Missing ingredient prefab: " +
                                 entry.Name);
                continue;
            }

            GameObject item = (GameObject)PrefabUtility.InstantiatePrefab(prefab, root.transform);
            item.name = entry.Name;
            item.transform.localPosition = new Vector3(
                -1.35f + (index % 6) * 0.54f,
                0.905f,
                index < 6 ? -0.32f : 0.32f);
            item.transform.localRotation = Quaternion.Euler(0f, (index % 2) * 180f, 0f);
            index++;
        }
    }

    private static void EnsureFloorNavMeshLink()
    {
        GameObject oldTest = GameObject.Find("TestFloorLink");
        if (oldTest != null) Object.DestroyImmediate(oldTest);

        EnsureNavMeshLink("NavMeshLink_HouseFloor1",
            new Vector3(84.94f, -2.25f, 67.17f),
            new Vector3(84.98f, -1.67f, 64.18f), 3f);

        EnsureNavMeshLink("NavMeshLink_HouseFloor2",
            new Vector3(88.00f, -1.67f, 24.80f),
            new Vector3(88.00f, -1.81f, 23.60f), 2.5f);
    }

    private static void EnsureNavMeshLink(string name, Vector3 start, Vector3 end,
        float width)
    {
        GameObject go = GameObject.Find(name);
        if (go == null)
        {
            go = new GameObject(name);
            Undo.RegisterCreatedObjectUndo(go, "Add " + name);
            go.transform.position = Vector3.zero;
        }

        Unity.AI.Navigation.NavMeshLink link =
            go.GetComponent<Unity.AI.Navigation.NavMeshLink>();
        if (link == null) link = go.AddComponent<Unity.AI.Navigation.NavMeshLink>();

        link.startPoint = start;
        link.endPoint = end;
        link.width = width;
        link.bidirectional = true;
        link.autoUpdate = false;
        EditorUtility.SetDirty(go);
    }

    // ------------------------------------------------------------------
    // HELPERS
    // ------------------------------------------------------------------

    private static GameObject AddPart(Transform parent, string name, PrimitiveType type,
        Vector3 localPosition, Vector3 localScale, Material material,
        Vector3 localEuler = default, bool keepCollider = false)
    {
        GameObject part = GameObject.CreatePrimitive(type);
        part.name = name;
        part.transform.SetParent(parent, false);
        part.transform.localPosition = localPosition;
        part.transform.localRotation = Quaternion.Euler(localEuler);
        part.transform.localScale = localScale;

        if (!keepCollider)
        {
            Collider collider = part.GetComponent<Collider>();
            if (collider != null) Object.DestroyImmediate(collider);
        }

        MeshRenderer renderer = part.GetComponent<MeshRenderer>();
        if (renderer != null)
        {
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
            renderer.receiveShadows = true;
        }

        return part;
    }

    private static Vector3 GroundPoint(Vector3 hint)
    {
        RaycastHit hit;
        Vector3 origin = new Vector3(hint.x, 60f, hint.z);
        if (Physics.Raycast(origin, Vector3.down, out hit, 120f, ~0,
                QueryTriggerInteraction.Ignore))
        {
            return hit.point;
        }

        return hint;
    }

    private static Material CreateMaterial(string name, Color color,
        float smoothness, float metallic)
    {
        string path = MaterialFolder + "/" + name + ".mat";
        Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (material == null)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) shader = Shader.Find("Standard");
            material = new Material(shader);
            AssetDatabase.CreateAsset(material, path);
        }

        if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", color);
        if (material.HasProperty("_Color")) material.SetColor("_Color", color);
        if (material.HasProperty("_Smoothness"))
            material.SetFloat("_Smoothness", smoothness);
        if (material.HasProperty("_Glossiness"))
            material.SetFloat("_Glossiness", smoothness);
        if (material.HasProperty("_Metallic")) material.SetFloat("_Metallic", metallic);
        material.color = color;
        material.enableInstancing = true;
        EditorUtility.SetDirty(material);
        return material;
    }

    private static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path)) return;

        string parent = System.IO.Path.GetDirectoryName(path);
        string leaf = System.IO.Path.GetFileName(path);
        if (string.IsNullOrEmpty(parent) || string.IsNullOrEmpty(leaf)) return;

        EnsureFolder(parent.Replace('\\', '/'));
        AssetDatabase.CreateFolder(parent.Replace('\\', '/'), leaf);
    }
}
#endif
