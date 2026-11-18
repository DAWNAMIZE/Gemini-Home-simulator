using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// The market screen opened by the Market_Man. Ingredients are bought on the
/// left and sold from the backpack on the right. Checkout nets the two carts,
/// validates backpack space and only then moves money and items.
/// </summary>
public class MarketUI : MonoBehaviour
{
    public static MarketUI instance;

    private sealed class BuyRow
    {
        public IngredientCatalog.Entry Entry;
        public TMP_Text Count;
    }

    private static readonly Color PanelColor = new Color(0.055f, 0.085f, 0.075f, 0.985f);
    private static readonly Color ColumnColor = new Color(0.085f, 0.12f, 0.11f, 1f);
    private static readonly Color RowColor = new Color(0.12f, 0.16f, 0.15f, 1f);
    private static readonly Color BuyAccent = new Color(0.30f, 0.72f, 0.46f, 1f);
    private static readonly Color SellAccent = new Color(0.88f, 0.66f, 0.28f, 1f);

    private Canvas canvas;
    private GameObject panel;
    private TMP_Text moneyLabel;
    private TMP_Text totalsLabel;
    private TMP_Text capacityLabel;
    private RectTransform sellRowsContainer;

    private readonly Dictionary<IngredientCatalog.Entry, int> buyCart =
        new Dictionary<IngredientCatalog.Entry, int>();
    private readonly Dictionary<string, int> sellCart =
        new Dictionary<string, int>();
    private readonly List<BuyRow> buyRows = new List<BuyRow>();
    private bool wired;

    public static MarketUI GetOrCreate()
    {
        if (instance != null) return instance;

        MarketUI existing = Object.FindFirstObjectByType<MarketUI>(FindObjectsInactive.Include);
        if (existing != null)
        {
            instance = existing;
            return existing;
        }

        GameObject go = new GameObject("MarketUI");
        instance = go.AddComponent<MarketUI>();
        return instance;
    }

    public static void Show(MarketMan marketMan)
    {
        GetOrCreate().ShowInternal(marketMan);
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
        // Reuse the editable hierarchy from the scene when it exists.
        if (TryBindExistingUI())
        {
            WireExistingButtons();
            if (panel != null) panel.SetActive(false);
        }
    }

    void OnDestroy()
    {
        if (instance == this) instance = null;
    }

    private void ShowInternal(MarketMan marketMan)
    {
        Build();
        buyCart.Clear();
        sellCart.Clear();
        panel.SetActive(true);
        EnsureParentsActive();
        RefreshAll();
    }

