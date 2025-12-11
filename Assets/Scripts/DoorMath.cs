using UnityEngine;
using System.Linq; // Cần dùng cho LINQ

public class DoorMath : MonoBehaviour
{
    [Header("AI Call Settings")]
    public float callRange = 15f; // Bán kính AI có thể nghe thấy lời kêu gọi
    private Range_Interaction ROI;
    public UI_Manager uiManager;
    private AudioSource audioSource;

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
        if (ROI.InRange && Input.GetKeyDown(KeyCode.E) && !uiManager.isSolving)
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
        if (uiManager.isSolving)
        {
            Cursor.lockState = CursorLockMode.None; 
            Cursor.visible = true;
        }
    }

    private void OpenQuestionPanel()
    {
        uiManager.isSolving = true;
        uiManager.toggleGuide = false;
        uiManager.SolvePanel.SetActive(true);
        
        // ⭐ Kích hoạt tương tác và chặn Raycast Game
        if (uiManager.solveCanvasGroup != null)
        {
            uiManager.solveCanvasGroup.interactable = true;
            uiManager.solveCanvasGroup.blocksRaycasts = true;
        }
        // 1. CHỌN CÂU HỎI NGẪU NHIÊN VÀ HIỂN THỊ
        QuestionData selectedQuestion = uiManager.GetRandomAvailableQuestion(questionList);
        
        if (selectedQuestion != null)
        {
            uiManager.currentQuestion = selectedQuestion; // Lưu câu hỏi hiện tại
            uiManager.DisplayQuestion(selectedQuestion); // Hiển thị lên UI
        }
        
        // 2. MỞ KHÓA CHUỘT
        Cursor.lockState = CursorLockMode.None; 
        Cursor.visible = true;
    }
    
    // ⭐ HÀM MỚI: Xử lý khi trả lời sai - Gọi AdultNPC
    public void AnswerFailed()
    {
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
        // 1. Huỷ đối tượng DoorMath này (và cả cửa)
        Destroy(this.gameObject); 
        
        Debug.Log("Cửa đã được mở và bị hủy.");
    }
// ⭐ HÀM MỚI: Vẽ Gizmos cho phạm vi kêu gọi
    public void OnDrawGizmos()
    {
        // Vẽ vòng tròn màu vàng cho phạm vi kêu gọi khi DoorMath được chọn trong Editor
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, callRange);
    }
}