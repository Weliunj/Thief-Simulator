using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class DynamicJoystick : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
{
    [Header("⚙️ Joystick UI Elements")]
    public RectTransform backgroundRect; // Vòng tròn nền Joystick
    public RectTransform handleRect;     // Cần gạt di chuyển ở tâm

    [Header("📐 Settings")]
    public float handleRange = 100f;     // Bán kính di chuyển tối đa của cần gạt (pixel)
    public bool hideOnRelease = true;    // Tự động ẩn Joystick khi không chạm vào màn hình

    private Vector2 inputVector = Vector2.zero;
    private Vector2 pointerDownPosition = Vector2.zero;
    private Canvas canvas;
    private Camera uiCamera;

    public bool IsPressed { get; private set; } = false;

    // Getter trả về giá trị đầu vào (x từ -1 đến 1, y từ -1 đến 1)
    public float Horizontal => inputVector.x;
    public float Vertical => inputVector.y;
    public Vector2 Direction => inputVector;

    void Start()
    {
        canvas = GetComponentInParent<Canvas>();
        if (canvas != null && canvas.renderMode == RenderMode.ScreenSpaceCamera)
        {
            uiCamera = canvas.worldCamera;
        }

        if (hideOnRelease && backgroundRect != null)
        {
            backgroundRect.gameObject.SetActive(false);
        }
    }

    // 1. Khi ngón tay CHẠM xuống màn hình
    public void OnPointerDown(PointerEventData eventData)
    {
        if (backgroundRect == null || handleRect == null) return;

        IsPressed = true;
        pointerDownPosition = eventData.position;

        // Hiện Joystick ngay tại vị trí chạm tay
        backgroundRect.gameObject.SetActive(true);

        // Tính tọa độ local RELATIVE TO parent của backgroundRect (TouchZoneLeft)
        // chứ không phải Canvas root — để anchoredPosition chính xác
        Vector2 localPoint;
        RectTransform parentRect = backgroundRect.parent as RectTransform;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            parentRect,
            eventData.position,
            uiCamera,
            out localPoint
        );

        backgroundRect.anchoredPosition = localPoint;
        handleRect.anchoredPosition = Vector2.zero;
        inputVector = Vector2.zero;
    }

    // 2. Khi ngón tay VUỐT / KÉO trên màn hình
    public void OnDrag(PointerEventData eventData)
    {
        if (backgroundRect == null || handleRect == null) return;

        Vector2 direction = eventData.position - pointerDownPosition;

        // Giới hạn khoảng cách di chuyển của Handle theo bán kính handleRange
        inputVector = direction.magnitude > handleRange ? direction.normalized : direction / handleRange;

        // Cập nhật vị trí của Handle UI
        handleRect.anchoredPosition = inputVector * handleRange;
    }

    // 3. Khi BUÔNG TAY khỏi màn hình
    public void OnPointerUp(PointerEventData eventData)
    {
        IsPressed = false;
        inputVector = Vector2.zero;

        if (handleRect != null) handleRect.anchoredPosition = Vector2.zero;

        if (hideOnRelease && backgroundRect != null)
        {
            backgroundRect.gameObject.SetActive(false);
        }
    }
}
