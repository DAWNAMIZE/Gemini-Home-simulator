using UnityEngine;
using UnityEngine.UI;

public class BackpackUIManager : MonoBehaviour
{
    public static BackpackUIManager instance;

    [Header("Hotbar References")]
    [Tooltip("Drag the Image components from your 4 UI boxes into this array.")]
    public Image[] slotIcons = new Image[4]; 

    void Awake()
    {
        if (instance == null) instance = this;
        else Destroy(gameObject);
    }

    void Start()
    {
        RefreshUI(); // Make sure boxes are empty when the game starts
    }

    public void RefreshUI()
    {
        if (PlayerInventory.instance == null) return;

        // Loop through our 4 fixed UI boxes
        for (int i = 0; i < 4; i++)
        {
            // Check if the player actually has an item for this box
            if (i < PlayerInventory.instance.backpackItems.Count)
            {
                // We HAVE an item! Let's get its picture.
                GameObject item = PlayerInventory.instance.backpackItems[i];
                ItemDetails details = item.GetComponent<ItemDetails>();

                if (details != null && details.itemIcon != null)
                {
                    slotIcons[i].sprite = details.itemIcon;
                    slotIcons[i].color = Color.white; // Make the picture fully visible
                }
            }
            else
            {
                // We DO NOT have an item. The slot should look empty.
                // This makes the picture invisible, leaving just your empty grey box template showing.
                slotIcons[i].sprite = null; 
                slotIcons[i].color = new Color(0, 0, 0, 0); 
            }
        }
    }
}