using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class BackpackUIManager : MonoBehaviour
{
    public static BackpackUIManager instance;

    [Header("Hotbar Slots")]
    [Tooltip("The Image on each of the four hotbar slots. These four slots are styled at runtime.")]
    public Image[] slotIcons = new Image[4];

    [Header("Custom Icons (Optional)")]
    [Tooltip("Optional icon for each hotbar slot. When assigned, it is shown in that slot instead of the item's own icon.")]
    public Sprite[] slotCustomIcons = new Sprite[4];

    [Tooltip("Optional icons matched by item name. These take priority over everything else, so you can give any item a custom icon without editing its prefab.")]
    public List<ItemIconOverride> itemIconOverrides = new List<ItemIconOverride>();

    [Tooltip("Icon shown for items that have no icon of their own and no override. Leave empty to show the item's initials instead.")]
    public Sprite fallbackItemIcon;

    [Header("Slot Style")]
    [Tooltip("Border color of an idle slot.")]
    public Color frameColor = new Color(0.12f, 0.12f, 0.15f, 1f);

    [Tooltip("Border color of the selected / equipped slot.")]
    public Color selectedFrameColor = new Color(1f, 0.78f, 0.25f, 1f);

    [Tooltip("Inner color of every slot.")]
    public Color backgroundColor = new Color(0.22f, 0.22f, 0.26f, 1f);

    [Tooltip("Tint applied to item icons.")]
    public Color iconTint = Color.white;

    [Tooltip("Color of the initials shown for items without an icon.")]
    public Color initialsColor = new Color(0.92f, 0.92f, 0.95f, 1f);

    [Tooltip("Color of the small slot number in the corner.")]
    public Color slotNumberColor = new Color(1f, 1f, 1f, 0.35f);

    [Min(0f)]
    [Tooltip("How thick the border is, in slot pixels.")]
    public float frameInset = 4f;

    [Min(0f)]
    [Tooltip("Space between the slot edge and the item icon, in slot pixels.")]
    public float iconPadding = 12f;

    [Tooltip("Show a small 1-4 label in the corner of each slot.")]
    public bool showSlotNumbers = true;

    [Tooltip("Show the item's initials when it has no icon.")]
    public bool showInitialsWhenNoIcon = true;

    private const int VisibleSlotCount = 4;

    /// <summary>
    /// One optional icon that replaces the icon of a named item.
    /// The name is matched against PlayerInventory.GetItemIdentifier.
    /// </summary>
    [Serializable]
    public class ItemIconOverride
    {
        [Tooltip("Item name, for example 'Apple' or 'Wooden Sword'.")]
        public string itemName;

        [Tooltip("Icon shown whenever this item is in the hotbar.")]
        public Sprite icon;
    }

    private sealed class SlotVisual
    {
        public Image Frame;
        public Image Background;
        public Image Icon;
        public TMP_Text Initials;
        public TMP_Text SlotNumber;
    }

    private readonly Dictionary<int, SlotVisual> slotVisuals = new Dictionary<int, SlotVisual>();
    private readonly GameObject[] lastItems = new GameObject[VisibleSlotCount];
    private bool hasItemsSnapshot;
    private int lastSelectedSlot = int.MinValue;

    void Awake()
    {
        if (instance == null) instance = this;
        else
        {
            Destroy(gameObject);
            return;
        }

        EnsureSlotIcons();
        EnsureSlotVisuals();
    }

    void Start()
    {
        RefreshUI();
    }

    void Update()
    {
        RefreshSelectionHighlight();

        if (PlayerInventory.instance == null) return;

        // Keep the hotbar in sync even if a pickup / drop forgets to call
        // RefreshUI directly.
        if (!hasItemsSnapshot || ItemsChanged())
            RefreshUI();
    }

    /// <summary>
    /// Finds the four slot Images from the InventorySlot components when the
    /// inspector array was not filled in.
    /// </summary>
    private void EnsureSlotIcons()
    {
        slotIcons = EnsureArraySize(slotIcons, VisibleSlotCount);
        slotCustomIcons = EnsureArraySize(slotCustomIcons, VisibleSlotCount);

        InventorySlot[] slots = GetComponentsInChildren<InventorySlot>(true);
        foreach (InventorySlot slot in slots)
        {
            if (slot == null) continue;

            int index = slot.slotIndex;
            if (index < 0 || index >= VisibleSlotCount) continue;
            if (slotIcons[index] != null) continue;

            Image icon = slot.GetComponent<Image>();
            if (icon == null) icon = slot.GetComponentInChildren<Image>(true);
            if (icon != null) slotIcons[index] = icon;
        }
    }

    private void EnsureSlotVisuals()
    {
        for (int i = 0; i < VisibleSlotCount && i < slotIcons.Length; i++)
        {
            Image frame = slotIcons[i];
            if (frame == null) continue;

            if (!slotVisuals.TryGetValue(i, out SlotVisual visual) ||
                visual == null ||
                visual.Frame != frame)
            {
                visual = BuildSlotVisual(i, frame);
                slotVisuals[i] = visual;
            }
        }
    }

    /// <summary>
    /// Builds a clean dark frame, an inset background and a centered icon
    /// layer under each existing hotbar slot. Everything is created at runtime
    /// so the scene and prefab stay untouched.
    /// </summary>
    private SlotVisual BuildSlotVisual(int index, Image frame)
    {
        SlotVisual visual = new SlotVisual();
        visual.Frame = frame;
        frame.type = Image.Type.Sliced;

        visual.Background = EnsureImageChild(frame.transform, "Slot Background");
        Stretch(visual.Background.rectTransform);
        visual.Background.sprite = frame.sprite;
        visual.Background.type = Image.Type.Sliced;
        visual.Background.color = backgroundColor;
        visual.Background.raycastTarget = false;

        visual.Icon = EnsureImageChild(frame.transform, "Slot Icon");
        Stretch(visual.Icon.rectTransform);
        visual.Icon.preserveAspect = true;
        visual.Icon.raycastTarget = false;

        visual.Initials = EnsureTextChild(frame.transform, "Slot Initials");
        Stretch(visual.Initials.rectTransform);

        // The prefab already has an empty "Text (TMP)" child. Reuse it as the
        // slot number instead of adding another label.
        TMP_Text slotNumber = FindTextChild(frame.transform, "Text (TMP)");
        if (slotNumber == null)
            slotNumber = EnsureTextChild(frame.transform, "Slot Number");

        visual.SlotNumber = slotNumber;
        TMP_FontAsset font = slotNumber.font;
        if (font != null)
        {
            visual.Initials.font = font;
        }
        else
        {
            TMP_FontAsset defaultFont = TMP_Settings.defaultFontAsset;
            if (defaultFont != null)
            {
                visual.Initials.font = defaultFont;
                slotNumber.font = defaultFont;
            }
        }

        StyleSlotNumber(visual.SlotNumber, index);
        StyleInitials(visual.Initials);

        // Keep the drawing order: frame, background, icon, initials, number.
        visual.Background.transform.SetSiblingIndex(0);
        visual.Icon.transform.SetSiblingIndex(1);
        visual.Initials.transform.SetSiblingIndex(2);
        visual.SlotNumber.transform.SetSiblingIndex(3);

        // Press feedback should tint the inner background, not the frame, so
        // the selected-slot border stays visible.
        Button button = frame.GetComponent<Button>();
        if (button != null) button.targetGraphic = visual.Background;

        return visual;
    }

    public void RefreshUI()
    {
        EnsureSlotIcons();
        EnsureSlotVisuals();

        PlayerInventory inventory = PlayerInventory.instance;

        for (int i = 0; i < VisibleSlotCount; i++)
        {
            GameObject item = null;
            if (inventory != null && i < inventory.backpackItems.Count)
                item = inventory.backpackItems[i];
            lastItems[i] = item;

            if (!slotVisuals.TryGetValue(i, out SlotVisual visual) || visual == null)
                continue;
            if (visual.Frame == null) continue;

            ApplySlotContents(i, visual, item, inventory);
        }

        RefreshSelectionHighlight(true);
        hasItemsSnapshot = true;
    }

    private void ApplySlotContents(int index, SlotVisual visual, GameObject item, PlayerInventory inventory)
    {
        visual.Background.rectTransform.offsetMin = new Vector2(frameInset, frameInset);
        visual.Background.rectTransform.offsetMax = new Vector2(-frameInset, -frameInset);
        visual.Background.sprite = visual.Frame.sprite;
        visual.Background.type = Image.Type.Sliced;
        visual.Background.color = backgroundColor;
        visual.Background.raycastTarget = false;

        visual.Icon.rectTransform.offsetMin = new Vector2(iconPadding, iconPadding);
        visual.Icon.rectTransform.offsetMax = new Vector2(-iconPadding, -iconPadding);
        visual.Icon.color = iconTint;
        visual.Icon.raycastTarget = false;

        visual.Initials.rectTransform.offsetMin = new Vector2(frameInset + 2f, frameInset + 2f);
        visual.Initials.rectTransform.offsetMax = new Vector2(-frameInset - 2f, -frameInset - 2f);
        visual.Initials.color = initialsColor;

        visual.SlotNumber.text = (index + 1).ToString();
        visual.SlotNumber.color = slotNumberColor;
        visual.SlotNumber.gameObject.SetActive(showSlotNumbers);

        if (item == null)
        {
            visual.Icon.sprite = null;
            visual.Icon.enabled = false;
            visual.Initials.text = string.Empty;
            visual.Initials.enabled = false;
            return;
        }

        string displayName = GetDisplayName(item, inventory);
        Sprite icon = ResolveItemIcon(index, item, displayName);

        if (icon != null)
        {
            visual.Icon.sprite = icon;
            visual.Icon.enabled = true;
            visual.Icon.preserveAspect = true;
            visual.Initials.text = string.Empty;
            visual.Initials.enabled = false;
            return;
        }

        visual.Icon.sprite = null;
        visual.Icon.enabled = false;

        string initials = showInitialsWhenNoIcon ? GetInitials(displayName) : string.Empty;
        visual.Initials.text = initials;
        visual.Initials.enabled = !string.IsNullOrEmpty(initials);
    }

    private void RefreshSelectionHighlight(bool force = false)
    {
        int selected = PlayerInventory.instance != null
            ? PlayerInventory.instance.selectedSlotIndex
            : -1;

        if (!force && selected == lastSelectedSlot) return;
        lastSelectedSlot = selected;

        foreach (KeyValuePair<int, SlotVisual> pair in slotVisuals)
        {
            SlotVisual visual = pair.Value;
            if (visual == null || visual.Frame == null) continue;

            visual.Frame.color = pair.Key == selected ? selectedFrameColor : frameColor;
        }
    }

    /// <summary>
    /// Priority: name override -> per-slot custom icon -> item's ItemReference
    /// icon -> fallback icon -> initials.
    /// </summary>
    private Sprite ResolveItemIcon(int slotIndex, GameObject item, string displayName)
    {
        if (itemIconOverrides != null)
        {
            foreach (ItemIconOverride entry in itemIconOverrides)
            {
                if (entry == null || entry.icon == null) continue;
                if (string.IsNullOrEmpty(entry.itemName)) continue;

                if (string.Equals(entry.itemName, displayName, StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(entry.itemName, item.name, StringComparison.OrdinalIgnoreCase))
                {
                    return entry.icon;
                }
            }
        }

        if (slotCustomIcons != null &&
            slotIndex >= 0 &&
            slotIndex < slotCustomIcons.Length &&
            slotCustomIcons[slotIndex] != null)
        {
            return slotCustomIcons[slotIndex];
        }

        ItemDetails details = item.GetComponent<ItemDetails>();
        if (details == null) details = item.GetComponentInChildren<ItemDetails>(true);
        if (details != null && details.itemIcon != null) return details.itemIcon;

        return fallbackItemIcon;
    }

    private static string GetDisplayName(GameObject item, PlayerInventory inventory)
    {
        if (item == null) return string.Empty;

        if (inventory != null)
        {
            string identifier = inventory.GetItemIdentifier(item);
            if (!string.IsNullOrEmpty(identifier)) return identifier;
        }

        ItemDetails details = item.GetComponent<ItemDetails>();
        if (details == null) details = item.GetComponentInChildren<ItemDetails>(true);
        if (details != null && !string.IsNullOrEmpty(details.itemName)) return details.itemName;

        WeaponInstance weapon = item.GetComponent<WeaponInstance>();
        if (weapon != null && !string.IsNullOrEmpty(weapon.weaponName)) return weapon.weaponName;

        return item.name;
    }

    private static string GetInitials(string displayName)
    {
        if (string.IsNullOrWhiteSpace(displayName)) return string.Empty;

        string[] words = displayName.Trim().Split(
            new[] { ' ', '_', '-' },
            StringSplitOptions.RemoveEmptyEntries);

        if (words.Length == 0) return string.Empty;

        if (words.Length == 1)
        {
            string word = words[0];
            return word.Length == 1
                ? word.ToUpperInvariant()
                : word.Substring(0, 2).ToUpperInvariant();
        }

        return (words[0][0].ToString() + words[1][0]).ToUpperInvariant();
    }

    private bool ItemsChanged()
    {
        PlayerInventory inventory = PlayerInventory.instance;
        if (inventory == null) return true;

        for (int i = 0; i < VisibleSlotCount; i++)
        {
            GameObject item = null;
            if (i < inventory.backpackItems.Count)
                item = inventory.backpackItems[i];

            if (lastItems[i] != item) return true;
        }

        return false;
    }

    private static void StyleSlotNumber(TMP_Text label, int index)
    {
        RectTransform rect = label.rectTransform;
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0f, 1f);
        rect.anchoredPosition = new Vector2(5f, -3f);
        rect.sizeDelta = new Vector2(30f, 20f);

        label.text = (index + 1).ToString();
        label.alignment = TextAlignmentOptions.TopLeft;
        label.enableAutoSizing = false;
        label.fontSize = 13f;
        label.fontStyle = FontStyles.Bold;
        label.color = new Color(1f, 1f, 1f, 0.35f);
        label.raycastTarget = false;
        label.richText = false;
    }

    private static void StyleInitials(TMP_Text initials)
    {
        initials.text = string.Empty;
        initials.alignment = TextAlignmentOptions.Center;
        initials.enableAutoSizing = true;
        initials.fontSizeMin = 6f;
        initials.fontSizeMax = 40f;
        initials.fontStyle = FontStyles.Bold;
        initials.color = new Color(0.92f, 0.92f, 0.95f, 1f);
        initials.raycastTarget = false;
        initials.richText = false;
        initials.enabled = false;
    }

    private static Image EnsureImageChild(Transform parent, string name)
    {
        Transform existing = parent.Find(name);
        if (existing != null)
        {
            Image image = existing.GetComponent<Image>();
            if (image != null) return image;
        }

        GameObject go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        go.transform.SetParent(parent, false);
        return go.GetComponent<Image>();
    }

    private static TMP_Text EnsureTextChild(Transform parent, string name)
    {
        TMP_Text existing = FindTextChild(parent, name);
        if (existing != null) return existing;

        GameObject go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        go.transform.SetParent(parent, false);
        return go.GetComponent<TMP_Text>();
    }

    private static TMP_Text FindTextChild(Transform parent, string name)
    {
        Transform existing = parent.Find(name);
        if (existing == null) return null;
        return existing.GetComponent<TMP_Text>();
    }

    private static void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = Vector2.zero;
    }

    private static T[] EnsureArraySize<T>(T[] array, int size) where T : class
    {
        if (array != null && array.Length >= size) return array;

        T[] resized = new T[size];
        if (array != null) Array.Copy(array, resized, array.Length);
        return resized;
    }
}
