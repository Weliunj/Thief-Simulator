
using UnityEngine;

using TMPro;

using UnityEngine.SceneManagement;

using UnityEngine.UI;

using System.Collections.Generic;

using System.Linq; // Cần dùng cho LINQ



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

    // Trong UI_Manager.cs, thêm vào phần khai báo biến
[HideInInspector] public DoorMath activeDoor;

    

    // BIẾN MỚI CẦN THIẾT

    [HideInInspector] public QuestionData currentQuestion;

    private List<string> solvedQuestionContents = new List<string>(); // Danh sách nội dung câu hỏi đã giải



    [Header("🔋 Stamina UI")]

    public TextMeshProUGUI currStamina; // Stamina hiện tại (ví dụ: 8.5)

    public TextMeshProUGUI Stamina;     // Max Stamina (ví dụ: 10)

    

    [Header("🏋️ Weight UI")]

    public TextMeshProUGUI currkg;      // Cân nặng hiện tại (ví dụ: 50)

    public TextMeshProUGUI kg;          // Max Cân nặng (ví dụ: 100)

    

    [Header("🌟 Point UI")]

    public TextMeshProUGUI point;       // Điểm hiện tại (ví dụ: 250)

    public TextMeshProUGUI targetPoint; // Mục tiêu điểm (ví dụ: 400)

    // =========================================================================



    // Hàm Start (Khởi tạo, chỉ chạy một lần)

    void Start()

    {

// Lấy Canvas Group của SolvePanel
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

            Debug.LogError("PlayerManager ScriptableObject chưa được gán trong UI_Manager.");

            enabled = false;

            return;

        }

        

        // Đảm bảo TextMeshPro được đặt giá trị Max/Target Point ngay từ đầu

        if (Stamina != null)

        {

             // Dùng PlayerManager.MaxStamina

             Stamina.text = $"{playerManager.MaxStamina:F0}"; // Hiển thị số nguyên

        }

        if (kg != null)

        {

             // Dùng PlayerManager.MaxWeight

             kg.text = $"{playerManager.Maxweight}";

        }

        if (targetPoint != null)

        {

             // Dùng PlayerManager.TotalPointGoal

             targetPoint.text = $"{playerManager.totalpoint}";

        }



        // Thêm Listener cho các nút đáp án

        // Thêm Listener cho các nút đáp án
    for (int i = 0; i < c.Length; i++)
    {
        // ⭐ BƯỚC 1: Lấy Game Object cha trực tiếp của Text (TMP)
        Transform parentTransform = c[i].transform.parent;

        if (parentTransform != null)
        {
            // ⭐ BƯỚC 2: Chỉ tìm component Button TRÊN Game Object cha đó
            Button btn = parentTransform.GetComponent<Button>();
            
            if (btn != null)
            {
                int index = i;
                btn.onClick.RemoveAllListeners(); 
                btn.onClick.AddListener(() => ChooseAnswer(c[index].text));
                Debug.Log($"[UI_Manager] Gán thành công Button đáp án số {i} trên Game Object: {parentTransform.name}");
            }
            else
            {
                Debug.LogError($"[UI_Manager] LỖI: Game Object '{parentTransform.name}' (cha của c[{i}]) KHÔNG có Component Button.");
            }
        }
        else
        {
            Debug.LogError($"[UI_Manager] LỖI CẤU TRÚC: TextMeshProUGUI c[{i}] không có Game Object cha.");
        }
    }

    }



    // Hàm Update (Chạy mỗi Frame để cập nhật giá trị động)

    void Update()

    {

        if (playerManager == null) return;

        

        // --- CẬP NHẬT TRẠNG THÁI THẮNG/THUA (ĐIỀU KHIỂN CHUỘT) ---

        if (playerManager.isDied)

        {

            diedPanel.SetActive(true);

            WinPanel.SetActive(false);

            Cursor.lockState = CursorLockMode.None; 

            Cursor.visible = true;

        } 

        else if(playerManager.currpoint == playerManager.totalpoint)

        {

            diedPanel.SetActive(false);

            WinPanel.SetActive(true);

            Cursor.lockState = CursorLockMode.None; 

            Cursor.visible = true;

        }



        // ====================================================================

        // --- KHỐI LOGIC UI BỊ MẤT ĐÃ ĐƯỢC THÊM LẠI Ở ĐÂY ---

        // ====================================================================

        

        // --- 1. Cập nhật Stamina hiện tại ---

        if (currStamina != null)

        {

            // Dùng PlayerManager.CurrentStamina (giá trị có thể có số lẻ)

            currStamina.text = $"{playerManager._stamina:F1}"; // Làm tròn 1 chữ số thập phân

        }



        // --- 2. Cập nhật Cân nặng hiện tại ---

        if (currkg != null)

        {

            // Dùng PlayerManager.CurrentWeight (giá trị số nguyên)

            currkg.text = $"{playerManager.currweight}";

        }



        // --- 3. Cập nhật Điểm hiện tại ---

        if (point != null)

        {

            // Dùng PlayerManager.CurrentPoint (giá trị số nguyên)

            point.text = $"{playerManager.currpoint}";

        }



        // --- 4. Xử lý Guide Panel ---

        if (Input.GetKeyDown(KeyCode.H) && !isSolving)

        {

            toggleGuide = !toggleGuide;

        }

        GuidePanel.SetActive(toggleGuide);

        

        // --- 5. Xử lý Escape để đóng bảng giải đố ---

        if (Input.GetKeyDown(KeyCode.Escape) && isSolving)

        {

            Clodetab();

        }

    }

    

    // =========================================================================

    //                            LOGIC CÂU HỎI (GIỮ NGUYÊN)

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

        

        if (available.Count == 0)

        {

            return null;

        }

        

        int randomIndex = Random.Range(0, available.Count);

        return available[randomIndex];

    }

    

    public void DisplayQuestion(QuestionData q)

    {

        content.text = q.questionContent;

        

        List<string> answers = new List<string>();

        answers.Add(q.correctAnswer);

        answers.AddRange(q.incorrectAnswers);

        

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

            if (i < answers.Count)

            {

                c[i].text = answers[i];

            }

            else

            {

                c[i].text = "";

            }

        }

    }

    

    public void ChooseAnswer(string chosenAnswer)
{
    if (currentQuestion == null) return;
    
    if (chosenAnswer == currentQuestion.correctAnswer)
    {
        Debug.Log("Đáp án Đúng!");
        solvedQuestionContents.Add(currentQuestion.questionContent); 
        
        
        // HỦY CỬA TRƯỚC KHI ĐÓNG PANEL (để tránh lỗi)
        if (activeDoor != null)
        {
            activeDoor.DoorSolved(); // Gọi hàm Destroy trên cánh cửa đã tương tác
        }
        
        // Đóng Panel
        Clodetab();
        
        // Quan trọng: Đặt lại activeDoor sau khi đã xử lý
        activeDoor = null; 
    }
    else
    {
        Debug.Log("Đáp án Sai! Gọi AI.");
        
        // ⭐ KÍCH HOẠT SỰ KIỆN GỌI AI TRÊN CÁNH CỬA ĐANG TƯƠNG TÁC
        if (activeDoor != null)
        {
            activeDoor.AnswerFailed(); // Gọi hàm mới trên DoorMath
        }
        
        // Đóng Panel và đặt lại activeDoor như thường lệ
        Clodetab();
        activeDoor = null;
    }
    
    // ⭐ XÓA BỎ HOÀN TOÀN KHỐI CODE BỊ LẶP LẠI (activeDoor = null) KHỎI CUỐI HÀM NÀY
}



    // =========================================================================

    //                            LOGIC UI CƠ BẢN (GIỮ NGUYÊN)

    // =========================================================================



    public void Replay()

    {

        Time.timeScale = 1f;

        SceneManager.LoadScene("Lv1");

    }

    

    public void Menu()

    {

        Time.timeScale = 1f;

        SceneManager.LoadScene("HomeMenu");

    }

    

    public void Clodetab()

    {

        isSolving = false;

        SolvePanel.SetActive(false);
        // ⭐ Vô hiệu hóa tương tác để Raycast Game hoạt động lại
    if (solveCanvasGroup != null)
    {
        solveCanvasGroup.interactable = false;
        solveCanvasGroup.blocksRaycasts = false;
    }

        // Khóa chuột lại khi thoát khỏi Panel

        Cursor.lockState = CursorLockMode.Locked; 

        Cursor.visible = false;

        

        // Khi đóng, câu hỏi hiện tại bị hủy để lần sau DoorMath gọi, nó sẽ random lại

        currentQuestion = null; 

    }

}