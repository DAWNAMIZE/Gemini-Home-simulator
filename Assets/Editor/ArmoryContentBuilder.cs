#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using Unity.AI.Navigation;
using UnityEngine;

/// <summary>
/// Builds the armory merchant, stall, checkout counter, weapon/trap prefabs,
/// the editable ArmoryUI hierarchy, the phone Tasks section, and the swinging
/// door pivots. Run from Tools > Gemini Home > Survival > Build Armory Content.
/// </summary>
public static class ArmoryContentBuilder
{
    private const string PrefabFolder = "Assets/Prefabs/Survival";
    private const string MaterialFolder = PrefabFolder + "/Materials";
    private const string TrapFolder = PrefabFolder + "/Traps";
    private const string ResourceFolder = "Assets/Resources/Armory";
    private const string ReferenceFolder = ResourceFolder + "/References";

    private const string ShopkeeperPrefabPath = PrefabFolder + "/Arms_Shopkeeper.prefab";
    private const string StallPrefabPath = PrefabFolder + "/Armory_Stall.prefab";
    private const string CheckoutPrefabPath = PrefabFolder + "/Armory_Checkout.prefab";

    [MenuItem("Tools/Gemini Home/Survival/Build Armory Content")]
    public static void BuildAll()
    {
        if (EditorApplication.isPlaying)
        {
            EditorUtility.DisplayDialog("Armory Content",
                "Exit play mode before building armory content.", "OK");
            return;
        }

        EnsureFolder("Assets/Prefabs");
        EnsureFolder(PrefabFolder);
        EnsureFolder(MaterialFolder);
        EnsureFolder(TrapFolder);
        EnsureFolder("Assets/Resources");
        EnsureFolder(ResourceFolder);
        EnsureFolder(ReferenceFolder);

        Material steel = CreateMaterial("Armory_Steel",
            new Color(0.43f, 0.46f, 0.50f), 0.62f, 0.72f);
        Material darkSteel = CreateMaterial("Armory_DarkSteel",
            new Color(0.22f, 0.24f, 0.27f), 0.55f, 0.65f);
        Material wood = CreateMaterial("Armory_Wood",
            new Color(0.34f, 0.21f, 0.11f), 0.18f, 0f);
        Material darkWood = CreateMaterial("Armory_DarkWood",
            new Color(0.20f, 0.13f, 0.08f), 0.12f, 0f);
        Material red = CreateMaterial("Armory_Red",
            new Color(0.56f, 0.15f, 0.12f), 0.24f, 0f);
        Material gold = CreateMaterial("Armory_Gold",
            new Color(0.92f, 0.67f, 0.22f), 0.78f, 0.55f);
        Material skin = CreateMaterial("Armory_Skin",
            new Color(0.40f, 0.25f, 0.16f), 0.22f, 0f);
        Material shirt = CreateMaterial("Armory_Shirt",
            new Color(0.42f, 0.16f, 0.12f), 0.16f, 0f);
        Material trousers = CreateMaterial("Armory_Trousers",
            new Color(0.14f, 0.15f, 0.18f), 0.12f, 0f);
        Material cap = CreateMaterial("Armory_Cap",
            new Color(0.10f, 0.09f, 0.08f), 0.12f, 0f);
        Material eyes = CreateMaterial("Armory_Eyes",
            new Color(0.04f, 0.035f, 0.03f), 0.65f, 0f);
        Material bone = CreateMaterial("Armory_Bone",
            new Color(0.80f, 0.77f, 0.68f), 0.28f, 0f);
        Material rope = CreateMaterial("Armory_Rope",
            new Color(0.56f, 0.44f, 0.26f), 0.10f, 0f);

        BuildWeaponPrefabs();
        BuildTrapPrefabs(steel, darkSteel, wood, darkWood, red, gold, rope);
        BuildShopkeeper(steel, darkSteel, wood, darkWood, red, gold, skin,
            shirt, trousers, cap, eyes);
        BuildStall(steel, wood, darkWood, red, gold, bone);
        BuildCheckout(steel, wood, darkWood, red, gold, bone);

        PlacePrefab(ShopkeeperPrefabPath, "Arms_Shopkeeper",
            new Vector3(95.0f, 0f, 89.8f), 180f);
        PlacePrefab(StallPrefabPath, "Armory_Stall",
            new Vector3(95.0f, 0f, 90.9f), 0f);
        PlacePrefab(CheckoutPrefabPath, "Armory_Checkout",
            new Vector3(91.8f, 0f, 90.6f), 180f);

        SetupDoor("Door_1");
        SetupDoor("Door_1 (1)");

        BuildPhoneTaskSection();
        BuildArmoryUI();
        RebuildNavMesh();

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log("[ArmoryContentBuilder] Built armory shopkeeper, stall, checkout, " +
                  "weapon/trap prefabs, UI, phone Tasks section and door pivots.");
    }

