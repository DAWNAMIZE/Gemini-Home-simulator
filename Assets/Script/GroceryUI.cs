using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Grocery page for the in-game phone. The GROCERY button sits beside TASKS on
/// the phone panel and opens a shopping list grouped by Nigerian cuisine.
/// Selecting a dish shows every ingredient it needs and whether the player
/// already has it in the backpack.
/// </summary>
public class GroceryUI : MonoBehaviour
{
    public static GroceryUI instance;

    private sealed class RecipeRow
    {
        public Recipe Recipe;
        public Image Background;
        public Image Accent;
    }

    private sealed class IngredientRow
    {
        public string Name;
        public TMP_Text Status;
    }

    private const float PanelWidth = 540f;
    private const float PanelHeight = 650f;
    private const float RecipeColumnWidth = 200f;
    private const float DetailColumnWidth = 290f;
    private const float ColumnTop = -128f;
    private const float ColumnBottom = 70f;
    private const float ColumnPadding = 10f;
    private const float RecipeListHeight = PanelHeight + ColumnTop - ColumnBottom - ColumnPadding * 2f;
    private const float IngredientRowHeight = 28f;

    private static readonly Color PanelColor = new Color(0.052f, 0.078f, 0.068f, 0.985f);
    private static readonly Color ColumnColor = new Color(0.078f, 0.112f, 0.100f, 1f);
    private static readonly Color DetailColumnColor = new Color(0.088f, 0.124f, 0.110f, 1f);
    private static readonly Color TabInactiveColor = new Color(0.130f, 0.170f, 0.155f, 1f);
    private static readonly Color AccentColor = new Color(0.33f, 0.76f, 0.49f, 1f);
    private static readonly Color AccentDimColor = new Color(0.20f, 0.46f, 0.31f, 1f);
    private static readonly Color RowColor = new Color(0.100f, 0.145f, 0.128f, 1f);
    private static readonly Color RowSelectedColor = new Color(0.160f, 0.270f, 0.215f, 1f);
    private static readonly Color MutedText = new Color(0.62f, 0.70f, 0.66f, 1f);
    private static readonly Color BuyColor = new Color(0.96f, 0.73f, 0.35f, 1f);
    private static readonly Color OwnedColor = new Color(0.47f, 0.87f, 0.62f, 1f);
    private static readonly Color Transparent = new Color(0f, 0f, 0f, 0f);

    private GameObject panel;
    private RectTransform recipeList;
    private RectTransform ingredientListRect;
    private TMP_Text recipeListEmpty;
    private TMP_Text detailTitle;
    private TMP_Text detailSubtitle;
    private TMP_Text detailSummary;
    private TMP_Text detailEmpty;
    private GameObject detailContent;

    private readonly List<RecipeRow> recipeRows = new List<RecipeRow>();
    private readonly List<IngredientRow> ingredientRows = new List<IngredientRow>();
    private readonly Image[] tabBackgrounds = new Image[3];
    private readonly TMP_Text[] tabLabels = new TMP_Text[3];

    private NigerianCuisine activeCuisine = NigerianCuisine.Igbo;
    private Recipe selectedRecipe;
    private bool built;
    private float nextPoll;

    public static GroceryUI GetOrCreate()
    {
        if (instance != null) return instance;

        GroceryUI existing = Object.FindAnyObjectByType<GroceryUI>(
            FindObjectsInactive.Include);
        if (existing != null)
        {
            instance = existing;
            return existing;
        }

        GameObject go = new GameObject("GroceryUI");
        instance = go.AddComponent<GroceryUI>();
        return instance;
    }

