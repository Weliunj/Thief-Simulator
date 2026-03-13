using UnityEngine;

public class GameSettings : MonoBehaviour
{
    void Awake()
    {
        // 1. Tắt VSync (Đồng bộ dọc) 
        // Nếu VSync bật, Unity sẽ ưu tiên theo tần số quét màn hình (ví dụ 144Hz) 
        // và bỏ qua lệnh TargetFrameRate.
        QualitySettings.vSyncCount = 0;

        // 2. Ép khung hình về 60
        Application.targetFrameRate = 60;
        
        // Giữ cho màn hình không bị tắt khi đang chơi (tùy chọn)
        //Screen.sleepTimeout = SleepTimeout.NeverSleep;
        
        Debug.Log("Game đã được ép về 60 FPS");
    }
}