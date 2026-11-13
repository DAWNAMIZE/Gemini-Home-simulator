using UnityEngine;
using UnityEngine.UI;

public class InventorySlot : MonoBehaviour
{
    public int slotIndex; // We will number these 0, 1, 2, 3... in the Inspector

    void Awake()
    {
        EnsureClickable();
    }

    /// <summary>
    /// Older scene slots had no persistent OnClick wiring. Attach the Button
    /// listener at runtime so every slot opens the Equip / Drop panel.
    /// </summary>
    private void EnsureClickable()
    {
        Button button = GetComponent<Button>();
        if (button == null) button = GetComponentInChildren<Button>(true);

        if (button == null)
        {
            // The slot needs a graphic to receive raycasts.
            if (GetComponent<Graphic>() == null)
            {
                Image image = gameObject.AddComponent<Image>();
                image.color = new Color(0f, 0f, 0f, 0.01f);
            }

            button = gameObject.AddComponent<Button>();
        }

        button.onClick.RemoveListener(OnSlotClicked);
        button.onClick.AddListener(OnSlotClicked);
    }

    // This runs when you tap the item slot on your phone screen
    public void OnSlotClicked()
    {
        if (PlayerInventory.instance != null)
        {
            PlayerInventory.instance.SelectSlot(slotIndex);
        }
    }
}