    void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }
        else if (instance != this)
        {
            Destroy(gameObject);
            return;
        }
    }

    void Start()
    {
        Build();
    }

    void OnDestroy()
    {
        if (instance == this) instance = null;
    }

    void Update()
    {
        if (!built)
        {
            Build();
            if (!built) return;
        }

        if (panel != null && panel.activeInHierarchy && selectedRecipe != null &&
            Time.unscaledTime >= nextPoll)
        {
            nextPoll = Time.unscaledTime + 0.4f;
            RefreshIngredientStatuses();
        }
    }

    /// <summary>Closes the grocery page if it is open.</summary>
    public static void Hide()
    {
        if (instance != null)
        {
            instance.HidePanel();
            return;
        }

        GroceryUI found = Object.FindAnyObjectByType<GroceryUI>(
            FindObjectsInactive.Include);
        if (found != null) found.HidePanel();
    }

    public void TogglePanel()
    {
        if (!built) Build();
        if (panel == null) return;

        if (panel.activeSelf) HidePanel();
        else ShowPanel();
    }

    public void ShowPanel()
    {
        if (!built) Build();
        if (panel == null) return;

        if (PhoneUIManager.instance != null)
            PhoneUIManager.instance.HideTaskSection();

        panel.SetActive(true);
        panel.transform.SetAsLastSibling();
        RefreshAll();
    }

    public void HidePanel()
    {
        if (panel != null) panel.SetActive(false);
    }

    // ------------------------------------------------------------------
    // BUILD
    // ------------------------------------------------------------------

    private void Build()
    {
        if (built) return;

        Transform root = ResolvePhoneRoot();
        if (root == null) return;

        EnsureButton(root);
        BuildPanel(root);
        built = true;
    }

    private static Transform ResolvePhoneRoot()
    {
        PhoneUIManager phone = PhoneUIManager.instance;
        if (phone == null)
            phone = Object.FindAnyObjectByType<PhoneUIManager>(
                FindObjectsInactive.Include);

        if (phone != null && phone.playerPhonePanel != null)
            return phone.playerPhonePanel.transform;

        Transform[] all = Object.FindObjectsByType<Transform>(
            FindObjectsInactive.Include);

        foreach (Transform t in all)
        {
            if (t != null && t.name == "PhonePanel") return t;
        }

        return null;
    }

    private void EnsureButton(Transform root)
    {
        Transform existing = root.Find("GroceryButton");
        GameObject buttonGo;

        if (existing != null)
        {
            buttonGo = existing.gameObject;
            buttonGo.SetActive(true);
        }
        else
        {
            buttonGo = CreateButton(root, "GroceryButton", "GROCERY",
                new Vector2(150f, 46f), new Vector2(162f, 18f),
                new Vector2(0f, 0f), new Vector2(0f, 0f),
                AccentDimColor, TogglePanel);
        }

        Image image = buttonGo.GetComponent<Image>();
        Button button = buttonGo.GetComponent<Button>();
        if (button == null) button = buttonGo.AddComponent<Button>();
        if (image != null) button.targetGraphic = image;

        Navigation navigation = button.navigation;
        navigation.mode = Navigation.Mode.None;
        button.navigation = navigation;

        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(TogglePanel);

        if (buttonGo.GetComponentInChildren<TMP_Text>(true) == null)
        {
            TMP_Text label = RuntimeUI.CreateText(buttonGo.transform, "Label",
                "GROCERY", 19f, Color.white);
            RectTransform labelRect = label.rectTransform;
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = Vector2.zero;
            labelRect.offsetMax = Vector2.zero;
        }
    }

    private void BuildPanel(Transform root)
    {
        Transform existing = root.Find("GroceryPanel");
        if (existing != null)
        {
            existing.gameObject.SetActive(false);
            Destroy(existing.gameObject);
        }

        Image background = RuntimeUI.CreateImage(root, "GroceryPanel", PanelColor);
        panel = background.gameObject;

        RectTransform rect = background.rectTransform;
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = new Vector2(0f, -5f);
        rect.sizeDelta = new Vector2(PanelWidth, PanelHeight);

        Image accent = RuntimeUI.CreateImage(panel.transform, "GroceryAccent", AccentColor);
        RectTransform accentRect = accent.rectTransform;
        accentRect.anchorMin = new Vector2(0f, 1f);
        accentRect.anchorMax = new Vector2(1f, 1f);
        accentRect.pivot = new Vector2(0.5f, 1f);
        accentRect.anchoredPosition = Vector2.zero;
        accentRect.sizeDelta = new Vector2(0f, 5f);

        TMP_Text title = RuntimeUI.CreateText(panel.transform, "GroceryTitle",
            "GROCERY", 32f, new Color(0.83f, 0.96f, 0.87f));
        title.fontStyle = FontStyles.Bold;
        RectTransform titleRect = title.rectTransform;
        titleRect.anchorMin = new Vector2(0.5f, 1f);
        titleRect.anchorMax = new Vector2(0.5f, 1f);
        titleRect.pivot = new Vector2(0.5f, 1f);
        titleRect.anchoredPosition = new Vector2(0f, -14f);
        titleRect.sizeDelta = new Vector2(320f, 46f);

        CreateButton(panel.transform, "GroceryCloseButton", "X",
            new Vector2(42f, 42f), new Vector2(-16f, -14f),
            new Vector2(1f, 1f), new Vector2(1f, 1f),
            new Color(0.24f, 0.22f, 0.22f, 1f), HidePanel);

        BuildTabs(panel.transform);
        BuildRecipeColumn(panel.transform);
        BuildDetailColumn(panel.transform);

        CreateButton(panel.transform, "GroceryBackButton", "BACK",
            new Vector2(96f, 40f), new Vector2(16f, 14f),
            new Vector2(0f, 0f), new Vector2(0f, 0f),
            new Color(0.22f, 0.30f, 0.26f, 1f), HidePanel);

        TMP_Text footer = RuntimeUI.CreateText(panel.transform, "GroceryFooter",
            "Prices are paid at the market.", 12f, MutedText,
            TextAlignmentOptions.MidlineRight);
        RectTransform footerRect = footer.rectTransform;
        footerRect.anchorMin = new Vector2(1f, 0f);
        footerRect.anchorMax = new Vector2(1f, 0f);
        footerRect.pivot = new Vector2(1f, 0f);
        footerRect.anchoredPosition = new Vector2(-16f, 14f);
        footerRect.sizeDelta = new Vector2(330f, 40f);

        panel.SetActive(false);
    }

    private void BuildTabs(Transform parent)
    {
        string[] names = { "IGBO", "YORUBA", "HAUSA" };
        NigerianCuisine[] cuisines =
        {
            NigerianCuisine.Igbo,
            NigerianCuisine.Yoruba,
            NigerianCuisine.Hausa
        };
        float[] xPositions = { -164f, 0f, 164f };

        RectTransform tabsRect = RuntimeUI.CreateRect(parent, "GroceryTabs");
        tabsRect.anchorMin = new Vector2(0.5f, 1f);
        tabsRect.anchorMax = new Vector2(0.5f, 1f);
        tabsRect.pivot = new Vector2(0.5f, 1f);
        tabsRect.anchoredPosition = new Vector2(0f, -66f);
        tabsRect.sizeDelta = new Vector2(PanelWidth - 32f, 38f);

        for (int i = 0; i < names.Length; i++)
        {
            NigerianCuisine cuisine = cuisines[i];
            GameObject tab = CreateButton(tabsRect, "GroceryTab" + names[i],
                names[i], new Vector2(156f, 38f), new Vector2(xPositions[i], 0f),
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                TabInactiveColor, delegate { SelectCuisine(cuisine); });

            tabBackgrounds[i] = tab.GetComponent<Image>();
            tabLabels[i] = tab.GetComponentInChildren<TMP_Text>(true);
        }
    }

    private void BuildRecipeColumn(Transform parent)
    {
        Image column = RuntimeUI.CreateImage(parent, "GroceryRecipeColumn", ColumnColor);
        RectTransform rect = column.rectTransform;
        rect.anchorMin = new Vector2(0f, 0f);
        rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0f, 0.5f);
        rect.offsetMin = new Vector2(18f, ColumnBottom);
        rect.offsetMax = new Vector2(18f + RecipeColumnWidth, ColumnTop);

        recipeList = RuntimeUI.CreateRect(column.transform, "GroceryRecipeList");
        recipeList.anchorMin = Vector2.zero;
        recipeList.anchorMax = Vector2.one;
        recipeList.offsetMin = new Vector2(ColumnPadding, ColumnPadding);
        recipeList.offsetMax = new Vector2(-ColumnPadding, -ColumnPadding);

        recipeListEmpty = RuntimeUI.CreateText(column.transform, "GroceryRecipeEmpty",
            "No dishes yet.", 18f, MutedText);
        RectTransform emptyRect = recipeListEmpty.rectTransform;
        emptyRect.anchorMin = Vector2.zero;
        emptyRect.anchorMax = Vector2.one;
        emptyRect.offsetMin = new Vector2(14f, 14f);
        emptyRect.offsetMax = new Vector2(-14f, -14f);
        recipeListEmpty.gameObject.SetActive(false);
    }

    private void BuildDetailColumn(Transform parent)
    {
        Image column = RuntimeUI.CreateImage(parent, "GroceryDetailColumn",
            DetailColumnColor);
        RectTransform rect = column.rectTransform;
        rect.anchorMin = new Vector2(0f, 0f);
        rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0f, 0.5f);
        rect.offsetMin = new Vector2(18f + RecipeColumnWidth + 14f, ColumnBottom);
        rect.offsetMax = new Vector2(
            18f + RecipeColumnWidth + 14f + DetailColumnWidth, ColumnTop);

        detailContent = new GameObject("GroceryDetailContent", typeof(RectTransform));
        detailContent.transform.SetParent(column.transform, false);
        RectTransform contentRect = detailContent.GetComponent<RectTransform>();
        contentRect.anchorMin = Vector2.zero;
        contentRect.anchorMax = Vector2.one;
        contentRect.offsetMin = Vector2.zero;
        contentRect.offsetMax = Vector2.zero;

        detailTitle = RuntimeUI.CreateText(detailContent.transform, "GroceryDetailTitle",
            "", 21f, Color.white, TextAlignmentOptions.TopLeft);
        detailTitle.fontStyle = FontStyles.Bold;
        RectTransform titleRect = detailTitle.rectTransform;
        titleRect.anchorMin = new Vector2(0f, 1f);
        titleRect.anchorMax = new Vector2(1f, 1f);
        titleRect.pivot = new Vector2(0f, 1f);
        titleRect.offsetMin = new Vector2(14f, -60f);
        titleRect.offsetMax = new Vector2(-14f, -12f);

        detailSubtitle = RuntimeUI.CreateText(detailContent.transform,
            "GroceryDetailSubtitle", "", 12f, MutedText, TextAlignmentOptions.TopLeft);
        RectTransform subtitleRect = detailSubtitle.rectTransform;
        subtitleRect.anchorMin = new Vector2(0f, 1f);
        subtitleRect.anchorMax = new Vector2(1f, 1f);
        subtitleRect.pivot = new Vector2(0f, 1f);
        subtitleRect.offsetMin = new Vector2(14f, -82f);
        subtitleRect.offsetMax = new Vector2(-14f, -62f);

        TMP_Text ingredientHeader = RuntimeUI.CreateText(detailContent.transform,
            "GroceryIngredientHeader", "INGREDIENTS TO BUY", 13f, AccentColor,
            TextAlignmentOptions.TopLeft);
        ingredientHeader.fontStyle = FontStyles.Bold;
        RectTransform headerRect = ingredientHeader.rectTransform;
        headerRect.anchorMin = new Vector2(0f, 1f);
        headerRect.anchorMax = new Vector2(1f, 1f);
        headerRect.pivot = new Vector2(0f, 1f);
        headerRect.offsetMin = new Vector2(14f, -102f);
        headerRect.offsetMax = new Vector2(-14f, -84f);

        ingredientListRect = RuntimeUI.CreateRect(detailContent.transform,
            "GroceryIngredientList");
        ingredientListRect.anchorMin = new Vector2(0f, 1f);
        ingredientListRect.anchorMax = new Vector2(1f, 1f);
        ingredientListRect.pivot = new Vector2(0f, 1f);
        ingredientListRect.offsetMin = new Vector2(14f, -370f);
        ingredientListRect.offsetMax = new Vector2(-14f, -106f);

        detailSummary = RuntimeUI.CreateText(detailContent.transform, "GrocerySummary",
            "", 12f, MutedText, TextAlignmentOptions.BottomLeft);
        RectTransform summaryRect = detailSummary.rectTransform;
        summaryRect.anchorMin = new Vector2(0f, 0f);
        summaryRect.anchorMax = new Vector2(1f, 0f);
        summaryRect.pivot = new Vector2(0f, 0f);
        summaryRect.offsetMin = new Vector2(14f, 14f);
        summaryRect.offsetMax = new Vector2(-14f, 64f);

        detailEmpty = RuntimeUI.CreateText(column.transform, "GroceryDetailEmpty",
            "", 20f, MutedText);
        RectTransform emptyRect = detailEmpty.rectTransform;
        emptyRect.anchorMin = Vector2.zero;
        emptyRect.anchorMax = Vector2.one;
        emptyRect.offsetMin = new Vector2(20f, 20f);
        emptyRect.offsetMax = new Vector2(-20f, -20f);
        detailEmpty.gameObject.SetActive(false);
    }

    // ------------------------------------------------------------------
    // CONTENT
    // ------------------------------------------------------------------

    private void SelectCuisine(NigerianCuisine cuisine)
    {
        activeCuisine = cuisine;
        selectedRecipe = null;
        RefreshAll();
    }

    private void RefreshAll()
    {
        if (!built) Build();
        if (panel == null) return;

        RefreshTabs();
        ClearRecipeRows();
        ClearIngredientRows();

        List<Recipe> recipes = RecipeCatalog.GetByCuisine(activeCuisine);

        if (recipes.Count == 0)
        {
            ShowEmptyCuisine();
            return;
        }

        BuildRecipeRows(recipes);

        Recipe target = selectedRecipe;
        if (target == null || !recipes.Contains(target))
            target = recipes[0];

        SelectRecipe(target);
    }

    private void RefreshTabs()
    {
        for (int i = 0; i < tabBackgrounds.Length; i++)
        {
            bool active = i == (int)activeCuisine;

            if (tabBackgrounds[i] != null)
                tabBackgrounds[i].color = active ? AccentColor : TabInactiveColor;

            if (tabLabels[i] != null)
            {
                tabLabels[i].color = active
                    ? new Color(0.04f, 0.10f, 0.06f)
                    : MutedText;
                tabLabels[i].fontStyle = active ? FontStyles.Bold : FontStyles.Normal;
            }
        }
    }

    private void BuildRecipeRows(List<Recipe> recipes)
    {
        if (recipeList == null) return;

        const float gap = 6f;
        float rowHeight = Mathf.Clamp(
            (RecipeListHeight - gap * (recipes.Count - 1)) / recipes.Count,
            38f, 54f);

        for (int i = 0; i < recipes.Count; i++)
        {
            Recipe recipe = recipes[i];

            Image row = RuntimeUI.CreateImage(recipeList, "GroceryRecipe_" + i, RowColor);
            RectTransform rowRect = row.rectTransform;
            rowRect.anchorMin = new Vector2(0f, 1f);
            rowRect.anchorMax = new Vector2(1f, 1f);
            rowRect.pivot = new Vector2(0.5f, 1f);
            rowRect.offsetMin = new Vector2(0f, -(i + 1) * rowHeight - i * gap);
            rowRect.offsetMax = new Vector2(0f, -i * (rowHeight + gap));

            Button button = row.gameObject.AddComponent<Button>();
            button.targetGraphic = row;
            Navigation navigation = button.navigation;
            navigation.mode = Navigation.Mode.None;
            button.navigation = navigation;

            Recipe captured = recipe;
            button.onClick.AddListener(delegate { SelectRecipe(captured); });

            Image accent = RuntimeUI.CreateImage(row.transform, "SelectedAccent",
                Transparent);
            RectTransform accentRect = accent.rectTransform;
            accentRect.anchorMin = new Vector2(0f, 0f);
            accentRect.anchorMax = new Vector2(0f, 1f);
            accentRect.pivot = new Vector2(0f, 0.5f);
            accentRect.offsetMin = Vector2.zero;
            accentRect.offsetMax = new Vector2(4f, 0f);

            TMP_Text label = RuntimeUI.CreateText(row.transform, "Name",
                recipe.recipeName, rowHeight >= 46f ? 15f : 14f, Color.white,
                TextAlignmentOptions.MidlineLeft);
            RectTransform labelRect = label.rectTransform;
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = new Vector2(12f, 3f);
            labelRect.offsetMax = new Vector2(-8f, -3f);

            recipeRows.Add(new RecipeRow
            {
                Recipe = recipe,
                Background = row,
                Accent = accent
            });
        }
    }

    private void SelectRecipe(Recipe recipe)
    {
        if (recipe == null) return;

        selectedRecipe = recipe;
        UpdateRowSelection();
        RefreshDetail(recipe);
    }

    private void UpdateRowSelection()
    {
        foreach (RecipeRow row in recipeRows)
        {
            if (row == null) continue;

            bool selected = row.Recipe == selectedRecipe;
            if (row.Background != null)
                row.Background.color = selected ? RowSelectedColor : RowColor;
            if (row.Accent != null)
                row.Accent.color = selected ? AccentColor : Transparent;
        }
    }

    private void RefreshDetail(Recipe recipe)
    {
        if (detailContent == null) return;

        detailContent.SetActive(true);
        if (detailEmpty != null) detailEmpty.gameObject.SetActive(false);
        if (recipeListEmpty != null) recipeListEmpty.gameObject.SetActive(false);

        ClearIngredientRows();

        int count = recipe.requiredIngredients != null
            ? recipe.requiredIngredients.Count
            : 0;

        if (detailTitle != null) detailTitle.text = recipe.recipeName;
        if (detailSubtitle != null)
        {
            detailSubtitle.text = count + (count == 1 ? " ingredient" : " ingredients") +
                "  |  sells for NGN " + Mathf.Max(0, recipe.sellPrice);
        }

        for (int i = 0; i < count; i++)
        {
            string ingredient = IngredientCatalog.Canonical(recipe.requiredIngredients[i]);
            AddIngredientRow(i, ingredient);
        }

        RefreshIngredientStatuses();
    }

    private void AddIngredientRow(int index, string ingredientName)
    {
        Color background = index % 2 == 0
            ? new Color(0.110f, 0.150f, 0.135f, 1f)
            : new Color(0.095f, 0.135f, 0.120f, 1f);

        Image row = RuntimeUI.CreateImage(ingredientListRect,
            "Ingredient_" + index, background);
        RectTransform rect = row.rectTransform;
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(1f, 1f);
        rect.pivot = new Vector2(0.5f, 1f);
        rect.offsetMin = new Vector2(0f, -(index + 1) * IngredientRowHeight);
        rect.offsetMax = new Vector2(0f, -index * IngredientRowHeight);

        Image swatch = RuntimeUI.CreateImage(row.transform, "Swatch",
            IngredientCatalog.ColorOf(ingredientName));
        RectTransform swatchRect = swatch.rectTransform;
        swatchRect.anchorMin = new Vector2(0f, 0.5f);
        swatchRect.anchorMax = new Vector2(0f, 0.5f);
        swatchRect.pivot = new Vector2(0f, 0.5f);
        swatchRect.anchoredPosition = new Vector2(7f, 0f);
        swatchRect.sizeDelta = new Vector2(9f, 9f);

        TMP_Text name = RuntimeUI.CreateText(row.transform, "Name", ingredientName,
            14f, new Color(0.92f, 0.96f, 0.93f), TextAlignmentOptions.MidlineLeft);
        RectTransform nameRect = name.rectTransform;
        nameRect.anchorMin = Vector2.zero;
        nameRect.anchorMax = Vector2.one;
        nameRect.offsetMin = new Vector2(22f, 0f);
        nameRect.offsetMax = new Vector2(-78f, 0f);

        TMP_Text status = RuntimeUI.CreateText(row.transform, "Status", "", 11f,
            BuyColor, TextAlignmentOptions.MidlineRight);
        RectTransform statusRect = status.rectTransform;
        statusRect.anchorMin = Vector2.zero;
        statusRect.anchorMax = Vector2.one;
        statusRect.offsetMin = Vector2.zero;
        statusRect.offsetMax = new Vector2(-7f, 0f);

        ingredientRows.Add(new IngredientRow
        {
            Name = ingredientName,
            Status = status
        });
    }

    private void RefreshIngredientStatuses()
    {
        if (selectedRecipe == null) return;

        int total = ingredientRows.Count;
        int ownedCount = 0;
        int missing = 0;
        int missingCost = 0;

        foreach (IngredientRow row in ingredientRows)
        {
            if (row == null) continue;

            int have = IngredientCatalog.CountInBackpack(row.Name);
            if (have > 0)
            {
                ownedCount++;
                if (row.Status != null)
                {
                    row.Status.text = have > 1 ? "OWNED x" + have : "OWNED";
                    row.Status.color = OwnedColor;
                }
            }
            else
            {
                missing++;
                missingCost += IngredientCatalog.BuyPriceOf(row.Name);
                if (row.Status != null)
                {
                    row.Status.text = "BUY";
                    row.Status.color = BuyColor;
                }
            }
        }

        if (detailSummary == null) return;

        if (total == 0)
        {
            detailSummary.text = "This recipe has no ingredients listed yet.";
        }
        else if (missing == 0)
        {
            detailSummary.text = "You already have all " + total +
                " ingredients for this dish.";
        }
        else
        {
            detailSummary.text = ownedCount + " of " + total + " in your bag. " +
                missing + (missing == 1 ? " ingredient" : " ingredients") +
                " to buy, about NGN " + missingCost + ".";
        }
    }

    private void ShowEmptyCuisine()
    {
        selectedRecipe = null;

        if (recipeListEmpty != null)
        {
            recipeListEmpty.text = "No " + CuisineName(activeCuisine) + " dishes yet.";
            recipeListEmpty.gameObject.SetActive(true);
        }

        if (detailContent != null) detailContent.SetActive(false);
        if (detailEmpty != null)
        {
            detailEmpty.text = CuisineName(activeCuisine) + " dishes\nare coming soon.";
            detailEmpty.gameObject.SetActive(true);
        }
    }

    private void ClearRecipeRows()
    {
        recipeRows.Clear();
        if (recipeList != null) ClearChildren(recipeList);
    }

    private void ClearIngredientRows()
    {
        ingredientRows.Clear();
        if (ingredientListRect != null) ClearChildren(ingredientListRect);
    }

    private static void ClearChildren(Transform parent)
    {
        for (int i = parent.childCount - 1; i >= 0; i--)
        {
            GameObject child = parent.GetChild(i).gameObject;
            child.SetActive(false);
            Destroy(child);
        }
    }

    private static string CuisineName(NigerianCuisine cuisine)
    {
        switch (cuisine)
        {
            case NigerianCuisine.Yoruba: return "Yoruba";
            case NigerianCuisine.Hausa: return "Hausa";
            default: return "Igbo";
        }
    }

    private static GameObject CreateButton(Transform parent, string name,
        string labelText, Vector2 size, Vector2 position, Vector2 anchor,
        Vector2 pivot, Color color, UnityAction onClick)
    {
        Image image = RuntimeUI.CreateImage(parent, name, color);
        RectTransform rect = image.rectTransform;
        rect.anchorMin = anchor;
        rect.anchorMax = anchor;
        rect.pivot = pivot;
        rect.anchoredPosition = position;
        rect.sizeDelta = size;

        Button button = image.gameObject.AddComponent<Button>();
        button.targetGraphic = image;
        Navigation navigation = button.navigation;
        navigation.mode = Navigation.Mode.None;
        button.navigation = navigation;
        if (onClick != null) button.onClick.AddListener(onClick);

        TMP_Text label = RuntimeUI.CreateText(image.transform, "Label", labelText,
            Mathf.Clamp(size.y * 0.40f, 12f, 26f), Color.white);
        RectTransform labelRect = label.rectTransform;
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = Vector2.zero;
        labelRect.offsetMax = Vector2.zero;

        return image.gameObject;
    }
}
