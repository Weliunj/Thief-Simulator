using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class LockpickMinigame : MonoBehaviour
{
    [Header("⚙️ UI Components")]
    public GameObject panelRoot;
    public RectTransform trackRect;         // Khung thanh chạy
    public RectTransform targetZoneRect;    // Vùng Target cần bấm trúng
    public Image targetImage;               // Image của Target để thay đổi hình theo stage
    public RectTransform indicatorRect;     // Vạch chạy qua lại
    public TextMeshProUGUI statusText;      // Text hiển thị câu nói nhảm khi thành công
    public CanvasGroup targetZoneCanvasGroup; // CanvasGroup của Target Zone
    public Button closeButton;              // Nút đóng minigame
    [Tooltip("Nút bấm bẻ khóa riêng trên màn hình Mobile (nằm trong Panel Minigame)")]
    public Button lockpickButton;           // Nút bấm bẻ khóa trên Mobile

    [Header("🖼️ Target Stage Sprites (3 Ảnh Target đổi theo tiến trình)")]
    [Tooltip("Ảnh 1 (Mặc định Stage 1), Ảnh 2 (Khi đạt 1/3 - Stage 2), Ảnh 3 (Khi đạt 2/3 - Stage 3)")]
    public Sprite[] stageSprites = new Sprite[3];

    [Header("📍 Stage Progress Icons (3 Ảnh xếp cạnh nhau báo tiến độ)")]
    [Tooltip("3 GameObject ảnh xếp cạnh nhau: Trúng 1 hiện ảnh 1, trúng 2 hiện thêm ảnh 2, trúng 3 hiện nốt ảnh 3")]
    public GameObject[] progressIcons = new GameObject[3];

    [Header("💬 Random Success Dialogues")]
    [Tooltip("Random funny / encouraging lockpicking dialogues on hit")]
    public string[] successMessages = new string[]
    {
        "Almost there! Just one more pin!",
        "Master lockpicker in the making!",
        "Click! Music to my ears!",
        "Almost unlocked, keep it steady!",
        "Smooth like butter!",
        "Pro thief vibes right here!",
        "Spot on! Perfect timing!",
        "Good loot awaits inside!",
        "Clean pick! Stay focused!"
    };

    [Header("📈 Difficulty & Transition Settings")]
    public int totalStages = 3;
    public float baseSpeed = 400f;                  // Tốc độ ban đầu
    public float speedMultiplierPerStage = 1.5f;    // Tăng tốc mỗi stage
    public float targetScaleReductionPerStage = 0.15f; // Tỉ lệ thu nhỏ target mỗi stage
    [Tooltip("Thời gian chờ (giây) trước khi bắt đầu di chuyển sang stage tiếp theo")]
    public float stageTransitionDelay = 1.0f;       // Delay 1s trước khi sang stage tiếp theo
    [Tooltip("Tự động đóng Minigame khi bấm trượt/sai để người chơi chạy trốn NPC")]
    public bool closeOnFail = true;
    public float failCloseDelay = 0.4f;

    // Dynamic State
    public bool isPlaying { get; private set; } = false;
    private bool isWaitingNextStage = false;
    private int currentStage = 1;
    private float currentSpeed;
    private float movingDirection = 1f; // 1: Phải, -1: Trái
    private float trackWidth;
    private float indicatorWidth;
    private float lastIndicatorX;
    private DoorController currentDoor;

    private Action onSuccessCallback;
    private Action onFailedCallback;

    void Awake()
    {
        // Không làm mờ Target Image: Giữ alpha luôn là 1.0 (rõ nét 100%)
        if (targetZoneCanvasGroup != null) targetZoneCanvasGroup.alpha = 1.0f;
        
        if (closeButton != null)
        {
            closeButton.onClick.RemoveListener(OnCloseButtonClicked);
            closeButton.onClick.AddListener(OnCloseButtonClicked);
        }

        if (lockpickButton != null)
        {
            lockpickButton.onClick.RemoveListener(OnLockpickButtonPressed);
            lockpickButton.onClick.AddListener(OnLockpickButtonPressed);
        }

        if (targetImage == null && targetZoneRect != null)
        {
            targetImage = targetZoneRect.GetComponent<Image>();
            if (targetImage == null)
            {
                targetImage = targetZoneRect.GetComponentInChildren<Image>();
            }
        }

        // Tự động bật preserveAspect để giữ nguyên tỉ lệ ảnh Target, không bị dãn 2 bên
        if (targetImage != null)
        {
            targetImage.preserveAspect = true;
        }
    }

    public void OnCloseButtonClicked()
    {
        UI_Manager uiManager = FindFirstObjectByType<UI_Manager>();
        if (uiManager != null)
        {
            uiManager.CancelLockpicking();
        }
        else
        {
            CloseMinigame();
        }
    }

    public void OnLockpickButtonPressed()
    {
        if (isPlaying && !isWaitingNextStage)
        {
            AttemptUnlock();
        }
    }

    public void StartMinigame(Action onSuccess, Action onFailed = null, DoorController door = null)
    {
        currentDoor = door;
        onSuccessCallback = onSuccess;
        onFailedCallback = onFailed;

        currentStage = 1;
        isPlaying = true;
        isWaitingNextStage = false;

        if (panelRoot != null) panelRoot.SetActive(true);
        if (statusText != null) statusText.text = "Hit the target zone to pick the lock!";

        SetupStage(currentStage);
        UpdateStageVisual();
        UpdateProgressIcons(0); // Lúc đầu: cả 3 ảnh tiến độ đều ẩn
    }

    public void CloseMinigame()
    {
        isPlaying = false;
        isWaitingNextStage = false;
        StopAllCoroutines();
        if (panelRoot != null) panelRoot.SetActive(false);
    }

    void SetupStage(int stage)
    {
        if (trackRect == null || indicatorRect == null || targetZoneRect == null) return;

        if (trackRect.rect.width <= 0)
        {
            Canvas.ForceUpdateCanvases();
        }

        trackWidth = trackRect.rect.width;
        indicatorWidth = indicatorRect.rect.width;

        // Tính toán tốc độ tăng dần theo stage (Kích thước Target giữ nguyên 100%)
        currentSpeed = baseSpeed * Mathf.Pow(speedMultiplierPerStage, stage - 1);
        targetZoneRect.localScale = Vector3.one;

        float currentTargetWidth = targetZoneRect.rect.width;

        // Random vị trí target trên thanh trượt
        float maxOffset = (trackWidth / 2f) - (currentTargetWidth / 2f) - 20f;
        float randomX = UnityEngine.Random.Range(-maxOffset, maxOffset);
        targetZoneRect.anchoredPosition = new Vector2(randomX, targetZoneRect.anchoredPosition.y);

        // Đặt lại vị trí ban đầu của thanh chạy
        float startX = -(trackWidth / 2f) + (indicatorWidth / 2f);
        indicatorRect.anchoredPosition = new Vector2(startX, indicatorRect.anchoredPosition.y);
        lastIndicatorX = startX;
        movingDirection = 1f;

        // Giữ ảnh target luôn rõ nét 100%
        if (targetZoneCanvasGroup != null) targetZoneCanvasGroup.alpha = 1.0f;
    }

    void Update()
    {
        if (!isPlaying || isWaitingNextStage) return;

        // 1. Di chuyển thanh chạy qua lại
        MoveIndicator();

        // 2. Nhận tương tác người chơi (Phím Space, Click chuột, Phím E)
        if (Input.GetKeyDown(KeyCode.Space) || Input.GetMouseButtonDown(0) || Input.GetKeyDown(KeyCode.E))
        {
            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
            {
                return;
            }

            AttemptUnlock();
        }
    }

    private void MoveIndicator()
    {
        float minX = -(trackWidth / 2f) + (indicatorWidth / 2f);
        float maxX = (trackWidth / 2f) - (indicatorWidth / 2f);

        lastIndicatorX = indicatorRect.anchoredPosition.x;

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
        float indicatorHalfWidth = (indicatorRect.rect.width * indicatorRect.localScale.x) / 2f;
        float targetHalfWidth = (targetZoneRect.rect.width * targetZoneRect.localScale.x) / 2f;

        float currentX = indicatorRect.anchoredPosition.x;
        float indicatorMin = Mathf.Min(lastIndicatorX, currentX) - indicatorHalfWidth;
        float indicatorMax = Mathf.Max(lastIndicatorX, currentX) + indicatorHalfWidth;

        float targetMin = targetZoneRect.anchoredPosition.x - targetHalfWidth;
        float targetMax = targetZoneRect.anchoredPosition.x + targetHalfWidth;

        bool isHit = (indicatorMin <= targetMax) && (indicatorMax >= targetMin);

        if (isHit)
        {
            // Bấm trúng nấc! Phát âm thanh 3D tại cửa
            if (currentDoor != null) currentDoor.PlayHitSound();

            StartCoroutine(FlashTarget(1f));

            currentStage++;
            if (currentStage > totalStages)
            {
                // Trúng lần 3 (Hoàn thành) -> Hiện đủ cả 3 ảnh và đóng panel
                isWaitingNextStage = true;
                UpdateProgressIcons(totalStages);

                if (currentDoor != null) currentDoor.PlayVictorySound();

                if (statusText != null) statusText.text = "<color=green>LOCK PICKED SUCCESSFULLY!</color>";
                
                StartCoroutine(CompleteMinigameCoroutine(true));
            }
            else
            {
                // Trúng lần 1 (hiện 1 ảnh) hoặc trúng lần 2 (hiện 2 ảnh) -> Delay 1s rồi sang stage tiếp theo
                UpdateProgressIcons(currentStage - 1);
                StartCoroutine(AdvanceStageRoutine());
            }
        }
        else
        {
            // Bấm trượt/sai -> Kích hoạt chuỗi thất bại (Gọi NPC + Đóng minigame)
            StartCoroutine(FailMinigameRoutine());
        }
    }

    private IEnumerator AdvanceStageRoutine()
    {
        // 1. Tạm dừng di chuyển thanh chạy
        isWaitingNextStage = true;

        // 2. Cập nhật ảnh target tương ứng với stage mới
        UpdateStageVisual();

        // 3. Hiển thị câu nói nhảm ngẫu nhiên khi bấm trúng
        ShowRandomSuccessMessage();

        // 4. Chờ đúng 1 giây trước khi chuyển stage tiếp theo
        yield return new WaitForSeconds(stageTransitionDelay);

        // 5. Setup lại vị trí Target mới và tiếp tục chạy
        SetupStage(currentStage);
        isWaitingNextStage = false;
    }

    private IEnumerator FailMinigameRoutine()
    {
        isWaitingNextStage = true;

        // Phát âm thanh gãy công cụ tại cửa
        if (currentDoor != null) currentDoor.PlayMissSound();

        StartCoroutine(FlashTarget(0.4f));

        if (statusText != null) statusText.text = "<color=red>Missed! Lockpicking failed!</color>";
        currentStage = 1;
        UpdateStageVisual();
        UpdateProgressIcons(0);

        // 1. Kích hoạt báo động gọi NPC trong Zone truy đuổi người chơi
        if (onFailedCallback != null)
        {
            onFailedCallback.Invoke();
        }

        // 2. Đóng Minigame và mở lại quyền điều khiển nhân vật
        if (closeOnFail)
        {
            yield return new WaitForSeconds(failCloseDelay);
            CloseMinigame();

            UI_Manager uiManager = FindFirstObjectByType<UI_Manager>();
            if (uiManager != null)
            {
                uiManager.CancelLockpicking();
            }
        }
        else
        {
            SetupStage(currentStage);
            isWaitingNextStage = false;
        }
    }

    private void UpdateStageVisual()
    {
        if (targetImage != null && stageSprites != null && stageSprites.Length > 0)
        {
            int spriteIndex = Mathf.Clamp(currentStage - 1, 0, stageSprites.Length - 1);
            if (stageSprites[spriteIndex] != null)
            {
                targetImage.sprite = stageSprites[spriteIndex];
                targetImage.enabled = true;
                targetImage.preserveAspect = true; // Bật preserveAspect để giữ nguyên tỉ lệ ảnh
            }
        }
    }

    /// <summary>
    /// Bật/tắt 3 ảnh tiến độ theo số lần trúng:
    /// 0: Tắt hết
    /// 1: Hiện ảnh 1
    /// 2: Hiện ảnh 1 + ảnh 2
    /// 3: Hiện cả 3 ảnh
    /// </summary>
    private void UpdateProgressIcons(int count)
    {
        if (progressIcons == null) return;

        for (int i = 0; i < progressIcons.Length; i++)
        {
            if (progressIcons[i] != null)
            {
                progressIcons[i].SetActive(i < count);
            }
        }
    }

    private void ShowRandomSuccessMessage()
    {
        if (statusText != null && successMessages != null && successMessages.Length > 0)
        {
            int randomIndex = UnityEngine.Random.Range(0, successMessages.Length);
            statusText.text = $"<color=yellow>{successMessages[randomIndex]}</color>";
        }
    }

    private IEnumerator FlashTarget(float flashAlpha)
    {
        if (targetZoneCanvasGroup == null) yield break;
        // Giữ nét 100%, chỉ phản hồi hiệu ứng chớp nhẹ nếu bấm trúng/trượt
        targetZoneCanvasGroup.alpha = flashAlpha;
        yield return new WaitForSeconds(0.12f);
        if (targetZoneCanvasGroup != null) targetZoneCanvasGroup.alpha = 1.0f;
    }

    private IEnumerator CompleteMinigameCoroutine(bool isWin)
    {
        float delay = 1.0f;
        if (isWin && currentDoor != null && currentDoor.victorySound != null)
        {
            delay = Mathf.Max(1.0f, currentDoor.victorySound.length);
        }
        yield return new WaitForSeconds(delay);
        CloseMinigame();

        if (isWin && onSuccessCallback != null)
        {
            onSuccessCallback.Invoke();
        }
    }
}
