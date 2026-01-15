using UnityEngine;
using UnityEngine.EventSystems;

public class MobileJoystick : MonoBehaviour, IDragHandler, IPointerUpHandler, IPointerDownHandler
{
    private RectTransform bg;
    private RectTransform handle;
    private Vector2 inputVector = Vector2.zero;

    void Awake()
    {
        // Joystick should have 2 children: [0] = BG, [1] = Handle
        bg = GetComponent<RectTransform>();
        handle = transform.GetChild(0).GetComponent<RectTransform>();
    }

    public void OnDrag(PointerEventData eventData)
    {
        Vector2 pos;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(bg, eventData.position, eventData.pressEventCamera, out pos);

        pos.x /= bg.sizeDelta.x / 2;
        pos.y /= bg.sizeDelta.y / 2;

        inputVector = new Vector2(pos.x, pos.y);
        if (inputVector.magnitude > 1.0f)
            inputVector = inputVector.normalized;

        handle.anchoredPosition = new Vector2(
            inputVector.x * (bg.sizeDelta.x / 2f),
            inputVector.y * (bg.sizeDelta.y / 2f));
    }

    public void OnPointerDown(PointerEventData eventData) => OnDrag(eventData);

    public void OnPointerUp(PointerEventData eventData)
    {
        inputVector = Vector2.zero;
        handle.anchoredPosition = Vector2.zero;
    }

    public float Horizontal() => inputVector.x;
    public float Vertical() => inputVector.y;
}