    // ------------------------------------------------------------------
    // WEAPONS
    // ------------------------------------------------------------------

    private static void BuildWeaponPrefabs()
    {
        CopyWeapon("Assets/LowPolyRPGWeapons_Lite/Prefabs/Dagger_01.prefab",
            "Dagger_01", true, 30);
        CopyWeapon("Assets/LowPolyRPGWeapons_Lite/Prefabs/Hammer_01.prefab",
            "Hammer_01", true, 50);
        CopyWeapon("Assets/LowPolyRPGWeapons_Lite/Prefabs/Ax_01.prefab",
            "Ax_01", true, 60);
        CopyWeapon("Assets/LowPolyRPGWeapons_Lite/Prefabs/Sword_01.prefab",
            "Sword_01", true, 55);
        CopyWeapon("Assets/Low Poly Pistol Weapon Pack 2/Prefabs/Weapons/Pistol_F.prefab",
            "Pistol_F", false, 0);
        CopyWeapon("Assets/Low Poly Pistol Weapon Pack 2/Prefabs/Weapons/Pistol_I.prefab",
            "Pistol_I", false, 0);
    }

    private static void CopyWeapon(string sourcePath, string targetName, bool melee, int damage)
    {
        string targetPath = ResourceFolder + "/" + targetName + ".prefab";
        if (AssetDatabase.LoadAssetAtPath<GameObject>(targetPath) == null)
        {
            if (!AssetDatabase.CopyAsset(sourcePath, targetPath))
            {
                Debug.LogWarning("[ArmoryContentBuilder] Could not copy " + sourcePath);
                return;
            }
        }

        GameObject root = PrefabUtility.LoadPrefabContents(targetPath);
        if (root == null) return;

        try
        {
            foreach (ItemPickup pickup in root.GetComponents<ItemPickup>())
                Object.DestroyImmediate(pickup);
            foreach (WeaponPickup pickup in root.GetComponents<WeaponPickup>())
                Object.DestroyImmediate(pickup);
            foreach (Rigidbody body in root.GetComponents<Rigidbody>())
                Object.DestroyImmediate(body);

            if (root.GetComponent<WeaponInstance>() == null)
                root.AddComponent<WeaponInstance>();

            if (melee)
            {
                MeleeWeapon weapon = root.GetComponent<MeleeWeapon>();
                if (weapon == null) weapon = root.AddComponent<MeleeWeapon>();
                weapon.damage = damage;
            }

            PrefabUtility.SaveAsPrefabAsset(root, targetPath);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }
    }

    // ------------------------------------------------------------------
    // TRAPS
    // ------------------------------------------------------------------

    private static void BuildTrapPrefabs(Material steel, Material darkSteel,
        Material wood, Material darkWood, Material red, Material gold, Material rope)
    {
        GameObject bear = BuildBearTrap(steel, darkSteel, red);
        GameObject spike = BuildSpikeTrap(steel, darkSteel, wood);
        GameObject snare = BuildSnareTrap(rope, darkWood, gold);

        BuildTrapKit("Trap_Bear_Kit", "Bear Trap", bear, darkSteel, red, steel);
        BuildTrapKit("Trap_Spike_Kit", "Spike Trap", spike, darkWood, steel, darkSteel);
        BuildTrapKit("Trap_Snare_Kit", "Snare Trap", snare, darkWood, rope, gold);
    }

