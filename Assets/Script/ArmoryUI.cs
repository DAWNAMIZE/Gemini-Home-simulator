using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// The armory screen opened by Arms_Shopkeeper. Three catalogue columns sell
/// weapons, ammo and traps. Checkout validates money and backpack space before
/// moving any items.
/// </summary>
public class ArmoryUI : MonoBehaviour
{
    public static ArmoryUI instance;

    private sealed class BuyRow
    {
        public ArmoryCatalog.Entry Entry;
        public TMP_Text Count;
    }

    private static readonly Color PanelColor = new Color(0.070f, 0.060f, 0.055f, 0.985f);
    private static readonly Color ColumnColor = new Color(0.115f, 0.095f, 0.085f, 1f);
    private static readonly Color RowColor = new Color(0.16f, 0.135f, 0.12f, 1f);
    private static readonly Color WeaponAccent = new Color(0.88f, 0.63f, 0.28f, 1f);
    private static readonly Color AmmoAccent = new Color(0.92f, 0.78f, 0.34f, 1f);
    private static readonly Color TrapAccent = new Color(0.73f, 0.38f, 0.26f, 1f);
    private static readonly Color TextMuted = new Color(0.72f, 0.69f, 0.63f, 1f);

    private Canvas canvas;
    private GameObject panel;
    private TMP_Text moneyLabel;
    private TMP_Text totalsLabel;
    private TMP_Text capacityLabel;

    private readonly Dictionary<ArmoryCatalog.Entry, int> buyCart =
        new Dictionary<ArmoryCatalog.Entry, int>();
    private readonly List<BuyRow> buyRows = new List<BuyRow>();
    private bool wired;

    public bool HasCart
    {
        get
        {
            foreach (int count in buyCart.Values)
                if (count > 0) return true;
            return false;
        }
    }

    public static ArmoryUI GetOrCreate()
    {
        if (instance != null) return instance;

        ArmoryUI existing = Object.FindFirstObjectByType<ArmoryUI>(
            FindObjectsInactive.Include);
        if (existing != null)
        {
            instance = existing;
            return existing;
        }

        GameObject go = new GameObject("ArmoryUI");
        instance = go.AddComponent<ArmoryUI>();
        return instance;
    }

    public static void Show(ArmoryMan armoryMan)
    {
        GetOrCreate().ShowInternal(armoryMan);
    }

    public static void Hide()
    {
        if (instance != null) instance.HideInternal();
    }

    void Awake()
    {
        if (instance == null) instance = this;
        else if (instance != this) { Destroy(gameObject); return; }
    }

    void Start()
    {
        if (TryBindExistingUI())
        {
            WireExistingButtons();
            HideInternal();
        }
    }

    void OnDestroy()
    {
        if (instance == this) instance = null;
    }

    public void ShowCatalogue()
    {
        ShowInternal(null);
    }

    public void CheckoutFromCounter()
    {
        Checkout();
    }

    private void ShowInternal(ArmoryMan armoryMan)
    {
        Build();
        panel.SetActive(true);
        EnsureParentsActive();
        RefreshAll();
    }

    private void HideInternal()
    {
        if (panel != null) panel.SetActive(false);
        if (canvas != null) canvas.gameObject.SetActive(false);
    }

    // ------------------------------------------------------------------
    // BUILD
    // ------------------------------------------------------------------

