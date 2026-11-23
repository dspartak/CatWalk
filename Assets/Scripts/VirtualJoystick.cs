using UnityEngine;
using UnityEngine.EventSystems;

public class VirtualJoystick : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
{
    public RectTransform handle;
    public float maxRadius = 80f;
    public CatDirectControl cat;

    RectTransform bg;

    void Awake() => bg = GetComponent<RectTransform>();

    public void OnPointerDown(PointerEventData e) => OnDrag(e);

    public void OnDrag(PointerEventData e)
    {
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            bg, e.position, e.pressEventCamera, out Vector2 pos);

        Vector2 input = Vector2.ClampMagnitude(pos / maxRadius, 1f);
        if (handle != null) handle.anchoredPosition = input * maxRadius;
        if (cat != null) cat.SetInput(input);
    }

    public void OnPointerUp(PointerEventData e)
    {
        if (handle != null) handle.anchoredPosition = Vector2.zero;
        if (cat != null) cat.SetInput(Vector2.zero);
    }
}
