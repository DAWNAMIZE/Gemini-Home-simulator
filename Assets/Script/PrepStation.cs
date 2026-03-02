using UnityEngine;
using UnityEngine.UI;
using TMPro; // Need this for Text

public class PrepStation : MonoBehaviour, IInteractable
{
    [Header("UI System")]
    public GameObject uiPanel;
    public Transform buttonContainer; // The empty area where buttons go
    public GameObject buttonPrefab;   // A template button
    public TMP_Text statusText;       // To say "No Recipes Found!"

    [Header("Pot Spawning")]
    public Transform potSpawnPoint;
    public GameObject potPrefab;

    private bool isOpen = false;

    public void Interact()
    {
        isOpen = !isOpen;
        
        if (isOpen)
        {
            OpenMenu();
        }
        else
        {
            CloseMenu();
        }
    }

    void OpenMenu()
    {
        uiPanel.SetActive(true);
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        // 1. Clear old buttons (so we don't get duplicates)
        foreach (Transform child in buttonContainer)
        {
            Destroy(child.gameObject);
        }

        // 2. Check if we know any recipes
        if (PlayerInventory.instance.unlockedRecipes.Count == 0)
        {
            statusText.text = "You don't know any recipes yet.\nFind cookbooks in the world!";
            return;
        }
        
        statusText.text = "Select a Meal to Prepare:";

        // 3. Create a button for each Unlocked Recipe
        foreach (Recipe recipe in PlayerInventory.instance.unlockedRecipes)
        {
            GameObject newBtn = Instantiate(buttonPrefab, buttonContainer);
            
            // Set Button Text (Assuming the button has a TMP_Text child)
            newBtn.GetComponentInChildren<TMP_Text>().text = recipe.recipeName;

            // Add Click Event
            newBtn.GetComponent<Button>().onClick.AddListener(() => PrepareFood(recipe));
        }
    }

    void CloseMenu()
    {
        uiPanel.SetActive(false);
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    // Called when you click a recipe button
    void PrepareFood(Recipe dish)
    {
        // Spawn the pot
        GameObject newPotObj = Instantiate(potPrefab, potSpawnPoint.position, potSpawnPoint.rotation);
        newPotObj.GetComponent<PortablePot>().SetupPot(dish);

        Debug.Log("Prepared: " + dish.recipeName);
        CloseMenu();
    }
}