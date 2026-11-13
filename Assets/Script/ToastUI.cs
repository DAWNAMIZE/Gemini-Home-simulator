using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Small transient message used for "hands full", "saved", "out of ammo"
/// and similar feedback. Built at runtime so it needs no scene wiring.
/// </summary>
public class ToastUI : MonoBehaviour
{
    private static ToastUI instance;

    private Canvas canvas;
    private Image background;
    private TMP_Text label;
    private float hideAt;
    private bool built;

    public static void Show(string message, float seconds = 2.4f)
    {
        if (string.IsNullOrEmpty(message)) return;
        GetOrCreate().ShowInternal(message, seconds);
    }

    private static ToastUI GetOrCreate()
    {
        if (instance != null) return instance;

        ToastUI existing = Object.FindFirstObjectByType<ToastUI>(FindObjectsInactive.Include);
        if (existing != null)
        {
            instance = existing;
            return existing;
        }

        GameObject go = new GameObject("ToastUI");
        instance = go.AddComponent<ToastUI>();
        return instance;
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

    private void ShowInternal(string message, float seconds)
    {
        Build();
        if (label != null) label.text = message;
        if (canvas != null) canvas.gameObject.SetActive(true);
        hideAt = Time.unscaledTime + Mathf.Max(0.5f, seconds);
    }

    void Update()
    {
        if (canvas == null || !canvas.gameObject.activeSelf) return;
        if (Time.unscaledTime >= hideAt) canvas.gameObject.SetActive(false);
    }

    private void Build()
    {
        if (built) return;
        built = true;

        canvas = RuntimeUI.CreateOverlayCanvas("ToastCanvas", 1100);
        canvas.transform.SetParent(transform, false);

        background = RuntimeUI.CreateImage(canvas.transform, "Toast",
            new Color(0.02f, 0.03f, 0.05f, 0.92f));

        RectTransform rt = background.rectTransform;
        rt.anchorMin = new Vector2(0.5f, 0f);
        rt.anchorMax = new Vector2(0.5f, 0f);
        rt.pivot = new Vector2(0.5f, 0f);
        rt.anchoredPosition = new Vector2(0f, 170f);
        rt.sizeDelta = new Vector2(760f, 74f);

        label = RuntimeUI.CreateText(background.transform, "Label", string.Empty,
            30f, Color.white);
        RectTransform labelRt = label.rectTransform;
        labelRt.anchorMin = Vector2.zero;
        labelRt.anchorMax = Vector2.one;
        labelRt.offsetMin = new Vector2(18f, 6f);
        labelRt.offsetMax = new Vector2(-18f, -6f);

        canvas.gameObject.SetActive(false);
    }
}
