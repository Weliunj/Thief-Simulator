using UnityEngine;
using TMPro;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using System.Linq; 

public class UI_Manager : MonoBehaviour
{
    // =========================================================================
    [Header("⚙️ References")]
    public PlayerManager playerManager;
    public GameObject GuidePanel;
    public bool isSolving = false;
    [HideInInspector]public bool toggleGuide = false;
    
    public GameObject diedPanel;
    public GameObject WinPanel;

    [Header("Math UI")]
    [HideInInspector] public CanvasGroup solveCanvasGroup;
    public GameObject SolvePanel;
    public TextMeshProUGUI content;
    public TextMeshProUGUI[] c; // c1 2 3 4
    
    [Header("Progress Slider")]
    public Slider progressSlider; // Slider hiển thị tiến độ
    public TextMeshProUGUI progressText; // Text hiển thị "1/3", "2/3", "3/3"

    [HideInInspector] public DoorMath activeDoor; // Cánh cửa đang được giải

    // BIẾN NỘI BỘ
    [HideInInspector] public QuestionData currentQuestion;
    private List<string> solvedQuestionContents = new List<string>(); 

    [Header("🔋 Stamina UI")]
    public TextMeshProUGUI currStamina; 
    public TextMeshProUGUI Stamina; 

    [Header("🏋️ Weight UI")]
    public TextMeshProUGUI currkg;       
    public TextMeshProUGUI kg;          

    [Header("⚠️ Warning Colors")]
    [Tooltip("Normal base color (used when value < threshold)")]
    public Color normalColor = Color.white;
    [Tooltip("Alert color (used at 100%)")]
    public Color alertColor = Color.red;
    [Range(0f, 1f)]
    [Tooltip("Normalized threshold where coloring starts (0.5 = 50%)")]
    public float warnThreshold = 0.5f;

    [Header("🌟 Point UI")]
    public TextMeshProUGUI point;       
    public TextMeshProUGUI targetPoint;
    
    [Header("⏰ Time UI")]
    public TextMeshProUGUI timeText; // Text hiển thị thời gian còn lại
    public AudioSource alarm;
    // =========================================================================

    void Start()
    {
        // ⭐ RESET DANH SÁCH CÂU HỎI ĐÃ GIẢI KHI BẮT ĐẦU SCENE MỚI (REPLAY)
        solvedQuestionContents.Clear();
        Debug.Log("Đã reset danh sách câu hỏi đã giải. Tất cả câu hỏi sẽ hiện lại khi replay.");
        
        solveCanvasGroup = SolvePanel.GetComponent<CanvasGroup>();
        if (solveCanvasGroup == null)
        {
            Debug.LogWarning("SolvePanel thiếu component Canvas Group. Vui lòng thêm vào để kiểm soát tương tác.");
        }
        diedPanel.SetActive(false);
        WinPanel.SetActive(false);
        SolvePanel.SetActive(false);

        if (playerManager == null)
        {
            Debug.LogError("PlayerManager ScriptableObject chưa được gán.");
            enabled = false;
            return;
        }
        
        // Thiết lập giá trị Max/Target Point cố định
        if (Stamina != null) { Stamina.text = $"{playerManager.MaxStamina:F0}"; }
        if (kg != null) { kg.text = $"{playerManager.Maxweight}"; }
        if (targetPoint != null) { targetPoint.text = $"{playerManager.totalpoint}"; }
        
        // Thiết lập thời gian ban đầu
        if (playerManager != null)
        {
            playerManager.currentTime = playerManager.MaxTime;
        }
        
        // Thiết lập Slider tiến độ
        if (progressSlider != null)
        {
            progressSlider.minValue = 0f;
            progressSlider.maxValue = 3f;
            progressSlider.value = 0f;
        }
        UpdateProgressUI(0);

        // Gán Listener cho các nút đáp án (Sử dụng cách tìm kiếm chính xác)
        for (int i = 0; i < c.Length; i++)
        {
            Transform parentTransform = c[i].transform.parent;
            if (parentTransform != null)
            {
                Button btn = parentTransform.GetComponent<Button>();
                if (btn != null)
                {
                    int index = i;
                    btn.onClick.RemoveAllListeners(); 
                    btn.onClick.AddListener(() => ChooseAnswer(c[index].text));
                }
            }
        }
    }

