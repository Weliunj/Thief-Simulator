using UnityEngine;
using StarterAssets; // Cần dùng namespace này để truy cập StarterAssetsInputs

public class Atk : MonoBehaviour
{
    // Cần tham chiếu đến Animator và Input
    private Animator _animator;
    private StarterAssetsInputs _input;

    // Animation ID cho Trigger Attack
    private int _animIDAttack;

    // --- Biến kiểm soát trạng thái tấn công (tùy chọn) ---
    // public bool IsAttacking { get; private set; } 
    // Nếu bạn muốn thêm logic phức tạp hơn (VD: cấm di chuyển khi tấn công)

    private void Start()
    {
        // Lấy tham chiếu đến các component cần thiết
        _animator = GetComponent<Animator>();
        _input = GetComponent<StarterAssetsInputs>();

        // Gán ID cho animation Trigger
        AssignAnimationIDs();
    }

    private void Update()
    {
        HandleAttackInput();
    }

    private void AssignAnimationIDs()
    {
        // Đảm bảo "Attack" là tên của tham số Trigger trong Animator của bạn
        _animIDAttack = Animator.StringToHash("Attack");
    }

    private void HandleAttackInput()
    {
        // Kiểm tra xem input tấn công có được kích hoạt không
        if (_input.attack)
        {
            // Kích hoạt animation Trigger
            if (_animator != null)
            {
                _animator.SetTrigger(_animIDAttack);
            }

            // [LƯU Ý QUAN TRỌNG]: Reset input ngay lập tức
            // để tránh kích hoạt Trigger liên tục qua mỗi frame.
            _input.attack = false;

            // **[LOGIC TẤN CÔNG THỰC TẾ (Hitbox/Damage) sẽ được kích hoạt bằng Animation Event**
        }
    }
    // Khai báo một Collider (hoặc GameObject chứa Collider) đại diện cho hitbox
    [Header("Attack Setup")]
    public Collider AttackHitbox;

    // Biến này được gọi chính xác khi đòn tấn công chạm mục tiêu (bằng Animation Event)
    public void EnableHitbox()
    {
        if (AttackHitbox != null)
        {
            AttackHitbox.enabled = true; // Kích hoạt Hitbox
            // Thường Hitbox chỉ bật trong một vài frame để tạo cảm giác chém/đấm
            // Sau đó bạn cần lên lịch tắt nó, hoặc tắt nó bằng Animation Event khác.
        }
        Debug.Log("Hitbox ĐÃ BẬT!");
        // 
    }

    // Biến này được gọi khi đòn tấn công kết thúc chạm mục tiêu (bằng Animation Event)
    public void DisableHitbox()
    {
        if (AttackHitbox != null)
        {
            AttackHitbox.enabled = false; // Tắt Hitbox
        }
        Debug.Log("Hitbox ĐÃ TẮT!");
    }
}