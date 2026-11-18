#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Generates the twelve ingredient prefabs, their ItemReference assets and
/// their materials, then writes the canonical ingredient list into every
/// Recipe asset. Re-running the menu item rebuilds the generated assets, so
/// the catalog in IngredientCatalog.cs always stays the single source of truth.
/// </summary>
public static class IngredientContentBuilder
{
    private const string ResourcesFolder = "Assets/Resources";
    private const string IngredientsFolder = ResourcesFolder + "/Ingredients";
    private const string MaterialsFolder = IngredientsFolder + "/Materials";
    private const string ReferencesFolder = IngredientsFolder + "/References";

    private sealed class RecipeTemplate
    {
        public readonly string Id;
        public readonly string Name;
        public readonly string[] Ingredients;
        public readonly int SellPrice;
        public readonly int HungerRestore;
        public readonly float CookTime;

        public RecipeTemplate(string id, string name, string[] ingredients,
            int sellPrice, int hungerRestore, float cookTime)
        {
            Id = id;
            Name = name;
            Ingredients = ingredients;
            SellPrice = sellPrice;
            HungerRestore = hungerRestore;
            CookTime = cookTime;
        }
    }

    private static readonly RecipeTemplate[] RecipeTemplates =
    {
        new RecipeTemplate(
            "Abacha",
            "Abacha",
            new[] { "Abacha", "Palm oil", "Stockfish", "Crayfish", "Pepper", "Onion", "Utazi" },
            180, 70, 4f),

        new RecipeTemplate(
            "Egusi Soup",
            "Egusi Soup",
            new[] { "Cocoyam paste", "Palm oil", "Stockfish", "Crayfish", "Pepper", "Onion", "Ugu" },
            200, 75, 5f),

        new RecipeTemplate(
            "Ofe Nsala (White Soup)",
            "Ofe Nsala (White Soup)",
            new[] { "Cocoyam paste", "Stockfish", "Crayfish", "Pepper", "Onion", "Beef", "Utazi" },
            220, 80, 5f),

        new RecipeTemplate(
            "Ofe Onugbu",
            "Ofe Onugbu",
            new[] { "Bitterleaf", "Palm oil", "Stockfish", "Crayfish", "Pepper", "Onion", "Beef", "Cocoyam paste" },
            210, 78, 5f),

        new RecipeTemplate(
            "Ofe Onugbu (Bitterleaf Soup)",
            "Ofe Onugbu (Bitterleaf Soup)",
            new[] { "Bitterleaf", "Palm oil", "Stockfish", "Crayfish", "Pepper", "Onion", "Beef", "Cocoyam paste" },
            210, 78, 5f),

        new RecipeTemplate(
            "Ofe Owerri",
            "Ofe Owerri",
            new[] { "Cocoyam paste", "Palm oil", "Stockfish", "Crayfish", "Pepper", "Onion", "Beef", "Uziza", "Ugu" },
            240, 85, 6f),

        new RecipeTemplate(
            "Pepper Soup",
            "Pepper Soup",
            new[] { "Pepper", "Onion", "Stockfish", "Crayfish", "Beef", "Utazi" },
            190, 72, 4f),

        new RecipeTemplate(
            "Ukwa (African Breadfruit)",
            "Ukwa (African Breadfruit)",
            new[] { "Cocoyam paste", "Palm oil", "Stockfish", "Crayfish", "Pepper", "Onion" },
            170, 68, 5f)
    };

    [MenuItem("Tools/Gemini Home/Ingredient Content/Build All")]
    public static void BuildAll()
    {
        EnsureFolder(ResourcesFolder);
        EnsureFolder(IngredientsFolder);
        EnsureFolder(MaterialsFolder);
        EnsureFolder(ReferencesFolder);

        int built = 0;
        foreach (IngredientCatalog.Entry entry in IngredientCatalog.All)
        {
            BuildIngredient(entry);
            built++;
        }

        int recipes = AssignRecipeData();

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log("[IngredientContentBuilder] Built " + built +
                  " ingredient prefabs and updated " + recipes + " recipe asset(s).");
    }