    private static GameObject BuildBearTrap(Material steel, Material darkSteel, Material red)
    {
        GameObject root = new GameObject("Trap_Bear");
        Trap trap = root.AddComponent<Trap>();
        trap.trapName = "Bear Trap";
        trap.damage = 55;
        trap.uses = 1;
        trap.triggerRadius = 1.15f;

        AddPart(root.transform, "Base", PrimitiveType.Cube,
            new Vector3(0f, 0.03f, 0f), new Vector3(0.95f, 0.06f, 0.62f), darkSteel);
        AddPart(root.transform, "TriggerPlate", PrimitiveType.Cube,
            new Vector3(0f, 0.075f, 0f), new Vector3(0.30f, 0.03f, 0.22f), red);

        AddPart(root.transform, "JawLeft", PrimitiveType.Cube,
            new Vector3(-0.18f, 0.16f, 0f), new Vector3(0.62f, 0.05f, 0.06f), steel,
            new Vector3(0f, 0f, -24f));
        AddPart(root.transform, "JawRight", PrimitiveType.Cube,
            new Vector3(0.18f, 0.16f, 0f), new Vector3(0.62f, 0.05f, 0.06f), steel,
            new Vector3(0f, 0f, 24f));

        for (int i = 0; i < 4; i++)
        {
            float x = -0.33f + i * 0.22f;
            AddPart(root.transform, "ToothA_" + i, PrimitiveType.Cube,
                new Vector3(x, 0.215f, -0.03f), new Vector3(0.045f, 0.10f, 0.035f), steel);
            AddPart(root.transform, "ToothB_" + i, PrimitiveType.Cube,
                new Vector3(x, 0.215f, 0.03f), new Vector3(0.045f, 0.10f, 0.035f), steel);
        }

        AddPart(root.transform, "SpringLeft", PrimitiveType.Cylinder,
            new Vector3(-0.48f, 0.10f, 0f), new Vector3(0.09f, 0.05f, 0.09f), steel,
            new Vector3(0f, 0f, 90f));
        AddPart(root.transform, "SpringRight", PrimitiveType.Cylinder,
            new Vector3(0.48f, 0.10f, 0f), new Vector3(0.09f, 0.05f, 0.09f), steel,
            new Vector3(0f, 0f, 90f));

        return SavePrefab(root, PrefabFolder + "/Traps/Trap_Bear.prefab");
    }

    private static GameObject BuildSpikeTrap(Material steel, Material darkSteel, Material wood)
    {
        GameObject root = new GameObject("Trap_Spike");
        Trap trap = root.AddComponent<Trap>();
        trap.trapName = "Spike Trap";
        trap.damage = 35;
        trap.uses = 3;
        trap.triggerRadius = 1.05f;

        AddPart(root.transform, "Board", PrimitiveType.Cube,
            new Vector3(0f, 0.035f, 0f), new Vector3(1.10f, 0.07f, 0.72f), wood);
        AddPart(root.transform, "Brace", PrimitiveType.Cube,
            new Vector3(0f, 0.08f, 0f), new Vector3(1.14f, 0.025f, 0.76f), darkSteel);

        for (int x = -1; x <= 1; x++)
        {
            for (int z = -1; z <= 1; z++)
            {
                AddPart(root.transform, "Spike_" + x + "_" + z, PrimitiveType.Cylinder,
                    new Vector3(x * 0.32f, 0.22f, z * 0.22f),
                    new Vector3(0.045f, 0.14f, 0.045f), steel);
            }
        }

        return SavePrefab(root, PrefabFolder + "/Traps/Trap_Spike.prefab");
    }

