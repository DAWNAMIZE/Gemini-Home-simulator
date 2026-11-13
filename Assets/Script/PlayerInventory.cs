using System.Collections.Generic;
using UnityEngine;

public class PlayerInventory : MonoBehaviour
{
    public static PlayerInventory instance;

    [Header("Recipes")]
    public List<Recipe> unlockedRecipes = new List<Recipe>();

    [Header("Backpack Items")]
    public List<GameObject> backpackItems = new List<GameObject>();

    [Header("Holding & Dropping")]
    public Transform playerHand;
    public Transform dropSpawnPoint;

    public int selectedSlotIndex = -1;
    public GameObject currentlyHeldModel;

    private bool heldModelIsTemporary;

    public bool IsHandEmpty => currentlyHeldModel == null;
    public bool HasItemInHand => currentlyHeldModel != null;

    void Awake()
    {
        if (instance == null) instance = this;
        else Destroy(gameObject);

        EnsureReferences();
    }

    void Start()
    {
        EnsureReferences();
        if (BackpackUIManager.instance != null) BackpackUIManager.instance.RefreshUI();
    }

    // ------------------------------------------------------------------
    // SETUP
    // ------------------------------------------------------------------

    private void EnsureReferences()
    {
        if (playerHand == null)
        {
            foreach (Transform child in GetComponentsInChildren<Transform>(true))
            {
                if (child.name == "HoldPosition")
                {
                    playerHand = child;
                    break;
                }
            }
        }

        if (dropSpawnPoint == null)
        {
            foreach (Transform child in GetComponentsInChildren<Transform>(true))
            {
                if (child.name == "DropPosition")
                {
                    dropSpawnPoint = child;
                    break;
                }
            }
        }

        if (playerHand == null && Camera.main != null)
            playerHand = Camera.main.transform;
    }

    public void LearnRecipe(Recipe newRecipe)
    {
        if (newRecipe == null) return;

        if (!unlockedRecipes.Contains(newRecipe))
        {
            unlockedRecipes.Add(newRecipe);
            Debug.Log("Learned new recipe: " + newRecipe.name);
        }
    }

    // ------------------------------------------------------------------
    // BACKPACK
    // ------------------------------------------------------------------

    public void AddItemToBackpack(GameObject itemPrefabToAdd)
    {
        if (itemPrefabToAdd == null) return;

        if (backpackItems.Count >= 4)
        {
            ToastUI.Show("Inventory is full!");
            Debug.Log("Inventory is full! Cannot pick up " + itemPrefabToAdd.name);
            return;
        }

        backpackItems.Add(itemPrefabToAdd);
        Debug.Log("Successfully added " + itemPrefabToAdd.name + " to the backpack!");

        if (BackpackUIManager.instance != null) BackpackUIManager.instance.RefreshUI();
    }

    public bool PickUpWeapon(GameObject weapon)
    {
        if (weapon == null) return false;

        if (!IsHandEmpty)
        {
            ToastUI.Show("Your hands are full - drop what you are holding first.");
            return false;
        }

        if (backpackItems.Count >= 4)
        {
            ToastUI.Show("Inventory is full!");
            return false;
        }

        if (backpackItems.Contains(weapon)) return false;

        WeaponInstance weaponInstance = WeaponInstance.Attach(weapon);
        if (weaponInstance == null) return false;

        weapon.transform.SetParent(transform, false);
        weapon.SetActive(false);

        backpackItems.Add(weapon);

        if (BackpackUIManager.instance != null) BackpackUIManager.instance.RefreshUI();

        ToastUI.Show("Picked up " + weaponInstance.weaponName);
        return true;
    }

    public bool RestoreWeaponByName(string weaponName)
    {
        if (string.IsNullOrEmpty(weaponName)) return false;
        if (FindSlotByItemName(weaponName) >= 0) return true;

        WeaponPickup[] pickups = Object.FindObjectsByType<WeaponPickup>(
            FindObjectsInactive.Include, FindObjectsSortMode.None);

        foreach (WeaponPickup pickup in pickups)
        {
            if (pickup == null) continue;

            WeaponInstance weapon = WeaponInstance.Attach(pickup.gameObject);
            string name = weapon != null ? weapon.weaponName : pickup.gameObject.name;

            if (string.Equals(name, weaponName, System.StringComparison.OrdinalIgnoreCase))
                return PickUpWeapon(pickup.gameObject);
        }

        return false;
    }

