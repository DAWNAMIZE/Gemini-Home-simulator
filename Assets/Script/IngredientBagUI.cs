using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Backpack access under the phone. The closed state is a compact button that
/// sits just below the phone panel; tapping it opens a 4x3 grid with every
/// backpack slot. Tapping a filled grid slot selects that item.
/// </summary>
public class IngredientBagUI : MonoBehaviour
{
    public static IngredientBagUI instance;

    private sealed class BagSlot
    {
        public int Index;
        public Image Frame;
        public Image Background;
        public Image Icon;
        public TMP_Text Initials;
        public TMP_Text Name;
        public TMP_Text Count;
    }

    private const int SlotCount = 12;
    private const int Columns = 4;
    private const int Rows = 3;
    private const float SlotSize = 82f;
    private const float SlotGap = 8f;
    private const float PanelPadding = 18f;
    private const float HeaderHeight = 46f;

    private const float PanelWidth = Columns * SlotSize + (Columns - 1) * SlotGap + PanelPadding * 2f;
    private const float PanelHeight = HeaderHeight + Rows * SlotSize + (Rows - 1) * SlotGap + PanelPadding * 2f;

    // PhonePanel is 600x800 with its centre at x = -90, y = 9 in the 1920x1080
    // reference canvas. Its bottom edge is y = -391, so the bag button sits in
    // the free band immediately below the phone.
    private static readonly Vector2 BagButtonPosition = new Vector2(-90f, -432f);
    private static readonly Vector2 GridPanelPosition = new Vector2(420f, 0f);
    private static readonly Vector2 BagButtonSize = new Vector2(264f, 58f);

    private static readonly Color FrameColor = new Color(0.16f, 0.17f, 0.21f, 0.94f);
    private static readonly Color SelectedFrameColor = new Color(1f, 0.78f, 0.25f, 1f);
    private static readonly Color SlotColor = new Color(0.10f, 0.11f, 0.14f, 1f);
    private static readonly Color PanelColor = new Color(0.035f, 0.045f, 0.065f, 0.97f);
    private static readonly Color ButtonColor = new Color(0.10f, 0.13f, 0.18f, 0.96f);
    private static readonly Color ButtonOpenColor = new Color(0.20f, 0.17f, 0.08f, 0.98f);

    private Canvas canvas;
    private Button bagButton;
    private Image bagButtonBackground;
    private TMP_Text capacityLabel;
    private GameObject gridPanel;
    private TMP_Text gridCapacityLabel;
    private readonly List<BagSlot> slots = new List<BagSlot>();
    private Button closeButton;
    private bool built;
    private bool wired;
    private bool isOpen;
    private float nextPoll;

    public bool IsOpen => isOpen;

    public static IngredientBagUI GetOrCreate()
    {
        if (instance != null) return instance;

        IngredientBagUI existing = Object.FindFirstObjectByType<IngredientBagUI>(
            FindObjectsInactive.Include);
        if (existing != null)
        {
            instance = existing;
            return existing;
        }

        GameObject go = new GameObject("IngredientBagUI");
        instance = go.AddComponent<IngredientBagUI>();
        return instance;
    }

    void Awake()
    {
        if (instance == null) instance = this;
        else if (instance != this) { Destroy(gameObject); return; }
    }

    void Start()
    {
        Build();
        WireEvents();
        RefreshNow();
    }

    void OnDestroy()
    {
        if (instance == this) instance = null;
    }

    void Update()
    {
        if (!built) Build();

        if (Time.unscaledTime < nextPoll) return;
        nextPoll = Time.unscaledTime + 0.25f;
        RefreshNow();
    }