    [MenuItem("Tools/Gemini Home/Ingredient Content/Validate")]
    public static void ValidateAll()
    {
        int problems = 0;

        foreach (IngredientCatalog.Entry entry in IngredientCatalog.All)
        {
            string prefabPath = PrefabPath(entry.Name);
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (prefab == null)
            {
                Debug.LogWarning("[IngredientContentBuilder] Missing prefab: " + prefabPath);
                problems++;
                continue;
            }

            ItemDetails details = prefab.GetComponent<ItemDetails>();
            if (details == null || details.itemRef == null)
            {
                Debug.LogWarning("[IngredientContentBuilder] Missing ItemDetails/ItemReference: " + prefabPath);
                problems++;
            }

            ItemPickup pickup = prefab.GetComponent<ItemPickup>();
            if (pickup == null || pickup.itemPrefab != prefab)
            {
                Debug.LogWarning("[IngredientContentBuilder] ItemPickup does not point at itself: " + prefabPath);
                problems++;
            }

            if (prefab.GetComponent<Collider>() == null)
            {
                Debug.LogWarning("[IngredientContentBuilder] Missing collider: " + prefabPath);
                problems++;
            }
        }

        foreach (string guid in AssetDatabase.FindAssets("t:Recipe"))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            Recipe recipe = AssetDatabase.LoadAssetAtPath<Recipe>(path);
            if (recipe == null) continue;

            if (recipe.requiredIngredients == null || recipe.requiredIngredients.Count == 0)
            {
                Debug.LogWarning("[IngredientContentBuilder] Recipe has no ingredients: " + path);
                problems++;
                continue;
            }

            foreach (string raw in recipe.requiredIngredients)
            {
                if (!IngredientCatalog.IsIngredient(raw))
                {
                    Debug.LogWarning("[IngredientContentBuilder] Unknown ingredient '" + raw +
                                     "' in " + path);
                    problems++;
                }
            }
        }

