using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Single source of truth for the Nigerian kitchen ingredients used by the
/// market, the backpack and the pot cooking screen.
/// Prefabs live under Resources/Ingredients so the market can load one by name.
/// </summary>
public static class IngredientCatalog
{
    public sealed class Entry
    {
        public readonly string Name;
        public readonly int BuyPrice;
        public readonly int SellPrice;
        public readonly Color Color;

        public Entry(string name, int buyPrice, int sellPrice, Color color)
        {
            Name = name;
            BuyPrice = buyPrice;
            SellPrice = sellPrice;
            Color = color;
        }

        public string Initials
        {
            get
            {
                string[] parts = Name.Split(' ');
                if (parts.Length > 1)
                    return (parts[0].Substring(0, 1) + parts[1].Substring(0, 1)).ToUpperInvariant();
                return Name.Length > 1
                    ? Name.Substring(0, 2).ToUpperInvariant()
                    : Name.ToUpperInvariant();
            }
        }
    }

    public static readonly Entry[] All =
    {
        new Entry("Palm oil", 120, 55, new Color32(217, 82, 24, 255)),
        new Entry("Abacha", 100, 45, new Color32(201, 162, 39, 255)),
        new Entry("Stockfish", 150, 70, new Color32(214, 199, 161, 255)),
        new Entry("Crayfish", 90, 40, new Color32(224, 122, 95, 255)),
        new Entry("Pepper", 40, 18, new Color32(214, 40, 40, 255)),
        new Entry("Onion", 50, 22, new Color32(123, 63, 158, 255)),
        new Entry("Beef", 180, 85, new Color32(139, 46, 46, 255)),
        new Entry("Cocoyam paste", 80, 35, new Color32(232, 220, 200, 255)),
        new Entry("Bitterleaf", 60, 26, new Color32(58, 125, 68, 255)),
        new Entry("Uziza", 70, 30, new Color32(76, 154, 42, 255)),
        new Entry("Utazi", 70, 30, new Color32(46, 125, 50, 255)),
        new Entry("Ugu", 60, 26, new Color32(95, 168, 46, 255))
    };

    private static readonly Dictionary<string, string> Aliases =
        new Dictionary<string, string>
        {
            { "palmoil", "Palm oil" },
            { "bitterleafbundle", "Bitterleaf" },
            { "bitterleaf", "Bitterleaf" },
            { "bitterleafs", "Bitterleaf" },
            { "chickenchunks", "Beef" },
            { "chicken", "Beef" },
            { "cocoyam", "Cocoyam paste" },
            { "cocoyampaste", "Cocoyam paste" },
            { "stockfish", "Stockfish" },
            { "crayfish", "Crayfish" },
            { "pepper", "Pepper" },
            { "onion", "Onion" },
            { "beef", "Beef" },
            { "abacha", "Abacha" },
            { "uziza", "Uziza" },
            { "utazi", "Utazi" },
            { "ugu", "Ugu" }
        };

    private static readonly Dictionary<string, GameObject> prefabCache =
        new Dictionary<string, GameObject>();

    private static readonly Dictionary<string, Sprite> iconCache =
        new Dictionary<string, Sprite>();

    public static string Canonical(string rawName)
    {
        if (string.IsNullOrWhiteSpace(rawName)) return string.Empty;

        string key = Normalize(rawName);
        if (key.Length == 0) return rawName.Trim();

        string alias;
        if (Aliases.TryGetValue(key, out alias)) return alias;

        foreach (Entry entry in All)
        {
            string entryKey = Normalize(entry.Name);
            if (entryKey == key) return entry.Name;
            if (key.Contains(entryKey) || entryKey.Contains(key)) return entry.Name;
        }

        return rawName.Trim();
    }

    public static Entry Get(string rawName)
    {
        string canonical = Canonical(rawName);
        if (canonical.Length == 0) return null;

        foreach (Entry entry in All)
        {
            if (string.Equals(entry.Name, canonical, System.StringComparison.OrdinalIgnoreCase))
                return entry;
        }

        return null;
    }

    public static bool IsIngredient(string rawName)
    {
        return Get(rawName) != null;
    }

    public static int BuyPriceOf(string rawName)
    {
        Entry entry = Get(rawName);
        return entry != null ? entry.BuyPrice : 40;
    }

    public static int SellPriceOf(string rawName)
    {
        Entry entry = Get(rawName);
        return entry != null ? entry.SellPrice : 15;
    }

    public static Color ColorOf(string rawName)
    {
        Entry entry = Get(rawName);
        return entry != null ? entry.Color : new Color(0.55f, 0.58f, 0.62f, 1f);
    }

    /// <summary>
    /// Loads the ingredient prefab from Resources/Ingredients. Returns null
    /// when the prefab has not been generated yet.
    /// </summary>
    public static GameObject LoadPrefab(string rawName)
    {
        string canonical = Canonical(rawName);
        if (canonical.Length == 0) return null;

        GameObject cached;
        if (prefabCache.TryGetValue(canonical, out cached)) return cached;

        GameObject prefab = Resources.Load<GameObject>("Ingredients/" + canonical);

        if (prefab == null)
        {
            GameObject[] loaded = Resources.LoadAll<GameObject>("Ingredients");
            foreach (GameObject candidate in loaded)
            {
                if (candidate == null) continue;
                if (string.Equals(Normalize(candidate.name), Normalize(canonical),
                        System.StringComparison.Ordinal))
                {
                    prefab = candidate;
                    break;
                }
            }
        }

        prefabCache[canonical] = prefab;
        return prefab;
    }

    public static Sprite LoadIcon(string rawName)
    {
        string canonical = Canonical(rawName);
        if (canonical.Length == 0) return null;

        Sprite cached;
        if (iconCache.TryGetValue(canonical, out cached)) return cached;

        Sprite icon = null;
        GameObject prefab = LoadPrefab(canonical);
        if (prefab != null)
        {
            ItemDetails details = prefab.GetComponent<ItemDetails>();
            if (details == null) details = prefab.GetComponentInChildren<ItemDetails>(true);
            if (details != null) icon = details.itemIcon;
        }

        iconCache[canonical] = icon;
        return icon;
    }

    // ------------------------------------------------------------------
    // BACKPACK HELPERS
    // ------------------------------------------------------------------

    public static int CountInBackpack(string rawName)
    {
        PlayerInventory inventory = PlayerInventory.instance;
        if (inventory == null || inventory.backpackItems == null) return 0;

        string canonical = Canonical(rawName);
        if (canonical.Length == 0) return 0;

        int count = 0;
        foreach (GameObject item in inventory.backpackItems)
        {
            if (item == null) continue;
            if (Matches(inventory.GetItemIdentifier(item), canonical)) count++;
        }

        return count;
    }

    public static bool ConsumeOne(string rawName)
    {
        PlayerInventory inventory = PlayerInventory.instance;
        if (inventory == null || inventory.backpackItems == null) return false;

        string canonical = Canonical(rawName);
        if (canonical.Length == 0) return false;

        for (int i = 0; i < inventory.backpackItems.Count; i++)
        {
            GameObject item = inventory.backpackItems[i];
            if (item == null) continue;

            if (!Matches(inventory.GetItemIdentifier(item), canonical)) continue;

            inventory.RemoveBackpackItemAt(i);
            return true;
        }

        return false;
    }

    public static bool Matches(string itemName, string canonicalIngredient)
    {
        if (string.IsNullOrWhiteSpace(itemName)) return false;
        string canonical = Canonical(itemName);
        return string.Equals(canonical, canonicalIngredient,
            System.StringComparison.OrdinalIgnoreCase);
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