    void Update()
    {
        if (playerManager == null) return;
        if( playerManager.currentTime <= 0f)
        {
            if (!alarm.isPlaying)
            {
                alarm.loop = true;
                alarm.Play();
            }
        }
        
        // --- CẬP NHẬT THỜI GIAN ---
        // ⭐ CHỈ ĐẾM THỜI GIAN KHI CHƯA CHẾT VÀ CHƯA WIN
        if (!playerManager.isDied && playerManager.currpoint < playerManager.totalpoint)
        {
            // Giảm thời gian mỗi frame
            playerManager.currentTime -= Time.deltaTime;
            
            // Kiểm tra hết thời gian
            if (playerManager.currentTime <= 0.3f)
            {
                playerManager.currentTime = 0f;
                Debug.Log("HẾT THỜI GIAN! Game Over (tạm thời chỉ debug)");
                // TODO: Thêm logic game over khi hết thời gian
            }
        }
        // ⭐ KHI WIN: DỪNG THỜI GIAN (không cần làm gì, thời gian sẽ tự động dừng vì điều kiện trên)
        
        // --- CẬP NHẬT UI ĐỘNG ---
        if (currStamina != null) 
        { 
            currStamina.text = $"{playerManager._stamina:F1}"; 
            // set color: low stamina -> alertColor. When normalized stamina <= warnThreshold start lerping to alert at 0.
            if (playerManager.MaxStamina > 0f)
            {
                float sNorm = Mathf.Clamp01(playerManager._stamina / playerManager.MaxStamina);
                Color sColor = normalColor;
                if (sNorm <= warnThreshold)
                {
                    // t = 0 at warnThreshold, t = 1 at 0 (fully depleted)
                    float t = Mathf.InverseLerp(warnThreshold, 0f, sNorm);
                    sColor = Color.Lerp(normalColor, alertColor, t);
                }
                currStamina.color = sColor;
            }
        }

        if (currkg != null) 
        { 
            currkg.text = $"{playerManager.currweight}"; 
            if (playerManager.Maxweight > 0)
            {
                float wNorm = Mathf.Clamp01((float)playerManager.currweight / (float)playerManager.Maxweight);
                Color wColor = normalColor;
                if (wNorm >= warnThreshold)
                {
                    float t = Mathf.InverseLerp(warnThreshold, 1f, wNorm);
                    wColor = Color.Lerp(normalColor, alertColor, t);
                }
                currkg.color = wColor;
            }
        }

        if (point != null) { point.text = $"{playerManager.currpoint}"; }
        
        // Cập nhật UI thời gian
        if (timeText != null)
        {
            int minutes = Mathf.FloorToInt(playerManager.currentTime / 60f);
            int seconds = Mathf.FloorToInt(playerManager.currentTime % 60f);
            timeText.text = $"{minutes:00}:{seconds:00}";
        }
        
        // --- XỬ LÝ TRẠNG THÁI GAME ---
        if (playerManager.isDied)
        {
            diedPanel.SetActive(true); WinPanel.SetActive(false);
            Cursor.lockState = CursorLockMode.None; Cursor.visible = true;
        } 
        else if(playerManager.currpoint >= playerManager.totalpoint) // ⭐ SỬA: >= thay vì == để tránh lỗi nếu vượt quá
        {
            diedPanel.SetActive(false); WinPanel.SetActive(true);
            Cursor.lockState = CursorLockMode.None; Cursor.visible = true;
            // ⭐ THỜI GIAN ĐÃ TỰ ĐỘNG DỪNG (vì điều kiện trên không thỏa mãn)
            Debug.Log($"WIN! Đã đạt {playerManager.currpoint}/{playerManager.totalpoint} điểm!");
        }
        
        // --- XỬ LÝ GUIDE & ESCAPE ---
        if (Input.GetKeyDown(KeyCode.H) && !isSolving) { toggleGuide = !toggleGuide; }
        GuidePanel.SetActive(toggleGuide);
        
        if (Input.GetKeyDown(KeyCode.Escape) && isSolving) { Clodetab(); }
    }

    // =========================================================================
    //                            LOGIC CÂU HỎI
    // =========================================================================
    
    public List<QuestionData> GetAvailableQuestions(Questions questionList)
    {
        return questionList.allQuestions
            .Where(q => !solvedQuestionContents.Contains(q.questionContent))
            .ToList();
    }
    
    public QuestionData GetRandomAvailableQuestion(Questions questionList)
    {
        List<QuestionData> available = GetAvailableQuestions(questionList);
        if (available.Count == 0) return null;
        int randomIndex = Random.Range(0, available.Count);
        return available[randomIndex];
    }
    
    public void DisplayQuestion(QuestionData q)
    {
        content.text = q.questionContent;
        List<string> answers = new List<string>();
        answers.Add(q.correctAnswer);
        answers.AddRange(q.incorrectAnswers);
        
        // Xáo trộn đáp án
        int n = answers.Count;
        while (n > 1)
        {
            n--;
            int k = Random.Range(0, n + 1);
            string value = answers[k];
            answers[k] = answers[n];
            answers[n] = value;
        }
        
        for (int i = 0; i < c.Length; i++)
        {
            c[i].text = (i < answers.Count) ? answers[i] : "";
        }
    }
    