        Debug.Log(problems == 0
            ? "[IngredientContentBuilder] Validation passed."
            : "[IngredientContentBuilder] Validation found " + problems + " problem(s).");
    }

    // ------------------------------------------------------------------
    // PREFABS
    // ------------------------------------------------------------------

    private static void BuildIngredient(IngredientCatalog.Entry entry)
    {
        ItemReference reference = BuildReference(entry);
        Material material = BuildMaterial(entry);

        PrimitiveType shape;
        Vector3 visualScale;
        Vector3 colliderCenter;
        Vector3 colliderSize;
        GetVisual(entry.Name, out shape, out visualScale, out colliderCenter, out colliderSize);

        GameObject root = new GameObject(entry.Name);

        GameObject visual = GameObject.CreatePrimitive(shape);
        visual.name = "Visual";
        visual.transform.SetParent(root.transform, false);
        visual.transform.localPosition = colliderCenter;
        visual.transform.localRotation = Quaternion.identity;
        visual.transform.localScale = visualScale;

        Collider primitiveCollider = visual.GetComponent<Collider>();
        if (primitiveCollider != null) UnityEngine.Object.DestroyImmediate(primitiveCollider);

        MeshRenderer renderer = visual.GetComponent<MeshRenderer>();
        if (renderer != null)
        {
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
            renderer.receiveShadows = true;
        }

        BoxCollider box = root.AddComponent<BoxCollider>();
        box.center = colliderCenter;
        box.size = colliderSize;
        box.isTrigger = false;

        ItemDetails details = root.AddComponent<ItemDetails>();
        details.itemRef = reference;

        ItemPickup pickup = root.AddComponent<ItemPickup>();

        GameObject saved = PrefabUtility.SaveAsPrefabAsset(root, PrefabPath(entry.Name));
        UnityEngine.Object.DestroyImmediate(root);

        if (saved == null)
        {
            Debug.LogError("[IngredientContentBuilder] Failed to save prefab for " + entry.Name);
            return;
        }

        // The pickup stores the prefab asset it belongs to so a world copy can
        // add the right backpack entry. That reference has to be written after
        // the prefab exists.
        ItemPickup savedPickup = saved.GetComponent<ItemPickup>();
        if (savedPickup != null)
        {
            savedPickup.itemPrefab = saved;
            EditorUtility.SetDirty(savedPickup);
        }

        ItemDetails savedDetails = saved.GetComponent<ItemDetails>();
        if (savedDetails != null)
        {
            savedDetails.itemRef = reference;
            EditorUtility.SetDirty(savedDetails);
        }
    }

    private static ItemReference BuildReference(IngredientCatalog.Entry entry)
    {
        string path = ReferencesFolder + "/" + entry.Name + ".asset";
        ItemReference reference = AssetDatabase.LoadAssetAtPath<ItemReference>(path);
        if (reference == null)
        {
            reference = ScriptableObject.CreateInstance<ItemReference>();
            AssetDatabase.CreateAsset(reference, path);
        }

        reference.itemName = entry.Name;
        reference.category = "Ingredient";
        reference.effectAmount = 0;
        EditorUtility.SetDirty(reference);
        return reference;
    }

    private static Material BuildMaterial(IngredientCatalog.Entry entry)
    {
        string path = MaterialsFolder + "/" + entry.Name + ".mat";
        Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (material == null)
        {
            material = new Material(FindLitShader());
            AssetDatabase.CreateAsset(material, path);
        }
        else
        {
            Shader lit = FindLitShader();
            if (lit != null && material.shader != lit) material.shader = lit;
        }

        Color color = entry.Color;
        color.a = 1f;

        if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", color);
        if (material.HasProperty("_Color")) material.SetColor("_Color", color);
        if (material.HasProperty("_Smoothness")) material.SetFloat("_Smoothness", 0.22f);
        if (material.HasProperty("_Glossiness")) material.SetFloat("_Glossiness", 0.22f);
        if (material.HasProperty("_Metallic")) material.SetFloat("_Metallic", 0.05f);
        material.color = color;
        material.enableInstancing = true;

        EditorUtility.SetDirty(material);
        return material;
    }

    private static Shader FindLitShader()
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null) shader = Shader.Find("Universal Render Pipeline/Simple Lit");
        if (shader == null) shader = Shader.Find("Standard");
        return shader;
    }

    /// <summary>
    /// Each ingredient gets a simple but distinct silhouette so market stock and
    /// dropped items are recognizable from colour and shape alone.
    /// </summary>
    private static void GetVisual(string name, out PrimitiveType shape,
        out Vector3 scale, out Vector3 colliderCenter, out Vector3 colliderSize)
    {
        colliderCenter = new Vector3(0f, 0.075f, 0f);

        switch (name)
        {
            case "Palm oil":
                shape = PrimitiveType.Cylinder;
                scale = new Vector3(0.13f, 0.15f, 0.13f);
                colliderCenter = new Vector3(0f, 0.15f, 0f);
                colliderSize = new Vector3(0.14f, 0.30f, 0.14f);
                break;

            case "Abacha":
                shape = PrimitiveType.Cube;
                scale = new Vector3(0.22f, 0.07f, 0.17f);
                colliderCenter = new Vector3(0f, 0.035f, 0f);
                colliderSize = new Vector3(0.22f, 0.07f, 0.17f);
                break;

            case "Stockfish":
                shape = PrimitiveType.Capsule;
                scale = new Vector3(0.075f, 0.19f, 0.075f);
                colliderCenter = new Vector3(0f, 0.19f, 0f);
                colliderSize = new Vector3(0.09f, 0.38f, 0.09f);
                break;

            case "Crayfish":
                shape = PrimitiveType.Sphere;
                scale = new Vector3(0.15f, 0.12f, 0.15f);
                colliderCenter = new Vector3(0f, 0.06f, 0f);
                colliderSize = new Vector3(0.15f, 0.12f, 0.15f);
                break;

            case "Pepper":
                shape = PrimitiveType.Capsule;
                scale = new Vector3(0.055f, 0.085f, 0.055f);
                colliderCenter = new Vector3(0f, 0.13f, 0f);
                colliderSize = new Vector3(0.07f, 0.24f, 0.07f);
                break;

            case "Onion":
                shape = PrimitiveType.Sphere;
                scale = new Vector3(0.17f, 0.13f, 0.17f);
                colliderCenter = new Vector3(0f, 0.065f, 0f);
                colliderSize = new Vector3(0.17f, 0.13f, 0.17f);
                break;

            case "Beef":
                shape = PrimitiveType.Cube;
                scale = new Vector3(0.19f, 0.10f, 0.16f);
                colliderCenter = new Vector3(0f, 0.05f, 0f);
                colliderSize = new Vector3(0.19f, 0.10f, 0.16f);
                break;

            case "Cocoyam paste":
                shape = PrimitiveType.Sphere;
                scale = new Vector3(0.20f, 0.11f, 0.20f);
                colliderCenter = new Vector3(0f, 0.055f, 0f);
                colliderSize = new Vector3(0.20f, 0.11f, 0.20f);
                break;

            case "Bitterleaf":
                shape = PrimitiveType.Cube;
                scale = new Vector3(0.19f, 0.05f, 0.19f);
                colliderCenter = new Vector3(0f, 0.025f, 0f);
                colliderSize = new Vector3(0.19f, 0.05f, 0.19f);
                break;

            case "Uziza":
                shape = PrimitiveType.Sphere;
                scale = new Vector3(0.12f, 0.10f, 0.12f);
                colliderCenter = new Vector3(0f, 0.05f, 0f);
                colliderSize = new Vector3(0.12f, 0.10f, 0.12f);
                break;

            case "Utazi":
                shape = PrimitiveType.Cube;
                scale = new Vector3(0.16f, 0.045f, 0.16f);
                colliderCenter = new Vector3(0f, 0.0225f, 0f);
                colliderSize = new Vector3(0.16f, 0.045f, 0.16f);
                break;

            case "Ugu":
                shape = PrimitiveType.Capsule;
                scale = new Vector3(0.06f, 0.16f, 0.06f);
                colliderCenter = new Vector3(0f, 0.16f, 0f);
                colliderSize = new Vector3(0.08f, 0.30f, 0.08f);
                break;

            default:
                shape = PrimitiveType.Cube;
                scale = new Vector3(0.16f, 0.12f, 0.16f);
                colliderCenter = new Vector3(0f, 0.06f, 0f);
                colliderSize = new Vector3(0.16f, 0.12f, 0.16f);
                break;
        }
    }

    // ------------------------------------------------------------------
    // RECIPES
    // ------------------------------------------------------------------

    private static int AssignRecipeData()
    {
        int updated = 0;

        foreach (string guid in AssetDatabase.FindAssets("t:Recipe"))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            Recipe recipe = AssetDatabase.LoadAssetAtPath<Recipe>(path);
            if (recipe == null) continue;

            RecipeTemplate template = MatchTemplate(recipe, path);
            if (template == null)
            {
                Debug.LogWarning("[IngredientContentBuilder] No template matched recipe: " + path);
                continue;
            }

            recipe.recipeID = template.Id;
            recipe.recipeName = template.Name;
            recipe.requiredIngredients = new List<string>(template.Ingredients);
            recipe.sellPrice = template.SellPrice;
            recipe.hungerRestore = template.HungerRestore;
            recipe.cookTime = template.CookTime;
            recipe.isUnlocked = true;

            EditorUtility.SetDirty(recipe);
            updated++;
        }

        return updated;
    }

    private static RecipeTemplate MatchTemplate(Recipe recipe, string assetPath)
    {
        string haystack = Normalize(
            (recipe.recipeID ?? string.Empty) + " " +
            (recipe.recipeName ?? string.Empty) + " " +
            Path.GetFileNameWithoutExtension(assetPath));

        if (haystack.Contains("breadfruit") || haystack.Contains("ukwa")) return Find("Ukwa (African Breadfruit)");
        if (haystack.Contains("peppersoup")) return Find("Pepper Soup");
        if (haystack.Contains("bitterleafsoup")) return Find("Ofe Onugbu (Bitterleaf Soup)");
        if (haystack.Contains("onugbu")) return Find("Ofe Onugbu");
        if (haystack.Contains("owerri")) return Find("Ofe Owerri");
        if (haystack.Contains("nsala") || haystack.Contains("whitesoup")) return Find("Ofe Nsala (White Soup)");
        if (haystack.Contains("egusi")) return Find("Egusi Soup");
        if (haystack.Contains("abacha")) return Find("Abacha");

        return null;
    }

    private static RecipeTemplate Find(string id)
    {
        foreach (RecipeTemplate template in RecipeTemplates)
            if (template.Id == id) return template;

        return null;
    }

    // ------------------------------------------------------------------
    // HELPERS
    // ------------------------------------------------------------------

    private static string PrefabPath(string ingredientName)
    {
        return IngredientsFolder + "/" + ingredientName + ".prefab";
    }

    private static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path)) return;

        string parent = Path.GetDirectoryName(path);
        string leaf = Path.GetFileName(path);
        if (string.IsNullOrEmpty(parent) || string.IsNullOrEmpty(leaf)) return;

        EnsureFolder(parent.Replace('\\', '/'));
        AssetDatabase.CreateFolder(parent.Replace('\\', '/'), leaf);
    }

    private static string Normalize(string value)
    {
        if (string.IsNullOrEmpty(value)) return string.Empty;

        System.Text.StringBuilder builder = new System.Text.StringBuilder(value.Length);
        foreach (char c in value)
        {
            if (char.IsLetterOrDigit(c))
                builder.Append(char.ToLowerInvariant(c));
        }

        return builder.ToString();
    }
}
#endif
