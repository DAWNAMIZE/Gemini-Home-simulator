using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PrepStation : MonoBehaviour, IInteractable
{
    [Header("Physical Setup")]
    public Transform potSnapPoint; // The yellow dot on the table
    public PortablePot placedPot;  // The pot currently sitting here (starts empty)

    [Header("UI References")]
    public GameObject uiPanel;        
    public Transform buttonContainer; 
    public GameObject buttonPrefab;   
    public TMP_Text statusText;

    private bool isMenuOpen = false;

    public void Interact()
    {
        // CASE 1: Player is holding a Pot -> Place it on the table
        if (PlayerInventory.instance.isHoldingPot && placedPot == null)
        {
            PlacePot();
            return; // Don't open menu yet, just place it
        }

        // CASE 2: Pot is on the table -> Open/Close the Cooking Menu
        if (placedPot != null)
        {
            isMenuOpen = !isMenuOpen;
            if (isMenuOpen) OpenMenu();
            else CloseMenu();
            return;
        }

        // CASE 3: Table is empty -> Tell player to get a pot
        if (placedPot == null)
        {
            Debug.Log("I need a pot here first!");
            // Optional: Show a small UI hint "Place Pot Here"
        }
    }

    void PlacePot()
    {
        // 1. Get the pot from player's hand
        PortablePot pot = PlayerInventory.instance.heldPot;
        
        // 2. Move it to the table snap point
        pot.gameObject.SetActive(true);
        pot.transform.position = potSnapPoint.position;
        pot.transform.rotation = potSnapPoint.rotation;
        
        // 3. Register it
        placedPot = pot;
        
        // 4. Clear player hand
        PlayerInventory.instance.DropPot();
        
        // 5. Auto-open menu for convenience
        OpenMenu();
    }

    void OpenMenu()
    {
        // Only open if the pot is actually empty!
        if (placedPot.state != PortablePot.CookingState.Empty)
        {
            Debug.Log("This pot is already full!");
            return;
        }

        uiPanel.SetActive(true);
        isMenuOpen = true;
        
        // Unlock cursor so player can click buttons
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        // Clear old buttons
        foreach (Transform child in buttonContainer) Destroy(child.gameObject);

        // Check Recipes
        if (PlayerInventory.instance.unlockedRecipes.Count == 0)
        {
            if (statusText) statusText.text = "No Recipes Known.";
            return;
        }

        if (statusText) statusText.text = "Select Meal to Prep:";

        // Generate a button for every recipe learned
        foreach (Recipe r in PlayerInventory.instance.unlockedRecipes)
        {
            GameObject btn = Instantiate(buttonPrefab, buttonContainer);
            btn.GetComponentInChildren<TMP_Text>().text = r.recipeName;
            btn.GetComponent<Button>().onClick.AddListener(() => PrepMeal(r));
        }
    }

    void CloseMenu()
    {
        uiPanel.SetActive(false);
        isMenuOpen = false;
        
        // Lock cursor back to gameplay
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void PrepMeal(Recipe dish)
    {
        // We fill the PLACED pot with the selected recipe data
        if (placedPot != null)
        {
            placedPot.SetupPot(dish); 
            Debug.Log("Filled pot with " + dish.recipeName);
            
            // Close menu so player can pick the pot back up
            CloseMenu();
        }
    }
    
    // Safety check: If player somehow bypasses logic and picks up the pot early
    void Update()
    {
        if (placedPot != null && !placedPot.gameObject.activeSelf)
        {
            placedPot = null;
            CloseMenu();
        }
    }
}