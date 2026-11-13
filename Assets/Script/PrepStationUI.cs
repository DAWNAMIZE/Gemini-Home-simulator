using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PrepStationUI : MonoBehaviour
{
    public static PrepStationUI instance;

    [Header("Recipe Database")]
    public List<Recipe> allGameRecipes = new List<Recipe>();

    [Header("UI References")]
    public Transform recipeContainer;
    public GameObject recipeSlotPrefab;
    public GameObject uiPanel;          // The RecipeMenuPanel that holds the list

    [Header("Active Cooking Data")]
    public PortablePot targetCookingPot;

    [Header("Fallback Spawning")]
    [Tooltip("Where food is spawned when no pot is on the stove. If empty, FinishedDishSpawnPoint is used.")]
    public Transform dishSpawnPoint;

    private bool uiBuilt;
    private bool fallbackCooking;
    private bool hasOpened;

    void Awake()
    {
        if (instance == null) instance = this;
        else { Destroy(gameObject); return; }
    }

    void Start()
    {
        SetupUI();
        if (!hasOpened) Close();
    }

    // ------------------------------------------------------------------
    // PUBLIC API
    // ------------------------------------------------------------------

    public void OpenPrepStation(PortablePot pot)
    {
        targetCookingPot = pot;
        hasOpened = true;
        SetupUI();
        EnsureParentsActive();

        if (uiPanel != null) uiPanel.SetActive(true);
        RefreshRecipeList();
    }

    public void Close()
    {
        if (uiPanel != null) uiPanel.SetActive(false);
        targetCookingPot = null;
    }

    public void RefreshRecipeList()
    {
        if (!uiBuilt) SetupUI();
        if (recipeContainer == null) return;

        for (int i = recipeContainer.childCount - 1; i >= 0; i--)
        {
            Destroy(recipeContainer.GetChild(i).gameObject);
        }

        List<Recipe> dishes = GetRecipeDatabase();
        if (dishes.Count == 0)
        {
            Debug.LogWarning("PrepStationUI: no recipes were found in the database.");
            return;
        }

        foreach (Recipe recipe in dishes)
        {
            if (recipe == null) continue;
            CreateRecipeSlot(recipe);
        }
    }

    // ------------------------------------------------------------------
    // UI SETUP
    // ------------------------------------------------------------------

    private void SetupUI()
    {
        if (uiBuilt) return;
        uiBuilt = true;

        // Resolve the panel (never use the whole canvas as the panel).
        if (uiPanel == null)
            uiPanel = FindChild(transform, "RecipeMenuPanel")?.gameObject;

        if (uiPanel == null)
            uiPanel = gameObject;

        if (uiPanel == gameObject)
        {
            Transform panel = FindChild(transform, "RecipeMenuPanel");
            if (panel != null) uiPanel = panel.gameObject;
        }

        // The scene canvas was authored in world space with a tiny panel scale.
        // Convert it to a full screen overlay so the recipe list is actually usable.
        Canvas canvas = GetComponent<Canvas>();
        if (canvas == null) canvas = GetComponentInParent<Canvas>();

        if (canvas != null && canvas.renderMode == RenderMode.WorldSpace)
        {
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.worldCamera = null;
            canvas.sortingOrder = 100;

            RectTransform canvasRt = canvas.transform as RectTransform;
            if (canvasRt != null)
            {
                canvasRt.localPosition = Vector3.zero;
                canvasRt.localRotation = Quaternion.identity;
                canvasRt.localScale = Vector3.one;
                canvasRt.anchorMin = Vector2.zero;
                canvasRt.anchorMax = Vector2.one;
                canvasRt.offsetMin = Vector2.zero;
                canvasRt.offsetMax = Vector2.zero;
                canvasRt.pivot = new Vector2(0.5f, 0.5f);
            }
        }

        if (uiPanel != null)
        {
            RectTransform panelRt = uiPanel.transform as RectTransform;
            if (panelRt != null)
            {
                panelRt.localPosition = Vector3.zero;
                panelRt.localRotation = Quaternion.identity;
                panelRt.localScale = Vector3.one;
                panelRt.anchorMin = Vector2.zero;
                panelRt.anchorMax = Vector2.one;
                panelRt.offsetMin = Vector2.zero;
                panelRt.offsetMax = Vector2.zero;
                panelRt.pivot = new Vector2(0.5f, 0.5f);
            }

            Image panelImage = uiPanel.GetComponent<Image>();
            if (panelImage != null)
                panelImage.color = new Color(0.02f, 0.03f, 0.05f, 0.82f);
        }

        // Find (or create) the recipe list container.
        bool containerBelongsToPanel = recipeContainer != null &&
            (recipeContainer == uiPanel.transform || recipeContainer.IsChildOf(uiPanel.transform));

        if (!containerBelongsToPanel)
            recipeContainer = FindChild(uiPanel.transform, "RecipeListContainer");

        if (recipeContainer == null)
            recipeContainer = FindChild(transform, "RecipeListContainer");

        if (recipeContainer == null)
            recipeContainer = CreateContainer(uiPanel.transform);

        ConfigureContainer();

        // Reposition the title / done button that came from the scene.
        TMP_Text status = FindText(uiPanel.transform, "StatusText");
        if (status != null)
        {
            status.text = "Choose a dish to cook";
            status.fontSize = 46f;
            status.color = Color.white;
            status.alignment = TextAlignmentOptions.Center;
            RectTransform rt = status.rectTransform;
            rt.localScale = Vector3.one;
            rt.localRotation = Quaternion.identity;
            rt.anchorMin = new Vector2(0.5f, 1f);
            rt.anchorMax = new Vector2(0.5f, 1f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = new Vector2(0f, -70f);
            rt.sizeDelta = new Vector2(760f, 80f);
        }

        Button doneButton = FindButton(uiPanel.transform, "Done");
        if (doneButton != null)
        {
            doneButton.onClick.RemoveAllListeners();
            doneButton.onClick.AddListener(Close);

            RectTransform rt = doneButton.GetComponent<RectTransform>();
            if (rt != null)
            {
                rt.localScale = Vector3.one;
                rt.localRotation = Quaternion.identity;
                rt.anchorMin = new Vector2(0.5f, 0f);
                rt.anchorMax = new Vector2(0.5f, 0f);
                rt.pivot = new Vector2(0.5f, 0f);
                rt.anchoredPosition = new Vector2(0f, 36f);
                rt.sizeDelta = new Vector2(240f, 72f);
            }

            TMP_Text doneLabel = doneButton.GetComponentInChildren<TMP_Text>(true);
            if (doneLabel != null)
            {
                doneLabel.text = "Done";
                doneLabel.fontSize = 30f;
                doneLabel.alignment = TextAlignmentOptions.Center;
            }
        }
    }

    private void ConfigureContainer()
    {
        RectTransform rt = recipeContainer as RectTransform;
        if (rt != null)
        {
            rt.localScale = Vector3.one;
            rt.localRotation = Quaternion.identity;
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(700f, 640f);
            rt.anchoredPosition = new Vector2(0f, -20f);
        }

        VerticalLayoutGroup layout = recipeContainer.GetComponent<VerticalLayoutGroup>();
        if (layout == null) layout = recipeContainer.gameObject.AddComponent<VerticalLayoutGroup>();

        layout.padding = new RectOffset(14, 14, 14, 14);
        layout.spacing = 12f;
        layout.childAlignment = TextAnchor.UpperCenter;
        layout.childControlWidth = true;
        layout.childControlHeight = false;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;
    }

    private Transform CreateContainer(Transform parent)
    {
        GameObject go = new GameObject("RecipeListContainer",
            typeof(RectTransform), typeof(VerticalLayoutGroup));
        go.transform.SetParent(parent, false);
        return go.transform;
    }

    private void CreateRecipeSlot(Recipe recipe)
    {
        GameObject slot = new GameObject("Slot_" + recipe.recipeName,
            typeof(RectTransform), typeof(CanvasRenderer), typeof(Image),
            typeof(Button), typeof(LayoutElement));

        slot.transform.SetParent(recipeContainer, false);

        RectTransform rt = slot.GetComponent<RectTransform>();
        rt.localScale = Vector3.one;
        rt.localRotation = Quaternion.identity;

        LayoutElement element = slot.GetComponent<LayoutElement>();
        element.minHeight = 64f;
        element.preferredHeight = 74f;
        element.flexibleHeight = 0f;

        Image background = slot.GetComponent<Image>();
        background.color = new Color(0.13f, 0.17f, 0.23f, 0.96f);

        Button button = slot.GetComponent<Button>();
        button.targetGraphic = background;
        Navigation navigation = button.navigation;
        navigation.mode = Navigation.Mode.None;
        button.navigation = navigation;

        if (recipe.recipeIcon != null)
        {
            GameObject iconGo = new GameObject("Icon",
                typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            iconGo.transform.SetParent(slot.transform, false);

            RectTransform iconRt = iconGo.GetComponent<RectTransform>();
            iconRt.anchorMin = new Vector2(0f, 0.5f);
            iconRt.anchorMax = new Vector2(0f, 0.5f);
            iconRt.pivot = new Vector2(0f, 0.5f);
            iconRt.sizeDelta = new Vector2(52f, 52f);
            iconRt.anchoredPosition = new Vector2(14f, 0f);

            Image icon = iconGo.GetComponent<Image>();
            icon.sprite = recipe.recipeIcon;
            icon.preserveAspect = true;
            icon.raycastTarget = false;
        }

        GameObject labelGo = new GameObject("Label",
            typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        labelGo.transform.SetParent(slot.transform, false);

        RectTransform labelRt = labelGo.GetComponent<RectTransform>();
        labelRt.anchorMin = Vector2.zero;
        labelRt.anchorMax = Vector2.one;
        labelRt.offsetMin = new Vector2(recipe.recipeIcon != null ? 78f : 22f, 0f);
        labelRt.offsetMax = new Vector2(-22f, 0f);

        TextMeshProUGUI label = labelGo.GetComponent<TextMeshProUGUI>();
        label.text = recipe.recipeName;
        label.fontSize = 30f;
        label.color = Color.white;
        label.alignment = TextAlignmentOptions.Left;
        label.verticalAlignment = VerticalAlignmentOptions.Middle;
        label.raycastTarget = false;

        Recipe captured = recipe;
        button.onClick.AddListener(() => OnSelectRecipeToCook(captured));
    }

    // ------------------------------------------------------------------
    // SELECTION / COOKING
    // ------------------------------------------------------------------

    private void OnSelectRecipeToCook(Recipe recipe)
    {
        if (recipe == null) return;

        PortablePot pot = targetCookingPot;

        // Every finished-dish point occupied: block cooking entirely.
        DishSpawnManager manager = DishSpawnManager.GetOrCreate();
        if (manager != null && manager.SlotCount > 0 && !manager.HasFreeSlot)
        {
            ToastUI.Show("All four dish slots are full - clear a slot before cooking.");
            return;
        }

        Close();

        if (pot != null)
        {
            pot.StartCooking(recipe);
            return;
        }

        // No pot on the stove: still let the station cook and spawn the dish.
        if (!fallbackCooking)
            StartCoroutine(FallbackCook(recipe));
    }

    private IEnumerator FallbackCook(Recipe recipe)
    {
        fallbackCooking = true;
        yield return new WaitForSeconds(Mathf.Max(0.1f, recipe.cookTime));
        SpawnFinishedDish(recipe);
        fallbackCooking = false;
    }

    private void SpawnFinishedDish(Recipe recipe)
    {
        if (recipe == null || recipe.finishedDishPrefab == null)
        {
            Debug.LogWarning("PrepStationUI: recipe has no finished dish prefab.");
            return;
        }

        DishSpawnManager manager = DishSpawnManager.GetOrCreate();
        if (manager != null && manager.SlotCount > 0)
        {
            if (manager.SpawnDish(recipe.finishedDishPrefab, recipe.recipeName) == null)
                ToastUI.Show("All four dish slots are full - clear a slot first.");
            return;
        }

        Transform spawn = ResolveSpawnPoint();
        Vector3 position = spawn != null ? spawn.position : transform.position + Vector3.up;
        Quaternion rotation = spawn != null ? spawn.rotation : Quaternion.identity;

        GameObject dish = Instantiate(recipe.finishedDishPrefab, position, rotation);
        dish.name = recipe.recipeName;
        FoodDish.Attach(dish, recipe.recipeName);
    }

    private Transform ResolveSpawnPoint()
    {
        if (dishSpawnPoint != null) return dishSpawnPoint;

        GameObject found = GameObject.Find("FinishedDishSpawnPoint");
        if (found != null) return found.transform;

        // FinishDishSpawnPoint may be inside an inactive object.
        Transform[] all = Resources.FindObjectsOfTypeAll<Transform>();
        foreach (Transform t in all)
        {
            if (t != null && t.name == "FinishedDishSpawnPoint" && t.gameObject.scene.IsValid())
                return t;
        }

        if (PlayerInventory.instance != null && PlayerInventory.instance.dropSpawnPoint != null)
            return PlayerInventory.instance.dropSpawnPoint;

        return null;
    }

    // ------------------------------------------------------------------
    // DATABASE
    // ------------------------------------------------------------------

    private List<Recipe> GetRecipeDatabase()
    {
        List<Recipe> results = new List<Recipe>();

        if (allGameRecipes != null)
        {
            foreach (Recipe recipe in allGameRecipes)
                if (recipe != null && !results.Contains(recipe)) results.Add(recipe);
        }

        // Extra safety: pull in recipes that are loaded but were not assigned.
        Recipe[] loaded = Resources.FindObjectsOfTypeAll<Recipe>();
        foreach (Recipe recipe in loaded)
            if (recipe != null && !results.Contains(recipe)) results.Add(recipe);

        if (PlayerInventory.instance != null && PlayerInventory.instance.unlockedRecipes != null)
        {
            foreach (Recipe recipe in PlayerInventory.instance.unlockedRecipes)
                if (recipe != null && !results.Contains(recipe)) results.Add(recipe);
        }

        results.Sort((a, b) => string.Compare(a.recipeName, b.recipeName, System.StringComparison.OrdinalIgnoreCase));
        return results;
    }

    // ------------------------------------------------------------------
    // HELPERS
    // ------------------------------------------------------------------

    private void EnsureParentsActive()
    {
        Transform current = uiPanel != null ? uiPanel.transform : transform;
        while (current != null)
        {
            if (!current.gameObject.activeSelf) current.gameObject.SetActive(true);
            current = current.parent;
        }
    }

    private static Transform FindChild(Transform parent, string name)
    {
        if (parent == null) return null;
        if (parent.name == name) return parent;

        for (int i = 0; i < parent.childCount; i++)
        {
            Transform result = FindChild(parent.GetChild(i), name);
            if (result != null) return result;
        }

        return null;
    }

    private static Button FindButton(Transform parent, string name)
    {
        if (parent == null) return null;
        Button[] buttons = parent.GetComponentsInChildren<Button>(true);
        foreach (Button button in buttons)
            if (button.name == name) return button;

        return null;
    }

    private static TMP_Text FindText(Transform parent, string name)
    {
        if (parent == null) return null;
        TMP_Text[] texts = parent.GetComponentsInChildren<TMP_Text>(true);
        foreach (TMP_Text text in texts)
            if (text.name == name) return text;

        return null;
    }
}