    private void Build()
    {
        if (panel != null)
        {
            if (!wired) WireExistingButtons();
            return;
        }

        if (TryBindExistingUI())
        {
            WireExistingButtons();
            return;
        }

        canvas = RuntimeUI.CreateOverlayCanvas("ArmoryCanvas", 970);
        canvas.transform.SetParent(transform, false);

        Image backdrop = RuntimeUI.CreateImage(canvas.transform, "Backdrop",
            new Color(0f, 0f, 0f, 0.70f));
        Stretch(backdrop.rectTransform);

        Image panelImage = RuntimeUI.CreateImage(backdrop.transform, "ArmoryPanel", PanelColor);
        panel = panelImage.gameObject;

        RectTransform panelRt = panelImage.rectTransform;
        panelRt.anchorMin = new Vector2(0.5f, 0.5f);
        panelRt.anchorMax = new Vector2(0.5f, 0.5f);
        panelRt.pivot = new Vector2(0.5f, 0.5f);
        panelRt.anchoredPosition = Vector2.zero;
        panelRt.sizeDelta = new Vector2(1600f, 900f);

        Image topStrip = RuntimeUI.CreateImage(panel.transform, "TopStrip", WeaponAccent);
        RectTransform stripRt = topStrip.rectTransform;
        stripRt.anchorMin = new Vector2(0f, 1f);
        stripRt.anchorMax = new Vector2(1f, 1f);
        stripRt.pivot = new Vector2(0.5f, 1f);
        stripRt.anchoredPosition = Vector2.zero;
        stripRt.sizeDelta = new Vector2(0f, 8f);

        TMP_Text title = RuntimeUI.CreateText(panel.transform, "Title", "ARMORY",
            44f, new Color(1f, 0.82f, 0.52f));
        title.fontStyle = FontStyles.Bold;
        title.alignment = TextAlignmentOptions.Left;
        SetRect(title.rectTransform, new Vector2(0f, 1f), new Vector2(36f, -24f),
            new Vector2(500f, 56f), new Vector2(0f, 1f));

        TMP_Text subtitle = RuntimeUI.CreateText(panel.transform, "Subtitle",
            "Weapons · ammo · traps   ·   checkout at the counter or below",
            20f, TextMuted, TextAlignmentOptions.Left);
        SetRect(subtitle.rectTransform, new Vector2(0f, 1f), new Vector2(38f, -78f),
            new Vector2(1000f, 30f), new Vector2(0f, 1f));

        moneyLabel = RuntimeUI.CreateText(panel.transform, "Money", "$0",
            38f, new Color(0.96f, 0.78f, 0.46f), TextAlignmentOptions.Right);
        moneyLabel.fontStyle = FontStyles.Bold;
        SetRect(moneyLabel.rectTransform, new Vector2(1f, 1f), new Vector2(-100f, -28f),
            new Vector2(420f, 52f), new Vector2(1f, 1f));

        capacityLabel = RuntimeUI.CreateText(panel.transform, "Capacity", string.Empty,
            20f, TextMuted, TextAlignmentOptions.Right);
        SetRect(capacityLabel.rectTransform, new Vector2(1f, 1f), new Vector2(-100f, -78f),
            new Vector2(460f, 30f), new Vector2(1f, 1f));

        BuildColumn(ArmoryCatalog.Category.Weapon, -520f, WeaponAccent);
        BuildColumn(ArmoryCatalog.Category.Ammo, 0f, AmmoAccent);
        BuildColumn(ArmoryCatalog.Category.Trap, 520f, TrapAccent);

        totalsLabel = RuntimeUI.CreateText(panel.transform, "Totals", string.Empty,
            27f, Color.white);
        totalsLabel.fontStyle = FontStyles.Bold;
        SetRect(totalsLabel.rectTransform, new Vector2(0.5f, 0f), new Vector2(0f, 62f),
            new Vector2(780f, 52f), new Vector2(0.5f, 0f));

        RuntimeUI.CreateButton(panel.transform, "ClearButton", "CLEAR",
            new Vector2(180f, 66f), new Vector2(-560f, 54f), new Vector2(0.5f, 0f),
            new Color(0.26f, 0.24f, 0.22f, 1f), ClearCart);

        RuntimeUI.CreateButton(panel.transform, "CheckoutButton", "CHECKOUT",
            new Vector2(300f, 76f), new Vector2(540f, 54f), new Vector2(0.5f, 0f),
            new Color(0.48f, 0.27f, 0.12f, 1f), Checkout);

        RuntimeUI.CreateButton(panel.transform, "CloseButton", "X",
            new Vector2(52f, 52f), new Vector2(-30f, -30f), new Vector2(1f, 1f),
            new Color(0.24f, 0.22f, 0.22f, 1f), HideInternal);

        wired = true;
        HideInternal();
    }

    private bool TryBindExistingUI()
    {
        Transform canvasTransform = RuntimeUI.FindDeepChild(transform, "ArmoryCanvas");
        if (canvasTransform == null) return false;

        Transform panelTransform = RuntimeUI.FindDeepChild(canvasTransform, "ArmoryPanel");
        if (panelTransform == null) return false;

        canvas = canvasTransform.GetComponent<Canvas>();
        panel = panelTransform.gameObject;
        moneyLabel = RuntimeUI.FindComponentByName<TMP_Text>(panelTransform, "Money");
        totalsLabel = RuntimeUI.FindComponentByName<TMP_Text>(panelTransform, "Totals");
        capacityLabel = RuntimeUI.FindComponentByName<TMP_Text>(panelTransform, "Capacity");

        buyRows.Clear();
        foreach (Transform row in panelTransform.GetComponentsInChildren<Transform>(true))
        {
            if (row == null || !row.name.StartsWith("Row_")) continue;

            ArmoryCatalog.Entry entry = ArmoryCatalog.Get(row.name.Substring(4));
            TMP_Text count = RuntimeUI.FindComponentByName<TMP_Text>(row, "Count");
            if (entry != null && count != null)
                buyRows.Add(new BuyRow { Entry = entry, Count = count });
        }

        return true;
    }