    // Trong UI_Manager.cs
// Trong UI_Manager.cs
public void ChooseAnswer(string chosenAnswer)
{
    if (currentQuestion == null) return;
    
    if (chosenAnswer == currentQuestion.correctAnswer)
    {
        Debug.Log("Đáp án Đúng!");
        
        // ⭐ QUAN TRỌNG: Đánh dấu câu hỏi này đã được giải đúng (không lặp lại)
        if (!solvedQuestionContents.Contains(currentQuestion.questionContent))
        {
            solvedQuestionContents.Add(currentQuestion.questionContent);
            Debug.Log($"Đã đánh dấu câu hỏi: {currentQuestion.questionContent}");
        }
        
        // 1. Ghi nhận câu trả lời đúng (activeDoor.AnswerCorrect() sẽ TĂNG ĐẾM và HỦY CỬA nếu đạt 3/3)
        if (activeDoor != null) 
        {
            activeDoor.AnswerCorrect(); 
        }

        // ⭐ KIỂM TRA ĐỦ 3 CÂU ĐÚNG TRƯỚC KHI HIỂN THỊ CÂU HỎI MỚI
        if (activeDoor != null && activeDoor.currentCorrectAnswers >= activeDoor.requiredCorrectAnswers)
        {
            // ⭐ CẬP NHẬT SLIDER ĐẾN 3/3
            UpdateProgressUI(activeDoor.currentCorrectAnswers);
            Debug.Log("Đã đủ 3 câu đúng! Cửa sẽ được mở sau 1 giây.");
            
            // Đợi 1 giây để slider chạy hết, sau đó đóng tab
            StartCoroutine(CloseTabAfterDelay(1f));
            return;
        }
        
        // ⭐ CẬP NHẬT SLIDER TIẾN ĐỘ
        if (activeDoor != null)
        {
            UpdateProgressUI(activeDoor.currentCorrectAnswers);
        }
        
        // ⭐ NẾU CHƯA ĐỦ 3 CÂU → HIỂN THỊ CÂU HỎI MỚI (KHÔNG ĐÓNG TAB)
        if (activeDoor != null)
        {
            Debug.Log($"Trả lời đúng! Còn {activeDoor.requiredCorrectAnswers - activeDoor.currentCorrectAnswers} câu nữa. Tiếp tục với câu hỏi mới.");
            
            // Tải câu hỏi mới từ danh sách chưa giải
            QuestionData nextQuestion = GetRandomAvailableQuestion(activeDoor.questionList);
            
            if (nextQuestion != null)
            {
                currentQuestion = nextQuestion;
                DisplayQuestion(nextQuestion);
            }
            else
            {
                // Trường hợp hiếm: Hết câu hỏi nhưng chưa đủ 3 câu 
                Debug.LogWarning("Không còn câu hỏi nào để giải! Đóng panel.");
                Clodetab();
                activeDoor = null;
            }
        }
    }
    else // Đáp án Sai
    {
        Debug.Log("Đáp án Sai! Reset đếm, gọi AI và đóng Panel.");
        
        // KÍCH HOẠT SỰ KIỆN GỌI AI VÀ RESET ĐẾM
        if (activeDoor != null) { activeDoor.AnswerFailed(); }
        
        // Đóng Panel và đặt lại activeDoor
        Clodetab(); 
        activeDoor = null;
    }
}

    // =========================================================================
    //                            LOGIC UI CƠ BẢN
    // =========================================================================

    public void Replay()
    {
        Time.timeScale = 1f; SceneManager.LoadScene("Lv1");
    }
    
    public void Menu()
    {
        Time.timeScale = 1f; SceneManager.LoadScene("HomeMenu");
    }
    
    public void Clodetab()
    {
        isSolving = false;
        SolvePanel.SetActive(false);
        
        // Vô hiệu hóa tương tác Raycast
        if (solveCanvasGroup != null)
        {
            solveCanvasGroup.interactable = false;
            solveCanvasGroup.blocksRaycasts = false;
        }

        Cursor.lockState = CursorLockMode.Locked; 
        Cursor.visible = false;
        currentQuestion = null;
        
        // ⭐ RESET TIẾN TRÌNH GIẢI KHI ĐÓNG TAB (trừ khi đã đủ 3 câu và cửa đang được destroy)
        if (activeDoor != null)
        {
            activeDoor.currentCorrectAnswers = 0;
            Debug.Log("Đã reset tiến trình giải về 0 khi đóng tab.");
        }
        
        // Reset slider khi đóng tab
        UpdateProgressUI(0);
        
        // Reset activeDoor
        activeDoor = null;
    }
    
    // ⭐ HÀM CẬP NHẬT SLIDER TIẾN ĐỘ
    public void UpdateProgressUI(int correctCount)
    {
        if (progressSlider != null)
        {
            progressSlider.value = correctCount;
        }
        
        if (progressText != null)
        {
            progressText.text = $"{correctCount}/3";
        }
    }
    
    // ⭐ COROUTINE ĐỢI 1 GIÂY TRƯỚC KHI ĐÓNG TAB (KHI ĐỦ 3 CÂU)
    private IEnumerator CloseTabAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        Clodetab();
        activeDoor = null;
    }
}