using UnityEngine;

/// <summary>
/// Tự động xoay ảnh Loading Spinner theo từng bước (Stepped / Frame-by-Frame):
/// - Mặc định xoay theo 2 FPS (mỗi 0.5 giây giật/nhảy 1 bước góc 45° hoặc 90°).
/// - Sử dụng Time.unscaledTime để hoạt động ổn định ngay cả khi game bị Pause.
/// </summary>
public class UISpinnerRotator : MonoBehaviour
{
    [Header("⚙️ Rotation Settings")]
    [Tooltip("Tốc độ khung hình xoay (Mặc định 2 FPS = giật 2 lần/giây)")]
    public float targetFPS = 2.0f;

    [Tooltip("Góc xoay mỗi bước nhảy (Ví dụ: -45 độ theo chiều kim đồng hồ)")]
    public float angleStep = -45.0f;

    private float timer = 0f;

    private void OnEnable()
    {
        timer = 0f;
    }

    private void Update()
    {
        if (targetFPS <= 0f) return;

        float interval = 1.0f / targetFPS; // 2 FPS -> 0.5 giây / 1 bước
        timer += Time.unscaledDeltaTime;

        if (timer >= interval)
        {
            timer -= interval;
            transform.Rotate(0f, 0f, angleStep);
        }
    }
}
