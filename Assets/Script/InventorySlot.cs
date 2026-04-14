using UnityEngine;
using UnityEngine.UI;

public class InventorySlot : MonoBehaviour
{
    public int slotIndex; // We will number these 0, 1, 2, 3... in the Inspector

    // This runs when you tap the item slot on your phone screen
    public void OnSlotClicked()
    {
        if (PlayerInventory.instance != null)
        {
            PlayerInventory.instance.SelectSlot(slotIndex);
        }
    }
}