    private void WireExistingButtons()
    {
        if (wired || panel == null) return;
        wired = true;

        Button clear = RuntimeUI.FindComponentByName<Button>(panel.transform, "ClearButton");
        if (clear != null)
        {
            clear.onClick.RemoveAllListeners();
            clear.onClick.AddListener(ClearCart);
        }

        Button checkout = RuntimeUI.FindComponentByName<Button>(panel.transform, "CheckoutButton");
        if (checkout != null)
        {
            checkout.onClick.RemoveAllListeners();
            checkout.onClick.AddListener(Checkout);
        }

        Button close = RuntimeUI.FindComponentByName<Button>(panel.transform, "CloseButton");
        if (close != null)
        {
            close.onClick.RemoveAllListeners();
            close.onClick.AddListener(HideInternal);
        }

        foreach (Transform row in panel.transform.GetComponentsInChildren<Transform>(true))
        {
            if (row == null || !row.name.StartsWith("Row_")) continue;

            ArmoryCatalog.Entry entry = ArmoryCatalog.Get(row.name.Substring(4));
            if (entry == null) continue;

            Button plus = RuntimeUI.FindComponentByName<Button>(row, "Plus");
            Button minus = RuntimeUI.FindComponentByName<Button>(row, "Minus");

            if (plus != null)
            {
                plus.onClick.RemoveAllListeners();
                plus.onClick.AddListener(() => ChangeBuy(entry, 1));
            }

            if (minus != null)
            {
                minus.onClick.RemoveAllListeners();
                minus.onClick.AddListener(() => ChangeBuy(entry, -1));
            }
        }
    }

#if UNITY_EDITOR
    /// <summary>Builds the editable hierarchy while not in play mode.</summary>
    public void BuildForEditor()
    {
        Build();
        if (canvas != null) canvas.gameObject.SetActive(true);
        if (panel != null) panel.SetActive(true);
    }
#endif

    private void BuildColumn(ArmoryCatalog.Category category, float x, Color accent)
    {
        string suffix = category.ToString();
        Image column = RuntimeUI.CreateImage(panel.transform, suffix + "Column", ColumnColor);
        RectTransform columnRt = column.rectTransform;
        columnRt.anchorMin = new Vector2(0.5f, 0.5f);
        columnRt.anchorMax = new Vector2(0.5f, 0.5f);
        columnRt.pivot = new Vector2(0.5f, 0.5f);
        columnRt.anchoredPosition = new Vector2(x, -38f);
        columnRt.sizeDelta = new Vector2(500f, 650f);

        TMP_Text header = RuntimeUI.CreateText(column.transform, "Header",
            category == ArmoryCatalog.Category.Weapon ? "WEAPONS" :
            category == ArmoryCatalog.Category.Ammo ? "AMMO" : "TRAPS",
            30f, accent, TextAlignmentOptions.Left);
        header.fontStyle = FontStyles.Bold;
        SetRect(header.rectTransform, new Vector2(0f, 1f), new Vector2(20f, -12f),
            new Vector2(420f, 40f), new Vector2(0f, 1f));

        TMP_Text hint = RuntimeUI.CreateText(column.transform, "Hint", "Tap + to add",
            17f, TextMuted, TextAlignmentOptions.Right);
        SetRect(hint.rectTransform, new Vector2(1f, 1f), new Vector2(-20f, -18f),
            new Vector2(260f, 26f), new Vector2(1f, 1f));

        GameObject containerGo = new GameObject("BuyRows_" + suffix,
            typeof(RectTransform), typeof(VerticalLayoutGroup));
        containerGo.transform.SetParent(column.transform, false);
        RectTransform container = containerGo.GetComponent<RectTransform>();
        container.anchorMin = new Vector2(0.5f, 1f);
        container.anchorMax = new Vector2(0.5f, 1f);
        container.pivot = new Vector2(0.5f, 1f);
        container.anchoredPosition = new Vector2(0f, -58f);
        container.sizeDelta = new Vector2(460f, 570f);

        VerticalLayoutGroup layout = containerGo.GetComponent<VerticalLayoutGroup>();
        layout.spacing = 5f;
        layout.childAlignment = TextAnchor.UpperCenter;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;

        foreach (ArmoryCatalog.Entry entry in ArmoryCatalog.ForCategory(category))
        {
            BuyRow row = CreateArmoryRow(container, entry, accent);
            if (row != null) buyRows.Add(row);
        }
    }