    private void HideInternal()
    {
        if (panel != null) panel.SetActive(false);
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

        canvas = RuntimeUI.CreateOverlayCanvas("MarketCanvas", 960);
        canvas.transform.SetParent(transform, false);

        Image backdrop = RuntimeUI.CreateImage(canvas.transform, "Backdrop",
            new Color(0f, 0f, 0f, 0.68f));
        Stretch(backdrop.rectTransform);

        Image panelImage = RuntimeUI.CreateImage(backdrop.transform, "MarketPanel", PanelColor);
        panel = panelImage.gameObject;

        RectTransform panelRt = panelImage.rectTransform;
        panelRt.anchorMin = new Vector2(0.5f, 0.5f);
        panelRt.anchorMax = new Vector2(0.5f, 0.5f);
        panelRt.pivot = new Vector2(0.5f, 0.5f);
        panelRt.anchoredPosition = Vector2.zero;
        panelRt.sizeDelta = new Vector2(1520f, 880f);

        Image topStrip = RuntimeUI.CreateImage(panel.transform, "TopStrip", BuyAccent);
        RectTransform stripRt = topStrip.rectTransform;
        stripRt.anchorMin = new Vector2(0f, 1f);
        stripRt.anchorMax = new Vector2(1f, 1f);
        stripRt.pivot = new Vector2(0.5f, 1f);
        stripRt.anchoredPosition = Vector2.zero;
        stripRt.sizeDelta = new Vector2(0f, 8f);

        TMP_Text title = RuntimeUI.CreateText(panel.transform, "Title", "MARKET",
            44f, new Color(0.72f, 0.96f, 0.80f));
        title.fontStyle = FontStyles.Bold;
        title.alignment = TextAlignmentOptions.Left;
        SetRect(title.rectTransform, new Vector2(0f, 1f), new Vector2(36f, -26f),
            new Vector2(500f, 56f), new Vector2(0f, 1f));

        TMP_Text subtitle = RuntimeUI.CreateText(panel.transform, "Subtitle",
            "Buy fresh ingredients on the left · sell from your backpack on the right",
            20f, new Color(0.72f, 0.78f, 0.74f), TextAlignmentOptions.Left);
        SetRect(subtitle.rectTransform, new Vector2(0f, 1f), new Vector2(38f, -78f),
            new Vector2(900f, 30f), new Vector2(0f, 1f));

        moneyLabel = RuntimeUI.CreateText(panel.transform, "Money", "$0",
            38f, new Color(0.60f, 0.95f, 0.68f), TextAlignmentOptions.Right);
        moneyLabel.fontStyle = FontStyles.Bold;
        SetRect(moneyLabel.rectTransform, new Vector2(1f, 1f), new Vector2(-100f, -30f),
            new Vector2(420f, 52f), new Vector2(1f, 1f));

        capacityLabel = RuntimeUI.CreateText(panel.transform, "Capacity", string.Empty,
            20f, new Color(0.72f, 0.78f, 0.74f), TextAlignmentOptions.Right);
        SetRect(capacityLabel.rectTransform, new Vector2(1f, 1f), new Vector2(-100f, -80f),
            new Vector2(420f, 30f), new Vector2(1f, 1f));

        BuildBuyColumn();
        BuildSellColumn();

        totalsLabel = RuntimeUI.CreateText(panel.transform, "Totals", string.Empty,
            25f, Color.white);
        totalsLabel.fontStyle = FontStyles.Bold;
        SetRect(totalsLabel.rectTransform, new Vector2(0.5f, 0f), new Vector2(0f, 66f),
            new Vector2(780f, 52f), new Vector2(0.5f, 0f));

        RuntimeUI.CreateButton(panel.transform, "ClearButton", "CLEAR",
            new Vector2(180f, 66f), new Vector2(-560f, 56f), new Vector2(0.5f, 0f),
            new Color(0.26f, 0.25f, 0.23f, 1f), ClearCarts);

        RuntimeUI.CreateButton(panel.transform, "CheckoutButton", "CHECKOUT",
            new Vector2(300f, 76f), new Vector2(540f, 56f), new Vector2(0.5f, 0f),
            new Color(0.16f, 0.47f, 0.27f, 1f), Checkout);

        RuntimeUI.CreateButton(panel.transform, "CloseButton", "X",
            new Vector2(52f, 52f), new Vector2(-30f, -30f), new Vector2(1f, 1f),
            new Color(0.24f, 0.22f, 0.22f, 1f), HideInternal);

        wired = true;
        panel.SetActive(false);
    }

    /// <summary>
    /// Binds the scene hierarchy created by the editor layout builder. Buy rows
    /// are reused, sell rows are still rebuilt from the backpack contents.
    /// </summary>
    private bool TryBindExistingUI()
    {
        Transform canvasTransform = RuntimeUI.FindDeepChild(transform, "MarketCanvas");
        if (canvasTransform == null) return false;

        Transform panelTransform = RuntimeUI.FindDeepChild(canvasTransform, "MarketPanel");
        if (panelTransform == null) return false;

        canvas = canvasTransform.GetComponent<Canvas>();
        panel = panelTransform.gameObject;
        moneyLabel = RuntimeUI.FindComponentByName<TMP_Text>(panelTransform, "Money");
        totalsLabel = RuntimeUI.FindComponentByName<TMP_Text>(panelTransform, "Totals");
        capacityLabel = RuntimeUI.FindComponentByName<TMP_Text>(panelTransform, "Capacity");

        RectTransform sellRows = RuntimeUI.FindComponentByName<RectTransform>(
            panelTransform, "SellRows");
        sellRowsContainer = sellRows;

        buyRows.Clear();
        Transform buyContainer = RuntimeUI.FindDeepChild(panelTransform, "BuyRows");
        if (buyContainer != null)
        {
            for (int i = 0; i < buyContainer.childCount; i++)
            {
                Transform row = buyContainer.GetChild(i);
                if (row == null) continue;

                string label = row.name.StartsWith("Row_")
                    ? row.name.Substring(4)
                    : row.name;
                IngredientCatalog.Entry entry = IngredientCatalog.Get(label);
                TMP_Text count = RuntimeUI.FindComponentByName<TMP_Text>(row, "Count");
                if (entry != null && count != null)
                    buyRows.Add(new BuyRow { Entry = entry, Count = count });
            }
        }

        return sellRowsContainer != null;
    }

