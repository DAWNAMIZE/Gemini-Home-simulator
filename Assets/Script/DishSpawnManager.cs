using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Tracks the four finished-dish spawn points around the oven and hands them
/// out in order. Cooking is blocked while every point is occupied.
/// </summary>
public class DishSpawnManager : MonoBehaviour
{
    public static DishSpawnManager instance;

    [Header("Slots")]
    public Transform[] spawnPoints;

    private readonly Dictionary<Transform, FoodDish> occupants = new Dictionary<Transform, FoodDish>();

    public int SlotCount => spawnPoints != null ? spawnPoints.Length : 0;
    public int OccupiedCount => occupants.Count;
    public bool HasFreeSlot => FindFreeSlot() != null;

    void Awake()
    {
        if (instance == null) instance = this;
    }

    void OnDestroy()
    {
        if (instance == this) instance = null;
    }

    public static DishSpawnManager GetOrCreate()
    {
        if (instance != null) return instance;

        DishSpawnManager existing = Object.FindFirstObjectByType<DishSpawnManager>(
            FindObjectsInactive.Include);
        if (existing != null)
        {
            instance = existing;
            return existing;
        }

        GameObject go = new GameObject("DishSpawnManager");
        DishSpawnManager manager = go.AddComponent<DishSpawnManager>();
        manager.DiscoverSpawnPoints();
        return manager;
    }

    /// <summary>
    /// Finds the base FinishedDishSpawnPoint plus its numbered variants.
    /// </summary>
    public void DiscoverSpawnPoints()
    {
        List<Transform> found = new List<Transform>();

        Transform[] all = Object.FindObjectsByType<Transform>(
            FindObjectsInactive.Include, FindObjectsSortMode.None);

        foreach (Transform t in all)
        {
            if (t == null) continue;
            if (!IsDishSpawnName(t.name)) continue;
            if (t.gameObject.scene.IsValid() == false) continue;
            if (!found.Contains(t)) found.Add(t);
        }

        found.Sort((a, b) => SpawnIndex(a.name).CompareTo(SpawnIndex(b.name)));
        spawnPoints = found.ToArray();

        // Drop stale references.
        List<Transform> stale = new List<Transform>();
        foreach (KeyValuePair<Transform, FoodDish> pair in occupants)
        {
            if (pair.Key == null || pair.Value == null) stale.Add(pair.Key);
        }
        foreach (Transform t in stale) occupants.Remove(t);

        Debug.Log("DishSpawnManager: tracking " + spawnPoints.Length + " finished-dish spawn points.");
    }

    private static bool IsDishSpawnName(string name)
    {
        if (string.IsNullOrEmpty(name)) return false;
        return name == "FinishedDishSpawnPoint" ||
               name.StartsWith("FinishedDishSpawnPoint (");
    }

    private static int SpawnIndex(string name)
    {
        if (name == "FinishedDishSpawnPoint") return 0;

        int open = name.IndexOf('(');
        int close = name.IndexOf(')', open + 1);
        if (open >= 0 && close > open)
        {
            int index;
            if (int.TryParse(name.Substring(open + 1, close - open - 1), out index))
                return index;
        }

        return 0;
    }

    public Transform FindFreeSlot()
    {
        if (spawnPoints == null || spawnPoints.Length == 0) DiscoverSpawnPoints();
        if (spawnPoints == null) return null;

        foreach (Transform slot in spawnPoints)
        {
            if (slot == null) continue;
            if (!occupants.ContainsKey(slot) || occupants[slot] == null)
                return slot;
        }

        return null;
    }

    public bool IsOccupied(Transform slot)
    {
        return slot != null && occupants.ContainsKey(slot) && occupants[slot] != null;
    }

    /// <summary>
    /// Spawns a dish in the first free point. Returns null when every point is full.
    /// </summary>
    public FoodDish SpawnDish(GameObject dishPrefab, string displayName)
    {
        if (dishPrefab == null)
        {
            Debug.LogWarning("DishSpawnManager: no finished dish prefab was supplied.");
            return null;
        }

        Transform slot = FindFreeSlot();
        if (slot == null)
        {
            Debug.Log("DishSpawnManager: all finished-dish points are occupied; cooking is blocked.");
            return null;
        }

        GameObject dish = Instantiate(dishPrefab, slot.position, slot.rotation);
        dish.name = string.IsNullOrEmpty(displayName) ? dishPrefab.name : displayName;

        FoodDish food = FoodDish.Attach(dish, dish.name);
        if (food != null)
        {
            food.assignedSpawnPoint = slot;
            occupants[slot] = food;
        }

        return food;
    }

    public void ReleaseDish(FoodDish dish)
    {
        if (dish == null) return;

        List<Transform> release = new List<Transform>();
        foreach (KeyValuePair<Transform, FoodDish> pair in occupants)
        {
            if (pair.Value == dish) release.Add(pair.Key);
        }

        foreach (Transform slot in release) occupants.Remove(slot);
    }

    public void ReleaseSlot(Transform slot)
    {
        if (slot != null) occupants.Remove(slot);
    }

    void Update()
    {
        // If a dish was destroyed without notifying us, clear its slot.
        List<Transform> dead = null;
        foreach (KeyValuePair<Transform, FoodDish> pair in occupants)
        {
            if (pair.Value == null)
            {
                if (dead == null) dead = new List<Transform>();
                dead.Add(pair.Key);
            }
        }

        if (dead != null)
        {
            foreach (Transform slot in dead) occupants.Remove(slot);
        }
    }
}
