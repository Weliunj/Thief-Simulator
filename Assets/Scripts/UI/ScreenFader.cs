using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Quản lý hiệu ứng chuyển cảnh mờ dần / rõ dần màn hình đen (Screen Fade In / Fade Out):
/// - Tự động tạo Canvas Overlay (sortingOrder = 9999) và hình nền đen toàn màn hình.
/// - Persist xuyên Scene (DontDestroyOnLoad).
/// - Hỗ trợ chuyển màn chơi mượt mà (LoadSceneWithFade).
/// - Hỗ trợ hiệu ứng đen màn hình khi tử vong / hồi sinh (FadeToBlack / FadeFromBlack).
/// </summary>
public class ScreenFader : MonoBehaviour
{
    public static ScreenFader Instance { get; private set; }

    [Header("🎨 UI Elements")]
    public Canvas faderCanvas;
    public CanvasGroup canvasGroup;
    public Image blackOverlayImage;

    [Header("⚙️ Default Settings")]
    public float defaultFadeDuration = 0.6f;

    private Coroutine _fadeCoroutine;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            EnsureUIHierarchy();
        }
        else if (Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            Instance = null;
        }
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // Nếu Scene có SceneItemSpawner, giữ màn hình đen và chờ SceneItemSpawner spawn xong + tính Target Point mới mờ dần ra
        if (FindFirstObjectByType<SceneItemSpawner>(FindObjectsInactive.Include) != null)
        {
            return;
        }

        // Khi Scene mới tải xong (Menu, Intro, v.v.): Nếu màn hình đang đen thì mờ dần ra (Fade From Black)
        if (canvasGroup != null && canvasGroup.alpha > 0.01f)
        {
            FadeFromBlack(0.6f);
        }
    }

    /// <summary>
    /// Đảm bảo luôn tồn tại Instance của ScreenFader trong game
    /// </summary>
    public static ScreenFader EnsureInstance()
    {
        if (Instance == null)
        {
            Instance = FindFirstObjectByType<ScreenFader>();
            if (Instance == null)
            {
                GameObject go = new GameObject("ScreenFader");
                Instance = go.AddComponent<ScreenFader>();
            }
        }
        return Instance;
    }

    private void EnsureUIHierarchy()
    {
        if (faderCanvas == null)
        {
            faderCanvas = GetComponent<Canvas>();
            if (faderCanvas == null) faderCanvas = gameObject.AddComponent<Canvas>();
            faderCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            faderCanvas.sortingOrder = 9999; // Luôn nằm trên cùng mọi UI khác
        }

        if (GetComponent<CanvasScaler>() == null)
        {
            var scaler = gameObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
        }

        if (canvasGroup == null)
        {
            canvasGroup = GetComponent<CanvasGroup>();
            if (canvasGroup == null)
            {
                canvasGroup = gameObject.AddComponent<CanvasGroup>();
                canvasGroup.alpha = 0f;
                canvasGroup.blocksRaycasts = false;
                canvasGroup.interactable = false;
            }
        }

        if (blackOverlayImage == null)
        {
            Transform imgChild = transform.Find("BlackOverlay");
            if (imgChild != null)
            {
                blackOverlayImage = imgChild.GetComponent<Image>();
            }
            else
            {
                GameObject imgObj = new GameObject("BlackOverlay");
                imgObj.transform.SetParent(transform, false);

                RectTransform rect = imgObj.AddComponent<RectTransform>();
                rect.anchorMin = Vector2.zero;
                rect.anchorMax = Vector2.one;
                rect.sizeDelta = Vector2.zero;
                rect.anchoredPosition = Vector2.zero;

                blackOverlayImage = imgObj.AddComponent<Image>();
                blackOverlayImage.color = Color.black;
                blackOverlayImage.raycastTarget = false;
            }
        }
    }

    /// <summary>
    /// Rõ dần màn hình đen (Alpha: hiện tại -> 1)
    /// </summary>
    public static void FadeToBlack(float duration = 0.6f, Action onComplete = null)
    {
        var inst = EnsureInstance();
        float currentAlpha = inst.canvasGroup != null ? inst.canvasGroup.alpha : 0f;
        inst.StartFade(currentAlpha, 1f, duration, onComplete);
    }

    /// <summary>
    /// Mờ dần màn hình đen (Alpha: hiện tại -> 0)
    /// </summary>
    public static void FadeFromBlack(float duration = 0.6f, Action onComplete = null)
    {
        var inst = EnsureInstance();
        float currentAlpha = inst.canvasGroup != null ? inst.canvasGroup.alpha : 1f;
        inst.StartFade(currentAlpha, 0f, duration, onComplete);
    }

    /// <summary>
    /// Chuyển cảnh với hiệu ứng rõ dần màn đen rồi nạp Scene mới
    /// </summary>
    public static void LoadSceneWithFade(string sceneName, float fadeOutDuration = 0.5f)
    {
        EnsureInstance().StartCoroutine(EnsureInstance().LoadSceneRoutine(sceneName, fadeOutDuration));
    }

    /// <summary>
    /// Chuyển cảnh theo Scene Index
    /// </summary>
    public static void LoadSceneWithFade(int sceneIndex, float fadeOutDuration = 0.5f)
    {
        EnsureInstance().StartCoroutine(EnsureInstance().LoadSceneIndexRoutine(sceneIndex, fadeOutDuration));
    }

    public void StartFade(float fromAlpha, float toAlpha, float duration, Action onComplete = null)
    {
        EnsureUIHierarchy();
        if (_fadeCoroutine != null) StopCoroutine(_fadeCoroutine);
        _fadeCoroutine = StartCoroutine(FadeRoutine(fromAlpha, toAlpha, duration, onComplete));
    }

    private IEnumerator FadeRoutine(float fromAlpha, float toAlpha, float duration, Action onComplete)
    {
        if (canvasGroup == null) yield break;

        duration = Mathf.Max(0.01f, duration);
        canvasGroup.blocksRaycasts = toAlpha > 0.5f;

        float elapsed = 0f;
        canvasGroup.alpha = fromAlpha;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            canvasGroup.alpha = Mathf.Lerp(fromAlpha, toAlpha, t);
            yield return null;
        }

        canvasGroup.alpha = toAlpha;
        canvasGroup.blocksRaycasts = toAlpha > 0.5f;

        _fadeCoroutine = null;
        onComplete?.Invoke();
    }

    private IEnumerator LoadSceneRoutine(string sceneName, float fadeDuration)
    {
        yield return FadeRoutine(canvasGroup.alpha, 1f, fadeDuration, null);
        yield return new WaitForSecondsRealtime(0.1f);
        SceneManager.LoadScene(sceneName);
    }

    private IEnumerator LoadSceneIndexRoutine(int sceneIndex, float fadeDuration)
    {
        yield return FadeRoutine(canvasGroup.alpha, 1f, fadeDuration, null);
        yield return new WaitForSecondsRealtime(0.1f);
        SceneManager.LoadScene(sceneIndex);
    }
}
