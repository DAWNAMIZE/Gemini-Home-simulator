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
        
        if (currentDish != null && currentDish.finishedDishPrefab != null)
        {
            // Spawn the final bowl of food at the target point
            if (finishedDishSpawnPoint != null)
            {
                GameObject dish = Instantiate(currentDish.finishedDishPrefab,
                    finishedDishSpawnPoint.position, finishedDishSpawnPoint.rotation);
                dish.name = currentDish.recipeName;
                FoodDish.Attach(dish, currentDish.recipeName);
            }
            else
            {
                Debug.LogWarning("Finished Dish Spawn Point is not assigned in the Inspector!");
            }
            
            // Reset state so you can cook again
            state = CookingState.Empty;
            currentDish = null;
            if (foodMesh != null) foodMesh.enabled = false;
        }
        else
        {
            state = CookingState.Empty;
            currentDish = null;
            if (foodMesh != null) foodMesh.enabled = false;
        }
    }

    public void Interact()
    {
        // Pick up the empty pot if we aren't holding anything
        if (PlayerInventory.instance != null && PlayerInventory.instance.currentlyHeldModel == null && state == CookingState.Empty)
        {
            PlayerInventory.instance.HoldPhysicalItem(this.gameObject);
        }
    }
}