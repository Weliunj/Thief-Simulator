using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// Vùng vuốt bên phải màn hình để xoay camera.
/// Gắn script này vào một Panel/Image trong suốt (alpha=0) phủ nửa phải màn hình.
/// Cần có Image component với Raycast Target = true.
/// </summary>
public class TouchLookZone : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
{
    [Header("⚙️ Settings")]
    [Tooltip("Hệ số nhạy khi vuốt (lấy trực tiếp từ Setting)")]
    public float sensitivity = 2.0f;

    /// <summary>
    /// Delta vuốt mỗi frame — StarterAssetsInputs sẽ đọc giá trị này.
    /// </summary>
    public Vector2 LookDelta { get; private set; } = Vector2.zero;
    public bool IsTouching { get; private set; } = false;

    private Vector2 lastPointerPosition;

    private void Start()
    {
        UpdateSensitivity(SettingsManager.Instance != null ? SettingsManager.Instance.Sensitivity : 2.0f);
    }

    private void OnEnable()
    {
        SettingsManager.OnSensitivityChanged += UpdateSensitivity;
    }

    private void OnDestroy()
    {
        SettingsManager.OnSensitivityChanged -= UpdateSensitivity;
    }

    private void UpdateSensitivity(float settingValue)
    {
        // Gán trực tiếp giá trị từ Setting (1.0 -> 5.0)
        sensitivity = settingValue;
    }

    // 1. Chạm xuống → ghi nhận vị trí đầu tiên
    public void OnPointerDown(PointerEventData eventData)
    {
        IsTouching = true;
        lastPointerPosition = eventData.position;
        LookDelta = Vector2.zero;
    }

    // 2. Vuốt → tính delta (chênh lệch) so với frame trước
    public void OnDrag(PointerEventData eventData)
    {
        Vector2 currentPosition = eventData.position;
        // Hệ số nhạy chuẩn cho màn hình cảm ứng:
        Vector2 delta = (currentPosition - lastPointerPosition) * (sensitivity * 0.15f);
        // Đảo trục Y: vuốt lên → camera nhìn lên (không bị invert)
        delta.y = -delta.y;
        LookDelta = delta;
        lastPointerPosition = currentPosition;
    }

    // 3. Thả tay → reset
    public void OnPointerUp(PointerEventData eventData)
    {
        IsTouching = false;
        LookDelta = Vector2.zero;
    }

    void LateUpdate()
    {
        // Reset delta mỗi frame để tránh giá trị cũ bị giữ lại
        // (OnDrag chỉ gọi khi CÓ di chuyển, nếu giữ yên thì delta phải = 0)
        if (IsTouching)
        {
            // Sau khi StarterAssetsInputs đã đọc xong trong Update(),
            // reset delta về zero cho frame tiếp theo
            LookDelta = Vector2.zero;
        }
    }
}
