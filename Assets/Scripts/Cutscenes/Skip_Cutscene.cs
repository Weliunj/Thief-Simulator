using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class Skip_Cutscene : MonoBehaviour, IPointerClickHandler
{
    [Header("🎬 Target Scene Settings")]
    [Tooltip("Tên Scene tiếp theo sẽ load khi bấm Skip (Mặc định: Lv1)")]
    public string targetSceneName = "Lv1";

    [Header("✨ Pulsing Alpha Effect (CanvasGroup / Text)")]
    public CanvasGroup canvasGroup;
    public float pulseSpeed = 3.5f;
    [Range(0f, 1f)] public float minAlpha = 0.1f;
    [Range(0f, 1f)] public float maxAlpha = 1.0f;

    [Header("⌨️ Input Settings")]
    public bool allowKeyPressToSkip = true;

    private bool isSkipping = false;

    void Start()
    {
        if (canvasGroup == null)
        {
            canvasGroup = GetComponent<CanvasGroup>();
        }
    }

    void Update()
    {
        // 1. Hiệu ứng làm mờ / nhấp nháy Alpha liên tục cho CanvasGroup (Breathe Effect)
        if (canvasGroup != null)
        {
            float t = (Mathf.Sin(Time.time * pulseSpeed) + 1f) / 2f;
            canvasGroup.alpha = Mathf.Lerp(minAlpha, maxAlpha, t);
        }

        // 2. Nhận đầu vào phím bấm (Space, Enter...)
        if (allowKeyPressToSkip && !isSkipping)
        {
            if (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
            {
                TriggerSkip();
            }
        }
    }

    /// <summary>
    /// Sự kiện tự động gọi khi người chơi chạm/click vào CanvasGroup UI trên màn hình.
    /// </summary>
    public void OnPointerClick(PointerEventData eventData)
    {
        if (!isSkipping)
        {
            TriggerSkip();
        }
    }

    /// <summary>
    /// Kích hoạt Skip sang Target Scene mặc định.
    /// </summary>
    public void TriggerSkip()
    {
        SkipToScene(targetSceneName);
    }

    /// <summary>
    /// Hàm Skip Cutscene truyền tên Chapter / Scene bất kỳ vào.
    /// Ví dụ từ code khác: skipCutscene.SkipToScene("Lv2");
    /// </summary>
    /// <param name="sceneName">Tên Scene Chapter sẽ chuyển tới</param>
    public void SkipToScene(string sceneName)
    {
        if (isSkipping) return;
        isSkipping = true;

        string nextScene = string.IsNullOrEmpty(sceneName) ? targetSceneName : sceneName;
        if (string.IsNullOrEmpty(nextScene)) nextScene = "Lv1";

        Debug.Log($"⏩ Skipping Cutscene to Scene: {nextScene}");
        SceneManager.LoadScene(nextScene);
    }
}
