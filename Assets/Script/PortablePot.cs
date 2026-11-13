using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class PortablePot : MonoBehaviour, IInteractable 
{
    [Header("Data")]
    public Recipe currentDish;
    public CookingState state = CookingState.Empty;

    [Header("Visuals")]
    public MeshRenderer foodMesh;       // The generic liquid inside the pot
    public GameObject steamParticles;

    [Header("Loading Bar UI")]
    public GameObject potUICanvas;      // The canvas above the pot
    public Slider cookingProgressBar;   // Drag your UI Slider here

    [Header("Spawning")]
    [Tooltip("Drag the empty GameObject where the plate should appear")]
    public Transform finishedDishSpawnPoint; // THE FIX: This variable is now correctly declared!

    public enum CookingState { Empty, Cooking, Cooked }

    void Start()
    {
        ResolveVisuals();

        if (potUICanvas != null) potUICanvas.SetActive(false);
        SetSmokeActive(false);
        if (foodMesh != null) foodMesh.enabled = false;
    }

    /// <summary>
    /// Finds the smoke effect and food renderer even when the inspector
    /// references were never wired. Prefers a child called CookingSmoke.
    /// </summary>
    private void ResolveVisuals()
    {
        if (steamParticles == null)
        {
            Transform smoke = FindDeepChild(transform, "CookingSmoke");
            if (smoke == null) smoke = FindDeepChild(transform, "VFX_Smoke");

            if (smoke == null)
            {
                ParticleSystem[] systems = GetComponentsInChildren<ParticleSystem>(true);
                foreach (ParticleSystem system in systems)
                {
                    if (system == null) continue;
                    if (system.gameObject.name.IndexOf("fire", System.StringComparison.OrdinalIgnoreCase) >= 0)
                        continue;

                    smoke = system.transform;
                    break;
                }
            }

            if (smoke != null) steamParticles = smoke.gameObject;
        }

        if (foodMesh == null)
        {
            MeshRenderer[] renderers = GetComponentsInChildren<MeshRenderer>(true);
            foreach (MeshRenderer renderer in renderers)
            {
                if (renderer == null) continue;

                string n = renderer.gameObject.name.ToLowerInvariant();
                if (n.Contains("liquid") || n.Contains("food") || n.Contains("soup"))
                {
                    foodMesh = renderer;
                    break;
                }
            }
        }
    }

    private static Transform FindDeepChild(Transform parent, string name)
    {
        if (parent == null) return null;
        if (parent.name == name) return parent;

        for (int i = 0; i < parent.childCount; i++)
        {
            Transform found = FindDeepChild(parent.GetChild(i), name);
            if (found != null) return found;
        }

        return null;
    }

    private void SetSmokeActive(bool active)
    {
        if (steamParticles == null) ResolveVisuals();
        if (steamParticles == null) return;

        steamParticles.SetActive(active);

        ParticleSystem[] systems = steamParticles.GetComponentsInChildren<ParticleSystem>(true);
        foreach (ParticleSystem system in systems)
        {
            if (system == null) continue;

            if (active)
                system.Play();
            else
                system.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }
    }

    public void StartCooking(Recipe dish)
    {
        if (dish == null)
        {
            Debug.LogWarning("PortablePot: cannot cook a null recipe.");
            return;
        }

        if (state != CookingState.Empty)
        {
            ToastUI.Show(state == CookingState.Cooked
                ? "The pot already holds a finished dish - collect it first."
                : "This pot is already cooking.");
            return;
        }

        // All four finished-dish points full: refuse to start another dish.
        DishSpawnManager manager = DishSpawnManager.GetOrCreate();
        if (manager != null && manager.SlotCount > 0 && !manager.HasFreeSlot)
        {
            ToastUI.Show("All four dish slots are full - clear a slot before cooking.");
            return;
        }

        // Four plates per wood load. Empty oven must be fed before cooking.
        OvenFuel fuel = OvenFuel.FindFor(this);
        if (fuel != null && !fuel.HasFuel)
        {
            ToastUI.Show("The oven is out of fuel. Pick up wood and interact with the oven.");
            return;
        }

        currentDish = dish;
        state = CookingState.Cooking;
        
        if (foodMesh != null) foodMesh.enabled = true; 
        SetSmokeActive(true);
        if (potUICanvas != null) potUICanvas.SetActive(true);

        // Start the loading bar coroutine
        StartCoroutine(CookRoutine());
    }

    private IEnumerator CookRoutine()
    {
        float timer = 0f;
        float cookTime = currentDish != null ? Mathf.Max(0.1f, currentDish.cookTime) : 0.1f;
        
        if (cookingProgressBar != null)
        {
            cookingProgressBar.maxValue = cookTime;
            cookingProgressBar.value = 0f;
        }

        while (timer < cookTime)
        {
            timer += Time.deltaTime;
            if (cookingProgressBar != null) cookingProgressBar.value = timer;
            yield return null; // Wait for the next frame
        }

        FinishCooking();
    }

    private void FinishCooking()
    {
        state = CookingState.Cooked;

        SetSmokeActive(false);
        if (potUICanvas != null) potUICanvas.SetActive(false);

        if (currentDish == null)
        {
            state = CookingState.Empty;
            if (foodMesh != null) foodMesh.enabled = false;
            return;
        }

        if (currentDish.finishedDishPrefab == null)
        {
            Debug.LogWarning("PortablePot: recipe '" + currentDish.recipeName +
                             "' has no finished dish prefab.");
            state = CookingState.Empty;
            currentDish = null;
            if (foodMesh != null) foodMesh.enabled = false;
            return;
        }

        // Try to fill the next free FinishedDishSpawnPoint. If every point is
        // occupied the pot stays cooked so the dish is not lost; tapping the
        // pot again retries the spawn.
        if (!TrySpawnCookedDish())
        {
            ToastUI.Show("All four dish slots are full - clear a slot to collect this dish.");
        }
    }

    private bool TrySpawnCookedDish()
    {
        if (currentDish == null || currentDish.finishedDishPrefab == null) return false;

        OvenFuel fuel = OvenFuel.FindFor(this);
        DishSpawnManager manager = DishSpawnManager.GetOrCreate();
        FoodDish spawned = null;

        if (manager != null && manager.SlotCount > 0)
        {
            spawned = manager.SpawnDish(currentDish.finishedDishPrefab, currentDish.recipeName);
        }
        else if (finishedDishSpawnPoint != null)
        {
            GameObject dish = Instantiate(currentDish.finishedDishPrefab,
                finishedDishSpawnPoint.position, finishedDishSpawnPoint.rotation);
            dish.name = currentDish.recipeName;
            spawned = FoodDish.Attach(dish, currentDish.recipeName);
        }
        else
        {
            Debug.LogWarning("PortablePot: no finished dish spawn points were found.");
        }

        if (spawned == null) return false;

        // One finished plate consumes one slot of the current wood load.
        if (fuel != null) fuel.ConsumePlate();

        state = CookingState.Empty;
        currentDish = null;
        if (foodMesh != null) foodMesh.enabled = false;
        return true;
    }

    public void Interact()
    {
        // Holding wood and tapping the pot feeds the oven behind it.
        OvenFuel fuel = OvenFuel.FindFor(this);
        if (fuel != null && fuel.TryInsertHeldWood()) return;

        // A cooked dish whose spawn was blocked can be collected on a later tap.
        if (state == CookingState.Cooked && currentDish != null)
        {
            string recipeName = currentDish.recipeName;
            if (TrySpawnCookedDish())
                ToastUI.Show("Finished " + recipeName + "!");
            else
                ToastUI.Show("All four dish slots are full - clear a slot first.");
            return;
        }

        // Pick up the empty pot if we aren't holding anything
        if (PlayerInventory.instance != null &&
            PlayerInventory.instance.IsHandEmpty &&
            state == CookingState.Empty)
        {
            PlayerInventory.instance.HoldPhysicalItem(this.gameObject);
        }
    }
}