    public bool RestoreItemByName(string itemName)
    {
        if (string.IsNullOrEmpty(itemName)) return false;
        if (FindSlotByItemName(itemName) >= 0) return true;

        GameObject prefab = Resources.Load<GameObject>(itemName);

        if (prefab == null)
        {
            ItemPickup[] pickups = Object.FindObjectsByType<ItemPickup>(
                FindObjectsInactive.Include, FindObjectsSortMode.None);

            foreach (ItemPickup pickup in pickups)
            {
                if (pickup == null || pickup.itemPrefab == null) continue;
                if (string.Equals(pickup.itemPrefab.name, itemName,
                        System.StringComparison.OrdinalIgnoreCase))
                {
                    prefab = pickup.itemPrefab;
                    break;
                }
            }
        }

        if (prefab == null) return false;

        AddItemToBackpack(prefab);
        return true;
    }

    // ------------------------------------------------------------------
    // SLOT SELECTION / EQUIP / DROP
    // ------------------------------------------------------------------

    /// <summary>
    /// Tapping a slot opens the Equip / Drop action panel.
    /// </summary>
    public void SelectSlot(int index)
    {
        if (index < 0 || index >= backpackItems.Count) return;

        selectedSlotIndex = index;
        SlotActionUI.Show(index);
    }

    public void EquipSlot(int index)
    {
        if (index < 0 || index >= backpackItems.Count) return;

        GameObject item = backpackItems[index];
        if (item == null) return;

        if (currentlyHeldModel != null)
        {
            if (currentlyHeldModel == item)
            {
                ToastUI.Show("Already equipped.");
                return;
            }

            bool heldIsInventoryWeapon =
                !heldModelIsTemporary && backpackItems.Contains(currentlyHeldModel);

            if (!heldIsInventoryWeapon)
            {
                ToastUI.Show("Your hands are full - drop what you are holding first.");
                return;
            }

            UnequipHeldWeapon();
        }

        if (item.scene.IsValid())
        {
            // Scene weapon instances are reused directly.
            item.SetActive(true);
            ApplyHeldTransform(item);

            currentlyHeldModel = item;
            selectedSlotIndex = index;
            heldModelIsTemporary = false;

            WeaponInstance weapon = WeaponInstance.Attach(item);
            if (weapon != null)
            {
                ToastUI.Show("Equipped " + weapon.weaponName);
            }
            else
            {
                ToastUI.Show("Equipped " + item.name);
            }
        }
        else
        {
            // Prefab-only items are instantiated into the hand.
            if (playerHand == null)
            {
                ToastUI.Show("No hand position found.");
                return;
            }

            GameObject instance = Instantiate(item, playerHand.position, playerHand.rotation);
            instance.name = item.name;
            ApplyHeldTransform(instance);

            currentlyHeldModel = instance;
            selectedSlotIndex = index;
            heldModelIsTemporary = true;

            ToastUI.Show("Equipped " + item.name);
        }

        SetDropButtonVisible(true);
        if (WeaponHUD.instance != null) WeaponHUD.instance.Refresh();
    }

    public void UnequipHeldWeapon()
    {
        if (currentlyHeldModel == null) return;

        GameObject held = currentlyHeldModel;

        if (!heldModelIsTemporary)
        {
            held.transform.SetParent(transform, false);
            SetPhysicsIdle(held);
            held.SetActive(false);
        }
        else
        {
            Destroy(held);
        }

        currentlyHeldModel = null;
        selectedSlotIndex = -1;
        heldModelIsTemporary = false;

        SetDropButtonVisible(false);
        if (WeaponHUD.instance != null) WeaponHUD.instance.Refresh();
    }

    public void DropSlot(int index)
    {
        if (index < 0 || index >= backpackItems.Count) return;

        GameObject item = backpackItems[index];
        if (item == null)
        {
            backpackItems.RemoveAt(index);
            if (BackpackUIManager.instance != null) BackpackUIManager.instance.RefreshUI();
            return;
        }

        bool wasHeld = item == currentlyHeldModel;
        GameObject droppedInstance = null;

        if (item.scene.IsValid())
        {
            droppedInstance = item;
        }
        else if (wasHeld && heldModelIsTemporary)
        {
            droppedInstance = currentlyHeldModel;
        }
        else
        {
            Vector3 position = GetDropPosition();
            Quaternion rotation = GetDropRotation();
            droppedInstance = Instantiate(item, position, rotation);
            droppedInstance.name = item.name;
        }

        backpackItems.RemoveAt(index);

        if (wasHeld)
        {
            currentlyHeldModel = null;
            heldModelIsTemporary = false;
        }

        if (selectedSlotIndex >= backpackItems.Count) selectedSlotIndex = -1;

        if (droppedInstance != null)
        {
            droppedInstance.SetActive(true);
            PlaceInWorld(droppedInstance);
            ToastUI.Show("Dropped " + GetItemIdentifier(droppedInstance));
        }

        SetDropButtonVisible(false);
        if (BackpackUIManager.instance != null) BackpackUIManager.instance.RefreshUI();
        if (WeaponHUD.instance != null) WeaponHUD.instance.Refresh();
        SlotActionUI.Hide();
    }

