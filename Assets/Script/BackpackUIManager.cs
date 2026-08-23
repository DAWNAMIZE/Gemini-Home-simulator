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
        RefreshUI(); 
    }

    public void RefreshUI()
    {
        if (PlayerInventory.instance == null) return;

        for (int i = 0; i < 4; i++)
        {
            if (i < PlayerInventory.instance.backpackItems.Count)
            {
                GameObject item = PlayerInventory.instance.backpackItems[i];
                ItemDetails details = item.GetComponent<ItemDetails>();

                // Check the stable reference for the icon
                if (details != null && details.itemIcon != null)
                {
                    slotIcons[i].sprite = details.itemIcon;
                    slotIcons[i].color = Color.white; 
                }
            }
            else
            {
                slotIcons[i].sprite = null; 
                slotIcons[i].color = new Color(0, 0, 0, 0); 
            }
        }
    }
}