    private BuyRow CreateArmoryRow(Transform parent, ArmoryCatalog.Entry entry, Color accent)
    {
        GameObject root = new GameObject("Row_" + entry.Id,
            typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(LayoutElement));
        root.transform.SetParent(parent, false);

        LayoutElement layout = root.GetComponent<LayoutElement>();
        layout.minHeight = 54f;
        layout.preferredHeight = 54f;
        layout.flexibleHeight = 0f;

        Image background = root.GetComponent<Image>();
        background.color = RowColor;

        Image swatch = RuntimeUI.CreateImage(root.transform, "Swatch", entry.Color);
        SetRect(swatch.rectTransform, new Vector2(0f, 0.5f), new Vector2(10f, 0f),
            new Vector2(24f, 24f), new Vector2(0f, 0.5f));
        swatch.raycastTarget = false;

        TMP_Text name = RuntimeUI.CreateText(root.transform, "Name", entry.DisplayName,
            21f, Color.white, TextAlignmentOptions.Left);
        SetRect(name.rectTransform, new Vector2(0f, 0.5f), new Vector2(40f, 0f),
            new Vector2(160f, 38f), new Vector2(0f, 0.5f));

        TMP_Text priceLabel = RuntimeUI.CreateText(root.transform, "Price", "$" + entry.Price,
            19f, new Color(0.90f, 0.85f, 0.72f), TextAlignmentOptions.Left);
        priceLabel.fontStyle = FontStyles.Bold;
        SetRect(priceLabel.rectTransform, new Vector2(0f, 0.5f), new Vector2(200f, 0f),
            new Vector2(70f, 36f), new Vector2(0f, 0.5f));

        TMP_Text countLabel = RuntimeUI.CreateText(root.transform, "Count", "0",
            21f, Color.white);
        countLabel.fontStyle = FontStyles.Bold;
        SetRect(countLabel.rectTransform, new Vector2(1f, 0.5f), new Vector2(-95f, 0f),
            new Vector2(38f, 36f), new Vector2(0.5f, 0.5f));

        RuntimeUI.CreateButton(root.transform, "Plus", "+",
            new Vector2(32f, 34f), new Vector2(-40f, 0f), new Vector2(1f, 0.5f),
            accent, () => ChangeBuy(entry, 1));

        RuntimeUI.CreateButton(root.transform, "Minus", "-",
            new Vector2(32f, 34f), new Vector2(-145f, 0f), new Vector2(1f, 0.5f),
            new Color(0.31f, 0.28f, 0.25f, 1f), () => ChangeBuy(entry, -1));

        return new BuyRow { Entry = entry, Count = countLabel };
    }

    // ------------------------------------------------------------------
    // CART
    // ------------------------------------------------------------------

    private void ChangeBuy(ArmoryCatalog.Entry entry, int delta)
    {
        if (entry == null) return;

        int count;
        buyCart.TryGetValue(entry, out count);
        count = Mathf.Clamp(count + delta, 0, 99);

        if (count <= 0) buyCart.Remove(entry);
        else buyCart[entry] = count;

        RefreshAll();
    }

    private void ClearCart()
    {
        buyCart.Clear();
        RefreshAll();
    }

    private void RefreshAll()
    {
        PlayerInventory inventory = PlayerInventory.instance;
        MoneyManager money = MoneyManager.GetOrCreate();

        if (moneyLabel != null)
            moneyLabel.text = "$" + (money != null ? money.money : 0);

        if (capacityLabel != null)
        {
            string bag = inventory != null
                ? "Backpack  " + inventory.backpackItems.Count + " / " +
                  Mathf.Max(1, inventory.maxBackpackItems)
                : "Backpack  ?";
            capacityLabel.text = bag + "     ·     Ammo  " +
                                 WeaponInstance.PistolAmmo + " / " +
                                 WeaponInstance.PistolMaxAmmo;
        }

        foreach (BuyRow row in buyRows)
        {
            if (row == null || row.Count == null) continue;

            int count;
            buyCart.TryGetValue(row.Entry, out count);
            row.Count.text = count.ToString();
        }

        int total = BuyTotal();
        if (totalsLabel != null)
        {
            totalsLabel.text = "ORDER TOTAL   $" + total;
            totalsLabel.color = total > 0
                ? new Color(1f, 0.80f, 0.48f)
                : new Color(0.70f, 0.70f, 0.68f);
        }
    }