    private static GameObject BuildSnareTrap(Material rope, Material darkWood, Material gold)
    {
        GameObject root = new GameObject("Trap_Snare");
        Trap trap = root.AddComponent<Trap>();
        trap.trapName = "Snare Trap";
        trap.damage = 15;
        trap.uses = 2;
        trap.triggerRadius = 0.95f;

        AddPart(root.transform, "Ring", PrimitiveType.Cylinder,
            new Vector3(0f, 0.025f, 0f), new Vector3(0.62f, 0.012f, 0.62f), rope);
        AddPart(root.transform, "Loop", PrimitiveType.Cylinder,
            new Vector3(0f, 0.035f, 0f), new Vector3(0.38f, 0.008f, 0.38f), darkWood);
        AddPart(root.transform, "Trigger", PrimitiveType.Cube,
            new Vector3(0f, 0.055f, 0f), new Vector3(0.16f, 0.02f, 0.16f), gold);

        for (int i = 0; i < 4; i++)
        {
            float angle = i * 90f;
            float radians = angle * Mathf.Deg2Rad;
            AddPart(root.transform, "Stake_" + i, PrimitiveType.Cylinder,
                new Vector3(Mathf.Cos(radians) * 0.58f, 0.12f, Mathf.Sin(radians) * 0.58f),
                new Vector3(0.025f, 0.12f, 0.025f), darkWood);
        }

        return SavePrefab(root, PrefabFolder + "/Traps/Trap_Snare.prefab");
    }

    private static void BuildTrapKit(string kitName, string displayName,
        GameObject worldTrap, Material caseMaterial, Material tagMaterial, Material metal)
    {
        GameObject root = new GameObject(kitName);

        BoxCollider box = root.AddComponent<BoxCollider>();
        box.center = new Vector3(0f, 0.10f, 0f);
        box.size = new Vector3(0.55f, 0.22f, 0.42f);

        TrapDeployer deployer = root.AddComponent<TrapDeployer>();
        deployer.trapPrefab = worldTrap;
        deployer.trapName = displayName;
        deployer.placeDistance = 1.65f;

        HeldItemPose pose = root.AddComponent<HeldItemPose>();
        pose.localPosition = new Vector3(0f, -0.05f, 0.18f);
        pose.localEuler = new Vector3(0f, 0f, 0f);
        pose.localScale = new Vector3(0.62f, 0.62f, 0.62f);

        ItemReference reference = CreateItemReference(kitName, displayName, "Trap");
        ItemDetails details = root.AddComponent<ItemDetails>();
        details.itemRef = reference;

        AddPart(root.transform, "Case", PrimitiveType.Cube,
            new Vector3(0f, 0.09f, 0f), new Vector3(0.55f, 0.16f, 0.42f), caseMaterial);
        AddPart(root.transform, "Lid", PrimitiveType.Cube,
            new Vector3(0f, 0.18f, 0f), new Vector3(0.57f, 0.04f, 0.44f), tagMaterial);
        AddPart(root.transform, "Handle", PrimitiveType.Cube,
            new Vector3(0f, 0.22f, 0f), new Vector3(0.20f, 0.035f, 0.05f), metal);
        AddPart(root.transform, "Tag", PrimitiveType.Cube,
            new Vector3(0.20f, 0.11f, 0.215f), new Vector3(0.11f, 0.07f, 0.01f), tagMaterial);

        string path = ResourceFolder + "/" + kitName + ".prefab";
        SavePrefab(root, path);
    }

    // ------------------------------------------------------------------
    // SHOPKEEPER / STALL / CHECKOUT
    // ------------------------------------------------------------------

