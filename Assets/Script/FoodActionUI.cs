using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Small runtime-built popup with Eat / Drop / Place actions for a picked up dish.
/// </summary>
public class FoodActionUI : MonoBehaviour
{
    public static FoodActionUI instance;

    public FoodDish currentDish;

    private GameObject canvasRoot;
    private GameObject panelRoot;
    private TMP_Text titleLabel;
    private bool built;

    void Awake()
    {
        if (instance == null) instance = this;
        else { Destroy(gameObject); return; }
    }

    void OnDestroy()
    {
        if (instance == this) instance = null;
    }

    // ------------------------------------------------------------------
    // PUBLIC API
    // ------------------------------------------------------------------

    public static FoodActionUI GetOrCreate()
    {
        if (instance != null) return instance;

        FoodActionUI existing = Object.FindFirstObjectByType<FoodActionUI>(FindObjectsInactive.Include);
        if (existing != null)
        {
            instance = existing;
            return existing;
        }

        GameObject go = new GameObject("FoodActionUI");
        instance = go.AddComponent<FoodActionUI>();
        return instance;
    }

    public static void Show(FoodDish dish)
    {
        GetOrCreate().ShowInternal(dish);
    }

    public static void Hide()
    {
        if (instance != null) instance.HideInternal();
    }

    // ------------------------------------------------------------------
    // INTERNAL
    // ------------------------------------------------------------------

    private void ShowInternal(FoodDish dish)
    {
        if (dish == null) return;

        EnsureEventSystem();
        Build();

        currentDish = dish;
        if (titleLabel != null)
            titleLabel.text = dish.dishName;

        if (canvasRoot != null) canvasRoot.SetActive(true);
    }

    private void HideInternal()
    {
        currentDish = null;
        if (canvasRoot != null) canvasRoot.SetActive(false);
    }

    private void OnEatClicked()
    {
        if (currentDish == null) { HideInternal(); return; }
        currentDish.Eat();
    }

    private void OnDropClicked()
    {
        if (currentDish == null) { HideInternal(); return; }
        currentDish.Drop();
    }

    private void OnPlaceClicked()
    {
        if (currentDish == null) { HideInternal(); return; }

        FoodTray tray = FoodTray.GetOrCreate();
        if (tray != null) currentDish.PlaceOnTray(tray);
        else currentDish.Drop();
    }

    // ------------------------------------------------------------------
    // BUILD
    // ------------------------------------------------------------------

    private void Build()
    {
        if (built) return;
        built = true;

        GameObject canvasGo = new GameObject("FoodActionCanvas",
            typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvasGo.transform.SetParent(transform, false);
        canvasRoot = canvasGo;

        Canvas canvas = canvasGo.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 900;

        CanvasScaler scaler = canvasGo.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        GameObject panel = new GameObject("Panel",
            typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        panel.transform.SetParent(canvasGo.transform, false);
        panelRoot = panel;

        RectTransform panelRt = panel.GetComponent<RectTransform>();
        panelRt.anchorMin = new Vector2(0.5f, 0f);
        panelRt.anchorMax = new Vector2(0.5f, 0f);
        panelRt.pivot = new Vector2(0.5f, 0f);
        panelRt.anchoredPosition = new Vector2(0f, 56f);
        panelRt.sizeDelta = new Vector2(780f, 220f);

        Image panelImage = panel.GetComponent<Image>();
        panelImage.color = new Color(0.04f, 0.05f, 0.08f, 0.94f);

        // Title
        GameObject titleGo = new GameObject("Title",
            typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        titleGo.transform.SetParent(panel.transform, false);

        RectTransform titleRt = titleGo.GetComponent<RectTransform>();
        titleRt.anchorMin = new Vector2(0.5f, 1f);
        titleRt.anchorMax = new Vector2(0.5f, 1f);
        titleRt.pivot = new Vector2(0.5f, 0.5f);
        titleRt.anchoredPosition = new Vector2(0f, -34f);
        titleRt.sizeDelta = new Vector2(720f, 52f);

        titleLabel = titleGo.GetComponent<TextMeshProUGUI>();
        titleLabel.text = "Dish";
        titleLabel.fontSize = 30f;
        titleLabel.fontStyle = FontStyles.Bold;
        titleLabel.color = Color.white;
        titleLabel.alignment = TextAlignmentOptions.Center;
        titleLabel.raycastTarget = false;

        // Buttons
        CreateActionButton(panel.transform, "Eat", "Eat",
            new Color(0.16f, 0.55f, 0.27f, 1f), new Vector2(-242f, -34f), OnEatClicked);

        CreateActionButton(panel.transform, "Drop", "Drop",
            new Color(0.42f, 0.42f, 0.42f, 1f), new Vector2(0f, -34f), OnDropClicked);

        CreateActionButton(panel.transform, "Place", "Place",
            new Color(0.15f, 0.40f, 0.70f, 1f), new Vector2(242f, -34f), OnPlaceClicked);

        canvasRoot.SetActive(false);
    }

    private void CreateActionButton(Transform parent, string objectName, string label,
        Color color, Vector2 position, UnityAction onClick)
    {
        GameObject go = new GameObject(objectName + "Button",
            typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        go.transform.SetParent(parent, false);

        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = position;
        rt.sizeDelta = new Vector2(210f, 82f);

        Image image = go.GetComponent<Image>();
        image.color = color;

        Button button = go.GetComponent<Button>();
        button.targetGraphic = image;
        button.onClick.AddListener(onClick);

        Navigation navigation = button.navigation;
        navigation.mode = Navigation.Mode.None;
        button.navigation = navigation;

        GameObject labelGo = new GameObject("Label",
            typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        labelGo.transform.SetParent(go.transform, false);

        RectTransform labelRt = labelGo.GetComponent<RectTransform>();
        labelRt.anchorMin = Vector2.zero;
        labelRt.anchorMax = Vector2.one;
        labelRt.offsetMin = Vector2.zero;
        labelRt.offsetMax = Vector2.zero;

        TextMeshProUGUI text = labelGo.GetComponent<TextMeshProUGUI>();
        text.text = label;
        text.fontSize = 32f;
        text.fontStyle = FontStyles.Bold;
        text.color = Color.white;
        text.alignment = TextAlignmentOptions.Center;
        text.raycastTarget = false;
    }

    private static void EnsureEventSystem()
    {
        if (Object.FindFirstObjectByType<EventSystem>() != null) return;

        GameObject go = new GameObject("EventSystem",
            typeof(EventSystem), typeof(StandaloneInputModule));
        DontDestroyOnLoad(go);
    }
}
