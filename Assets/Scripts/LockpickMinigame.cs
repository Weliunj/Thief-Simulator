using System;
using System.Collections;
using TMPro;
using UnityEngine;

public class LockpickMinigame : MonoBehaviour
{
    [Header("⚙️ UI Components")]
    public GameObject panelRoot;
    public RectTransform trackRect;         // Khung thanh chạy (Track container)
    public RectTransform targetZoneRect;    // Vùng xanh lá (Target / Success Zone)
    public RectTransform indicatorRect;     // Thanh vạch chạy qua lại (Moving Bar)
    public TextMeshProUGUI stageText;       // Hiển thị "Lần bẻ khóa: 1/3"
    public TextMeshProUGUI statusText;      // Thông báo trạng thái (Bấm SPACE / Click!)
    public CanvasGroup targetZoneCanvasGroup; // Để nhấp nháy visual khi trúng/trượt

    [Header("🎮 Audio (Optional)")]
    public AudioSource audioSource;
    public AudioClip hitSound;
    public AudioClip missSound;
    public AudioClip victorySound;

    [Header("📈 Difficulty Settings")]
    public int totalStages = 3;
    public float baseSpeed = 400f;                  // Tốc độ ban đầu (pixel/sec)
    public float speedMultiplierPerStage = 1.5f;    // Tăng tốc mỗi màn
    public float baseTargetWidth = 140f;            // Độ rộng vùng xanh ban đầu
    public float targetWidthReductionPerStage = 30f;// Thu nhỏ vùng xanh mỗi màn

    // Dynamic State
    public bool isPlaying { get; private set; } = false;
    private int currentStage = 1;
    private float currentSpeed;
    private float movingDirection = 1f; // 1: Right, -1: Left
    private float trackWidth;
    private float indicatorWidth;

    private Action onSuccessCallback;
    private Action onFailedCallback;

    void Start()
    {
        if (panelRoot != null) panelRoot.SetActive(false);
        if (targetZoneCanvasGroup != null) targetZoneCanvasGroup.alpha = 0.7f;
    }

    public void StartMinigame(Action onSuccess, Action onFailed = null)
    {
        onSuccessCallback = onSuccess;
        onFailedCallback = onFailed;

        currentStage = 1;
        isPlaying = true;

        if (panelRoot != null) panelRoot.SetActive(true);

        SetupStage(currentStage);
        UpdateUI();
    }

    public void CloseMinigame()
    {
        isPlaying = false;
        if (panelRoot != null) panelRoot.SetActive(false);
    }

    void SetupStage(int stage)
    {
        if (trackRect == null || indicatorRect == null || targetZoneRect == null) return;

        trackWidth = trackRect.rect.width;
        indicatorWidth = indicatorRect.rect.width;

        // Tính toán tốc độ và độ rộng vùng mục tiêu theo Cấp độ (càng cao càng nhanh, càng hẹp)
        currentSpeed = baseSpeed * Mathf.Pow(speedMultiplierPerStage, stage - 1);
        float targetWidth = Mathf.Max(40f, baseTargetWidth - (targetWidthReductionPerStage * (stage - 1)));

        // Cập nhật size vùng xanh (Target Zone)
        targetZoneRect.sizeDelta = new Vector2(targetWidth, targetZoneRect.sizeDelta.y);

        // Đặt ngẫu nhiên vị trí vùng xanh trên thanh track (tránh rìa quá sát)
        float maxOffset = (trackWidth / 2f) - (targetWidth / 2f) - 20f;
        float randomX = UnityEngine.Random.Range(-maxOffset, maxOffset);
        targetZoneRect.anchoredPosition = new Vector2(randomX, targetZoneRect.anchoredPosition.y);

        // Đặt lại vị trí thanh vạch ở bên trái track
        float startX = -(trackWidth / 2f) + (indicatorWidth / 2f);
        indicatorRect.anchoredPosition = new Vector2(startX, indicatorRect.anchoredPosition.y);
        movingDirection = 1f;

        if (targetZoneCanvasGroup != null) targetZoneCanvasGroup.alpha = 0.7f;
    }

