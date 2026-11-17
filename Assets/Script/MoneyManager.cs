using System;
using UnityEngine;
using TMPro;

/// <summary>
/// Tracks the player's money and draws a small HUD in the top-left corner.
/// The manager survives scene loads so the balance is persistent during a run.
/// </summary>
public class MoneyManager : MonoBehaviour
{
    public static MoneyManager instance;

    [Header("Money")]
    public int startingMoney = 0;
    public int money;

    public event Action<int> MoneyChanged;

    private Canvas canvas;
    private TMP_Text amountLabel;
    private bool built;

    void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        money = startingMoney;
    }

    void Start()
    {
        Build();
        RefreshHUD();
    }

    void OnDestroy()
    {
        if (instance == this) instance = null;
    }

    public static MoneyManager GetOrCreate()
    {
        if (instance != null) return instance;

        MoneyManager existing = UnityEngine.Object.FindAnyObjectByType<MoneyManager>(
            FindObjectsInactive.Include);
        if (existing != null)
        {
            instance = existing;
            return existing;
        }

        GameObject go = new GameObject("MoneyManager");
        instance = go.AddComponent<MoneyManager>();
        DontDestroyOnLoad(go);
        return instance;
    }

    public static void Add(int amount)
    {
        GetOrCreate().AddMoney(amount);
    }

    public void AddMoney(int amount)
    {
        if (amount == 0) return;

        money = Mathf.Max(0, money + amount);
        RefreshHUD();
        MoneyChanged?.Invoke(money);

        Debug.Log("MoneyManager: " + (amount > 0 ? "+" : "") + amount +
                  " | balance $" + money);
    }

    public bool TrySpend(int amount)
    {
        if (amount <= 0) return true;

        if (money < amount)
        {
            ToastUI.Show("Not enough money.");
            return false;
        }

        money -= amount;
        RefreshHUD();
        MoneyChanged?.Invoke(money);
        return true;
    }

    private void RefreshHUD()
    {
        Build();
        if (amountLabel != null) amountLabel.text = "$" + money;
    }

    private void Build()
    {
        if (built) return;
        built = true;

        canvas = RuntimeUI.CreateOverlayCanvas("MoneyCanvas", 1000);
        canvas.transform.SetParent(transform, false);

        UnityEngine.UI.Image panel = RuntimeUI.CreateImage(canvas.transform,
            "MoneyPanel", new Color(0.03f, 0.05f, 0.08f, 0.88f));

        RectTransform panelRt = panel.rectTransform;
        panelRt.anchorMin = new Vector2(0f, 1f);
        panelRt.anchorMax = new Vector2(0f, 1f);
        panelRt.pivot = new Vector2(0f, 1f);
        panelRt.anchoredPosition = new Vector2(26f, -26f);
        panelRt.sizeDelta = new Vector2(300f, 78f);

        UnityEngine.UI.Image accent = RuntimeUI.CreateImage(panel.transform,
            "Accent", new Color(0.32f, 0.82f, 0.43f, 1f));

        RectTransform accentRt = accent.rectTransform;
        accentRt.anchorMin = new Vector2(0f, 0f);
        accentRt.anchorMax = new Vector2(0f, 1f);
        accentRt.pivot = new Vector2(0f, 0.5f);
        accentRt.anchoredPosition = Vector2.zero;
        accentRt.sizeDelta = new Vector2(7f, 0f);

        TMP_Text title = RuntimeUI.CreateText(panel.transform, "Title", "MONEY",
            19f, new Color(0.68f, 0.76f, 0.86f), TextAlignmentOptions.Left);
        RectTransform titleRt = title.rectTransform;
        titleRt.anchorMin = new Vector2(0f, 1f);
        titleRt.anchorMax = new Vector2(1f, 1f);
        titleRt.pivot = new Vector2(0f, 1f);
        titleRt.anchoredPosition = new Vector2(20f, -10f);
        titleRt.sizeDelta = new Vector2(-32f, 26f);

        amountLabel = RuntimeUI.CreateText(panel.transform, "Amount", "$0",
            34f, new Color(0.63f, 1f, 0.68f), TextAlignmentOptions.Left);
        amountLabel.fontStyle = FontStyles.Bold;

        RectTransform amountRt = amountLabel.rectTransform;
        amountRt.anchorMin = new Vector2(0f, 0f);
        amountRt.anchorMax = new Vector2(1f, 0f);
        amountRt.pivot = new Vector2(0f, 0f);
        amountRt.anchoredPosition = new Vector2(20f, 9f);
        amountRt.sizeDelta = new Vector2(-32f, 42f);
    }
}
