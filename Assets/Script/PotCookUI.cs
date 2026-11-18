using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Pot-focused cooking screen. Opened when a recipe is tapped at the prep
/// station. Every required ingredient gets its own slot; a slot can only be
/// filled while that ingredient sits in the backpack. Ingredients are consumed
/// only after <see cref="PortablePot.CanStartCooking"/> confirms the pot can
/// actually start, so a full oven or cold stove never eats the player's food.
/// </summary>
public class PotCookUI : MonoBehaviour
{
    public static PotCookUI instance;

    private sealed class IngredientSlot
    {
        public int Index;
        public string Ingredient;
        public Image Frame;
        public Button Button;
        public Image Swatch;
        public TMP_Text Initials;
        public TMP_Text Name;
        public TMP_Text BagCount;
        public TMP_Text State;
    }

    private static readonly Color PanelColor = new Color(0.105f, 0.075f, 0.055f, 0.98f);
    private static readonly Color SlotColor = new Color(0.165f, 0.118f, 0.085f, 1f);
    private static readonly Color SlotFilledColor = new Color(0.145f, 0.34f, 0.20f, 1f);
    private static readonly Color SlotMissingColor = new Color(0.36f, 0.14f, 0.11f, 1f);
    private static readonly Color FrameReady = new Color(0.93f, 0.68f, 0.28f, 1f);
    private static readonly Color FrameFilled = new Color(0.38f, 0.89f, 0.49f, 1f);
    private static readonly Color FrameMissing = new Color(0.78f, 0.30f, 0.24f, 1f);
    private static readonly Color FrameIdle = new Color(0.36f, 0.27f, 0.20f, 1f);

    private Canvas canvas;
    private GameObject panel;
    private RectTransform slotContainer;
    private TMP_Text recipeLabel;
    private TMP_Text statusLabel;
    private Button cookButton;
    private Image cookButtonImage;
    private TMP_Text cookButtonLabel;
    private Button backButton;
    private bool wired;

    private Recipe currentRecipe;
    private PortablePot currentPot;
    private readonly List<IngredientSlot> slots = new List<IngredientSlot>();
    private bool[] filled = new bool[0];
    private string builtIngredientSignature = string.Empty;
    private float nextRefresh;

    public static PotCookUI GetOrCreate()
    {
        if (instance != null) return instance;

        PotCookUI existing = Object.FindFirstObjectByType<PotCookUI>(FindObjectsInactive.Include);
        if (existing != null)
        {
            instance = existing;
            return existing;
        }

        GameObject go = new GameObject("PotCookUI");
        instance = go.AddComponent<PotCookUI>();
        return instance;
    }

    public static void Show(Recipe recipe, PortablePot pot)
    {
        GetOrCreate().ShowInternal(recipe, pot);
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
        // Reuse the editable hierarchy from the scene when it exists. The
        // cooking panel stays hidden until a recipe is selected.
        if (TryBindExistingUI())
        {
            WireEvents();
            if (panel != null) panel.SetActive(false);
        }
    }

    void OnDestroy()
    {
        if (instance == this) instance = null;
    }

    void Update()
    {
        if (panel == null || !panel.activeSelf) return;
        if (Time.unscaledTime < nextRefresh) return;

        nextRefresh = Time.unscaledTime + 0.5f;
        RefreshSlots();
    }

    // ------------------------------------------------------------------
    // SHOW / HIDE
    // ------------------------------------------------------------------

    private void ShowInternal(Recipe recipe, PortablePot pot)
    {
        if (recipe == null) return;

        Build();

        currentRecipe = recipe;
        currentPot = pot;
        panel.SetActive(true);
        EnsureParentsActive();

        string signature = BuildIngredientSignature(recipe);
        if (signature != builtIngredientSignature)
        {
            builtIngredientSignature = signature;
            RebuildSlots();
        }

        for (int i = 0; i < filled.Length; i++) filled[i] = false;

        recipeLabel.text = recipe.recipeName;
        RefreshSlots();
    }

    private void HideInternal()
    {
        if (panel != null) panel.SetActive(false);
        currentRecipe = null;
        currentPot = null;
    }

    private void CloseToRecipeList()
    {
        PortablePot pot = currentPot;
        HideInternal();

        if (PrepStationUI.instance != null)
            PrepStationUI.instance.OpenPrepStation(pot);
    }

    // ------------------------------------------------------------------
    // BUILD
    // ------------------------------------------------------------------

