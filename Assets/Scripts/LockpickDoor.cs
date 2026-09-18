using UnityEngine;

public class LockpickDoor : MonoBehaviour
{
    [Header("🚪 Lock Settings")]
    public string doorName = "Cửa gỗ bẻ khóa";
    public int rewardPoints = 10;
    public bool destroyOnUnlock = true;
    public GameObject doorModel; // Mô hình cửa (nếu muốn ẩn thay vì destroy cả GameObject)
    
    [Header("🔊 Audio")]
    public AudioSource audioSource;
    public AudioClip unlockSound;

    [Header("📍 Interaction Settings")]
    public float interactRadius = 3f;
    public Transform interactPivot;
    
    [HideInInspector] public bool isUnlocked = false;
    private bool playerInRange = false;
    private GameObject playerObj;
    private UI_Manager uiManager;

    void Start()
    {
        if (interactPivot == null) interactPivot = transform;
        playerObj = GameObject.FindGameObjectWithTag("Player");
        uiManager = FindFirstObjectByType<UI_Manager>();
    }

    void Update()
    {
        if (isUnlocked) return;
        if (UI_Manager.isSolving) return;

        CheckPlayerDistance();

        if (playerInRange && Input.GetKeyDown(KeyCode.E))
        {
            StartLockpicking();
        }
    }

    private void CheckPlayerDistance()
    {
        if (playerObj == null) return;
        float dist = Vector3.Distance(interactPivot.position, playerObj.transform.position);
        playerInRange = (dist <= interactRadius);
    }

    public void StartLockpicking()
    {
        if (uiManager == null) uiManager = FindFirstObjectByType<UI_Manager>();

        if (uiManager != null)
        {
            uiManager.StartLockpicking(this);
        }
        else
        {
            Debug.LogError("UI_Manager không tìm thấy trong Scene!");
        }
    }

    public void OnUnlockSuccess()
    {
        isUnlocked = true;
        Debug.Log($"<color=green>Đã mở thành công cửa: {doorName}!</color>");

        if (uiManager != null && uiManager.playerManager != null)
        {
            uiManager.playerManager.currpoint += rewardPoints;
            Debug.Log($"Cộng {rewardPoints} điểm. Tổng điểm hiện tại: {uiManager.playerManager.currpoint}");
        }

        if (audioSource != null && unlockSound != null)
        {
            audioSource.PlayOneShot(unlockSound);
        }

        if (destroyOnUnlock)
        {
            if (doorModel != null)
            {
                doorModel.SetActive(false);
            }
            else
            {
                gameObject.SetActive(false);
            }
        }
    }

    [Header("⚠️ Alarm & NPC Alert Settings")]
    public float callRange = 20f;

    public void OnUnlockFailed()
    {
        AnswerFailed();
    }

    public void AnswerFailed()
    {
        if (audioSource != null)
        {
            if (!audioSource.isPlaying)
            {
                audioSource.Play();
            }
        }
        
        Debug.Log($"Trò chơi thất bại! Đang kêu gọi AdultNPC trong phạm vi {callRange}m.");

        // 1. Tìm tất cả colliders trong phạm vi callRange
        Collider[] colliders = Physics.OverlapSphere(transform.position, callRange);
        
        int adultCount = 0;
        
        foreach (var collider in colliders)
        {
            // 2. Kiểm tra Tag "adult"
            if (collider.CompareTag("adult")) 
            {
                AI_Move_NavMesh adultNpc = collider.GetComponent<AI_Move_NavMesh>();
                
                if (adultNpc != null)
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

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Transform p = (interactPivot != null) ? interactPivot : transform;
        Gizmos.DrawWireSphere(p.position, interactRadius);

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(p.position, callRange);
    }
}
