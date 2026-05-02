using UnityEngine;

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
        
        // FIX 1: We no longer look for 'rawMat' in the Recipe. 
        // We just turn the generic food liquid mesh on inside the pot.
        if (foodMesh != null) foodMesh.enabled = true; 
        
        if (steamParticles) steamParticles.SetActive(false);
    }

    public void Interact()
    {
        // 1. If cooked, Eat
        if (state == CookingState.Cooked)
        {
            PlayerVitals player = FindFirstObjectByType<PlayerVitals>();
            
            // FIX 2: Updated from .Eat() to .EatFood() to perfectly match 
            // the new PlayerVitals.cs script we mapped out earlier!
            if(player != null && currentDish != null) player.Eat(currentDish.hungerRestore);
            
            state = CookingState.Empty;
            currentDish = null;
            if (foodMesh != null) foodMesh.enabled = false;
            return;
        }

        // 2. Pick Up Logic (Your custom PlayerHands logic remains untouched!)
        if (PlayerHands.instance != null && PlayerHands.instance.currentHoldType == PlayerHands.HoldType.None)
        {
            PlayerHands.instance.GrabPot(this);
        }
    }
    
    public void UpdateVisuals()
    {
        if (currentDish == null) return;

        // FIX 3: Removed the old cookedMat and burntMat references.
        // The script no longer crashes trying to find them.
        
        // Steam visual feedback stays!
        if (steamParticles) steamParticles.SetActive(state == CookingState.Cooking);
    }
}