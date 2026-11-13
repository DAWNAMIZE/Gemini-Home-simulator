using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Repair prompt shown while the player holds Hammer_01 near Window-1.
/// </summary>
public class RepairUI : MonoBehaviour
{
    public static RepairUI instance;

    private GameObject panel;
    private TMP_Text titleLabel;
    private WindowRepairPoint activeWindow;
    private bool built;

    public static RepairUI GetOrCreate()
    {
        if (instance != null) return instance;

        RepairUI existing = Object.FindFirstObjectByType<RepairUI>(FindObjectsInactive.Include);
        if (existing != null)
        {
            instance = existing;
            return existing;
        }

        GameObject go = new GameObject("RepairUI");
        instance = go.AddComponent<RepairUI>();
        return instance;
    }

    public static void Show(WindowRepairPoint window)
    {
        GetOrCreate().ShowInternal(window);
    }

    public static void Hide()
    {
        if (instance != null) instance.HideInternal();
    }

    public static void HideIf(WindowRepairPoint window)
    {
        if (instance != null && instance.activeWindow == window) instance.HideInternal();
    }

    void Awake()
    {
        if (instance == null) instance = this;
        else if (instance != this) { Destroy(gameObject); return; }
    }

    void OnDestroy()
    {
        if (instance == this) instance = null;
    }

    private void ShowInternal(WindowRepairPoint window)
    {
        Build();
        activeWindow = window;

        if (titleLabel != null)
            titleLabel.text = window != null && !string.IsNullOrEmpty(window.windowName)
                ? window.windowName
                : "Window";

        if (panel != null) panel.SetActive(true);
    }

    private void HideInternal()
    {
        if (panel != null) panel.SetActive(false);
        activeWindow = null;
    }

    private void OnRepairClicked()
    {
        WindowRepairPoint window = activeWindow;
        HideInternal();
        if (window != null) window.Repair();
    }

    private void Build()
    {
        if (built) return;
        built = true;

        Canvas canvas = RuntimeUI.CreateOverlayCanvas("RepairCanvas", 960);
        canvas.transform.SetParent(transform, false);

        Image background = RuntimeUI.CreateImage(canvas.transform, "RepairPanel",
            new Color(0.04f, 0.05f, 0.08f, 0.95f));
        panel = background.gameObject;

        RectTransform panelRt = background.rectTransform;
        panelRt.anchorMin = new Vector2(0.5f, 0.5f);
        panelRt.anchorMax = new Vector2(0.5f, 0.5f);
        panelRt.pivot = new Vector2(0.5f, 0.5f);
        panelRt.anchoredPosition = new Vector2(0f, 20f);
        panelRt.sizeDelta = new Vector2(580f, 300f);

        // Simple hammer "icon".
        Image icon = RuntimeUI.CreateImage(panel.transform, "Icon",
            new Color(0.95f, 0.75f, 0.24f, 1f));
        RectTransform iconRt = icon.rectTransform;
        iconRt.anchorMin = new Vector2(0.5f, 1f);
        iconRt.anchorMax = new Vector2(0.5f, 1f);
        iconRt.pivot = new Vector2(0.5f, 1f);
        iconRt.anchoredPosition = new Vector2(0f, -24f);
        iconRt.sizeDelta = new Vector2(64f, 64f);

        TMP_Text iconLabel = RuntimeUI.CreateText(icon.transform, "IconLabel", "!",
            42f, new Color(0.15f, 0.12f, 0.05f), TextAlignmentOptions.Center);
        RectTransform iconLabelRt = iconLabel.rectTransform;
        iconLabelRt.anchorMin = Vector2.zero;
        iconLabelRt.anchorMax = Vector2.one;
        iconLabelRt.offsetMin = Vector2.zero;
        iconLabelRt.offsetMax = Vector2.zero;

        titleLabel = RuntimeUI.CreateText(panel.transform, "Title", "Window",
            36f, Color.white, TextAlignmentOptions.Center);
        RectTransform titleRt = titleLabel.rectTransform;
        titleRt.anchorMin = new Vector2(0f, 1f);
        titleRt.anchorMax = new Vector2(1f, 1f);
        titleRt.pivot = new Vector2(0.5f, 1f);
        titleRt.anchoredPosition = new Vector2(0f, -100f);
        titleRt.sizeDelta = new Vector2(-30f, 52f);

        TMP_Text caption = RuntimeUI.CreateText(panel.transform, "Caption",
            "Hold Hammer_01 to board this window with Wooden-Bars.", 24f,
            new Color(0.82f, 0.84f, 0.88f), TextAlignmentOptions.Center);
        RectTransform captionRt = caption.rectTransform;
        captionRt.anchorMin = new Vector2(0f, 1f);
        captionRt.anchorMax = new Vector2(1f, 1f);
        captionRt.pivot = new Vector2(0.5f, 1f);
        captionRt.anchoredPosition = new Vector2(0f, -150f);
        captionRt.sizeDelta = new Vector2(-50f, 50f);

        RuntimeUI.CreateButton(panel.transform, "RepairButton", "REPAIR",
            new Vector2(280f, 76f), new Vector2(0f, 40f), new Vector2(0.5f, 0f),
            new Color(0.62f, 0.42f, 0.14f, 1f), new UnityAction(OnRepairClicked));

        RuntimeUI.CreateButton(panel.transform, "CloseButton", "X",
            new Vector2(46f, 46f), new Vector2(-22f, -22f), new Vector2(1f, 1f),
            new Color(0.22f, 0.22f, 0.25f, 1f), new UnityAction(HideInternal));

        panel.SetActive(false);
    }
}
