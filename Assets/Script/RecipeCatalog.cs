using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Shared runtime view of the recipes that exist in the game. The prep station
/// owns the curated list, so it is preferred when present; loaded and learned
/// recipes are used as a fallback for scenes without a prep station.
/// </summary>
public static class RecipeCatalog
{
    public static List<Recipe> GetAll()
    {
        List<Recipe> results = new List<Recipe>();

        PrepStationUI prep = Object.FindFirstObjectByType<PrepStationUI>(
            FindObjectsInactive.Include);

        if (prep != null && prep.allGameRecipes != null && prep.allGameRecipes.Count > 0)
        {
            foreach (Recipe recipe in prep.allGameRecipes)
                AddUnique(results, recipe);
        }
        else
        {
            Recipe[] loaded = Resources.FindObjectsOfTypeAll<Recipe>();
            foreach (Recipe recipe in loaded)
                AddUnique(results, recipe);
        }

        if (PlayerInventory.instance != null && PlayerInventory.instance.unlockedRecipes != null)
        {
            foreach (Recipe recipe in PlayerInventory.instance.unlockedRecipes)
                AddUnique(results, recipe);
        }

        results.Sort((a, b) => string.Compare(
            a.recipeName, b.recipeName, System.StringComparison.OrdinalIgnoreCase));

        return results;
    }

    public static List<Recipe> GetByCuisine(NigerianCuisine cuisine)
    {
        List<Recipe> results = new List<Recipe>();
        List<Recipe> all = GetAll();

        foreach (Recipe recipe in all)
        {
            if (recipe != null && recipe.cuisine == cuisine)
                results.Add(recipe);
        }

        return results;
    }

    private static void AddUnique(List<Recipe> results, Recipe recipe)
    {
        if (recipe == null || results.Contains(recipe)) return;
        results.Add(recipe);
    }
}
