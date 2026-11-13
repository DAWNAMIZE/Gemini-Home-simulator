using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;

[Serializable]
public class SaveData
{
    public int version = 1;
    public string sceneName;
    public string savedAtUtc;

    public float health;
    public float hunger;
    public float thirst;
    public float hygiene;
    public float stress;
    public float timeOfDay;

    public bool hasPlayerPosition;
    public float playerX;
    public float playerY;
    public float playerZ;
    public float playerYaw;

    public int pistolAmmo;
    public string equippedWeapon;
    public string[] weaponInventory = new string[0];
    public string[] itemInventory = new string[0];
}

/// <summary>
/// JSON save/load for the survival house. Stores vitals, world time, player
/// position, pistol ammo and the weapon slots. Used by the bed and the main
/// menu's Continue button.
/// </summary>
public static class SaveGameManager
{
    public const string FileName = "gemini_save.json";

    /// <summary>Set by the main menu before loading the game scene.</summary>
    public static bool pendingContinue;

    public static SaveData lastLoaded;

    public static string SavePath
    {
        get { return Path.Combine(Application.persistentDataPath, FileName); }
    }

    public static bool HasSave
    {
        get
        {
            try { return File.Exists(SavePath); }
            catch { return false; }
        }
    }

    public static bool Save()
    {
        try
        {
            SaveData data = Capture();
            string json = JsonUtility.ToJson(data, true);
            Directory.CreateDirectory(Path.GetDirectoryName(SavePath));
            File.WriteAllText(SavePath, json);
            Debug.Log("SaveGameManager: saved to " + SavePath);
            return true;
        }
        catch (Exception exception)
        {
            Debug.LogError("SaveGameManager: could not save - " + exception.Message);
            return false;
        }
    }

    public static SaveData Capture()
    {
        SaveData data = new SaveData();
        data.sceneName = SceneManager.GetActiveScene().name;
        data.savedAtUtc = DateTime.UtcNow.ToString("o");
        data.pistolAmmo = WeaponInstance.PistolAmmo;

        PlayerVitals vitals = PlayerVitals.instance;
        if (vitals == null) vitals = UnityEngine.Object.FindFirstObjectByType<PlayerVitals>();

        if (vitals != null)
        {
            data.health = vitals.currentHealth;
            data.hunger = vitals.hunger;
            data.thirst = vitals.thirst;
            data.hygiene = vitals.hygiene;
            data.stress = vitals.stress;

            Transform player = vitals.transform;
            data.hasPlayerPosition = true;
            data.playerX = player.position.x;
            data.playerY = player.position.y;
            data.playerZ = player.position.z;
            data.playerYaw = player.eulerAngles.y;
        }

        DayNightCycle clock = DayNightCycle.instance;
        if (clock == null) clock = UnityEngine.Object.FindFirstObjectByType<DayNightCycle>();
        if (clock != null) data.timeOfDay = clock.timeOfDay;

        PlayerInventory inventory = PlayerInventory.instance;
        if (inventory != null)
        {
            List<string> weapons = new List<string>();
            List<string> items = new List<string>();

            foreach (GameObject item in inventory.backpackItems)
            {
                if (item == null) continue;

                string name = inventory.GetItemIdentifier(item);
                WeaponInstance weapon = item.GetComponent<WeaponInstance>();
                bool isWeapon = weapon != null || inventory.LooksLikeWeapon(item.name);

                if (isWeapon) weapons.Add(name);
                else items.Add(name);
            }

            data.weaponInventory = weapons.ToArray();
            data.itemInventory = items.ToArray();

            if (inventory.currentlyHeldModel != null)
            {
                WeaponInstance equipped = inventory.currentlyHeldModel.GetComponent<WeaponInstance>();
                if (equipped != null) data.equippedWeapon = equipped.weaponName;
            }
        }

        return data;
    }

    public static bool LoadAndApply()
    {
        SaveData data = Read();
        if (data == null) return false;

        lastLoaded = data;
        Apply(data);
        return true;
    }

    public static SaveData Read()
    {
        try
        {
            if (!File.Exists(SavePath)) return null;
            string json = File.ReadAllText(SavePath);
            if (string.IsNullOrEmpty(json)) return null;

            SaveData data = JsonUtility.FromJson<SaveData>(json);
            return data;
        }
        catch (Exception exception)
        {
            Debug.LogError("SaveGameManager: could not read save - " + exception.Message);
            return null;
        }
    }

    public static void Apply(SaveData data)
    {
        if (data == null) return;

        PlayerVitals vitals = PlayerVitals.instance;
        if (vitals == null) vitals = UnityEngine.Object.FindFirstObjectByType<PlayerVitals>();

        if (vitals != null)
        {
            vitals.currentHealth = Mathf.Clamp(data.health, 0f, vitals.maxHealth);
            vitals.hunger = Mathf.Clamp(data.hunger, 0f, 100f);
            vitals.thirst = Mathf.Clamp(data.thirst, 0f, 100f);
            vitals.hygiene = Mathf.Clamp(data.hygiene, 0f, 100f);
            vitals.stress = Mathf.Clamp(data.stress, 0f, 100f);

            if (data.hasPlayerPosition)
            {
                CharacterController controller = vitals.GetComponent<CharacterController>();
                if (controller != null) controller.enabled = false;

                vitals.transform.position = new Vector3(data.playerX, data.playerY, data.playerZ);
                vitals.transform.rotation = Quaternion.Euler(0f, data.playerYaw, 0f);

                if (controller != null) controller.enabled = true;
            }
        }

        DayNightCycle clock = DayNightCycle.instance;
        if (clock == null) clock = UnityEngine.Object.FindFirstObjectByType<DayNightCycle>();
        if (clock != null) clock.timeOfDay = Mathf.Repeat(data.timeOfDay, 1f);

        WeaponInstance.PistolAmmo = Mathf.Clamp(data.pistolAmmo, 0, WeaponInstance.PistolMaxAmmo);

        PlayerInventory inventory = PlayerInventory.instance;
        if (inventory != null)
        {
            if (data.weaponInventory != null)
            {
                foreach (string weaponName in data.weaponInventory)
                    inventory.RestoreWeaponByName(weaponName);
            }

            if (data.itemInventory != null)
            {
                foreach (string itemName in data.itemInventory)
                    inventory.RestoreItemByName(itemName);
            }

            if (!string.IsNullOrEmpty(data.equippedWeapon))
            {
                int index = inventory.FindSlotByItemName(data.equippedWeapon);
                if (index >= 0) inventory.EquipSlot(index);
            }

            if (BackpackUIManager.instance != null) BackpackUIManager.instance.RefreshUI();
        }

        if (PlayerVitals.instance != null && PhoneUIManager.instance != null)
            PhoneUIManager.instance.RefreshFromVitals();

        Debug.Log("SaveGameManager: save loaded.");
    }

    public static void Delete()
    {
        try
        {
            if (File.Exists(SavePath)) File.Delete(SavePath);
        }
        catch (Exception exception)
        {
            Debug.LogWarning("SaveGameManager: could not delete save - " + exception.Message);
        }
    }
}