    // ------------------------------------------------------------------
    // PHYSICAL ITEMS (POT / PLATE)
    // ------------------------------------------------------------------

    /// <summary>
    /// Puts a world object into the player's hand. Refuses if the hand is
    /// already occupied so a second pickup can never stack items.
    /// </summary>
    public bool TryHoldPhysicalItem(GameObject physicalObject)
    {
        if (physicalObject == null) return false;

        if (currentlyHeldModel != null)
        {
            ToastUI.Show("Your hands are full - drop what you are holding first.");
            return false;
        }

        currentlyHeldModel = physicalObject;
        selectedSlotIndex = -1;
        heldModelIsTemporary = false;

        ApplyHeldTransform(physicalObject);
        SetDropButtonVisible(true);

        if (WeaponHUD.instance != null) WeaponHUD.instance.Refresh();
        return true;
    }

    /// <summary>Kept for existing callers; returns false when the hand is full.</summary>
    public void HoldPhysicalItem(GameObject physicalObject)
    {
        TryHoldPhysicalItem(physicalObject);
    }

    public void ClearHandIfHolding(GameObject physicalObject)
    {
        if (currentlyHeldModel == physicalObject) ClearHand();
    }

    public void ClearHand()
    {
        currentlyHeldModel = null;
        selectedSlotIndex = -1;
        heldModelIsTemporary = false;

        SetDropButtonVisible(false);
        if (WeaponHUD.instance != null) WeaponHUD.instance.Refresh();
    }

    /// <summary>
    /// Phone drop button / legacy drop API. Food goes through FoodDish.Drop so
    /// tray and spawn-point bookkeeping stays correct.
    /// </summary>
    public void DropSelectedItem()
    {
        GameObject dropped = currentlyHeldModel;
        if (dropped == null) return;

        FoodDish food = dropped.GetComponentInParent<FoodDish>();
        if (food != null)
        {
            food.Drop();
            return;
        }

        bool isInventoryItem = !heldModelIsTemporary &&
            selectedSlotIndex >= 0 &&
            selectedSlotIndex < backpackItems.Count &&
            backpackItems[selectedSlotIndex] == dropped;

        if (isInventoryItem)
        {
            DropSlot(selectedSlotIndex);
            return;
        }

        if (heldModelIsTemporary && selectedSlotIndex >= 0)
        {
            DropSlot(selectedSlotIndex);
            return;
        }

        PlaceInWorld(dropped);
        ClearHand();
        ToastUI.Show("Dropped " + dropped.name);
    }

    // ------------------------------------------------------------------
    // SHOOTING
    // ------------------------------------------------------------------

    public void FireEquippedWeapon()
    {
        if (currentlyHeldModel == null)
        {
            ToastUI.Show("Equip a pistol first.");
            return;
        }

        WeaponInstance weapon = currentlyHeldModel.GetComponent<WeaponInstance>();
        if (weapon == null)
        {
            ToastUI.Show(currentlyHeldModel.name + " is not a firearm.");
            return;
        }

        weapon.TryFire();
    }

    // ------------------------------------------------------------------
    // DISPLAY HELPERS
    // ------------------------------------------------------------------

    public string GetSlotDisplayName(int index)
    {
        if (index < 0 || index >= backpackItems.Count) return "Empty";
        return GetItemIdentifier(backpackItems[index]);
    }

    public string GetItemIdentifier(GameObject item)
    {
        if (item == null) return string.Empty;

        WeaponInstance weapon = item.GetComponent<WeaponInstance>();
        if (weapon != null) return weapon.weaponName;

        ItemDetails details = item.GetComponent<ItemDetails>();
        if (details != null && details.itemRef != null) return details.itemRef.itemName;

        return item.name;
    }

    public int FindSlotByItemName(string itemName)
    {
        if (string.IsNullOrEmpty(itemName)) return -1;

        for (int i = 0; i < backpackItems.Count; i++)
        {
            if (backpackItems[i] == null) continue;

            string identifier = GetItemIdentifier(backpackItems[i]);
            if (string.Equals(identifier, itemName, System.StringComparison.OrdinalIgnoreCase) ||
                string.Equals(backpackItems[i].name, itemName, System.StringComparison.OrdinalIgnoreCase))
                return i;
        }

        return -1;
    }

