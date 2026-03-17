using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PrepStation : MonoBehaviour, IInteractable
{
    [Header("Connections")]
    public PotSnapPoint potSocket; 

    [Header("UI References")]
    public GameObject uiPanel;        
    public Transform buttonContainer; 
    public GameObject buttonPrefab;   
    public TMP_Text statusText;

    private bool isMenuOpen = false;
    private bool wasOccupied = false; // Tracks the socket's status from the previous frame

    void Update()
    {
        bool isCurrentlyOccupied = (potSocket != null && potSocket.IsOccupied && potSocket.CurrentPot != null);

        // 1. AUTO-OPEN: If a pot just snapped into the socket this exact frame
        if (!wasOccupied && isCurrentlyOccupied && !isMenuOpen)
        {
            if (potSocket.CurrentPot.state == PortablePot.CookingState.Empty)
            {
                isMenuOpen = true;
                OpenMenu();
            }
        }

        // 2. AUTO-CLOSE: If the pot is snatched away while the menu is open
        if (isMenuOpen && !isCurrentlyOccupied)
        {
            CloseMenu();
        }

        // 3. BILLBOARD EFFECT: Make the floating UI always face the player's camera
        if (isMenuOpen && uiPanel.activeSelf && Camera.main != null)
        {
            uiPanel.transform.LookAt(uiPanel.transform.position + Camera.main.transform.rotation * Vector3.forward,
                                     Camera.main.transform.rotation * Vector3.up);
        }

        // Remember the state for the next frame
        wasOccupied = isCurrentlyOccupied; 
    }

    public void Interact()
    {
        // Helper: Forward the pot to the socket if the player aims at the cutting board
        if (PlayerInventory.instance.isHoldingPot && potSocket != null && !potSocket.IsOccupied)
        {
            potSocket.Interact(); 
            return;
        }

        if (potSocket == null || !potSocket.IsOccupied || potSocket.CurrentPot == null)
        {
            Debug.Log("Place a pot on the counter first!");
            return;
        }

        // Manual open/close fallback if the pot is already sitting there
        if (potSocket.CurrentPot.state != PortablePot.CookingState.Empty) 
        {
            Debug.Log("This pot already has food!");
            return;
        }

        isMenuOpen = !isMenuOpen;
        if (isMenuOpen) OpenMenu();
        else CloseMenu();
    }

    void OpenMenu()
    {
        uiPanel.SetActive(true);
        
        #if UNITY_EDITOR
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        #endif

        foreach (Transform child in buttonContainer) Destroy(child.gameObject);

        if (PlayerInventory.instance.unlockedRecipes.Count == 0)
        {
            if (statusText) statusText.text = "No recipes learned yet!";
            return;
        }

        if (statusText) statusText.text = "Choose a dish to prepare:";

        foreach (Recipe r in PlayerInventory.instance.unlockedRecipes)
        {
            GameObject btn = Instantiate(buttonPrefab, buttonContainer);
            btn.GetComponentInChildren<TMP_Text>().text = r.recipeName;
            btn.GetComponent<Button>().onClick.AddListener(() => PrepMeal(r));
        }
    }

    void PrepMeal(Recipe dish)
    {
        if (potSocket != null && potSocket.CurrentPot != null)
        {
            potSocket.CurrentPot.SetupPot(dish); 
            CloseMenu();
        }
    }

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