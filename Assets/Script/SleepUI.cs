using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Prompt shown when the player is near Bed_Save. Sleeping advances to the
/// morning and stores the game.
/// </summary>
public class SleepUI : MonoBehaviour
{
    public static SleepUI instance;

    private GameObject panel;
    private TMP_Text titleLabel;
    private BedSavePoint activeBed;
    private bool built;

    public static SleepUI GetOrCreate()
    {
        if (instance != null) return instance;

        SleepUI existing = Object.FindFirstObjectByType<SleepUI>(FindObjectsInactive.Include);
        if (existing != null)
        {
            instance = existing;
            return existing;
        }

        GameObject go = new GameObject("SleepUI");
        instance = go.AddComponent<SleepUI>();
        return instance;
    }

    public static void Show(BedSavePoint bed)
    {
        GetOrCreate().ShowInternal(bed);
    }

    public static void Hide()
    {
        if (instance != null) instance.HideInternal();
    }

    public static void HideIf(BedSavePoint bed)
    {
        if (instance != null && instance.activeBed == bed) instance.HideInternal();
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

    private void ShowInternal(BedSavePoint bed)
    {
        Build();
        activeBed = bed;

        if (titleLabel != null)
            titleLabel.text = bed != null && !string.IsNullOrEmpty(bed.bedName)
                ? bed.bedName
                : "Bed";

        if (panel != null) panel.SetActive(true);
    }

    private void HideInternal()
    {
        if (panel != null) panel.SetActive(false);
        activeBed = null;
    }

    private void OnSleepClicked()
    {
        BedSavePoint bed = activeBed;
        HideInternal();
        if (bed != null) bed.Sleep();
    }

    private void Build()
    {
        if (built) return;
        built = true;

        Canvas canvas = RuntimeUI.CreateOverlayCanvas("SleepCanvas", 960);
        canvas.transform.SetParent(transform, false);

        Image background = RuntimeUI.CreateImage(canvas.transform, "SleepPanel",
            new Color(0.03f, 0.04f, 0.07f, 0.95f));
        panel = background.gameObject;

        RectTransform panelRt = background.rectTransform;
        panelRt.anchorMin = new Vector2(0.5f, 0.5f);
        panelRt.anchorMax = new Vector2(0.5f, 0.5f);
        panelRt.pivot = new Vector2(0.5f, 0.5f);
        panelRt.anchoredPosition = new Vector2(0f, 40f);
        panelRt.sizeDelta = new Vector2(620f, 330f);

        titleLabel = RuntimeUI.CreateText(panel.transform, "Title", "Bed",
            42f, Color.white, TextAlignmentOptions.Center);
        RectTransform titleRt = titleLabel.rectTransform;
        titleRt.anchorMin = new Vector2(0f, 1f);
        titleRt.anchorMax = new Vector2(1f, 1f);
        titleRt.pivot = new Vector2(0.5f, 1f);
        titleRt.anchoredPosition = new Vector2(0f, -30f);
        titleRt.sizeDelta = new Vector2(-30f, 60f);

        TMP_Text caption = RuntimeUI.CreateText(panel.transform, "Caption",
            "Sleep until morning and save your progress?", 26f,
            new Color(0.82f, 0.84f, 0.88f), TextAlignmentOptions.Center);
        RectTransform captionRt = caption.rectTransform;
        captionRt.anchorMin = new Vector2(0f, 1f);
        captionRt.anchorMax = new Vector2(1f, 1f);
        captionRt.pivot = new Vector2(0.5f, 1f);
        captionRt.anchoredPosition = new Vector2(0f, -112f);
        captionRt.sizeDelta = new Vector2(-50f, 70f);

        RuntimeUI.CreateButton(panel.transform, "SleepButton", "SLEEP & SAVE",
            new Vector2(300f, 82f), new Vector2(0f, 46f), new Vector2(0.5f, 0f),
            new Color(0.16f, 0.35f, 0.60f, 1f), new UnityAction(OnSleepClicked));

        RuntimeUI.CreateButton(panel.transform, "CloseButton", "NOT NOW",
            new Vector2(220f, 62f), new Vector2(0f, 118f), new Vector2(0.5f, 0f),
            new Color(0.24f, 0.24f, 0.27f, 1f), new UnityAction(HideInternal));

        panel.SetActive(false);
    }
}