    public bool LooksLikeWeapon(string objectName)
    {
        if (string.IsNullOrEmpty(objectName)) return false;

        string lower = objectName.ToLowerInvariant();
        return lower.StartsWith("pistol") ||
               lower.StartsWith("ax") ||
               lower.StartsWith("dagger") ||
               lower.StartsWith("sword") ||
               lower.StartsWith("hammer");
    }

    public bool ConsumeItemFromBackpack(GameObject prefabToFind)
    {
        if (prefabToFind == null) return false;

        ItemDetails targetDetails = prefabToFind.GetComponent<ItemDetails>();
        if (targetDetails == null || targetDetails.itemRef == null)
        {
            Debug.LogError("The target prefab is missing an ItemReference asset! Please assign one in the Inspector.");
            return false;
        }

        ItemReference targetRef = targetDetails.itemRef;

        for (int i = 0; i < backpackItems.Count; i++)
        {
            if (backpackItems[i] != null)
            {
                ItemDetails bagItemDetails = backpackItems[i].GetComponent<ItemDetails>();

                if (bagItemDetails != null && bagItemDetails.itemRef == targetRef)
                {
                    Debug.Log("MATCH FOUND! Consuming [" + targetRef.itemName + "]");
                    backpackItems.RemoveAt(i);

                    if (selectedSlotIndex >= backpackItems.Count) selectedSlotIndex = -1;
                    if (BackpackUIManager.instance != null) BackpackUIManager.instance.RefreshUI();

                    return true;
                }
            }
        }

        Debug.Log("FAILED -> Could not find the stable reference for [" + targetRef.itemName + "] in the bag.");
        return false;
    }

    // ------------------------------------------------------------------
    // INTERNAL
    // ------------------------------------------------------------------

    private void ApplyHeldTransform(GameObject held)
    {
        if (held == null) return;

        if (playerHand != null)
        {
            held.transform.SetParent(playerHand, false);
            held.transform.localPosition = Vector3.zero;
            held.transform.localRotation = Quaternion.identity;
        }

        Rigidbody rb = held.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = true;
            rb.useGravity = false;
#if UNITY_6000_0_OR_NEWER
            rb.linearVelocity = Vector3.zero;
#else
            rb.velocity = Vector3.zero;
#endif
        }

        Collider[] colliders = held.GetComponentsInChildren<Collider>(true);
        foreach (Collider collider in colliders)
            collider.enabled = false;

        held.layer = 2;
    }

    private void SetPhysicsIdle(GameObject held)
    {
        if (held == null) return;

        Rigidbody rb = held.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = true;
            rb.useGravity = false;
        }

        Collider[] colliders = held.GetComponentsInChildren<Collider>(true);
        foreach (Collider collider in colliders)
            collider.enabled = false;
    }

    private void PlaceInWorld(GameObject dropped)
    {
        if (dropped == null) return;

        dropped.transform.SetParent(null, true);
        dropped.transform.SetPositionAndRotation(GetDropPosition(), GetDropRotation());

        FoodDish food = dropped.GetComponentInParent<FoodDish>();
        if (food != null)
        {
            food.Drop();
            return;
        }

        Rigidbody rb = dropped.GetComponent<Rigidbody>();
        if (rb == null) rb = dropped.AddComponent<Rigidbody>();
        rb.isKinematic = false;
        rb.useGravity = true;
        rb.interpolation = RigidbodyInterpolation.Interpolate;

        Collider[] colliders = dropped.GetComponentsInChildren<Collider>(true);
        foreach (Collider collider in colliders)
            collider.enabled = true;

        dropped.layer = 0;
    }

    private Vector3 GetDropPosition()
    {
        if (dropSpawnPoint != null) return dropSpawnPoint.position;
        if (playerHand != null) return playerHand.position + playerHand.forward * 0.5f;
        return transform.position + transform.forward * 0.7f + Vector3.up;
    }

    private Quaternion GetDropRotation()
    {
        if (dropSpawnPoint != null) return dropSpawnPoint.rotation;
        return transform.rotation;
    }

    private static void SetDropButtonVisible(bool visible)
    {
        if (PhoneUIManager.instance != null && PhoneUIManager.instance.dropButton != null)
            PhoneUIManager.instance.dropButton.SetActive(visible);
    }
}
