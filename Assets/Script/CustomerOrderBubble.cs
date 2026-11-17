using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// World-space order bubble that floats above a customer's head. It shows the
/// requested dish and turns into a progress bar while the customer is eating.
/// </summary>
public class CustomerOrderBubble : MonoBehaviour
{
    [Header("Follow")]
    public Transform followTarget;
    public float heightOffset = 1.7f;
    public float worldScale = 0.0042f;

    private Canvas canvas;
    private GameObject bubbleRoot;
    private TMP_Text headerLabel;
    private TMP_Text dishLabel;
    private GameObject progressRoot;
    private Image progressFill;
    private float hideAt = -1f;
    private bool built;

    public static CustomerOrderBubble Attach(Transform target)
    {
        if (target == null) return null;

        CustomerOrderBubble bubble = target.GetComponentInChildren<CustomerOrderBubble>(true);
        if (bubble == null)
        {
            GameObject go = new GameObject("OrderBubble");
            go.transform.SetParent(target, false);
            bubble = go.AddComponent<CustomerOrderBubble>();
        }

        bubble.followTarget = target;
        return bubble;
    }

    void Awake()
    {
        if (followTarget == null) followTarget = transform.parent;
        Build();
        Hide();
    }

    void LateUpdate()
    {
        if (followTarget != null)
        {
            transform.position = followTarget.position + Vector3.up * heightOffset;

            Camera cam = Camera.main;
            if (cam != null)
                transform.rotation = cam.transform.rotation;
        }

        if (hideAt > 0f && Time.time >= hideAt)
        {
            hideAt = -1f;
            Hide();
        }
    }

    public void ShowOrder(string dishName)
    {
        Build();
        hideAt = -1f;
        if (headerLabel != null) headerLabel.text = "WANTS";
        if (dishLabel != null) dishLabel.text = dishName;
        if (progressRoot != null) progressRoot.SetActive(false);
        if (canvas != null) canvas.gameObject.SetActive(true);
    }

    public void ShowEating(float progress)
    {
        Build();
        hideAt = -1f;
        if (headerLabel != null) headerLabel.text = "EATING";
        if (dishLabel != null) dishLabel.text = "Enjoying the meal...";
        if (progressRoot != null) progressRoot.SetActive(true);
        if (progressFill != null)
        {
            RectTransform fillRt = progressFill.rectTransform;
            fillRt.anchorMin = Vector2.zero;
            fillRt.anchorMax = new Vector2(Mathf.Clamp01(progress), 1f);
            fillRt.offsetMin = Vector2.zero;
            fillRt.offsetMax = Vector2.zero;
        }
        if (canvas != null) canvas.gameObject.SetActive(true);
    }

    public void ShowPaid(int amount)
    {
        Build();
        if (headerLabel != null) headerLabel.text = "PAID";
        if (dishLabel != null) dishLabel.text = "+$" + amount;
        if (progressRoot != null) progressRoot.SetActive(false);
        if (canvas != null) canvas.gameObject.SetActive(true);
        hideAt = Time.time + 1.6f;
    }

    public void Hide()
    {
        hideAt = -1f;
        if (canvas != null) canvas.gameObject.SetActive(false);
    }

    private void Build()
    {
        if (built) return;
        built = true;

        GameObject canvasGo = new GameObject("OrderBubbleCanvas",
            typeof(RectTransform), typeof(Canvas));
        canvasGo.transform.SetParent(transform, false);
        canvas = canvasGo.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvas.sortingOrder = 20;

        RectTransform canvasRt = canvasGo.GetComponent<RectTransform>();
        canvasRt.sizeDelta = new Vector2(340f, 190f);

        GameObject bubble = new GameObject("Bubble",
            typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        bubble.transform.SetParent(canvasGo.transform, false);
        bubbleRoot = bubble;

        RectTransform bubbleRt = bubble.GetComponent<RectTransform>();
        bubbleRt.anchorMin = Vector2.zero;
        bubbleRt.anchorMax = Vector2.one;
        bubbleRt.offsetMin = Vector2.zero;
        bubbleRt.offsetMax = Vector2.zero;

        Image background = bubble.GetComponent<Image>();
        background.color = new Color(0.98f, 0.97f, 0.92f, 0.97f);

        Image accent = RuntimeUI.CreateImage(bubble.transform, "Accent",
            new Color(0.90f, 0.47f, 0.17f, 1f));
        RectTransform accentRt = accent.rectTransform;
        accentRt.anchorMin = new Vector2(0f, 1f);
        accentRt.anchorMax = new Vector2(1f, 1f);
        accentRt.pivot = new Vector2(0.5f, 1f);
        accentRt.anchoredPosition = Vector2.zero;
        accentRt.sizeDelta = new Vector2(0f, 12f);

        headerLabel = RuntimeUI.CreateText(bubble.transform, "Header", "WANTS",
            17f, new Color(0.44f, 0.34f, 0.24f), TextAlignmentOptions.Left);
        RectTransform headerRt = headerLabel.rectTransform;
        headerRt.anchorMin = new Vector2(0f, 1f);
        headerRt.anchorMax = new Vector2(1f, 1f);
        headerRt.pivot = new Vector2(0f, 1f);
        headerRt.anchoredPosition = new Vector2(18f, -26f);
        headerRt.sizeDelta = new Vector2(-36f, 24f);

        dishLabel = RuntimeUI.CreateText(bubble.transform, "Dish", "Dish",
            27f, new Color(0.11f, 0.12f, 0.14f), TextAlignmentOptions.Left);
        dishLabel.fontStyle = FontStyles.Bold;
        RectTransform dishRt = dishLabel.rectTransform;
        dishRt.anchorMin = new Vector2(0f, 0f);
        dishRt.anchorMax = new Vector2(1f, 1f);
        dishRt.pivot = new Vector2(0.5f, 0.5f);
        dishRt.offsetMin = new Vector2(18f, 58f);
        dishRt.offsetMax = new Vector2(-18f, -48f);

        GameObject progress = new GameObject("Progress",
            typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        progress.transform.SetParent(bubble.transform, false);
        progressRoot = progress;

        RectTransform progressRt = progress.GetComponent<RectTransform>();
        progressRt.anchorMin = new Vector2(0f, 0f);
        progressRt.anchorMax = new Vector2(1f, 0f);
        progressRt.pivot = new Vector2(0.5f, 0f);
        progressRt.anchoredPosition = new Vector2(0f, 20f);
        progressRt.sizeDelta = new Vector2(-36f, 24f);

        Image progressBackground = progress.GetComponent<Image>();
        progressBackground.color = new Color(0.16f, 0.17f, 0.19f, 1f);

        RectTransform fillArea = RuntimeUI.CreateRect(progress.transform, "FillArea");
        fillArea.anchorMin = Vector2.zero;
        fillArea.anchorMax = Vector2.one;
        fillArea.offsetMin = new Vector2(4f, 4f);
        fillArea.offsetMax = new Vector2(-4f, -4f);

        Image fill = RuntimeUI.CreateImage(fillArea, "Fill",
            new Color(0.32f, 0.78f, 0.42f, 1f));
        progressFill = fill;

        RectTransform fillRt = fill.rectTransform;
        fillRt.anchorMin = Vector2.zero;
        fillRt.anchorMax = Vector2.zero;
        fillRt.offsetMin = Vector2.zero;
        fillRt.offsetMax = Vector2.zero;

        transform.localScale = Vector3.one * worldScale;
        progress.SetActive(false);
        canvasGo.SetActive(false);
    }
}