    private static void BuildShopkeeper(Material steel, Material darkSteel,
        Material wood, Material darkWood, Material red, Material gold,
        Material skin, Material shirt, Material trousers, Material cap, Material eyes)
    {
        GameObject root = new GameObject("Arms_Shopkeeper");

        AddPart(root.transform, "LegLeft", PrimitiveType.Cylinder,
            new Vector3(-0.16f, 0.45f, 0f), new Vector3(0.17f, 0.45f, 0.17f), trousers);
        AddPart(root.transform, "LegRight", PrimitiveType.Cylinder,
            new Vector3(0.16f, 0.45f, 0f), new Vector3(0.17f, 0.45f, 0.17f), trousers);
        AddPart(root.transform, "FootLeft", PrimitiveType.Cube,
            new Vector3(-0.16f, 0.06f, 0.08f), new Vector3(0.22f, 0.12f, 0.34f), cap);
        AddPart(root.transform, "FootRight", PrimitiveType.Cube,
            new Vector3(0.16f, 0.06f, 0.08f), new Vector3(0.22f, 0.12f, 0.34f), cap);

        AddPart(root.transform, "Torso", PrimitiveType.Capsule,
            new Vector3(0f, 1.24f, 0f), new Vector3(0.50f, 0.38f, 0.36f), shirt);
        AddPart(root.transform, "Apron", PrimitiveType.Cube,
            new Vector3(0f, 1.02f, 0.185f), new Vector3(0.46f, 0.76f, 0.035f), darkWood);
        AddPart(root.transform, "Harness", PrimitiveType.Cube,
            new Vector3(0f, 1.31f, 0.205f), new Vector3(0.50f, 0.10f, 0.035f), steel);

        AddPart(root.transform, "Neck", PrimitiveType.Cylinder,
            new Vector3(0f, 1.66f, 0f), new Vector3(0.14f, 0.08f, 0.14f), skin);
        AddPart(root.transform, "Head", PrimitiveType.Sphere,
            new Vector3(0f, 1.9f, 0f), new Vector3(0.36f, 0.4f, 0.34f), skin);
        AddPart(root.transform, "EyeLeft", PrimitiveType.Sphere,
            new Vector3(-0.075f, 1.94f, 0.16f), new Vector3(0.045f, 0.045f, 0.03f), eyes);
        AddPart(root.transform, "EyeRight", PrimitiveType.Sphere,
            new Vector3(0.075f, 1.94f, 0.16f), new Vector3(0.045f, 0.045f, 0.03f), eyes);
        AddPart(root.transform, "Nose", PrimitiveType.Sphere,
            new Vector3(0f, 1.86f, 0.18f), new Vector3(0.07f, 0.09f, 0.08f), skin);
        AddPart(root.transform, "Mouth", PrimitiveType.Cube,
            new Vector3(0f, 1.8f, 0.165f), new Vector3(0.1f, 0.02f, 0.02f), eyes);

        AddPart(root.transform, "CapBrim", PrimitiveType.Cylinder,
            new Vector3(0f, 2.1f, 0.03f), new Vector3(0.44f, 0.015f, 0.44f), cap);
        AddPart(root.transform, "CapTop", PrimitiveType.Cylinder,
            new Vector3(0f, 2.15f, 0f), new Vector3(0.32f, 0.075f, 0.32f), cap);
        AddPart(root.transform, "Badge", PrimitiveType.Cube,
            new Vector3(0.16f, 1.45f, 0.205f), new Vector3(0.09f, 0.09f, 0.02f), gold,
            new Vector3(0f, 0f, 45f));

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

        root.AddComponent<ArmoryMan>();
        SavePrefab(root, ShopkeeperPrefabPath);
    }