    void Update()
    {
        if (!isPlaying) return;

        // 1. Di chuyển thanh indicator qua lại (Bounce Back & Forth)
        MoveIndicator();

        // 2. Nhận nút ấn từ người chơi (Space, Chuột Trái, hoặc phím E)
        if (Input.GetKeyDown(KeyCode.Space) || Input.GetMouseButtonDown(0) || Input.GetKeyDown(KeyCode.E))
        {
            AttemptUnlock();
        }
    }

    private void MoveIndicator()
    {
        float minX = -(trackWidth / 2f) + (indicatorWidth / 2f);
        float maxX = (trackWidth / 2f) - (indicatorWidth / 2f);

        Vector2 pos = indicatorRect.anchoredPosition;
        pos.x += movingDirection * currentSpeed * Time.deltaTime;

        if (pos.x >= maxX)
        {
            pos.x = maxX;
            movingDirection = -1f;
        }
        else if (pos.x <= minX)
        {
            pos.x = minX;
            movingDirection = 1f;
        }

        indicatorRect.anchoredPosition = pos;
    }

    private void AttemptUnlock()
    {
        // Tính toán phạm vi (Left -> Right) của cả Indicator và Target Zone
        float indicatorHalfWidth = indicatorRect.rect.width / 2f;
        float targetHalfWidth = targetZoneRect.rect.width / 2f;

        float indicatorMin = indicatorRect.anchoredPosition.x - indicatorHalfWidth;
        float indicatorMax = indicatorRect.anchoredPosition.x + indicatorHalfWidth;

        float targetMin = targetZoneRect.anchoredPosition.x - targetHalfWidth;
        float targetMax = targetZoneRect.anchoredPosition.x + targetHalfWidth;

        // Trúng (Hit) khi có bất kỳ phần giao nhau nào giữa Indicator và Target Zone
        bool isHit = (indicatorMin <= targetMax) && (indicatorMax >= targetMin);

        if (isHit)
        {
            // Trúng mục tiêu!
            if (audioSource != null && hitSound != null) audioSource.PlayOneShot(hitSound);
            StartCoroutine(FlashTarget(1f));

            currentStage++;
            if (currentStage > totalStages)
            {
                // Thắng Mini game (Thành công cả 3 lần)
                if (audioSource != null && victorySound != null) audioSource.PlayOneShot(victorySound);
                statusText.text = "<color=green>BẺ KHÓA THÀNH CÔNG!</color>";
                
                StartCoroutine(CompleteMinigameCoroutine(true));
            }
            else
            {
                // Qua màn tiếp theo (Khó hơn & Nhanh hơn)
                statusText.text = $"<color=yellow>CHÍNH XÁC! Tăng tốc độ ({currentStage}/{totalStages})</color>";
                SetupStage(currentStage);
                UpdateUI();
            }
        }
        else
        {
            // Trượt mục tiêu! Reset lại màn 1
            if (audioSource != null && missSound != null) audioSource.PlayOneShot(missSound);
            StartCoroutine(FlashTarget(0.2f));

            statusText.text = "<color=red>TRƯỢT RỒI! Làm lại từ đầu!</color>";
            currentStage = 1;
            SetupStage(currentStage);
            UpdateUI();

            if (onFailedCallback != null) onFailedCallback.Invoke();
        }
    }

    private void UpdateUI()
    {
        if (stageText != null)
        {
            stageText.text = $"Tiến độ: {currentStage}/{totalStages}";
        }
    }

    private IEnumerator FlashTarget(float flashAlpha)
    {
        if (targetZoneCanvasGroup == null) yield break;
        targetZoneCanvasGroup.alpha = flashAlpha;
        yield return new WaitForSeconds(0.15f);
        if (targetZoneCanvasGroup != null) targetZoneCanvasGroup.alpha = 0.7f;
    }

    private IEnumerator CompleteMinigameCoroutine(bool isWin)
    {
        yield return new WaitForSeconds(0.3f);
        CloseMinigame();

        if (isWin && onSuccessCallback != null)
        {
            onSuccessCallback.Invoke();
        }
    }
}
