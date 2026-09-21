using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Playables;
using UnityEngine.SceneManagement;
using UnityEngine.Video;

public class CutsceneManager : MonoBehaviour, IPointerClickHandler
{
    [Header("🎬 Target Scene Settings")]
    [Tooltip("Tên Scene tiếp theo sẽ load khi hết Cutscene hoặc bấm Skip (Mặc định: Chapter1)")]
    public string targetSceneName = "Chapter1";

    [Header("⏳ Auto Load Settings")]
    [Tooltip("Tự động tìm và lắng nghe khi Timeline (PlayableDirector) kết thúc")]
    public bool autoDetectTimeline = true;
    [Tooltip("Gán trực tiếp PlayableDirector của Cutscene (nếu để trống script sẽ tự tìm trong Scene)")]
    public PlayableDirector playableDirector;

    [Tooltip("Gán VideoPlayer nếu Cutscene chạy bằng video mp4 (nếu có)")]
    public VideoPlayer videoPlayer;

    [Tooltip("Tự động chuyển Scene sau số giây này (đặt <= 0 nếu không muốn dùng timer)")]
    public float autoLoadAfterSeconds = 0f;

    [Header("✨ Pulsing Alpha Effect (CanvasGroup / Text Skip)")]
    public CanvasGroup canvasGroup;
    public float pulseSpeed = 3.5f;
    [Range(0f, 1f)] public float minAlpha = 0.1f;
    [Range(0f, 1f)] public float maxAlpha = 1.0f;

    [Header("⌨️ Skip Input Settings")]
    public bool allowKeyPressToSkip = true;

    private bool isTransitioning = false;
    private List<PlayableDirector> activeDirectors = new List<PlayableDirector>();

    void Awake()
    {
        if (canvasGroup == null)
        {
            canvasGroup = GetComponent<CanvasGroup>();
        }

        SetupAutoLoadListeners();
    }

    void Start()
    {
        if (autoLoadAfterSeconds > 0f)
        {
            StartCoroutine(AutoLoadTimerRoutine(autoLoadAfterSeconds));
        }
    }

    void Update()
    {
        // 1. Hiệu ứng nhấp nháy / breathing cho chữ "Tap to skip"
        if (canvasGroup != null)
        {
            float t = (Mathf.Sin(Time.time * pulseSpeed) + 1f) / 2f;
            canvasGroup.alpha = Mathf.Lerp(minAlpha, maxAlpha, t);
        }

        // 2. Nhận phím bấm để Skip
        if (allowKeyPressToSkip && !isTransitioning)
        {
            if (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter) || Input.GetKeyDown(KeyCode.Escape))
            {
                TriggerSkip();
            }
        }
    }

    private void SetupAutoLoadListeners()
    {
        // 1. Lắng nghe Timeline (PlayableDirector)
        if (playableDirector != null)
        {
            activeDirectors.Add(playableDirector);
            playableDirector.stopped += OnDirectorStopped;
        }
        else if (autoDetectTimeline)
        {
            PlayableDirector[] foundDirectors = FindObjectsByType<PlayableDirector>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            foreach (var director in foundDirectors)
            {
                if (director != null)
                {
                    activeDirectors.Add(director);
                    director.stopped += OnDirectorStopped;
                }
            }
        }

        // 2. Lắng nghe VideoPlayer (nếu có)
        if (videoPlayer != null)
        {
            videoPlayer.loopPointReached += OnVideoFinished;
        }
    }

    private void OnDirectorStopped(PlayableDirector director)
    {
        Debug.Log($"🎬 Timeline '{director.name}' đã chạy xong -> Tự động chuyển Scene!");
        LoadTargetScene();
    }

    private void OnVideoFinished(VideoPlayer vp)
    {
        Debug.Log($"🎬 Video Cutscene đã phát xong -> Tự động chuyển Scene!");
        LoadTargetScene();
    }

    private IEnumerator AutoLoadTimerRoutine(float delay)
    {
        yield return new WaitForSeconds(delay);
        if (!isTransitioning)
        {
            Debug.Log($"⏳ Hết thời gian chờ {delay}s -> Tự động chuyển Scene!");
            LoadTargetScene();
        }
    }

    /// <summary>
    /// Hàm public có thể gọi từ Timeline Signal, Animation Event hoặc script khác khi kết thúc Cutscene.
    /// </summary>
    public void OnCutsceneFinished()
    {
        LoadTargetScene();
    }

    /// <summary>
    /// Xử lý click/tap lên màn hình để Skip Cutscene.
    /// </summary>
    public void OnPointerClick(PointerEventData eventData)
    {
        if (!isTransitioning)
        {
            TriggerSkip();
        }
    }

    /// <summary>
    /// Kích hoạt Skip sang Target Scene (ưu tiên Scene của Chapter đang chọn trong GameSession).
    /// </summary>
    public void TriggerSkip()
    {
        string destination = targetSceneName;
        if (GameSession.SelectedChapter != null && !string.IsNullOrEmpty(GameSession.SelectedChapter.sceneName))
        {
            destination = GameSession.SelectedChapter.sceneName;
        }
        SkipToScene(destination);
    }

    /// <summary>
    /// Chuyển tới một Scene cụ thể.
    /// </summary>
    public void SkipToScene(string sceneName)
    {
        if (isTransitioning) return;

        string nextScene = string.IsNullOrEmpty(sceneName) ? targetSceneName : sceneName;
        if (string.IsNullOrEmpty(nextScene)) nextScene = "HomeMenu";

        Debug.Log($"⏩ Skipping Cutscene to Scene: {nextScene}");
        LoadSceneInternal(nextScene);
    }

    /// <summary>
    /// Load Scene mục tiêu đã cài đặt (ưu tiên Scene của Chapter trong GameSession).
    /// </summary>
    public void LoadTargetScene()
    {
        if (isTransitioning) return;

        string destination = targetSceneName;
        if (GameSession.SelectedChapter != null && !string.IsNullOrEmpty(GameSession.SelectedChapter.sceneName))
        {
            destination = GameSession.SelectedChapter.sceneName;
        }

        if (string.IsNullOrEmpty(destination)) destination = "HomeMenu";
        LoadSceneInternal(destination);
    }

    private void LoadSceneInternal(string sceneName)
    {
        if (isTransitioning) return;
        isTransitioning = true;
        SceneManager.LoadScene(sceneName);
    }

    void OnDestroy()
    {
        // Dọn dẹp event tránh memory leak
        foreach (var director in activeDirectors)
        {
            if (director != null)
            {
                director.stopped -= OnDirectorStopped;
            }
        }
        activeDirectors.Clear();

        if (videoPlayer != null)
        {
            videoPlayer.loopPointReached -= OnVideoFinished;
        }
    }
}