    public void RefreshNow()
    {
        if (!built) Build();
        if (bagButton == null) return;

        PlayerInventory inventory = PlayerInventory.instance;
        int count = inventory != null ? inventory.backpackItems.Count : 0;
        int max = inventory != null ? Mathf.Max(1, inventory.maxBackpackItems) : SlotCount;

        string capacity = count + " / " + max;
        if (capacityLabel != null)
            capacityLabel.text = "BACKPACK  " + capacity;
        if (gridCapacityLabel != null)
            gridCapacityLabel.text = capacity;

        for (int i = 0; i < slots.Count; i++)
        {
            BagSlot slot = slots[i];
            if (slot == null || slot.Frame == null) continue;

            GameObject item = inventory != null && i < inventory.backpackItems.Count
                ? inventory.backpackItems[i]
                : null;

            bool selected = inventory != null && inventory.selectedSlotIndex == i;
            slot.Frame.color = selected ? SelectedFrameColor : FrameColor;

            if (item == null)
            {
                slot.Background.color = SlotColor;
                slot.Icon.enabled = false;
                slot.Initials.enabled = false;
                slot.Initials.text = string.Empty;
                slot.Name.text = string.Empty;
                slot.Count.text = string.Empty;
                continue;
            }

            string identifier = inventory.GetItemIdentifier(item);
            string canonical = IngredientCatalog.Canonical(identifier);
            IngredientCatalog.Entry entry = IngredientCatalog.Get(canonical);

            Color color = entry != null
                ? entry.Color
                : new Color(0.42f, 0.45f, 0.52f, 1f);
            color.a = 1f;

            slot.Background.color = new Color(
                Mathf.Lerp(color.r, 0.08f, 0.68f),
                Mathf.Lerp(color.g, 0.09f, 0.68f),
                Mathf.Lerp(color.b, 0.12f, 0.68f),
                1f);

            Sprite icon = null;
            ItemDetails details = item.GetComponent<ItemDetails>();
            if (details == null) details = item.GetComponentInChildren<ItemDetails>(true);
            if (details != null) icon = details.itemIcon;

            if (icon != null)
            {
                slot.Icon.sprite = icon;
                slot.Icon.enabled = true;
                slot.Initials.enabled = false;
                slot.Initials.text = string.Empty;
            }
            else
            {
                slot.Icon.enabled = false;
                slot.Initials.enabled = true;
                slot.Initials.color = Color.Lerp(color, Color.white, 0.72f);
                slot.Initials.text = entry != null ? entry.Initials : InitialsOf(identifier);
            }

            slot.Name.text = entry != null ? entry.Name : identifier;

            int copies = CountCopies(inventory, identifier);
            slot.Count.text = copies > 1 ? "x" + copies : string.Empty;
        }
    }

    /// <summary>Toggles the grid from code (used by the bag button).</summary>
    public void ToggleBag()
    {
        SetBagOpen(!isOpen);
    }

    public void SetBagOpen(bool open)
    {
        if (!built) Build();
        if (gridPanel == null) return;

        isOpen = open;
        gridPanel.SetActive(isOpen);

        if (bagButtonBackground != null)
            bagButtonBackground.color = isOpen ? ButtonOpenColor : ButtonColor;

        if (isOpen) gridPanel.transform.SetAsLastSibling();
    }

    // ------------------------------------------------------------------
    // BUILD
    // ------------------------------------------------------------------

    private void Build()
    {
        if (built) return;

        // The scene's legacy Inventory button is not wired to anything, and it
        // sits in the lower-left HUD. Hide it so it cannot overlap the bag.
        if (Application.isPlaying)
        {
            GameObject legacyInventory = GameObject.Find("Canvas/MobileControls/Inventory");
            if (legacyInventory != null) legacyInventory.SetActive(false);
        }

        if (TryBindExistingUI())
        {
            built = true;
            WireEvents();
            if (Application.isPlaying) SetBagOpen(false);
            return;
        }

        built = true;

        canvas = RuntimeUI.CreateOverlayCanvas("IngredientBagCanvas", 880);
        canvas.transform.SetParent(transform, false);

        BuildBagButton();
        BuildGridPanel();
        WireEvents();
        if (Application.isPlaying) SetBagOpen(false);
    }