    private static void BuildStall(Material steel, Material wood, Material darkWood,
        Material red, Material gold, Material bone)
    {
        GameObject root = new GameObject("Armory_Stall");

        AddPart(root.transform, "TableTop", PrimitiveType.Cube,
            new Vector3(0f, 0.92f, 0f), new Vector3(3.2f, 0.14f, 1.3f), wood);
        AddPart(root.transform, "TableCloth", PrimitiveType.Cube,
            new Vector3(0f, 1.00f, 0f), new Vector3(3.0f, 0.02f, 1.15f), red);

        for (int i = 0; i < 4; i++)
        {
            float x = i < 2 ? -1.42f : 1.42f;
            float z = i % 2 == 0 ? -0.48f : 0.48f;
            AddPart(root.transform, "Leg_" + i, PrimitiveType.Cube,
                new Vector3(x, 0.45f, z), new Vector3(0.14f, 0.90f, 0.14f), darkWood);
        }

        AddPart(root.transform, "BackBoard", PrimitiveType.Cube,
            new Vector3(0f, 1.75f, 0.64f), new Vector3(3.3f, 1.7f, 0.10f), darkWood);
        AddPart(root.transform, "SignBoard", PrimitiveType.Cube,
            new Vector3(0f, 2.85f, 0.64f), new Vector3(3.4f, 0.75f, 0.10f), wood);

        // Crossed swords over the sign.
        AddPart(root.transform, "SwordSignLeft", PrimitiveType.Cube,
            new Vector3(-0.30f, 2.86f, 0.58f), new Vector3(0.10f, 0.66f, 0.04f), gold,
            new Vector3(0f, 0f, 38f));
        AddPart(root.transform, "SwordSignRight", PrimitiveType.Cube,
            new Vector3(0.30f, 2.86f, 0.58f), new Vector3(0.10f, 0.66f, 0.04f), gold,
            new Vector3(0f, 0f, -38f));

        // Display weapons on the backboard.
        AddPart(root.transform, "DisplaySwordBlade", PrimitiveType.Cube,
            new Vector3(-1.05f, 1.72f, 0.55f), new Vector3(0.10f, 1.05f, 0.03f), steel);
        AddPart(root.transform, "DisplaySwordGuard", PrimitiveType.Cube,
            new Vector3(-1.05f, 1.18f, 0.55f), new Vector3(0.38f, 0.06f, 0.06f), gold);
        AddPart(root.transform, "DisplaySwordGrip", PrimitiveType.Cube,
            new Vector3(-1.05f, 1.05f, 0.55f), new Vector3(0.08f, 0.24f, 0.06f), darkWood);

        AddPart(root.transform, "DisplayAxeHandle", PrimitiveType.Cube,
            new Vector3(0f, 1.52f, 0.55f), new Vector3(0.08f, 1.05f, 0.06f), darkWood);
        AddPart(root.transform, "DisplayAxeHead", PrimitiveType.Cube,
            new Vector3(0.15f, 2.0f, 0.55f), new Vector3(0.38f, 0.28f, 0.05f), steel);

        AddPart(root.transform, "DisplayPistolBody", PrimitiveType.Cube,
            new Vector3(1.05f, 1.72f, 0.55f), new Vector3(0.45f, 0.16f, 0.07f), steel);
        AddPart(root.transform, "DisplayPistolGrip", PrimitiveType.Cube,
            new Vector3(1.05f, 1.53f, 0.55f), new Vector3(0.15f, 0.28f, 0.07f), darkWood);

        AddPart(root.transform, "Skull", PrimitiveType.Sphere,
            new Vector3(0.95f, 1.24f, 0.20f), new Vector3(0.22f, 0.20f, 0.20f), bone);
        AddPart(root.transform, "SkullJaw", PrimitiveType.Cube,
            new Vector3(0.95f, 1.12f, 0.20f), new Vector3(0.18f, 0.08f, 0.16f), bone);

        BoxCollider box = root.AddComponent<BoxCollider>();
        box.center = new Vector3(0f, 1.0f, 0f);
        box.size = new Vector3(3.2f, 2.0f, 1.3f);

        SavePrefab(root, StallPrefabPath);
    }

