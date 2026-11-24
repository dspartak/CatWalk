using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public class TapToMove : MonoBehaviour
{
    public CatWander cat;
    public Camera cam;
    public LayerMask groundMask = ~0;
    public GameObject tapMarkerPrefab;
    public float markerLifetime = 1f;

    void Update()
    {
        bool tapped = false;
        Vector2 pos = Vector2.zero;

#if ENABLE_INPUT_SYSTEM
        if (Pointer.current != null && Pointer.current.press.wasPressedThisFrame)
        {
            tapped = true;
            pos = Pointer.current.position.ReadValue();
        }
        else if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.wasPressedThisFrame)
        {
            tapped = true;
            pos = Touchscreen.current.primaryTouch.position.ReadValue();
        }
        else if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
        {
            tapped = true;
            pos = Mouse.current.position.ReadValue();
        }
#elif ENABLE_LEGACY_INPUT_MANAGER
        if (Input.GetMouseButtonDown(0))
        {
            tapped = true;
            pos = Input.mousePosition;
        }
        else if (Input.touchCount > 0)
        {
            UnityEngine.Touch t = Input.GetTouch(0);
            if (t.phase == UnityEngine.TouchPhase.Began)
            {
                tapped = true;
                pos = t.position;
            }
        }
#endif

        if (tapped) HandleTap(pos);
    }

    void HandleTap(Vector2 screenPos)
    {
        // Не двигать котика при кликах по UI или когда открыто главное меню
        if (UnityEngine.EventSystems.EventSystem.current != null && UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject())
            return;

        if (MainMenuController.Instance != null && MainMenuController.Instance.isMenuOpen)
            return;

        if (cam == null) cam = Camera.main;
        if (cam == null) return;

        Ray ray = cam.ScreenPointToRay(screenPos);
        if (Physics.Raycast(ray, out RaycastHit hit, 200f, groundMask))
        {
            if (cat != null) cat.SetManualTarget(hit.point);
            if (tapMarkerPrefab != null)
            {
                GameObject m = Instantiate(tapMarkerPrefab, hit.point + Vector3.up * 0.05f, Quaternion.identity);
                Destroy(m, markerLifetime);
            }
        }
    }
}
