using UnityEngine;
using System.Collections;
using System.Linq; // Cần dùng cho LINQ

public class DoorMath : MonoBehaviour
{
    [Header("AI Call Settings")]
    public float callRange = 15f; // Bán kính AI có thể nghe thấy lời kêu gọi
    private Range_Interaction ROI;
    public UI_Manager uiManager;
    private AudioSource audioSource;

    [Header("Question Requirements")] // ⭐ THÊM BIẾN MỚI
    public int requiredCorrectAnswers = 3; // Số câu hỏi phải trả lời đúng liên tiếp
    
    [HideInInspector] public int currentCorrectAnswers = 0; // Số câu hỏi đã đúng cho lần tương tác này
    // Tham chiếu đến file ScriptableObject Questions (Cần gán trong Inspector)
    public Questions questionList; 

    private void Start()
    {
        audioSource = GetComponent<AudioSource>();
        ROI = GetComponentInChildren<Range_Interaction>();
        // Khởi tạo trạng thái đã hoàn thành của cửa (Nếu cần)
        // Nếu cửa này đã được mở, có thể kiểm tra ở đây.
    }

    private void Update()
    {
        // Điều kiện kích hoạt Panel câu hỏi
        if (ROI.InRange && Input.GetKeyDown(KeyCode.E) && !UI_Manager.isSolving)
        {
            // Chỉ mở Panel nếu còn câu hỏi chưa giải
            if (uiManager.GetAvailableQuestions(questionList).Count > 0)
            {
                uiManager.activeDoor = this;
                OpenQuestionPanel();
                
            }
            else
            {
                // Xử lý khi đã hết câu hỏi (ví dụ: in log hoặc không làm gì)
                Debug.Log("Đã hoàn thành tất cả câu hỏi. Cửa này không cần giải nữa.");
            }
        }
        
        // Giữ chuột mở nếu đang giải
        if (UI_Manager.isSolving)
        {
            Cursor.lockState = CursorLockMode.None; 
            Cursor.visible = true;
        }
    }

    private void OpenQuestionPanel()
    {
        UI_Manager.isSolving = true;
        uiManager.toggleGuide = false;
        uiManager.SolvePanel.SetActive(true);
        
        // ⭐ Kích hoạt tương tác và chặn Raycast Game
        if (uiManager.solveCanvasGroup != null)
        {
            uiManager.solveCanvasGroup.interactable = true;
            uiManager.solveCanvasGroup.blocksRaycasts = true;
        }
        
        // ⭐ MỖI LẦN MỞ CỬA: CHỌN CÂU HỎI NGẪU NHIÊN TỪ DANH SÁCH CHƯA GIẢI
        QuestionData selectedQuestion = uiManager.GetRandomAvailableQuestion(questionList);
        
        if (selectedQuestion != null)
        {
            uiManager.currentQuestion = selectedQuestion; // Lưu câu hỏi hiện tại
            uiManager.DisplayQuestion(selectedQuestion); // Hiển thị lên UI
            Debug.Log($"Đã random câu hỏi mới: {selectedQuestion.questionContent}");
        }
        else
        {
            Debug.LogWarning("Không còn câu hỏi nào để giải! Đóng panel.");
            uiManager.Clodetab();
            return;
        }
        
        // MỞ KHÓA CHUỘT
        Cursor.lockState = CursorLockMode.None; 
        Cursor.visible = true;
        
        // ⭐ CẬP NHẬT SLIDER TIẾN ĐỘ KHI MỞ CỬA
        if (uiManager != null)
        {
            uiManager.UpdateProgressUI(currentCorrectAnswers);
        }

        Debug.Log($"Cửa này cần {requiredCorrectAnswers - currentCorrectAnswers} câu đúng nữa (đã đúng {currentCorrectAnswers}/{requiredCorrectAnswers}).");
    }
    
    public void AnswerCorrect()
    {
        currentCorrectAnswers++;
        Debug.Log($"Trả lời đúng! Đã đúng {currentCorrectAnswers}/{requiredCorrectAnswers} câu.");
        
        if (currentCorrectAnswers >= requiredCorrectAnswers)
        {
            // ⭐ ĐỦ 3 CÂU ĐÚNG KHÁC NHAU -> HỦY CỬA
            Debug.Log("Đã đủ 3 câu đúng! Cửa sẽ được mở.");
            DoorSolved();
        }
        else
        {
            // ⭐ CHƯA ĐỦ -> ĐÓNG PANEL, PLAYER PHẢI MỞ LẠI ĐỂ GIẢI TIẾP
            Debug.Log($"Còn {requiredCorrectAnswers - currentCorrectAnswers} câu nữa. Đóng tab, mở lại để giải tiếp.");
        }
    }
    public void AnswerFailed()
    {
        currentCorrectAnswers = 0; // RESET đếm khi trả lời sai
        
        // ⭐ RESET SLIDER VỀ 0 KHI TRẢ LỜI SAI
        if (uiManager != null)
        {
            uiManager.UpdateProgressUI(0);
        }
        
        if(audioSource.isPlaying == false)
        {
            audioSource.Play();
        }
        Debug.Log($"Trò chơi thất bại! Đang kêu gọi AdultNPC trong phạm vi {callRange}m.");

        // 1. Tìm tất cả colliders trong phạm vi callRange
        Collider[] colliders = Physics.OverlapSphere(transform.position, callRange);
        
        int adultCount = 0;
        
        foreach (var collider in colliders)
        {
            // 2. Kiểm tra Tag "adult" (Giả sử AdultNPC có tag này)
            if(collider.CompareTag("adult")) 
            {
                // ⭐ Cần đảm bảo script AdultNPC của bạn có tên là AI_Move_NavMesh
                AI_Move_NavMesh adultNpc = collider.GetComponent<AI_Move_NavMesh>();
                
                if(adultNpc != null)
                {
                    // 3. Kích hoạt chế độ đuổi (chase) trên AdultNPC
                    adultNpc.PlayDetectionSound(); 
                            adultNpc.HandleChaseMusic(true);
                    adultNpc.targetDetected = true; 
                    
                    // Thiết lập thời gian theo đuổi ngẫu nhiên
                    adultNpc.chaseDuration = Random.Range(
                        adultNpc.chaseDurationPublic.x, 
                        adultNpc.chaseDurationPublic.y);

                    adultCount++;
                    Debug.Log($"Kích hoạt chase trên NPC: {collider.gameObject.name}");
                }
            }
        }
        
        if (adultCount == 0)
        {
            Debug.Log("Không tìm thấy AdultNPC nào trong phạm vi.");
        }
    }
    // Hàm này sẽ được gọi từ UI_Manager sau khi trả lời đúng
    public void DoorSolved()
    {
        // ⭐ ĐỢI 1 GIÂY ĐỂ SLIDER CHẠY HẾT TRƯỚC KHI DESTROY CỬA
        StartCoroutine(DestroyDoorAfterDelay());
    }
    
    private IEnumerator DestroyDoorAfterDelay()
    {
        // Đợi 1 giây để slider có thời gian chạy đến 3/3
        yield return new WaitForSeconds(1f);
        
        // Sau đó mới destroy cửa
        Debug.Log("Cửa đã được mở và bị hủy.");
        Destroy(this.gameObject);
    }
// ⭐ HÀM MỚI: Vẽ Gizmos cho phạm vi kêu gọi
    public void OnDrawGizmos()
    {
        // Vẽ vòng tròn màu vàng cho phạm vi kêu gọi khi DoorMath được chọn trong Editor
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, callRange);
    }
}