    /// <summary>
    /// Reuses an IngredientBagCanvas that was built in the scene by the editor
    /// tool so the hierarchy can be arranged by hand.
    /// </summary>
    private bool TryBindExistingUI()
    {
        Transform canvasTransform = RuntimeUI.FindDeepChild(transform, "IngredientBagCanvas");
        if (canvasTransform == null) return false;

        canvas = canvasTransform.GetComponent<Canvas>();
        bagButton = RuntimeUI.FindComponentByName<Button>(canvasTransform, "BagButton");
        if (bagButton == null) return false;

        bagButtonBackground = bagButton.GetComponent<Image>();
        capacityLabel = RuntimeUI.FindComponentByName<TMP_Text>(bagButton.transform, "Label");

        Transform grid = RuntimeUI.FindDeepChild(canvasTransform, "BagGridPanel");
        if (grid == null) return false;

        gridPanel = grid.gameObject;
        gridCapacityLabel = RuntimeUI.FindComponentByName<TMP_Text>(grid, "Capacity");
        closeButton = RuntimeUI.FindComponentByName<Button>(grid, "CloseButton");

        slots.Clear();
        for (int i = 0; i < SlotCount; i++)
        {
            Transform slotTransform = RuntimeUI.FindDeepChild(grid, "BagSlot_" + i);
            if (slotTransform == null) continue;

            Transform inner = RuntimeUI.FindDeepChild(slotTransform, "Inner");
            if (inner == null) continue;

            slots.Add(new BagSlot
            {
                Index = i,
                Frame = slotTransform.GetComponent<Image>(),
                Background = inner.GetComponent<Image>(),
                Icon = RuntimeUI.FindComponentByName<Image>(inner, "Icon"),
                Initials = RuntimeUI.FindComponentByName<TMP_Text>(inner, "Initials"),
                Name = RuntimeUI.FindComponentByName<TMP_Text>(inner, "Name"),
                Count = RuntimeUI.FindComponentByName<TMP_Text>(inner, "Count")
            });
        }

        return slots.Count > 0;
    }

    private void WireEvents()
    {
        if (wired) return;
        wired = true;

        if (bagButton != null)
        {
            bagButton.onClick.RemoveAllListeners();
            bagButton.onClick.AddListener(ToggleBag);
        }

        if (closeButton == null && gridPanel != null)
            closeButton = RuntimeUI.FindComponentByName<Button>(gridPanel.transform, "CloseButton");

        if (closeButton != null)
        {
            closeButton.onClick.RemoveAllListeners();
            closeButton.onClick.AddListener(() => SetBagOpen(false));
        }

        for (int i = 0; i < slots.Count; i++)
        {
            BagSlot slot = slots[i];
            if (slot == null || slot.Frame == null) continue;

            Button button = slot.Frame.GetComponent<Button>();
            if (button == null) continue;

            int captured = i;
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() => OnSlotClicked(captured));
        }
    }

#if UNITY_EDITOR
    /// <summary>Builds the editable hierarchy while not in play mode.</summary>
    public void BuildForEditor()
    {
        Build();
        SetBagOpen(true);
    }
