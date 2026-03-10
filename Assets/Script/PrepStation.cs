using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PrepStation : MonoBehaviour, IInteractable
{
    [Header("Physical Setup")]
    public Transform potSnapPoint; 
    public PortablePot placedPot;  

    [Header("Visual Indicators")]
    public GameObject ghostCylinder; 

    [Header("UI References")]
    public GameObject uiPanel;        
    public Transform buttonContainer; 
    public GameObject buttonPrefab;   
    public TMP_Text statusText;

    private bool isMenuOpen = false;

    void Update()
    {
        // 1. MEMORY FIX: If the table thinks it has a pot, but the player is holding it, clear the table's memory
        if (placedPot != null && PlayerInventory.instance.heldPot == placedPot)
        {
            placedPot = null;
            if (isMenuOpen) CloseMenu(); // Force close the menu if they snatch the pot
        }

        // 2. Controls the Ghost Cylinder visibility
        if (ghostCylinder != null)
        {
            bool shouldShowGhost = (placedPot == null && PlayerInventory.instance != null && PlayerInventory.instance.isHoldingPot);
            if (ghostCylinder.activeSelf != shouldShowGhost) ghostCylinder.SetActive(shouldShowGhost);
        }
    }

    public void Interact()
    {
        // CASE 1: Player is holding a pot and table is empty -> Place it
        if (PlayerInventory.instance.isHoldingPot && placedPot == null)
        {
            PlacePot();
            return;
        }

        // CASE 2: Pot is already on the table -> Open/Close the Cooking Menu
        if (placedPot != null)
        {
            if (placedPot.state != PortablePot.CookingState.Empty) 
            {
                Debug.Log("This pot already has food!");
                return;
            }

            isMenuOpen = !isMenuOpen;
            if (isMenuOpen) OpenMenu();
            else CloseMenu();
        }
    }

    void PlacePot()
    {
        PortablePot pot = PlayerInventory.instance.heldPot;
        
        pot.transform.SetParent(null);
        pot.transform.position = potSnapPoint.position;
        pot.transform.rotation = potSnapPoint.rotation;
        
        placedPot = pot;
        PlayerInventory.instance.DropPot(); // Clear from hand
    }

    void OpenMenu()
    {
        uiPanel.SetActive(true);
        
        // Unlock cursor for menu navigation (PC Test)
        #if UNITY_EDITOR
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        #endif

        // Clear previous buttons
        foreach (Transform child in buttonContainer) Destroy(child.gameObject);

        if (PlayerInventory.instance.unlockedRecipes.Count == 0)
        {
            if (statusText) statusText.text = "No recipes learned yet!";
            return;
        }

        if (statusText) statusText.text = "Choose a dish to prepare:";

        // Generate buttons for each learned recipe
        foreach (Recipe r in PlayerInventory.instance.unlockedRecipes)
        {
            GameObject btn = Instantiate(buttonPrefab, buttonContainer);
            btn.GetComponentInChildren<TMP_Text>().text = r.recipeName;
            btn.GetComponent<Button>().onClick.AddListener(() => PrepMeal(r));
        }
    }

    void PrepMeal(Recipe dish)
    {
        if (placedPot != null)
        {
            placedPot.SetupPot(dish); 
            CloseMenu();
        }
    }

    // FIXED: Made public so the "Done" button can find it in the Inspector
    public void CloseMenu()
    {
        uiPanel.SetActive(false);
        isMenuOpen = false;
        
        #if UNITY_EDITOR
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        #endif
    }
}