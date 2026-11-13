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
        if (potUICanvas != null) potUICanvas.SetActive(false);
        if (steamParticles != null) steamParticles.SetActive(false);
        if (foodMesh != null) foodMesh.enabled = false;
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

        currentDish = dish;
        state = CookingState.Cooking;
        
        if (foodMesh != null) foodMesh.enabled = true; 
        if (steamParticles != null) steamParticles.SetActive(true);
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

        if (steamParticles != null) steamParticles.SetActive(false);
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

        state = CookingState.Empty;
        currentDish = null;
        if (foodMesh != null) foodMesh.enabled = false;
        return true;
    }

    public void Interact()
    {
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