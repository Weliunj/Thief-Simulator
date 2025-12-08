using UnityEngine;
using UnityEngine.Rendering;
[CreateAssetMenu(fileName = "PlayerManager", menuName = "ScriptableObjects/PlayerManager", order = 1)]
public class PlayerManager : ScriptableObject
{
    public float MoveSpeed = 2f;
    public float SprintSpeed = 7f;
    public float MaxStamina = 10f;
    public int Maxweight = 100;
    public int currweight = 0;

    [Header("Stamina Settings")]
    public float StaminaDepletionRate = 3.0f; // Tốc độ giảm Stamina khi Sprint (VD: 3f/giây)
    public float StaminaRegenRate = 2.0f;     // Tốc độ hồi phục Stamina (VD: 2f/giây)
    public float StaminaRegenCooldown = 1.0f; // Thời gian chờ trước khi hồi phục (1 giây)

    [HideInInspector]
    public float _MoveSpeed = 2.0f;
    [HideInInspector]
    public float _SprintSpeed = 7.0f;
    public float _stamina = 10f;
    public int currpoint = 0;
    public int totalpoint = 400;

    private void OnEnable()
    {
        _stamina = MaxStamina; // Đảm bảo stamina được thiết lập lại khi khởi động (trong Editor)
    }
 
}
