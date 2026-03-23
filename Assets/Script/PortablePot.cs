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
        foodMesh.enabled = true;
        foodMesh.material = dish.rawMat;
        if (steamParticles) steamParticles.SetActive(false);
    }

    public void Interact()
    {
        if (state == CookingState.Cooked)
        {
            PlayerVitals player = FindFirstObjectByType<PlayerVitals>();
            if(player != null) player.Eat(currentDish.hungerRestore);
            Destroy(gameObject); 
            return;
        }

        // Make sure it ONLY has this piece of code for picking up!
        if (PlayerHands.instance != null && PlayerHands.instance.currentHoldType == PlayerHands.HoldType.None)
        {
            PlayerHands.instance.GrabPot(this);
        }
        else
        {
            Debug.Log("Hands are full!");
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