    private static void BuildCheckout(Material steel, Material wood, Material darkWood,
        Material red, Material gold, Material bone)
    {
        GameObject root = new GameObject("Armory_Checkout");

        AddPart(root.transform, "CounterTop", PrimitiveType.Cube,
            new Vector3(0f, 1.00f, 0f), new Vector3(2.0f, 0.12f, 0.95f), wood);
        AddPart(root.transform, "CounterFront", PrimitiveType.Cube,
            new Vector3(0f, 0.52f, -0.38f), new Vector3(2.0f, 0.92f, 0.16f), darkWood);

        AddPart(root.transform, "LeftLeg", PrimitiveType.Cube,
            new Vector3(-0.78f, 0.48f, 0.27f), new Vector3(0.15f, 0.96f, 0.15f), darkWood);
        AddPart(root.transform, "RightLeg", PrimitiveType.Cube,
            new Vector3(0.78f, 0.48f, 0.27f), new Vector3(0.15f, 0.96f, 0.15f), darkWood);

        AddPart(root.transform, "Register", PrimitiveType.Cube,
            new Vector3(0f, 1.20f, 0f), new Vector3(0.55f, 0.30f, 0.38f), steel);
        AddPart(root.transform, "RegisterScreen", PrimitiveType.Cube,
            new Vector3(0f, 1.29f, 0.20f), new Vector3(0.35f, 0.14f, 0.02f), gold);
        AddPart(root.transform, "RegisterKey", PrimitiveType.Cube,
            new Vector3(-0.16f, 1.10f, 0.20f), new Vector3(0.10f, 0.04f, 0.10f), red);
        AddPart(root.transform, "RegisterKey2", PrimitiveType.Cube,
            new Vector3(0.02f, 1.10f, 0.20f), new Vector3(0.10f, 0.04f, 0.10f), darkWood);
        AddPart(root.transform, "RegisterKey3", PrimitiveType.Cube,
            new Vector3(0.20f, 1.10f, 0.20f), new Vector3(0.10f, 0.04f, 0.10f), red);

        AddPart(root.transform, "CheckoutSign", PrimitiveType.Cube,
            new Vector3(0f, 1.85f, -0.12f), new Vector3(1.9f, 0.42f, 0.06f), darkWood);
        AddPart(root.transform, "CoinLeft", PrimitiveType.Cylinder,
            new Vector3(-0.35f, 1.85f, -0.08f), new Vector3(0.16f, 0.015f, 0.16f), gold,
            new Vector3(90f, 0f, 0f));
        AddPart(root.transform, "CoinRight", PrimitiveType.Cylinder,
            new Vector3(0.35f, 1.85f, -0.08f), new Vector3(0.16f, 0.015f, 0.16f), gold,
            new Vector3(90f, 0f, 0f));
        AddPart(root.transform, "CoinMiddle", PrimitiveType.Cylinder,
            new Vector3(0f, 1.85f, -0.08f), new Vector3(0.20f, 0.015f, 0.20f), gold,
            new Vector3(90f, 0f, 0f));

        BoxCollider box = root.AddComponent<BoxCollider>();
        box.center = new Vector3(0f, 0.9f, 0f);
        box.size = new Vector3(2.0f, 1.8f, 0.95f);

        root.AddComponent<ArmoryCheckoutPoint>();
        SavePrefab(root, CheckoutPrefabPath);
    }

    // ------------------------------------------------------------------
    // DOORS / PHONE / UI
    // ------------------------------------------------------------------

    private static void SetupDoor(string doorName)
    {
        GameObject door = GameObject.Find(doorName);
        if (door == null) return;

        if (door.GetComponent<Door>() != null || door.GetComponentInParent<Door>() != null)
            return;

        Renderer renderer = door.GetComponentInChildren<Renderer>();
        if (renderer == null)
        {
            Debug.LogWarning("[ArmoryContentBuilder] No renderer on " + doorName);
            return;
        }

        Bounds bounds = renderer.bounds;
        Transform parent = door.transform.parent;
        Vector3 hingePoint = new Vector3(bounds.max.x, door.transform.position.y,
            door.transform.position.z);

        GameObject pivot = new GameObject(doorName + "_Pivot");
        Undo.RegisterCreatedObjectUndo(pivot, "Add door pivot");
        pivot.transform.SetParent(parent, false);
        pivot.transform.position = hingePoint;
        pivot.transform.rotation = Quaternion.identity;
        pivot.transform.localScale = Vector3.one;

        door.transform.SetParent(pivot.transform, true);

        Door interactive = pivot.AddComponent<Door>();
        interactive.hinge = pivot.transform;
        interactive.openAngle = 90f;
        interactive.openSpeed = 2.6f;
        interactive.autoCloseDelay = 0f;

        Undo.RegisterFullObjectHierarchyUndo(pivot, "Configure door");
    }

