using UnityEngine;

// THE FIX IS HERE: It must say ": MonoBehaviour"
public class PortablePot : MonoBehaviour, IInteractable 
{
    [Header("Data")]
    public Recipe currentDish;
    public CookingState state = CookingState.Empty;
    public float cookProgress = 0f;

    [Header("Visuals")]
    public MeshRenderer foodMesh;
    public GameObject steamParticles;

    public enum CookingState { Empty, Raw, Cooking, Cooked, Burnt }

    public void SetupPot(Recipe dish)
    {
        currentDish = dish;
        state = CookingState.Raw;
        foodMesh.enabled = true;
        foodMesh.material = dish.rawMat;
        if (steamParticles) steamParticles.SetActive(false);
    }

    public void Interact()
    {
        // 1. If cooked, Eat
        if (state == CookingState.Cooked)
        {
            // Find player and restore hunger
            PlayerVitals player = FindFirstObjectByType<PlayerVitals>();
            if(player != null) player.Eat(currentDish.hungerRestore);
            
            Destroy(gameObject); // Poof, ate it
            return;
        }

        // 2. Pick Up Logic
        if (PlayerInventory.instance != null && !PlayerInventory.instance.isHoldingPot)
        {
            PlayerInventory.instance.PickUpPot(this);
            gameObject.SetActive(false); // Hide visuals
        }
    }

    public void UpdateVisuals()
    {
        if (currentDish == null) return;

        if (state == CookingState.Cooked) foodMesh.material = currentDish.cookedMat;
        else if (state == CookingState.Burnt) foodMesh.material = currentDish.burntMat;
        
        if (steamParticles) steamParticles.SetActive(state == CookingState.Cooking);
    }
}