    private void Build()
    {
        if (panel != null)
        {
            WireEvents();
            return;
        }

        if (TryBindExistingUI())
        {
            WireEvents();
            return;
        }

        canvas = RuntimeUI.CreateOverlayCanvas("PotCookCanvas", 970);
        canvas.transform.SetParent(transform, false);

        Image backdrop = RuntimeUI.CreateImage(canvas.transform, "Backdrop",
            new Color(0f, 0f, 0f, 0.62f));
        Stretch(backdrop.rectTransform);

        Image panelImage = RuntimeUI.CreateImage(backdrop.transform, "PotPanel", PanelColor);
        panel = panelImage.gameObject;

        RectTransform panelRt = panelImage.rectTransform;
        panelRt.anchorMin = new Vector2(0.5f, 0.5f);
        panelRt.anchorMax = new Vector2(0.5f, 0.5f);
        panelRt.pivot = new Vector2(0.5f, 0.5f);
        panelRt.anchoredPosition = Vector2.zero;
        panelRt.sizeDelta = new Vector2(1240f, 830f);

        // Warm copper top strip so the screen reads as "the pot".
        Image topStrip = RuntimeUI.CreateImage(panel.transform, "TopStrip",
            new Color(0.78f, 0.43f, 0.16f, 1f));
        RectTransform stripRt = topStrip.rectTransform;
        stripRt.anchorMin = new Vector2(0f, 1f);
        stripRt.anchorMax = new Vector2(1f, 1f);
        stripRt.pivot = new Vector2(0.5f, 1f);
        stripRt.anchoredPosition = Vector2.zero;
        stripRt.sizeDelta = new Vector2(0f, 8f);

        TMP_Text title = RuntimeUI.CreateText(panel.transform, "Title",
            "POT COOKING", 40f, new Color(1f, 0.84f, 0.58f));
        title.fontStyle = FontStyles.Bold;
        SetRect(title.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -30f),
            new Vector2(1100f, 54f), new Vector2(0.5f, 1f));