    private int BuyTotal()
    {
        int total = 0;
        foreach (KeyValuePair<ArmoryCatalog.Entry, int> pair in buyCart)
            total += pair.Key.Price * pair.Value;
        return total;
    }

    private int CartItemCount()
    {
        int total = 0;
        foreach (int count in buyCart.Values) total += count;
        return total;
    }

    // ------------------------------------------------------------------
    // CHECKOUT
    // ------------------------------------------------------------------

    private void Checkout()
    {
        int itemCount = CartItemCount();
        if (itemCount == 0)
        {
            ToastUI.Show("Your armory order is empty.");
            return;
        }

        PlayerInventory inventory = PlayerInventory.instance;
        if (inventory == null)
        {
            ToastUI.Show("No backpack was found.");
            return;
        }

        int bagItems = 0;
        int ammoRounds = 0;

        foreach (KeyValuePair<ArmoryCatalog.Entry, int> pair in buyCart)
        {
            if (pair.Key.IsAmmo) ammoRounds += pair.Key.AmmoAmount * pair.Value;
            else bagItems += pair.Value;
        }

        if (bagItems > inventory.FreeBackpackSpace)
        {
            ToastUI.Show("Not enough backpack space. Free " +
                         (bagItems - inventory.FreeBackpackSpace) + " slot(s).");
            return;
        }

        if (ammoRounds > 0)
        {
            int capacity = WeaponInstance.PistolMaxAmmo - WeaponInstance.PistolAmmo;
            if (ammoRounds > capacity)
            {
                ToastUI.Show("Pistol ammo holds " + WeaponInstance.PistolMaxAmmo +
                             " rounds. You can buy " + Mathf.Max(0, capacity) + " more.");
                return;
            }
        }

        int total = BuyTotal();
        MoneyManager money = MoneyManager.GetOrCreate();
        if (money == null || money.money < total)
        {
            ToastUI.Show("Not enough money for that order.");
            return;
        }

        Dictionary<ArmoryCatalog.Entry, GameObject> prefabs =
            new Dictionary<ArmoryCatalog.Entry, GameObject>();

        foreach (KeyValuePair<ArmoryCatalog.Entry, int> pair in buyCart)
        {
            if (pair.Key.IsAmmo) continue;

            GameObject prefab = ArmoryCatalog.LoadPrefab(pair.Key);
            if (prefab == null)
            {
                ToastUI.Show("Missing armory prefab: " + pair.Key.Id);
                return;
            }
            prefabs[pair.Key] = prefab;
        }

        if (!money.TrySpend(total)) return;

        foreach (KeyValuePair<ArmoryCatalog.Entry, int> pair in buyCart)
        {
            if (!pair.Key.IsAmmo) continue;
            WeaponInstance.AddAmmo(pair.Key.AmmoAmount * pair.Value);
        }

        foreach (KeyValuePair<ArmoryCatalog.Entry, GameObject> pair in prefabs)
        {
            int count;
            buyCart.TryGetValue(pair.Key, out count);
            for (int i = 0; i < count; i++)
                inventory.AddItemToBackpack(pair.Value);
        }

        ToastUI.Show("Armory purchase complete: " + itemCount +
                     " item(s) for $" + total + ".");
        buyCart.Clear();
        RefreshAll();
    }

    // ------------------------------------------------------------------
    // HELPERS
    // ------------------------------------------------------------------

    private void EnsureParentsActive()
    {
        Transform current = panel != null ? panel.transform : transform;
        while (current != null)
        {
            if (!current.gameObject.activeSelf) current.gameObject.SetActive(true);
            current = current.parent;
        }
    }

    private static void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    private static void SetRect(RectTransform rect, Vector2 anchor, Vector2 position,
        Vector2 size, Vector2 pivot)
    {
        rect.anchorMin = anchor;
        rect.anchorMax = anchor;
        rect.pivot = pivot;
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
    }
}