    private static void BuildPhoneTaskSection()
    {
        PhoneUIManager phone = Object.FindFirstObjectByType<PhoneUIManager>(
            FindObjectsInactive.Include);
        if (phone == null)
        {
            Debug.LogWarning("[ArmoryContentBuilder] No PhoneUIManager found.");
            return;
        }

        phone.EnsureTaskSection(true);
        phone.ShowTaskSection();
    }

    private static void BuildArmoryUI()
    {
        GameObject root = GameObject.Find("NewUI_Editable");
        if (root == null)
        {
            root = new GameObject("NewUI_Editable");
            Undo.RegisterCreatedObjectUndo(root, "Add editable UI root");
        }

        Transform existing = root.transform.Find("ArmoryUI");
        ArmoryUI ui = existing != null ? existing.GetComponent<ArmoryUI>() : null;

        if (ui == null)
        {
            GameObject screen = new GameObject("ArmoryUI");
            screen.transform.SetParent(root.transform, false);
            ui = screen.AddComponent<ArmoryUI>();
            Undo.RegisterCreatedObjectUndo(screen, "Add ArmoryUI");
        }

        ui.BuildForEditor();
        if (ui.transform.childCount > 0)
        {
            Transform canvas = ui.transform.Find("ArmoryCanvas");
            if (canvas != null) canvas.gameObject.SetActive(true);
        }
    }

    private static void RebuildNavMesh()
    {
        NavMeshSurface[] surfaces = Object.FindObjectsByType<NavMeshSurface>(
            FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (NavMeshSurface surface in surfaces)
        {
            if (surface != null) surface.BuildNavMesh();
        }
    }

    // ------------------------------------------------------------------
    // SCENE / PREFAB HELPERS
    // ------------------------------------------------------------------

    private static GameObject PlacePrefab(string prefabPath, string instanceName,
        Vector3 xzHint, float yaw)
    {
        GameObject existing = GameObject.Find(instanceName);
        if (existing != null)
        {
            existing.transform.position = GroundPoint(xzHint);
            existing.transform.rotation = Quaternion.Euler(0f, yaw, 0f);
            EditorUtility.SetDirty(existing);
            return existing;
        }

        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
        if (prefab == null)
        {
            Debug.LogWarning("[ArmoryContentBuilder] Missing prefab: " + prefabPath);
            return null;
        }

        GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        instance.name = instanceName;
        instance.transform.position = GroundPoint(xzHint);
        instance.transform.rotation = Quaternion.Euler(0f, yaw, 0f);
        Undo.RegisterCreatedObjectUndo(instance, "Add " + instanceName);
        return instance;
    }

    private static GameObject SavePrefab(GameObject root, string path)
    {
        GameObject asset = PrefabUtility.SaveAsPrefabAsset(root, path);
        Object.DestroyImmediate(root);
        return asset;
    }

    private static ItemReference CreateItemReference(string id, string displayName,
        string category)
    {
        string path = ReferenceFolder + "/" + id + ".asset";
        ItemReference reference = AssetDatabase.LoadAssetAtPath<ItemReference>(path);
        if (reference == null)
        {
            reference = ScriptableObject.CreateInstance<ItemReference>();
            AssetDatabase.CreateAsset(reference, path);
        }

        reference.itemName = displayName;
        reference.category = category;
        EditorUtility.SetDirty(reference);
        return reference;
    }

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
        // Cast only against the terrain so previously placed props (or the
        // merchant being repositioned) do not become the new floor.
        GameObject ground = GameObject.Find("Environment/Ground/Ground");
        Collider groundCollider = ground != null ? ground.GetComponent<Collider>() : null;
        Ray ray = new Ray(new Vector3(hint.x, 60f, hint.z), Vector3.down);

        RaycastHit hit;
        if (groundCollider != null && groundCollider.Raycast(ray, out hit, 200f))
            return hit.point;

        if (Physics.Raycast(ray, out hit, 200f, ~0, QueryTriggerInteraction.Ignore))
            return hit.point;

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
