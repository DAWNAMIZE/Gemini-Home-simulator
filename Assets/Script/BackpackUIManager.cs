using UnityEngine;
using UnityEngine.UI;

public class BackpackUIManager : MonoBehaviour
{
    [Header("UI References")]
    public GameObject backpackPanel;
    public Transform gridContainer;
    public GameObject slotPrefab; // Your grey square prefab

    void Start()
    {
        // Ensure the bag is closed when the game starts
        if (backpackPanel != null) backpackPanel.SetActive(false);
    }

    public void ToggleBackpack()
    {
        if (backpackPanel == null) return;

        bool isOpen = !backpackPanel.activeSelf;
        backpackPanel.SetActive(isOpen);

        if (isOpen)
        {
            RefreshUI();
        }
    }

    public void RefreshUI()
    {
        // 1. Destroy old icons so they don't duplicate
        foreach (Transform child in gridContainer)
        {
            Destroy(child.gameObject);
        }

        if (PlayerInventory.instance == null) return;

        // 2. Loop through every item in the player's inventory
        for (int i = 0; i < PlayerInventory.instance.backpackItems.Count; i++)
        {
            GameObject item = PlayerInventory.instance.backpackItems[i];
            if (item == null) continue;

            // 3. Spawn a new UI square
            GameObject newSlot = Instantiate(slotPrefab, gridContainer);
            Image slotImage = newSlot.GetComponent<Image>();
            
            // Wire up your existing InventorySlot logic so tapping works!
            InventorySlot slotLogic = newSlot.GetComponent<InventorySlot>();
            if (slotLogic != null)
            {
                slotLogic.slotIndex = i; 
            }

            // 4. The Magic: Read the icon DIRECTLY from the item's ItemDetails script!
            ItemDetails details = item.GetComponent<ItemDetails>();

            if (details != null && slotImage != null && details.itemIcon != null)
            {
                slotImage.sprite = details.itemIcon;
                slotImage.color = Color.white; // Remove the dark tint
            }
            else if (slotImage != null)
            {
                // Fallback just in case you forgot to assign an icon
                slotImage.color = new Color(0.3f, 0.3f, 0.3f, 1f); 
                Debug.LogWarning("Item " + item.name + " is missing an ItemDetails script or Icon!");
            }
        }
    }
}