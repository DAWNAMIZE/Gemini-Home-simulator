using System.Collections.Generic;
using UnityEngine;

public class PlayerInventory : MonoBehaviour
{
    public static PlayerInventory instance;

    [Header("Pot Tracking")]
    public bool isHoldingPot = false;
    public PortablePot heldPot;

    [Header("Holding Setup")]
    public Transform holdPosition; // Drag your new camera hold point here

    [Header("Recipes")]
    public List<Recipe> unlockedRecipes = new List<Recipe>();

    [Header("UI Elements")]
    public GameObject dropButtonUI; 

    void Awake()
    {
        if (instance == null) instance = this;
        else Destroy(gameObject);
    }

    void Start()
    {
        if (dropButtonUI != null) dropButtonUI.SetActive(false);
    }

    // ==========================================
    // RECIPE MANAGEMENT
    // ==========================================
    public void LearnRecipe(Recipe newRecipe)
    {
        if (!unlockedRecipes.Contains(newRecipe))
        {
            unlockedRecipes.Add(newRecipe);
        }
    }

    // ==========================================
    // POT MANAGEMENT
    // ==========================================
    public void PickUpPot(PortablePot pot)
    {
        isHoldingPot = true;
        heldPot = pot;

        // 1. Turn off the collider so it doesn't block your crosshair!
        Collider col = pot.GetComponent<Collider>();
        if (col != null) col.enabled = false;

        // 2. Parent it to the player's camera/hands instead of hiding it
        if (holdPosition != null)
        {
            pot.transform.SetParent(holdPosition);
            pot.transform.localPosition = Vector3.zero;       // Center it exactly on the hold point
            pot.transform.localRotation = Quaternion.identity; // Match the hold point's rotation
        }

        // Turn ON the manual drop button
        if (dropButtonUI != null) dropButtonUI.SetActive(true);
    }

    public void DropPot()
    {
        isHoldingPot = false;
        heldPot = null;

        if (dropButtonUI != null) dropButtonUI.SetActive(false);
    }

    // ==========================================
    // UI BUTTON FUNCTION (Manual Drop)
    // ==========================================
    public void ManualDrop()
    {
        if (isHoldingPot && heldPot != null)
        {
            // 1. Un-parent the pot from the camera
            heldPot.transform.SetParent(null);
            
            // 2. Turn its collider back on so it can be picked up again
            Collider col = heldPot.GetComponent<Collider>();
            if (col != null) col.enabled = true;

            // 3. Drop it slightly in front of the player
            if (Camera.main != null)
            {
                heldPot.transform.position = Camera.main.transform.position + Camera.main.transform.forward * 1.5f;
            }

            // 4. Clear the memory and hide the button
            DropPot(); 
        }
    }
}