    private void WireExistingButtons()
    {
        if (wired || panel == null) return;
        wired = true;

        Button clear = RuntimeUI.FindComponentByName<Button>(panel.transform, "ClearButton");
        if (clear != null)
        {
            clear.onClick.RemoveAllListeners();
            clear.onClick.AddListener(ClearCarts);
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

        Transform buyContainer = RuntimeUI.FindDeepChild(panel.transform, "BuyRows");
        if (buyContainer == null) return;

        for (int i = 0; i < buyContainer.childCount; i++)
        {
            Transform row = buyContainer.GetChild(i);
            if (row == null) continue;

            string label = row.name.StartsWith("Row_")
                ? row.name.Substring(4)
                : row.name;
            IngredientCatalog.Entry entry = IngredientCatalog.Get(label);
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
        if (panel != null) panel.SetActive(true);
    }
#endif

    private void BuildBuyColumn()
    {
        Image column = RuntimeUI.CreateImage(panel.transform, "BuyColumn", ColumnColor);
        RectTransform columnRt = column.rectTransform;
        columnRt.anchorMin = new Vector2(0.5f, 0.5f);
        columnRt.anchorMax = new Vector2(0.5f, 0.5f);
        columnRt.pivot = new Vector2(0.5f, 0.5f);
        columnRt.anchoredPosition = new Vector2(-375f, -40f);
        columnRt.sizeDelta = new Vector2(700f, 660f);

        TMP_Text header = RuntimeUI.CreateText(column.transform, "Header", "BUY",
            30f, BuyAccent, TextAlignmentOptions.Left);
        header.fontStyle = FontStyles.Bold;
        SetRect(header.rectTransform, new Vector2(0f, 1f), new Vector2(22f, -14f),
            new Vector2(420f, 40f), new Vector2(0f, 1f));

        TMP_Text hint = RuntimeUI.CreateText(column.transform, "Hint", "Tap + to add to cart",
            18f, new Color(0.66f, 0.74f, 0.70f), TextAlignmentOptions.Right);
        SetRect(hint.rectTransform, new Vector2(1f, 1f), new Vector2(-22f, -20f),
            new Vector2(300f, 28f), new Vector2(1f, 1f));

        GameObject containerGo = new GameObject("BuyRows",
            typeof(RectTransform), typeof(VerticalLayoutGroup));
        containerGo.transform.SetParent(column.transform, false);
        RectTransform container = containerGo.GetComponent<RectTransform>();
        container.anchorMin = new Vector2(0.5f, 1f);
        container.anchorMax = new Vector2(0.5f, 1f);
        container.pivot = new Vector2(0.5f, 1f);
        container.anchoredPosition = new Vector2(0f, -62f);
        container.sizeDelta = new Vector2(664f, 580f);

        VerticalLayoutGroup layout = containerGo.GetComponent<VerticalLayoutGroup>();
        layout.spacing = 4f;
        layout.childAlignment = TextAnchor.UpperCenter;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;

        foreach (IngredientCatalog.Entry entry in IngredientCatalog.All)
        {
            BuyRow row = CreateMarketRow(container, entry.Name, entry.BuyPrice,
                entry.Color, BuyAccent, true, entry);
            if (row != null) buyRows.Add(row);
        }
    }

    private void BuildSellColumn()
    {
        Image column = RuntimeUI.CreateImage(panel.transform, "SellColumn", ColumnColor);
        RectTransform columnRt = column.rectTransform;
        columnRt.anchorMin = new Vector2(0.5f, 0.5f);
        columnRt.anchorMax = new Vector2(0.5f, 0.5f);
        columnRt.pivot = new Vector2(0.5f, 0.5f);
        columnRt.anchoredPosition = new Vector2(375f, -40f);
        columnRt.sizeDelta = new Vector2(700f, 660f);

        TMP_Text header = RuntimeUI.CreateText(column.transform, "Header", "SELL",
            30f, SellAccent, TextAlignmentOptions.Left);
        header.fontStyle = FontStyles.Bold;
        SetRect(header.rectTransform, new Vector2(0f, 1f), new Vector2(22f, -14f),
            new Vector2(420f, 40f), new Vector2(0f, 1f));

        TMP_Text hint = RuntimeUI.CreateText(column.transform, "Hint", "From your backpack",
            18f, new Color(0.66f, 0.74f, 0.70f), TextAlignmentOptions.Right);
        SetRect(hint.rectTransform, new Vector2(1f, 1f), new Vector2(-22f, -20f),
            new Vector2(300f, 28f), new Vector2(1f, 1f));

        GameObject containerGo = new GameObject("SellRows",
            typeof(RectTransform), typeof(VerticalLayoutGroup));
        containerGo.transform.SetParent(column.transform, false);
        sellRowsContainer = containerGo.GetComponent<RectTransform>();
        sellRowsContainer.anchorMin = new Vector2(0.5f, 1f);
        sellRowsContainer.anchorMax = new Vector2(0.5f, 1f);
        sellRowsContainer.pivot = new Vector2(0.5f, 1f);
        sellRowsContainer.anchoredPosition = new Vector2(0f, -62f);
        sellRowsContainer.sizeDelta = new Vector2(664f, 580f);

        VerticalLayoutGroup layout = containerGo.GetComponent<VerticalLayoutGroup>();
        layout.spacing = 4f;
        layout.childAlignment = TextAnchor.UpperCenter;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;
    }

    private BuyRow CreateMarketRow(Transform parent, string label, int price,
        Color swatchColor, Color accent, bool buying, IngredientCatalog.Entry entry)
    {
        GameObject root = new GameObject("Row_" + label,
            typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(LayoutElement));
        root.transform.SetParent(parent, false);

        LayoutElement layout = root.GetComponent<LayoutElement>();
        layout.minHeight = 40f;
        layout.preferredHeight = 40f;
        layout.flexibleHeight = 0f;

        Image background = root.GetComponent<Image>();
        background.color = RowColor;

        Image swatch = RuntimeUI.CreateImage(root.transform, "Swatch", swatchColor);
        SetRect(swatch.rectTransform, new Vector2(0f, 0.5f), new Vector2(12f, 0f),
            new Vector2(26f, 26f), new Vector2(0f, 0.5f));
        swatch.raycastTarget = false;

        TMP_Text name = RuntimeUI.CreateText(root.transform, "Name", label,
            21f, Color.white, TextAlignmentOptions.Left);
        SetRect(name.rectTransform, new Vector2(0f, 0.5f), new Vector2(48f, 0f),
            new Vector2(280f, 36f), new Vector2(0f, 0.5f));

        TMP_Text priceLabel = RuntimeUI.CreateText(root.transform, "Price", "$" + price,
            19f, new Color(0.76f, 0.86f, 0.78f), TextAlignmentOptions.Left);
        priceLabel.fontStyle = FontStyles.Bold;
        SetRect(priceLabel.rectTransform, new Vector2(0f, 0.5f), new Vector2(336f, 0f),
            new Vector2(90f, 36f), new Vector2(0f, 0.5f));

        TMP_Text countLabel = RuntimeUI.CreateText(root.transform, "Count", "0",
            21f, Color.white);
        countLabel.fontStyle = FontStyles.Bold;
        SetRect(countLabel.rectTransform, new Vector2(1f, 0.5f), new Vector2(-120f, 0f),
            new Vector2(52f, 36f), new Vector2(0.5f, 0.5f));

        IngredientCatalog.Entry capturedEntry = entry;
        string capturedName = label;

        RuntimeUI.CreateButton(root.transform, "Plus", "+",
            new Vector2(36f, 32f), new Vector2(-50f, 0f), new Vector2(1f, 0.5f),
            accent, () =>
            {
                if (buying) ChangeBuy(capturedEntry, 1);
                else ChangeSell(capturedName, 1);
            });

        RuntimeUI.CreateButton(root.transform, "Minus", "-",
            new Vector2(36f, 32f), new Vector2(-176f, 0f), new Vector2(1f, 0.5f),
            new Color(0.31f, 0.28f, 0.25f, 1f), () =>
            {
                if (buying) ChangeBuy(capturedEntry, -1);
                else ChangeSell(capturedName, -1);
            });

        return buying ? new BuyRow { Entry = capturedEntry, Count = countLabel } : null;
    }

    // ------------------------------------------------------------------
    // CART
    // ------------------------------------------------------------------

    private void ChangeBuy(IngredientCatalog.Entry entry, int delta)
    {
        if (entry == null) return;

        int count;
        buyCart.TryGetValue(entry, out count);
        count = Mathf.Clamp(count + delta, 0, 99);

        if (count <= 0) buyCart.Remove(entry);
        else buyCart[entry] = count;

        RefreshAll();
    }

    private void ChangeSell(string canonical, int delta)
    {
        if (string.IsNullOrEmpty(canonical)) return;

        int available = IngredientCatalog.CountInBackpack(canonical);
        int count;
        sellCart.TryGetValue(canonical, out count);
        count = Mathf.Clamp(count + delta, 0, Mathf.Max(0, available));

        if (count <= 0) sellCart.Remove(canonical);
        else sellCart[canonical] = count;

        RefreshAll();
    }

    private void ClearCarts()
    {
        buyCart.Clear();
        sellCart.Clear();
        RefreshAll();
    }

    private void RefreshAll()
    {
        PlayerInventory inventory = PlayerInventory.instance;
        MoneyManager money = MoneyManager.GetOrCreate();

        if (moneyLabel != null)
            moneyLabel.text = "$" + (money != null ? money.money : 0);

        if (capacityLabel != null && inventory != null)
            capacityLabel.text = "Backpack  " + inventory.backpackItems.Count +
                                 " / " + Mathf.Max(1, inventory.maxBackpackItems);

        foreach (BuyRow row in buyRows)
        {
            if (row == null || row.Count == null) continue;

            int count;
            buyCart.TryGetValue(row.Entry, out count);
            row.Count.text = count.ToString();
        }

        RebuildSellRows();

        int buyTotal = BuyTotal();
        int sellTotal = SellTotal();
        int net = buyTotal - sellTotal;

        if (totalsLabel != null)
        {
            totalsLabel.text = "BUY  $" + buyTotal + "    SELL  $" + sellTotal +
                               "    NET  " + (net >= 0 ? "-$" + net : "+$" + (-net));
            totalsLabel.color = net > 0
                ? new Color(1f, 0.74f, 0.52f)
                : new Color(0.68f, 0.96f, 0.74f);
        }
    }

    private void RebuildSellRows()
    {
        if (sellRowsContainer == null) return;

        // Detach before the deferred Destroy so the layout group does not keep
        // both old and new rows for a frame.
        for (int i = sellRowsContainer.childCount - 1; i >= 0; i--)
        {
            Transform child = sellRowsContainer.GetChild(i);
            child.SetParent(null, false);
            Destroy(child.gameObject);
        }

        bool any = false;

        foreach (IngredientCatalog.Entry entry in IngredientCatalog.All)
        {
            int inBag = IngredientCatalog.CountInBackpack(entry.Name);
            if (inBag <= 0) continue;

            int carted;
            sellCart.TryGetValue(entry.Name, out carted);

            CreateMarketRow(sellRowsContainer, entry.Name, entry.SellPrice,
                entry.Color, SellAccent, false, entry);

            // The helper returns null for sell rows; find the freshly added row
            // and fill in its cart count.
            if (sellRowsContainer.childCount > 0)
            {
                Transform row = sellRowsContainer.GetChild(sellRowsContainer.childCount - 1);
                TMP_Text countLabel = FindText(row, "Count");
                if (countLabel != null) countLabel.text = carted + " / " + inBag;
            }

            any = true;
        }

        if (!any)
        {
            GameObject empty = new GameObject("EmptySell",
                typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(LayoutElement));
            empty.transform.SetParent(sellRowsContainer, false);

            LayoutElement layout = empty.GetComponent<LayoutElement>();
            layout.preferredHeight = 60f;
            layout.minHeight = 60f;

            Image bg = empty.GetComponent<Image>();
            bg.color = RowColor;

            TMP_Text label = RuntimeUI.CreateText(empty.transform, "Label",
                "No ingredients in the backpack to sell.", 20f,
                new Color(0.70f, 0.75f, 0.72f));
            Stretch(label.rectTransform);
        }
    }

    private int BuyTotal()
    {
        int total = 0;
        foreach (KeyValuePair<IngredientCatalog.Entry, int> pair in buyCart)
            total += pair.Key.BuyPrice * pair.Value;
        return total;
    }

    private int SellTotal()
    {
        int total = 0;
        foreach (KeyValuePair<string, int> pair in sellCart)
            total += IngredientCatalog.SellPriceOf(pair.Key) * pair.Value;
        return total;
    }

    // ------------------------------------------------------------------
    // CHECKOUT
    // ------------------------------------------------------------------

    private void Checkout()
    {
        int buyCount = 0;
        foreach (int count in buyCart.Values) buyCount += count;

        int sellCount = 0;
        foreach (int count in sellCart.Values) sellCount += count;

        if (buyCount == 0 && sellCount == 0)
        {
            ToastUI.Show("Your cart is empty.");
            return;
        }

        PlayerInventory inventory = PlayerInventory.instance;
        if (inventory == null)
        {
            ToastUI.Show("No backpack was found.");
            return;
        }

        int freeAfterSales = inventory.FreeBackpackSpace + sellCount;
        if (buyCount > freeAfterSales)
        {
            ToastUI.Show("Not enough backpack space. Free " + (buyCount - freeAfterSales) +
                         " slot(s) or sell more.");
            return;
        }

        int buyTotal = BuyTotal();
        int sellTotal = SellTotal();
        MoneyManager money = MoneyManager.GetOrCreate();

        if (money == null || money.money + sellTotal < buyTotal)
        {
            ToastUI.Show("Not enough money for that trade.");
            return;
        }

        // Confirm every bought prefab exists before charging anything.
        Dictionary<IngredientCatalog.Entry, GameObject> prefabs =
            new Dictionary<IngredientCatalog.Entry, GameObject>();

        foreach (KeyValuePair<IngredientCatalog.Entry, int> pair in buyCart)
        {
            GameObject prefab = IngredientCatalog.LoadPrefab(pair.Key.Name);
            if (prefab == null)
            {
                ToastUI.Show("Missing ingredient prefab: " + pair.Key.Name);
                return;
            }
            prefabs[pair.Key] = prefab;
        }

        // Sell first so the freed slots can receive the purchased items.
        foreach (KeyValuePair<string, int> pair in sellCart)
            RemoveIngredientCopies(pair.Key, pair.Value);

        if (sellTotal > 0) money.AddMoney(sellTotal);
        if (buyTotal > 0 && !money.TrySpend(buyTotal))
        {
            // Validation above should make this unreachable; undo the sale.
            if (sellTotal > 0) money.AddMoney(-sellTotal);
            ToastUI.Show("Not enough money for that trade.");
            return;
        }

        foreach (KeyValuePair<IngredientCatalog.Entry, GameObject> pair in prefabs)
        {
            for (int i = 0; i < buyCart[pair.Key]; i++)
                inventory.AddItemToBackpack(pair.Value);
        }

        int spentNet = buyTotal - sellTotal;
        ToastUI.Show(spentNet >= 0
            ? "Market: bought " + buyCount + " item(s), sold " + sellCount + "."
            : "Market: sold " + sellCount + " item(s) for $" + (-spentNet) + ".");

        buyCart.Clear();
        sellCart.Clear();
        RefreshAll();
    }

    private static void RemoveIngredientCopies(string canonical, int count)
    {
        PlayerInventory inventory = PlayerInventory.instance;
        if (inventory == null || count <= 0) return;

        for (int i = inventory.backpackItems.Count - 1; i >= 0 && count > 0; i--)
        {
            GameObject item = inventory.backpackItems[i];
            if (item == null) continue;

            string identifier = inventory.GetItemIdentifier(item);
            if (!IngredientCatalog.Matches(identifier, canonical)) continue;

            inventory.RemoveBackpackItemAt(i);
            count--;
        }
    }

    // ------------------------------------------------------------------
    // HELPERS
    // ------------------------------------------------------------------

    private static TMP_Text FindText(Transform parent, string name)
    {
        if (parent == null) return null;

        TMP_Text[] labels = parent.GetComponentsInChildren<TMP_Text>(true);
        foreach (TMP_Text label in labels)
            if (label.name == name) return label;

        return null;
    }

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
