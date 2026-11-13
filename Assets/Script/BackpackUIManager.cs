using UnityEngine;
using UnityEngine.UI;

public class BackpackUIManager : MonoBehaviour
{
    public static BackpackUIManager instance;

    [Header("Hotbar References")]
    [Tooltip("Drag the Image components from your 4 UI boxes into this array.")]
    public Image[] slotIcons = new Image[4];

    private static readonly Color WeaponSquareColor = new Color(0.16f, 0.16f, 0.19f, 1f);
    private static readonly Color ItemSquareColor = new Color(0.55f, 0.55f, 0.55f, 1f);

    void Awake()
    {
        if (instance == null) instance = this;
        else
        {
            Destroy(gameObject);
            return;
        }

        EnsureSlotIcons();
    }

    void Start()
    {
        RefreshUI();
    }

    /// <summary>
    /// Finds the four icon Images from the InventorySlot components when the
    /// inspector array was not filled in.
    /// </summary>
    private void EnsureSlotIcons()
    {
        if (slotIcons == null || slotIcons.Length < 4)
        {
            Image[] resized = new Image[4];
            if (slotIcons != null)
            {
                for (int i = 0; i < slotIcons.Length && i < 4; i++)
                    resized[i] = slotIcons[i];
            }
            slotIcons = resized;
        }

        InventorySlot[] slots = GetComponentsInChildren<InventorySlot>(true);
        foreach (InventorySlot slot in slots)
        {
            if (slot == null) continue;

            int index = slot.slotIndex;
            if (index < 0 || index >= slotIcons.Length) continue;
            if (slotIcons[index] != null) continue;

            Image icon = slot.GetComponent<Image>();
            if (icon == null) icon = slot.GetComponentInChildren<Image>(true);
            if (icon != null) slotIcons[index] = icon;
        }
    }

    public void RefreshUI()
    {
        if (PlayerInventory.instance == null) return;
        EnsureSlotIcons();

        for (int i = 0; i < slotIcons.Length && i < 4; i++)
        {
            Image icon = slotIcons[i];
            if (icon == null) continue;

            if (i >= PlayerInventory.instance.backpackItems.Count)
            {
                ClearSlot(icon);
                continue;
            }

            GameObject item = PlayerInventory.instance.backpackItems[i];
            if (item == null)
            {
                ClearSlot(icon);
                continue;
            }

            // Weapons always get the dark-square treatment.
            bool isWeapon = item.GetComponent<WeaponInstance>() != null ||
                            PlayerInventory.instance.LooksLikeWeapon(item.name);

            if (isWeapon)
            {
                icon.sprite = null;
                icon.preserveAspect = false;
                icon.color = WeaponSquareColor;
                continue;
            }

            Sprite sprite = null;
            ItemDetails details = item.GetComponent<ItemDetails>();
            if (details != null) sprite = details.itemIcon;

            if (sprite != null)
            {
                icon.sprite = sprite;
                icon.color = Color.white;
                icon.preserveAspect = true;
                continue;
            }

            icon.sprite = null;
            icon.preserveAspect = false;
            icon.color = ItemSquareColor;
        }
    }

    private static void ClearSlot(Image icon)
    {
        icon.sprite = null;
        icon.color = new Color(0f, 0f, 0f, 0f);
    }
}