        recipeLabel = RuntimeUI.CreateText(panel.transform, "RecipeName",
            string.Empty, 32f, Color.white);
        SetRect(recipeLabel.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -84f),
            new Vector2(1100f, 44f), new Vector2(0.5f, 1f));

        statusLabel = RuntimeUI.CreateText(panel.transform, "Status",
            string.Empty, 22f, new Color(1f, 0.73f, 0.42f));
        SetRect(statusLabel.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -132f),
            new Vector2(1120f, 36f), new Vector2(0.5f, 1f));

        TMP_Text section = RuntimeUI.CreateText(panel.transform, "Section",
            "INGREDIENTS NEEDED", 20f, new Color(0.80f, 0.72f, 0.62f));
        section.fontStyle = FontStyles.Bold;
        SetRect(section.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -172f),
            new Vector2(1100f, 32f), new Vector2(0.5f, 1f));

        GameObject containerGo = new GameObject("IngredientSlots",
            typeof(RectTransform), typeof(GridLayoutGroup));
        containerGo.transform.SetParent(panel.transform, false);
        slotContainer = containerGo.GetComponent<RectTransform>();
        slotContainer.anchorMin = new Vector2(0.5f, 1f);
        slotContainer.anchorMax = new Vector2(0.5f, 1f);
        slotContainer.pivot = new Vector2(0.5f, 1f);
        slotContainer.anchoredPosition = new Vector2(0f, -214f);
        slotContainer.sizeDelta = new Vector2(1120f, 480f);

        GridLayoutGroup grid = containerGo.GetComponent<GridLayoutGroup>();
        grid.cellSize = new Vector2(262f, 146f);
        grid.spacing = new Vector2(18f, 16f);
        grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        grid.constraintCount = 4;
        grid.childAlignment = TextAnchor.UpperCenter;

        cookButton = RuntimeUI.CreateButton(panel.transform, "CookButton", "COOK",
            new Vector2(280f, 82f), new Vector2(380f, 58f), new Vector2(0.5f, 0f),
            new Color(0.18f, 0.48f, 0.24f, 1f), OnCookClicked);
        cookButtonImage = cookButton.GetComponent<Image>();
        cookButtonLabel = cookButton.GetComponentInChildren<TMP_Text>(true);

        backButton = RuntimeUI.CreateButton(panel.transform, "BackButton", "BACK",
            new Vector2(240f, 76f), new Vector2(-400f, 58f), new Vector2(0.5f, 0f),
            new Color(0.30f, 0.24f, 0.19f, 1f), CloseToRecipeList);

        WireEvents();
        panel.SetActive(false);
    }

    /// <summary>
    /// Binds the scene hierarchy created by the editor layout builder. The
    /// ingredient slots are still created dynamically because they depend on
    /// the selected recipe.
    /// </summary>
    private bool TryBindExistingUI()
    {
        Transform canvasTransform = RuntimeUI.FindDeepChild(transform, "PotCookCanvas");
        if (canvasTransform == null) return false;

        Transform panelTransform = RuntimeUI.FindDeepChild(canvasTransform, "PotPanel");
        if (panelTransform == null) return false;

        canvas = canvasTransform.GetComponent<Canvas>();
        panel = panelTransform.gameObject;
        slotContainer = RuntimeUI.FindComponentByName<RectTransform>(panelTransform, "IngredientSlots");
        recipeLabel = RuntimeUI.FindComponentByName<TMP_Text>(panelTransform, "RecipeName");
        statusLabel = RuntimeUI.FindComponentByName<TMP_Text>(panelTransform, "Status");
        cookButton = RuntimeUI.FindComponentByName<Button>(panelTransform, "CookButton");
        backButton = RuntimeUI.FindComponentByName<Button>(panelTransform, "BackButton");

        if (cookButton != null)
        {
            cookButtonImage = cookButton.GetComponent<Image>();
            cookButtonLabel = cookButton.GetComponentInChildren<TMP_Text>(true);
        }

        return slotContainer != null && cookButton != null && backButton != null;
    }

    private void WireEvents()
    {
        if (wired) return;
        wired = true;

        if (cookButton != null)
        {
            cookButton.onClick.RemoveAllListeners();
            cookButton.onClick.AddListener(OnCookClicked);
        }

        if (backButton != null)
        {
            backButton.onClick.RemoveAllListeners();
            backButton.onClick.AddListener(CloseToRecipeList);
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

    private void RebuildSlots()
    {
        foreach (IngredientSlot slot in slots)
        {
            if (slot == null || slot.Frame == null) continue;

            // Detach before the deferred Destroy so the grid never lays out
            // the old slots alongside the new ones for a frame.
            slot.Frame.transform.SetParent(null, false);
            Destroy(slot.Frame.gameObject);
        }
        slots.Clear();

        List<string> ingredients = currentRecipe != null && currentRecipe.requiredIngredients != null
            ? currentRecipe.requiredIngredients
            : new List<string>();

        filled = new bool[ingredients.Count];

        for (int i = 0; i < ingredients.Count; i++)
            CreateIngredientSlot(i, ingredients[i]);

        slotContainer.gameObject.SetActive(ingredients.Count > 0);
    }

    private void CreateIngredientSlot(int index, string rawIngredient)
    {
        string canonical = IngredientCatalog.Canonical(rawIngredient);
        Color color = IngredientCatalog.ColorOf(canonical);

        // Frame is the outer button; the inner image carries the state color.
        GameObject root = new GameObject("Slot_" + canonical,
            typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        root.transform.SetParent(slotContainer, false);

        Image frame = root.GetComponent<Image>();
        frame.color = FrameIdle;

        Button button = root.GetComponent<Button>();
        button.targetGraphic = frame;
        Navigation navigation = button.navigation;
        navigation.mode = Navigation.Mode.None;
        button.navigation = navigation;

        Image body = RuntimeUI.CreateImage(root.transform, "Body", SlotColor);
        RectTransform bodyRt = body.rectTransform;
        bodyRt.anchorMin = Vector2.zero;
        bodyRt.anchorMax = Vector2.one;
        bodyRt.offsetMin = new Vector2(4f, 4f);
        bodyRt.offsetMax = new Vector2(-4f, -4f);
        body.raycastTarget = false;

        Image swatch = RuntimeUI.CreateImage(body.transform, "Swatch", color);
        RectTransform swatchRt = swatch.rectTransform;
        swatchRt.anchorMin = new Vector2(0f, 1f);
        swatchRt.anchorMax = new Vector2(0f, 1f);
        swatchRt.pivot = new Vector2(0f, 1f);
        swatchRt.anchoredPosition = new Vector2(14f, -14f);
        swatchRt.sizeDelta = new Vector2(56f, 56f);
        swatch.raycastTarget = false;

        TMP_Text initials = RuntimeUI.CreateText(swatch.transform, "Initials",
            InitialsOf(canonical), 22f, new Color(0.08f, 0.08f, 0.08f, 0.85f));
        initials.fontStyle = FontStyles.Bold;
        Stretch(initials.rectTransform);

        TMP_Text name = RuntimeUI.CreateText(body.transform, "Name", canonical,
            23f, Color.white, TextAlignmentOptions.TopLeft);
        name.fontStyle = FontStyles.Bold;
        RectTransform nameRt = name.rectTransform;
        nameRt.anchorMin = new Vector2(0f, 1f);
        nameRt.anchorMax = new Vector2(1f, 1f);
        nameRt.pivot = new Vector2(0.5f, 1f);
        nameRt.anchoredPosition = new Vector2(42f, -16f);
        nameRt.sizeDelta = new Vector2(-96f, 56f);

        TMP_Text bagCount = RuntimeUI.CreateText(body.transform, "BagCount",
            string.Empty, 18f, new Color(0.82f, 0.84f, 0.88f), TextAlignmentOptions.Left);
        RectTransform bagRt = bagCount.rectTransform;
        bagRt.anchorMin = new Vector2(0f, 0f);
        bagRt.anchorMax = new Vector2(1f, 0f);
        bagRt.pivot = new Vector2(0.5f, 0f);
        bagRt.anchoredPosition = new Vector2(12f, 40f);
        bagRt.sizeDelta = new Vector2(-24f, 24f);

        TMP_Text state = RuntimeUI.CreateText(body.transform, "State",
            string.Empty, 18f, new Color(1f, 0.78f, 0.36f), TextAlignmentOptions.Left);
        state.fontStyle = FontStyles.Bold;
        RectTransform stateRt = state.rectTransform;
        stateRt.anchorMin = new Vector2(0f, 0f);
        stateRt.anchorMax = new Vector2(1f, 0f);
        stateRt.pivot = new Vector2(0.5f, 0f);
        stateRt.anchoredPosition = new Vector2(12f, 12f);
        stateRt.sizeDelta = new Vector2(-24f, 24f);

        IngredientSlot slot = new IngredientSlot
        {
            Index = index,
            Ingredient = canonical,
            Frame = frame,
            Button = button,
            Swatch = swatch,
            Initials = initials,
            Name = name,
            BagCount = bagCount,
            State = state
        };

        int captured = index;
        button.onClick.AddListener(() => OnSlotClicked(captured));
        slots.Add(slot);
    }

    // ------------------------------------------------------------------
    // SLOT STATE
    // ------------------------------------------------------------------

    private void OnSlotClicked(int index)
    {
        if (currentRecipe == null || index < 0 || index >= filled.Length) return;

        if (filled[index])
        {
            filled[index] = false;
            RefreshSlots();
            return;
        }

        string canonical = IngredientCatalog.Canonical(currentRecipe.requiredIngredients[index]);
        int reserved = CountFilledOf(canonical, index);
        int inBag = IngredientCatalog.CountInBackpack(canonical);

        if (inBag > reserved)
        {
            filled[index] = true;
            RefreshSlots();
            return;
        }

        ToastUI.Show("You need " + canonical + " in your backpack.");
        RefreshSlots();
    }

    private int CountFilledOf(string canonical, int ignoreIndex)
    {
        if (currentRecipe == null) return 0;

        int count = 0;
        for (int i = 0; i < filled.Length && i < currentRecipe.requiredIngredients.Count; i++)
        {
            if (i == ignoreIndex || !filled[i]) continue;

            string other = IngredientCatalog.Canonical(currentRecipe.requiredIngredients[i]);
            if (string.Equals(other, canonical, System.StringComparison.OrdinalIgnoreCase))
                count++;
        }

        return count;
    }

    private void RefreshSlots()
    {
        if (currentRecipe == null) return;

        List<string> ingredients = currentRecipe.requiredIngredients;
        int total = ingredients != null ? ingredients.Count : 0;

        for (int i = 0; i < slots.Count; i++)
        {
            IngredientSlot slot = slots[i];
            if (slot == null || slot.Frame == null) continue;

            int inBag = IngredientCatalog.CountInBackpack(slot.Ingredient);
            int reserved = CountFilledOf(slot.Ingredient, i);
            bool available = inBag > reserved;

            slot.Name.text = slot.Ingredient;
            slot.BagCount.text = "In backpack: " + inBag;
            slot.Swatch.color = IngredientCatalog.ColorOf(slot.Ingredient);
            slot.Initials.text = InitialsOf(slot.Ingredient);

            bool isFilled = i < filled.Length && filled[i];

            if (isFilled)
            {
                slot.Frame.color = FrameFilled;
                slot.State.text = "READY";
                slot.State.color = new Color(0.55f, 0.96f, 0.62f);
            }
            else if (available)
            {
                slot.Frame.color = FrameReady;
                slot.State.text = "TAP TO ADD";
                slot.State.color = new Color(1f, 0.80f, 0.40f);
            }
            else
            {
                slot.Frame.color = FrameMissing;
                slot.State.text = "NOT IN BACKPACK";
                slot.State.color = new Color(1f, 0.55f, 0.48f);
            }
        }

        RefreshCookState(total);
    }

    private void RefreshCookState(int totalIngredients)
    {
        bool allFilled = totalIngredients == 0;
        if (!allFilled)
        {
            allFilled = true;
            for (int i = 0; i < totalIngredients; i++)
            {
                if (i >= filled.Length || !filled[i]) { allFilled = false; break; }
            }
        }

        string potProblem = null;
        if (currentPot == null)
            potProblem = "Place the pot on the stove before cooking.";
        else
        {
            string reason;
            if (!currentPot.CanStartCooking(currentRecipe, out reason))
                potProblem = reason;
        }

        bool canCook = allFilled && potProblem == null;

        if (cookButtonImage != null)
        {
            cookButtonImage.color = canCook
                ? new Color(0.18f, 0.48f, 0.24f, 1f)
                : new Color(0.24f, 0.25f, 0.24f, 1f);
        }

        if (cookButton != null) cookButton.interactable = true;
        if (cookButtonLabel != null) cookButtonLabel.color = canCook ? Color.white : new Color(0.7f, 0.7f, 0.7f);

        if (statusLabel != null)
        {
            if (potProblem != null)
            {
                statusLabel.text = potProblem;
                statusLabel.color = new Color(1f, 0.62f, 0.46f);
            }
            else if (!allFilled)
            {
                statusLabel.text = "Fill every slot with ingredients from your backpack.";
                statusLabel.color = new Color(1f, 0.80f, 0.44f);
            }
            else
            {
                statusLabel.text = "Everything is ready. Tap COOK to start the pot.";
                statusLabel.color = new Color(0.62f, 0.95f, 0.66f);
            }
        }
    }

    // ------------------------------------------------------------------
    // COOK
    // ------------------------------------------------------------------

    private void OnCookClicked()
    {
        if (currentRecipe == null) return;

        if (currentPot == null)
        {
            ToastUI.Show("Place the pot on the stove before cooking.");
            RefreshSlots();
            return;
        }

        if (!AllSlotsFilled())
        {
            ToastUI.Show("Add every required ingredient to the pot first.");
            RefreshSlots();
            return;
        }

        string reason;
        if (!currentPot.CanStartCooking(currentRecipe, out reason))
        {
            ToastUI.Show(reason);
            RefreshSlots();
            return;
        }

        // Verify the backpack still holds every required item before we remove
        // anything. This is the last line of defence against ingredient loss.
        Dictionary<string, int> required = new Dictionary<string, int>();
        foreach (string raw in currentRecipe.requiredIngredients)
        {
            string canonical = IngredientCatalog.Canonical(raw);
            if (canonical.Length == 0) continue;

            int count;
            required.TryGetValue(canonical, out count);
            required[canonical] = count + 1;
        }

        foreach (KeyValuePair<string, int> pair in required)
        {
            if (IngredientCatalog.CountInBackpack(pair.Key) < pair.Value)
            {
                ToastUI.Show("You no longer have enough " + pair.Key + ".");
                RefreshSlots();
                return;
            }
        }

        foreach (KeyValuePair<string, int> pair in required)
        {
            for (int i = 0; i < pair.Value; i++)
                IngredientCatalog.ConsumeOne(pair.Key);
        }

        currentPot.StartCooking(currentRecipe);
        ToastUI.Show("Cooking " + currentRecipe.recipeName + "...");
        HideInternal();
    }

    private bool AllSlotsFilled()
    {
        if (currentRecipe == null || currentRecipe.requiredIngredients == null ||
            currentRecipe.requiredIngredients.Count == 0)
            return true;

        for (int i = 0; i < currentRecipe.requiredIngredients.Count; i++)
        {
            if (i >= filled.Length || !filled[i]) return false;
        }

        return true;
    }

    // ------------------------------------------------------------------
    // HELPERS
    // ------------------------------------------------------------------

    private string BuildIngredientSignature(Recipe recipe)
    {
        if (recipe == null || recipe.requiredIngredients == null) return string.Empty;

        System.Text.StringBuilder builder = new System.Text.StringBuilder();
        foreach (string ingredient in recipe.requiredIngredients)
        {
            builder.Append(IngredientCatalog.Canonical(ingredient));
            builder.Append('|');
        }

        return builder.ToString();
    }

    private static string InitialsOf(string canonical)
    {
        IngredientCatalog.Entry entry = IngredientCatalog.Get(canonical);
        if (entry != null) return entry.Initials;

        if (string.IsNullOrWhiteSpace(canonical)) return "?";
        return canonical.Length > 1
            ? canonical.Substring(0, 2).ToUpperInvariant()
            : canonical.ToUpperInvariant();
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
