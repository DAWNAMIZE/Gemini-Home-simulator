using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Right-side serving prompt. It appears when the player stands near the
/// customer tray while a customer is waiting, and lets the player snap the
/// requested dish onto the tray.
/// </summary>
public class ServeUI : MonoBehaviour
{
    public static ServeUI instance;

    [Header("Range")]
    public float serveRange = 3.6f;

    private Canvas canvas;
    private TMP_Text dishLabel;
    private TMP_Text hintLabel;
    private Button serveButton;
    private Image serveImage;
    private CustomerTray currentTray;
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

    public static ServeUI GetOrCreate()
    {
        if (instance != null) return instance;

        ServeUI existing = UnityEngine.Object.FindAnyObjectByType<ServeUI>(
            FindObjectsInactive.Include);
        if (existing != null)
        {
            instance = existing;
            return existing;
        }

        GameObject go = new GameObject("ServeUI");
        instance = go.AddComponent<ServeUI>();
        return instance;
    }

    void Update()
    {
        Refresh();
    }

    private void Refresh()
    {
        PlayerInventory inventory = PlayerInventory.instance;
        if (inventory == null)
        {
            Hide();
            return;
        }

        Vector3 playerPosition = inventory.transform.position;
        CustomerTray best = null;
        float bestDistance = serveRange;

        foreach (CustomerTray tray in CustomerTray.All)
        {
            if (tray == null) continue;
            if (tray.assignedCustomer == null) continue;
            if (tray.assignedCustomer.IsEating) continue;
            if (tray.currentFood != null) continue;

            float distance = Vector3.Distance(playerPosition, tray.transform.position);
            if (distance <= bestDistance)
            {
                best = tray;
                bestDistance = distance;
            }
        }

        if (best == null)
        {
            Hide();
            return;
        }

        Build();
        currentTray = best;
        if (canvas != null) canvas.gameObject.SetActive(true);

        string wanted = best.assignedCustomer.requestedDish;
        FoodDish held = inventory.HeldFood;
        bool canServe = held != null &&
                        string.Equals(held.dishName, wanted, StringComparison.OrdinalIgnoreCase);

        if (dishLabel != null) dishLabel.text = wanted;

        if (hintLabel != null)
        {
            if (held == null)
                hintLabel.text = "Pick up the dish, then tap Serve";
            else if (canServe)
                hintLabel.text = "Ready to serve";
            else
                hintLabel.text = "Holding: " + held.dishName;
        }

        if (serveButton != null) serveButton.interactable = canServe;
        if (serveImage != null)
        {
            serveImage.color = canServe
                ? new Color(0.18f, 0.62f, 0.30f, 1f)
                : new Color(0.30f, 0.32f, 0.36f, 1f);
        }
    }

    private void OnServeClicked()
    {
        if (currentTray == null) return;

        if (currentTray.TryServeHeldDish())
            Hide();
    }

    private void Hide()
    {
        currentTray = null;
        if (canvas != null) canvas.gameObject.SetActive(false);
    }

    private void Build()
    {
        if (built) return;
        built = true;

        canvas = RuntimeUI.CreateOverlayCanvas("ServeCanvas", 950);
        canvas.transform.SetParent(transform, false);

        Image panel = RuntimeUI.CreateImage(canvas.transform, "ServePanel",
            new Color(0.035f, 0.05f, 0.075f, 0.94f));

        RectTransform panelRt = panel.rectTransform;
        panelRt.anchorMin = new Vector2(1f, 0.5f);
        panelRt.anchorMax = new Vector2(1f, 0.5f);
        panelRt.pivot = new Vector2(1f, 0.5f);
        panelRt.anchoredPosition = new Vector2(-40f, -60f);
        panelRt.sizeDelta = new Vector2(370f, 230f);

        Image accent = RuntimeUI.CreateImage(panel.transform, "Accent",
            new Color(0.90f, 0.47f, 0.17f, 1f));
        RectTransform accentRt = accent.rectTransform;
        accentRt.anchorMin = new Vector2(0f, 1f);
        accentRt.anchorMax = new Vector2(1f, 1f);
        accentRt.pivot = new Vector2(0.5f, 1f);
        accentRt.anchoredPosition = Vector2.zero;
        accentRt.sizeDelta = new Vector2(0f, 8f);

        TMP_Text title = RuntimeUI.CreateText(panel.transform, "Title",
            "CUSTOMER ORDER", 18f, new Color(0.90f, 0.62f, 0.34f),
            TextAlignmentOptions.Center);
        RectTransform titleRt = title.rectTransform;
        titleRt.anchorMin = new Vector2(0f, 1f);
        titleRt.anchorMax = new Vector2(1f, 1f);
        titleRt.pivot = new Vector2(0.5f, 1f);
        titleRt.anchoredPosition = new Vector2(0f, -18f);
        titleRt.sizeDelta = new Vector2(-30f, 26f);

        dishLabel = RuntimeUI.CreateText(panel.transform, "Dish", "Dish",
            30f, Color.white, TextAlignmentOptions.Center);
        dishLabel.fontStyle = FontStyles.Bold;
        RectTransform dishRt = dishLabel.rectTransform;
        dishRt.anchorMin = new Vector2(0f, 1f);
        dishRt.anchorMax = new Vector2(1f, 1f);
        dishRt.pivot = new Vector2(0.5f, 1f);
        dishRt.anchoredPosition = new Vector2(0f, -50f);
        dishRt.sizeDelta = new Vector2(-34f, 52f);

        hintLabel = RuntimeUI.CreateText(panel.transform, "Hint", string.Empty,
            18f, new Color(0.68f, 0.74f, 0.82f), TextAlignmentOptions.Center);
        RectTransform hintRt = hintLabel.rectTransform;
        hintRt.anchorMin = new Vector2(0f, 1f);
        hintRt.anchorMax = new Vector2(1f, 1f);
        hintRt.pivot = new Vector2(0.5f, 1f);
        hintRt.anchoredPosition = new Vector2(0f, -104f);
        hintRt.sizeDelta = new Vector2(-34f, 30f);

        serveButton = RuntimeUI.CreateButton(panel.transform, "ServeButton", "SERVE",
            new Vector2(300f, 64f), new Vector2(0f, -72f), new Vector2(0.5f, 0.5f),
            new Color(0.18f, 0.62f, 0.30f, 1f), OnServeClicked);

        serveImage = serveButton.GetComponent<Image>();

        canvas.gameObject.SetActive(false);
    }
}
