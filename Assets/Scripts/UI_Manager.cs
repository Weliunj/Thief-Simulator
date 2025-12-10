using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class UI_Manager : MonoBehaviour
{
    // =========================================================================
    [Header("⚙️ References")]
    public PlayerManager playerManager;
    public GameObject GuidePanel;
    private bool toggle = false;
    public GameObject diedPanel;
    public GameObject WinPanel;
    
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
        diedPanel.SetActive(false);
        WinPanel.SetActive(false);

        if (playerManager == null)
        {
            Debug.LogError("PlayerManager ScriptableObject chưa được gán trong UI_Manager.");
            enabled = false;
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
    }

    // Hàm Update (Chạy mỗi Frame để cập nhật giá trị động)
    void Update()
    {
        if (playerManager == null) return;
        if (playerManager.isDied)
        {
            diedPanel.SetActive(true);
            WinPanel.SetActive(false);
        } 
        else if(playerManager.currpoint == playerManager.totalpoint)
        {
            diedPanel.SetActive(false);
            WinPanel.SetActive(true);
        }

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

        // Note: Các giá trị MaxStamina, MaxWeight, và TotalPointGoal (Target Point) 
        // không đổi, nên chúng ta chỉ cần cập nhật chúng trong hàm Start().

        if (Input.GetKeyDown(KeyCode.H))
        {
            toggle = !toggle;
        }
        GuidePanel.SetActive(toggle);


        if(Input.GetKeyDown(KeyCode.R))
        {
            SceneManager.LoadScene("Lv1");
        }
        if(Input.GetKeyDown(KeyCode.M))
        {
            SceneManager.LoadScene("HomeMenu");
        }
    }
}