#endif

    private void BuildBagButton()
    {
        bagButton = RuntimeUI.CreateButton(canvas.transform, "BagButton", "BACKPACK  0 / 12",
            BagButtonSize, BagButtonPosition, new Vector2(0.5f, 0.5f),
            ButtonColor, ToggleBag);
        bagButtonBackground = bagButton.GetComponent<Image>();

        RectTransform rt = bagButton.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = BagButtonPosition;
        rt.sizeDelta = BagButtonSize;

        capacityLabel = bagButton.GetComponentInChildren<TMP_Text>(true);
        if (capacityLabel != null)
        {
            capacityLabel.fontSize = 21f;
            capacityLabel.fontStyle = FontStyles.Bold;
            capacityLabel.color = new Color(0.92f, 0.95f, 1f, 1f);
            capacityLabel.characterSpacing = 2f;

            // Leave room for the little bag glyph on the left.
            RectTransform labelRt = capacityLabel.rectTransform;
            labelRt.offsetMin = new Vector2(46f, 0f);
            labelRt.offsetMax = new Vector2(-12f, 0f);
            capacityLabel.alignment = TextAlignmentOptions.Center;
        }

        // Small stitched-bag mark: a body, a flap and two straps drawn from
        // plain UI images so the button reads as a bag without external art.
        RectTransform iconRoot = RuntimeUI.CreateRect(bagButton.transform, "BagIcon");
        iconRoot.anchorMin = new Vector2(0f, 0.5f);
        iconRoot.anchorMax = new Vector2(0f, 0.5f);
        iconRoot.pivot = new Vector2(0f, 0.5f);
        iconRoot.anchoredPosition = new Vector2(16f, 0f);
        iconRoot.sizeDelta = new Vector2(22f, 26f);

        AddIconPart(iconRoot, "Body", new Vector2(1f, 0f), new Vector2(20f, 18f),
            new Color(0.76f, 0.58f, 0.28f, 1f));
        AddIconPart(iconRoot, "Flap", new Vector2(2f, 14f), new Vector2(18f, 5f),
            new Color(0.94f, 0.78f, 0.42f, 1f));
        AddIconPart(iconRoot, "HandleLeft", new Vector2(5f, 18f), new Vector2(2f, 5f),
            new Color(0.30f, 0.22f, 0.12f, 1f));
        AddIconPart(iconRoot, "HandleRight", new Vector2(15f, 18f), new Vector2(2f, 5f),
            new Color(0.30f, 0.22f, 0.12f, 1f));
        AddIconPart(iconRoot, "HandleTop", new Vector2(5f, 23f), new Vector2(12f, 2f),
            new Color(0.30f, 0.22f, 0.12f, 1f));
        AddIconPart(iconRoot, "Latch", new Vector2(9f, 10f), new Vector2(4f, 4f),
            new Color(0.30f, 0.22f, 0.12f, 1f));

        Image accent = RuntimeUI.CreateImage(bagButton.transform, "Accent",
            new Color(0.95f, 0.73f, 0.24f, 0.95f));
        RectTransform accentRt = accent.rectTransform;
        accentRt.anchorMin = new Vector2(0f, 0f);
        accentRt.anchorMax = new Vector2(1f, 0f);
        accentRt.pivot = new Vector2(0.5f, 0f);
        accentRt.offsetMin = new Vector2(14f, 6f);
        accentRt.offsetMax = new Vector2(-14f, 9f);
        accent.raycastTarget = false;
    }

    private void BuildGridPanel()
    {
        Image panel = RuntimeUI.CreateImage(canvas.transform, "BagGridPanel", PanelColor);
        gridPanel = panel.gameObject;

        RectTransform panelRt = panel.rectTransform;
        panelRt.anchorMin = new Vector2(0.5f, 0.5f);
        panelRt.anchorMax = new Vector2(0.5f, 0.5f);
        panelRt.pivot = new Vector2(0.5f, 0.5f);
        panelRt.anchoredPosition = GridPanelPosition;
        panelRt.sizeDelta = new Vector2(PanelWidth, PanelHeight);

        Outline outline = panel.gameObject.AddComponent<Outline>();
        outline.effectColor = new Color(0.42f, 0.48f, 0.58f, 0.55f);
        outline.effectDistance = new Vector2(2f, -2f);

        TMP_Text title = RuntimeUI.CreateText(panel.transform, "Title", "BACKPACK",
            22f, new Color(0.94f, 0.96f, 1f, 1f), TextAlignmentOptions.MidlineLeft);
        title.fontStyle = FontStyles.Bold;
        RectTransform titleRt = title.rectTransform;
        titleRt.anchorMin = new Vector2(0f, 1f);
        titleRt.anchorMax = new Vector2(0f, 1f);
        titleRt.pivot = new Vector2(0f, 1f);
        titleRt.anchoredPosition = new Vector2(PanelPadding, -10f);
        titleRt.sizeDelta = new Vector2(210f, 28f);

        gridCapacityLabel = RuntimeUI.CreateText(panel.transform, "Capacity", "0 / 12",
            18f, new Color(0.66f, 0.74f, 0.86f, 1f), TextAlignmentOptions.MidlineRight);
        gridCapacityLabel.fontStyle = FontStyles.Bold;
        RectTransform capacityRt = gridCapacityLabel.rectTransform;
        capacityRt.anchorMin = new Vector2(1f, 1f);
        capacityRt.anchorMax = new Vector2(1f, 1f);
        capacityRt.pivot = new Vector2(1f, 1f);
        capacityRt.anchoredPosition = new Vector2(-56f, -10f);
        capacityRt.sizeDelta = new Vector2(130f, 28f);

        closeButton = RuntimeUI.CreateButton(panel.transform, "CloseButton", "X",
            new Vector2(34f, 34f), new Vector2(-10f, -10f),
            new Vector2(1f, 1f), new Color(0.24f, 0.26f, 0.32f, 1f),
            null);
        TMP_Text closeLabel = closeButton.GetComponentInChildren<TMP_Text>(true);
        if (closeLabel != null)
        {
            closeLabel.fontSize = 20f;
            closeLabel.fontStyle = FontStyles.Bold;
        }

        Image divider = RuntimeUI.CreateImage(panel.transform, "Divider",
            new Color(0.28f, 0.32f, 0.40f, 0.75f));
        RectTransform dividerRt = divider.rectTransform;
        dividerRt.anchorMin = new Vector2(0f, 1f);
        dividerRt.anchorMax = new Vector2(1f, 1f);
        dividerRt.pivot = new Vector2(0.5f, 1f);
        dividerRt.anchoredPosition = new Vector2(0f, -HeaderHeight + 8f);
        dividerRt.sizeDelta = new Vector2(-PanelPadding * 2f, 2f);
        divider.raycastTarget = false;

        for (int i = 0; i < SlotCount; i++)
        {
            int row = i / Columns;
            int column = i % Columns;

            GameObject root = new GameObject("BagSlot_" + i,
                typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            root.transform.SetParent(panel.transform, false);

            RectTransform rt = root.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(0f, 1f);
            rt.pivot = new Vector2(0f, 1f);
            rt.anchoredPosition = new Vector2(
                PanelPadding + column * (SlotSize + SlotGap),
                -(HeaderHeight + PanelPadding + row * (SlotSize + SlotGap)));
            rt.sizeDelta = new Vector2(SlotSize, SlotSize);

            Image frame = root.GetComponent<Image>();
            frame.color = FrameColor;

            Image inner = RuntimeUI.CreateImage(root.transform, "Inner", SlotColor);
            RectTransform innerRt = inner.rectTransform;
            innerRt.anchorMin = Vector2.zero;
            innerRt.anchorMax = Vector2.one;
            innerRt.offsetMin = new Vector2(3f, 3f);
            innerRt.offsetMax = new Vector2(-3f, -3f);
            inner.raycastTarget = false;

            Image icon = RuntimeUI.CreateImage(inner.transform, "Icon", Color.white);
            RectTransform iconRt = icon.rectTransform;
            iconRt.anchorMin = Vector2.zero;
            iconRt.anchorMax = Vector2.one;
            iconRt.offsetMin = new Vector2(8f, 25f);
            iconRt.offsetMax = new Vector2(-8f, -8f);
            icon.preserveAspect = true;
            icon.raycastTarget = false;
            icon.enabled = false;

            TMP_Text initials = RuntimeUI.CreateText(inner.transform, "Initials",
                string.Empty, 24f, Color.white);
            initials.fontStyle = FontStyles.Bold;
            RectTransform initialsRt = initials.rectTransform;
            initialsRt.anchorMin = Vector2.zero;
            initialsRt.anchorMax = Vector2.one;
            initialsRt.offsetMin = new Vector2(4f, 23f);
            initialsRt.offsetMax = new Vector2(-4f, -4f);

            Image nameStrip = RuntimeUI.CreateImage(inner.transform, "NameStrip",
                new Color(0f, 0f, 0f, 0.42f));
            RectTransform nameStripRt = nameStrip.rectTransform;
            nameStripRt.anchorMin = new Vector2(0f, 0f);
            nameStripRt.anchorMax = new Vector2(1f, 0f);
            nameStripRt.pivot = new Vector2(0.5f, 0f);
            nameStripRt.offsetMin = new Vector2(3f, 3f);
            nameStripRt.offsetMax = new Vector2(-3f, 21f);
            nameStrip.raycastTarget = false;

            TMP_Text name = RuntimeUI.CreateText(inner.transform, "Name",
                string.Empty, 10f, new Color(0.92f, 0.95f, 1f, 1f),
                TextAlignmentOptions.Center);
            name.enableAutoSizing = true;
            name.fontSizeMin = 7f;
            name.fontSizeMax = 11f;
            RectTransform nameRt = name.rectTransform;
            nameRt.anchorMin = new Vector2(0f, 0f);
            nameRt.anchorMax = new Vector2(1f, 0f);
            nameRt.pivot = new Vector2(0.5f, 0f);
            nameRt.offsetMin = new Vector2(3f, 4f);
            nameRt.offsetMax = new Vector2(-3f, 20f);

            TMP_Text count = RuntimeUI.CreateText(inner.transform, "Count",
                string.Empty, 14f, Color.white, TextAlignmentOptions.TopRight);
            count.fontStyle = FontStyles.Bold;
            RectTransform countRt = count.rectTransform;
            countRt.anchorMin = Vector2.zero;
            countRt.anchorMax = Vector2.one;
            countRt.offsetMin = new Vector2(4f, 4f);
            countRt.offsetMax = new Vector2(-5f, -4f);

            Button button = root.GetComponent<Button>();
            button.targetGraphic = inner;
            Navigation navigation = button.navigation;
            navigation.mode = Navigation.Mode.None;
            button.navigation = navigation;

            int captured = i;
            button.onClick.AddListener(() => OnSlotClicked(captured));

            slots.Add(new BagSlot
            {
                Index = i,
                Frame = frame,
                Background = inner,
                Icon = icon,
                Initials = initials,
                Name = name,
                Count = count
            });
        }
    }

    private static void AddIconPart(Transform parent, string name, Vector2 position,
        Vector2 size, Color color)
    {
        Image part = RuntimeUI.CreateImage(parent, name, color);
        RectTransform rt = part.rectTransform;
        rt.anchorMin = new Vector2(0f, 0f);
        rt.anchorMax = new Vector2(0f, 0f);
        rt.pivot = new Vector2(0f, 0f);
        rt.anchoredPosition = position;
        rt.sizeDelta = size;
        part.raycastTarget = false;
    }

    private void OnSlotClicked(int index)
    {
        PlayerInventory inventory = PlayerInventory.instance;
        if (inventory == null) return;

        if (index < 0 || index >= inventory.backpackItems.Count)
        {
            ToastUI.Show("That backpack slot is empty.");
            return;
        }

        inventory.SelectSlot(index);
        RefreshNow();
    }

    // ------------------------------------------------------------------
    // HELPERS
    // ------------------------------------------------------------------

    private static int CountCopies(PlayerInventory inventory, string identifier)
    {
        if (inventory == null || string.IsNullOrEmpty(identifier)) return 0;

        int count = 0;
        foreach (GameObject item in inventory.backpackItems)
        {
            if (item == null) continue;
            if (string.Equals(inventory.GetItemIdentifier(item), identifier,
                    System.StringComparison.OrdinalIgnoreCase))
                count++;
        }

        return count;
    }

    private static string InitialsOf(string value)
    {
        if (string.IsNullOrWhiteSpace(value)) return string.Empty;

        string[] words = value.Trim().Split(
            new[] { ' ', '_', '-' }, System.StringSplitOptions.RemoveEmptyEntries);

        if (words.Length == 0) return string.Empty;
        if (words.Length == 1)
            return words[0].Length > 1
                ? words[0].Substring(0, 2).ToUpperInvariant()
                : words[0].ToUpperInvariant();

        return (words[0][0].ToString() + words[1][0]).ToUpperInvariant();
    }
}
