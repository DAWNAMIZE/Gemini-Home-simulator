using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Single source of truth for the armory merchant. Weapons and trap kits are
/// loaded from Resources/Armory; ammo is applied straight to the pistol pool.
/// </summary>
public static class ArmoryCatalog
{
    public enum Category
    {
        Weapon,
        Ammo,
        Trap
    }

    public sealed class Entry
    {
        public readonly string Id;
        public readonly string DisplayName;
        public readonly Category Category;
        public readonly int Price;
        public readonly Color Color;
        public readonly int AmmoAmount;

        public Entry(string id, string displayName, Category category, int price,
            Color color, int ammoAmount = 0)
        {
            Id = id;
            DisplayName = displayName;
            Category = category;
            Price = price;
            Color = color;
            AmmoAmount = ammoAmount;
        }

        public string ResourceName => "Armory/" + Id;

        public string CategoryLabel
        {
            get
            {
                switch (Category)
                {
                    case Category.Weapon: return "WEAPONS";
                    case Category.Ammo: return "AMMO";
                    default: return "TRAPS";
                }
            }
        }

        public bool IsAmmo => Category == Category.Ammo;
    }

    public static readonly Entry[] All =
    {
        new Entry("Dagger_01", "Dagger", Category.Weapon, 80,
            new Color32(176, 186, 198, 255)),
        new Entry("Hammer_01", "Hammer", Category.Weapon, 110,
            new Color32(188, 140, 82, 255)),
        new Entry("Ax_01", "Axe", Category.Weapon, 160,
            new Color32(198, 108, 58, 255)),
        new Entry("Sword_01", "Sword", Category.Weapon, 190,
            new Color32(212, 216, 224, 255)),
        new Entry("Pistol_F", "Pistol F", Category.Weapon, 320,
            new Color32(140, 146, 158, 255)),
        new Entry("Pistol_I", "Pistol I", Category.Weapon, 420,
            new Color32(112, 126, 148, 255)),

        new Entry("Ammo_9mm", "9mm x5", Category.Ammo, 25,
            new Color32(232, 190, 72, 255), 5),
        new Entry("Ammo_9mm_bulk", "9mm x15", Category.Ammo, 65,
            new Color32(232, 158, 42, 255), 15),

        new Entry("Trap_Snare_Kit", "Snare Trap", Category.Trap, 70,
            new Color32(132, 116, 82, 255)),
        new Entry("Trap_Spike_Kit", "Spike Trap", Category.Trap, 95,
            new Color32(148, 152, 160, 255)),
        new Entry("Trap_Bear_Kit", "Bear Trap", Category.Trap, 140,
            new Color32(120, 128, 138, 255))
    };

    private static readonly Dictionary<string, Entry> ById =
        new Dictionary<string, Entry>();

    private static readonly Dictionary<string, GameObject> prefabCache =
        new Dictionary<string, GameObject>();

    static ArmoryCatalog()
    {
        foreach (Entry entry in All)
            if (!ById.ContainsKey(entry.Id)) ById.Add(entry.Id, entry);
    }

    public static Entry Get(string id)
    {
        if (string.IsNullOrEmpty(id)) return null;
        Entry entry;
        return ById.TryGetValue(id, out entry) ? entry : null;
    }

    public static Entry[] ForCategory(Category category)
    {
        List<Entry> entries = new List<Entry>();
        foreach (Entry entry in All)
            if (entry.Category == category) entries.Add(entry);
        return entries.ToArray();
    }

    public static GameObject LoadPrefab(Entry entry)
    {
        if (entry == null || entry.IsAmmo) return null;

        GameObject cached;
        if (prefabCache.TryGetValue(entry.Id, out cached)) return cached;

        GameObject prefab = Resources.Load<GameObject>(entry.ResourceName);
        prefabCache[entry.Id] = prefab;
        return prefab;
    }

#if UNITY_EDITOR
    public static void ClearCache()
    {
        prefabCache.Clear();
    }
#endif
}
