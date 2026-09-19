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
    public RectTransform trackRect;         // Track container
    public RectTransform targetZoneRect;    // Target / Success Zone
    public RectTransform indicatorRect;     // Moving Bar
    public TextMeshProUGUI stageText;       // Display "Stage: 1/3"
    public TextMeshProUGUI statusText;      // Status text (Press SPACE / Click!)
    public CanvasGroup targetZoneCanvasGroup; // Flash visual effect on hit/miss
    public Button closeButton;              // Close button to cancel lockpicking

    [Header("🎮 Audio (Optional)")]
    public AudioSource audioSource;
    public AudioClip hitSound;
    public AudioClip missSound;
    public AudioClip victorySound;

    [Header("📈 Difficulty Settings")]
    public int totalStages = 3;
    public float baseSpeed = 400f;                  // Base speed (pixel/sec)
    public float speedMultiplierPerStage = 1.5f;    // Speed multiplier per stage
    public float baseTargetWidth = 140f;            // Initial target width
    public float targetWidthReductionPerStage = 30f;// Target width reduction per stage

    // Dynamic State
    public bool isPlaying { get; private set; } = false;
    private int currentStage = 1;
    private float currentSpeed;
    private float movingDirection = 1f; // 1: Right, -1: Left
    private float trackWidth;
    private float indicatorWidth;
    private float lastIndicatorX; // Stores previous frame's indicator position for low-FPS swept hit detection

    private Action onSuccessCallback;
    private Action onFailedCallback;

    void Start()
    {
        if (panelRoot != null) panelRoot.SetActive(false);
        if (targetZoneCanvasGroup != null) targetZoneCanvasGroup.alpha = 0.7f;
        if (closeButton != null)
        {
            closeButton.onClick.AddListener(OnCloseButtonClicked);
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

    public void StartMinigame(Action onSuccess, Action onFailed = null)
    {
        onSuccessCallback = onSuccess;
        onFailedCallback = onFailed;

        currentStage = 1;
        isPlaying = true;

        if (panelRoot != null) panelRoot.SetActive(true);
        if (statusText != null) statusText.text = "Press SPACE / Click / E";

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

        // Calculate speed & target width per stage
        currentSpeed = baseSpeed * Mathf.Pow(speedMultiplierPerStage, stage - 1);
        float targetWidth = Mathf.Max(40f, baseTargetWidth - (targetWidthReductionPerStage * (stage - 1)));

        // Update target zone size
        targetZoneRect.sizeDelta = new Vector2(targetWidth, targetZoneRect.sizeDelta.y);

        // Randomize target zone position on track
        float maxOffset = (trackWidth / 2f) - (targetWidth / 2f) - 20f;
        float randomX = UnityEngine.Random.Range(-maxOffset, maxOffset);
        targetZoneRect.anchoredPosition = new Vector2(randomX, targetZoneRect.anchoredPosition.y);

        // Reset indicator position to left side of track
        float startX = -(trackWidth / 2f) + (indicatorWidth / 2f);
        indicatorRect.anchoredPosition = new Vector2(startX, indicatorRect.anchoredPosition.y);
        lastIndicatorX = startX;
        movingDirection = 1f;

        if (targetZoneCanvasGroup != null) targetZoneCanvasGroup.alpha = 0.7f;
    }

    void Update()
    {
        if (!isPlaying) return;

        // 1. Move indicator bar back and forth
        MoveIndicator();

        // 2. Receive player input (Space, Left Click, or E)
        if (Input.GetKeyDown(KeyCode.Space) || Input.GetMouseButtonDown(0) || Input.GetKeyDown(KeyCode.E))
        {
            // Ignore click if clicking on UI elements like Close Button
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
        float indicatorHalfWidth = indicatorRect.rect.width / 2f;
        float targetHalfWidth = targetZoneRect.rect.width / 2f;

        // Swept bounds over frame step to ensure accurate detection on low FPS
        float currentX = indicatorRect.anchoredPosition.x;
        float indicatorMin = Mathf.Min(lastIndicatorX, currentX) - indicatorHalfWidth;
        float indicatorMax = Mathf.Max(lastIndicatorX, currentX) + indicatorHalfWidth;

        float targetMin = targetZoneRect.anchoredPosition.x - targetHalfWidth;
        float targetMax = targetZoneRect.anchoredPosition.x + targetHalfWidth;

        // Hit condition: overlap between swept indicator bounds and target zone bounds
        bool isHit = (indicatorMin <= targetMax) && (indicatorMax >= targetMin);

        if (isHit)
        {
            // Hit!
            if (audioSource != null && hitSound != null) audioSource.PlayOneShot(hitSound);
            StartCoroutine(FlashTarget(1f));

            currentStage++;
            if (currentStage > totalStages)
            {
                // Victory (All stages cleared)
                if (audioSource != null && victorySound != null) audioSource.PlayOneShot(victorySound);
                if (statusText != null) statusText.text = "<color=green>UNLOCK SUCCESSFUL!</color>";
                
                StartCoroutine(CompleteMinigameCoroutine(true));
            }
            else
            {
                // Advance to next stage
                if (statusText != null) statusText.text = $"<color=yellow>SUCCESS! Speed increased ({currentStage}/{totalStages})</color>";
                SetupStage(currentStage);
                UpdateUI();
            }
        }
        else
        {
            // Missed! Reset to stage 1
            if (audioSource != null && missSound != null) audioSource.PlayOneShot(missSound);
            StartCoroutine(FlashTarget(0.2f));

            if (statusText != null) statusText.text = "<color=red>MISSED! Try again!</color>";
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
            stageText.text = $"Stage: {currentStage}/{totalStages}";
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
        float delay = 1.0f;
        if (isWin && victorySound != null)
        {
            delay = Mathf.Max(1.0f, victorySound.length);
        }
        yield return new WaitForSeconds(delay);
        CloseMinigame();

        if (isWin && onSuccessCallback != null)
        {
            onSuccessCallback.Invoke();
        }
    }
}
