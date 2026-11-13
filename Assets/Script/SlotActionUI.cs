using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Opens when an inventory slot is tapped. Offers Equip / Drop for that slot.
/// </summary>
public class SlotActionUI : MonoBehaviour
{
    public static SlotActionUI instance;

    public int activeIndex = -1;

    private GameObject panel;
    private TMP_Text titleLabel;
    private bool built;

    public static SlotActionUI GetOrCreate()
    {
        if (instance != null) return instance;

        SlotActionUI existing = Object.FindFirstObjectByType<SlotActionUI>(FindObjectsInactive.Include);
        if (existing != null)
        {
            instance = existing;
            return existing;
        }

        GameObject go = new GameObject("SlotActionUI");
        instance = go.AddComponent<SlotActionUI>();
        return instance;
    }

    public static void Show(int index)
    {
        GetOrCreate().ShowInternal(index);
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

    void OnDestroy()
    {
        if (instance == this) instance = null;
    }

    private void ShowInternal(int index)
    {
        if (PlayerInventory.instance == null) return;
        if (index < 0 || index >= PlayerInventory.instance.backpackItems.Count) return;

        Build();
        activeIndex = index;

        if (titleLabel != null)
            titleLabel.text = PlayerInventory.instance.GetSlotDisplayName(index);

        if (panel != null) panel.SetActive(true);
    }

    private void HideInternal()
    {
        activeIndex = -1;
        if (panel != null) panel.SetActive(false);
    }

    private void OnEquipClicked()
    {
        int index = activeIndex;
        HideInternal();
        if (PlayerInventory.instance != null) PlayerInventory.instance.EquipSlot(index);
    }

    private void OnDropClicked()
    {
        int index = activeIndex;
        HideInternal();
        if (PlayerInventory.instance != null) PlayerInventory.instance.DropSlot(index);
    }

    private void Build()
    {
        if (built) return;
        built = true;

        Canvas canvas = RuntimeUI.CreateOverlayCanvas("SlotActionCanvas", 950);
        canvas.transform.SetParent(transform, false);

        Image background = RuntimeUI.CreateImage(canvas.transform, "SlotActionPanel",
            new Color(0.04f, 0.05f, 0.08f, 0.95f));
        panel = background.gameObject;

        RectTransform panelRt = background.rectTransform;
        panelRt.anchorMin = new Vector2(0.5f, 0f);
        panelRt.anchorMax = new Vector2(0.5f, 0f);
        panelRt.pivot = new Vector2(0.5f, 0f);
        panelRt.anchoredPosition = new Vector2(0f, 280f);
        panelRt.sizeDelta = new Vector2(560f, 190f);

        titleLabel = RuntimeUI.CreateText(panel.transform, "Title", "Item",
            32f, Color.white, TextAlignmentOptions.Center);
        RectTransform titleRt = titleLabel.rectTransform;
        titleRt.anchorMin = new Vector2(0f, 1f);
        titleRt.anchorMax = new Vector2(1f, 1f);
        titleRt.pivot = new Vector2(0.5f, 1f);
        titleRt.anchoredPosition = new Vector2(0f, -16f);
        titleRt.sizeDelta = new Vector2(-24f, 46f);

        RuntimeUI.CreateButton(panel.transform, "EquipButton", "EQUIP",
            new Vector2(220f, 76f), new Vector2(-120f, 46f), new Vector2(0.5f, 0f),
            new Color(0.15f, 0.46f, 0.26f, 1f), new UnityAction(OnEquipClicked));

        RuntimeUI.CreateButton(panel.transform, "DropButton", "DROP",
            new Vector2(220f, 76f), new Vector2(120f, 46f), new Vector2(0.5f, 0f),
            new Color(0.45f, 0.22f, 0.18f, 1f), new UnityAction(OnDropClicked));

        RuntimeUI.CreateButton(panel.transform, "CloseButton", "X",
            new Vector2(48f, 48f), new Vector2(-24f, -24f), new Vector2(1f, 1f),
            new Color(0.22f, 0.22f, 0.25f, 1f), new UnityAction(HideInternal));

        panel.SetActive(false);
    }
}
