using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Small helpers for the runtime-built UI used by the phone HUD, inventory
/// actions, sleep prompt and repair prompt. Building these in code keeps the
/// scene wiring minimal and avoids breaking existing prefab instances.
/// </summary>
public static class RuntimeUI
{
    public static Canvas CreateOverlayCanvas(string name, int sortingOrder)
    {
        GameObject canvasGo = new GameObject(name,
            typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));

        Canvas canvas = canvasGo.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = sortingOrder;

        CanvasScaler scaler = canvasGo.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        EnsureEventSystem();
        return canvas;
    }

    public static void EnsureEventSystem()
    {
        if (Object.FindFirstObjectByType<EventSystem>() != null) return;

        GameObject go = new GameObject("EventSystem",
            typeof(EventSystem), typeof(StandaloneInputModule));
        Object.DontDestroyOnLoad(go);
    }

    public static RectTransform CreateRect(Transform parent, string name)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        return go.GetComponent<RectTransform>();
    }

    public static Image CreateImage(Transform parent, string name, Color color)
    {
        GameObject go = new GameObject(name,
            typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        go.transform.SetParent(parent, false);
        Image image = go.GetComponent<Image>();
        image.color = color;
        return image;
    }

    public static TMP_Text CreateText(Transform parent, string name, string text,
        float fontSize, Color color, TextAlignmentOptions alignment = TextAlignmentOptions.Center)
    {
        GameObject go = new GameObject(name,
            typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        go.transform.SetParent(parent, false);

        TextMeshProUGUI label = go.GetComponent<TextMeshProUGUI>();
        label.text = text;
        label.fontSize = fontSize;
        label.color = color;
        label.alignment = alignment;
        label.raycastTarget = false;

        TMP_FontAsset font = GetDefaultFont();
        if (font != null) label.font = font;

        return label;
    }

    public static Button CreateButton(Transform parent, string name, string labelText,
        Vector2 size, Vector2 anchoredPosition, Vector2 anchor, Color color, UnityAction onClick)
    {
        GameObject go = new GameObject(name,
            typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        go.transform.SetParent(parent, false);

        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = anchor;
        rt.anchorMax = anchor;
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = anchoredPosition;
        rt.sizeDelta = size;

        Image image = go.GetComponent<Image>();
        image.color = color;

        Button button = go.GetComponent<Button>();
        button.targetGraphic = image;
        Navigation navigation = button.navigation;
        navigation.mode = Navigation.Mode.None;
        button.navigation = navigation;
        if (onClick != null) button.onClick.AddListener(onClick);

        TMP_Text label = CreateText(go.transform, "Label", labelText,
            Mathf.Clamp(size.y * 0.36f, 18f, 42f), Color.white);
        RectTransform labelRt = label.rectTransform;
        labelRt.anchorMin = Vector2.zero;
        labelRt.anchorMax = Vector2.one;
        labelRt.offsetMin = Vector2.zero;
        labelRt.offsetMax = Vector2.zero;

        return button;
    }

    public static TMP_FontAsset GetDefaultFont()
    {
        if (TMP_Settings.defaultFontAsset != null) return TMP_Settings.defaultFontAsset;

        TMP_Text[] texts = Object.FindObjectsByType<TMP_Text>(
            FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (TMP_Text text in texts)
        {
            if (text != null && text.font != null) return text.font;
        }

        return null;
    }

    public static void SetLabel(Button button, string text)
    {
        if (button == null) return;
        TMP_Text label = button.GetComponentInChildren<TMP_Text>(true);
        if (label != null) label.text = text;
    }
}
