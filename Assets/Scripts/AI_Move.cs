using StarterAssets;
using UnityEngine;
using UnityEngine.AI;

public class AI_Move_NavMesh : MonoBehaviour
{
    public enum MovementState { RandomMove, Stationary, Patrol }

    // =========================================================================
    // BIẾN STATIC TOÀN CỤC CHO NHẠC CHASE (QUAN TRỌNG: Đảm bảo chỉ 1 AudioSource phát)
    public static bool isChaseMusicPlaying = false; 
    // =========================================================================

    [Header("🤖 AI Core Components")]
    private Animator animator;
    private string currAnimState; 
    private NavMeshAgent agent;
    private ThirdPersonController player; 
    public PlayerManager playerManager;
    public Light flashlight;
    
    // BIẾN THEO DÕI NỘI BỘ

    [Header("🏃 Movement Settings")]
    public AudioSource[] audioSources; // 0: Bước chân, 1: Phát hiện, 2: Chase Music
    private bool isWalkSoundPlaying = false; 
    public float walkSpeed = 1.5f;   
    public float runSpeed = 3f;      
    public float stoppingDistanceThreshold = 0.5f; 

    [Header("👀 Detection & Chase")]
    public float raycastRangePublic = 15f; 
    private float raycastRange = 15f;
    public float raycastAngle = 30f;     
    public Vector2 chaseDurationPublic = new Vector2(5f, 10f); 
    [HideInInspector] public bool targetDetected = false;
    public float chaseDuration = 0f;

    [Header("🚶 Normal Movement States")]
    public MovementState currentMovementState = MovementState.RandomMove; 
    public float targetRadius = 20f; 
    public Vector2 minMaxIdleTime = new Vector2(2f, 5f); 
    private float IdleTime = 0f;
    
    [Header("🔎 Stationary Scan")]
    public float scanDuration = 1f; 
    private float scanTimer = 0f;
    private Quaternion targetScanRotation; 
    
    [Header("📍 Patrol Points (Chỉ dùng cho chế độ Patrol)")]
    public Transform[] patrolPoints; 
    private int currentPatrolIndex = 0; 

    [Header("🏡 AI Stay Area")]
    public float maxChaseRadius = 30f;   
    public float returnSpeed = 2f;       
    private Vector3 initialPosition;      
    private Quaternion initialRotation;    
    private bool isReturningToStayArea = false; 

    [Header("🎨 Appearance Settings")]
    public Material[] availableMaterials; 
    private Renderer aiRenderer;         
    // =========================================================================

    void Start()
    {
        // ... (Khởi tạo Component & Material) ...
        agent = GetComponent<NavMeshAgent>();
        animator = GetComponent<Animator>();
        player = FindAnyObjectByType<ThirdPersonController>(); 
        
        aiRenderer = GetComponentInChildren<Renderer>(); 
        if (availableMaterials.Length > 0 && aiRenderer != null)
        {
            int randomIndex = Random.Range(0, availableMaterials.Length);
            aiRenderer.material = availableMaterials[randomIndex];
            Debug.Log($"Đã gán Material: {availableMaterials[randomIndex].name}");
        }
        
        if (agent == null) { Debug.LogError("NavMeshAgent component không được tìm thấy."); enabled = false; return; }
        if (player == null) { Debug.LogWarning("Không tìm thấy ThirdPersonController (Player)."); }
        
        // ⭐ Đảm bảo NavMeshAgent.stoppingDistance thấp (ví dụ: 0.1 trong Inspector)
        agent.speed = walkSpeed;
        initialPosition = transform.position;
        initialRotation = transform.rotation;
        
        InitializeMovementState();
    }

    // =========================================================================

    void Update()
    {
        if (agent == null || !agent.enabled) return;

        // 1. Luôn kiểm tra mục tiêu (Player)
        RayCastHitTarget();

        // 2. Xử lý trạng thái CHASE / QUAY VỀ (Ưu tiên cao nhất)
        if (targetDetected)
        {
            isReturningToStayArea = false; 
            Chase();
            return; 
        }
        
        if (isReturningToStayArea)
        {
            ReturnToStayArea();
            return;
        }

        // 3. Xử lý trạng thái di chuyển thông thường 
        switch (currentMovementState)
        {
            case MovementState.RandomMove:
                HandleRandomMove();
                break;
            case MovementState.Stationary:
                HandleStationary();
                break;
            case MovementState.Patrol:
                HandlePatrol();
                break;
        }
    }

    // =========================================================================
    //                            LOGIC CHUYỂN ĐỘNG THƯỜNG
    // =========================================================================

    private void InitializeMovementState()
    {
        IdleTime = Random.Range(minMaxIdleTime.x, minMaxIdleTime.y);
        agent.speed = walkSpeed;
        
        // Thiết lập Scan ban đầu cho RandomMove và Stationary
        targetScanRotation = initialRotation; 
        SetRandomScanRotation(); 

        if (currentMovementState == MovementState.RandomMove)
        {
            SetRandomDestination();
        }
        else if (currentMovementState == MovementState.Patrol)
        {
             if (patrolPoints != null && patrolPoints.Length > 0)
            {
                agent.SetDestination(patrolPoints[currentPatrolIndex].position);
            }
        }
    }
    
    // --- Hàm tìm và thiết lập điểm đến ngẫu nhiên ---
    private void SetRandomDestination()
    {
        Vector3 randomPoint;
        if (GetRandomPoint(transform.position, targetRadius, out randomPoint))
        {
            agent.SetDestination(randomPoint);
            // Debug.Log($"AI đang di chuyển đến vị trí ngẫu nhiên: {randomPoint}");
        }
        else
        {
            // Debug.LogWarning("Không thể tìm thấy vị trí ngẫu nhiên hợp lệ trên NavMesh.");
        }
    }

    // --- Hàm thiết lập Góc Quét Ngẫu nhiên ---
    private void SetRandomScanRotation()
    {
        scanTimer = scanDuration; // Reset thời gian quét

        float[] angles = { -45f, 0f, 45f };
        float randomAngle = angles[Random.Range(0, angles.Length)];

        Quaternion initialYRotation = Quaternion.Euler(0, initialRotation.eulerAngles.y, 0);
        targetScanRotation = initialYRotation * Quaternion.Euler(0, randomAngle, 0);
    }


    // --- Xử lý Trạng thái Di chuyển Ngẫu nhiên (Kèm Scan khi Idle) ---
    private void HandleRandomMove()
    {
        // Kiểm tra xem AI đã đến gần đích chưa
        if (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance + stoppingDistanceThreshold )
        {
            // Đã đến đích, bắt đầu thời gian idle/quét
            
            if (IdleTime > 0f)
            {
                SetAnimation("idle");
                IdleTime -= Time.deltaTime;
                
                // Xử lý Quét (Scan) trong thời gian Idle
                transform.rotation = Quaternion.Slerp(transform.rotation, targetScanRotation, Time.deltaTime * 3f);
                scanTimer -= Time.deltaTime;
                
                if (scanTimer <= 0f)
                {
                    SetRandomScanRotation(); 
                }
                return; 
            }
            else
            {
                // Hết thời gian Idle -> Chọn điểm mới
                IdleTime = Random.Range(minMaxIdleTime.x, minMaxIdleTime.y);
                SetRandomDestination();
                SetRandomScanRotation(); // Thiết lập Góc quét mới
            }
        }
        else
        {
            // Đang di chuyển
            agent.speed = walkSpeed;
            SetAnimation("walk");
        }
    }

    // --- Xử lý Trạng thái Đứng Im (Chỉ quét) ---
    private void HandleStationary()
    {
        if (agent.hasPath) { agent.SetDestination(transform.position); }
        SetAnimation("idle");
        
        if (!targetDetected && !isReturningToStayArea)
        {
            transform.rotation = Quaternion.Slerp(transform.rotation, targetScanRotation, Time.deltaTime * 3f);
            scanTimer -= Time.deltaTime;

            if (scanTimer <= 0f) { SetRandomScanRotation(); }
        }
    }

    // --- Xử lý Trạng thái Tuần tra ---
    private void HandlePatrol()
    {
        if (patrolPoints == null || patrolPoints.Length == 0)
        {
            currentMovementState = MovementState.Stationary;
            return;
        }

        if (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance + stoppingDistanceThreshold)
        {
            if (IdleTime > 0f)
            {
                IdleTime -= Time.deltaTime;
                SetAnimation("idle");
                return;
            }
            else
            {
                IdleTime = Random.Range(minMaxIdleTime.x, minMaxIdleTime.y);
                currentPatrolIndex = (currentPatrolIndex + 1) % patrolPoints.Length;
                agent.SetDestination(patrolPoints[currentPatrolIndex].position);
                SetAnimation("walk");
            }
        }
        else
        {
             agent.speed = walkSpeed;
             SetAnimation("walk");
        }
    }
    
    // --- Hàm hỗ trợ tìm kiếm điểm ngẫu nhiên trên NavMesh ---
    private bool GetRandomPoint(Vector3 center, float range, out Vector3 result)
    {
        Vector3 randomDirection = Random.insideUnitSphere * range;
        randomDirection += center;
        
        NavMeshHit hit;
        if (NavMesh.SamplePosition(randomDirection, out hit, range, NavMesh.AllAreas))
        {
            result = hit.position;
            return true;
        }

        result = Vector3.zero;
        return false;
    }


    // =========================================================================
    //                            PHÁT HIỆN, CHASE & QUAY VỀ
    // =========================================================================

    public void RayCastHitTarget()
    {
        if (player == null) return; 
        
        raycastRange = (player.Crouching) ? raycastRangePublic / 2.2f : raycastRangePublic;
        flashlight.range = (player.Crouching) ? 8f : 27f;
        Vector3 rayStart = transform.position + Vector3.up;
        Vector3 forward = transform.forward;
        Vector3[] directions = new Vector3[]
        {
             forward,
             Quaternion.AngleAxis(-raycastAngle, Vector3.up) * forward,
             Quaternion.AngleAxis(raycastAngle, Vector3.up) * forward,
             Quaternion.AngleAxis(raycastAngle, transform.right) * forward,
             Quaternion.AngleAxis(-raycastAngle, transform.right) * forward
        };

        foreach (Vector3 dir in directions)
        {

            if (Physics.Raycast(rayStart, dir, out RaycastHit hit, raycastRange))
            {
                Debug.DrawLine(rayStart, hit.point, Color.red);
                if (hit.collider.CompareTag("Player") && !playerManager.isDied)
                {
                    PlayDetectionSound(); 
                    HandleChaseMusic(true); 
                    chaseDuration = Random.Range(chaseDurationPublic.x, chaseDurationPublic.y);
                    targetDetected = true;
                    return; // Thoát ngay khi tìm thấy player
                }
            }
            else
            {
                Debug.DrawLine(rayStart, rayStart + dir * raycastRange, Color.green);
            }
        }
        
        // Cập nhật Chase Duration/Lost Target Timer
        if(targetDetected && chaseDuration > 0f)
        {
            chaseDuration -= Time.deltaTime;
        }

        // Nếu AI đang Chase nhưng Player vừa mất dấu (hết thời gian grace)
        if (targetDetected && chaseDuration <= 0f)
        {
            targetDetected = false;
            isReturningToStayArea = true;
            agent.SetDestination(initialPosition);
            agent.speed = returnSpeed;
            SetAnimation("walk"); 
            HandleChaseMusic(false); 
        }
    }

    public void Chase()
    {
        if (player == null) return;
        
        // 1. Kiểm tra thời gian Chase & Bán kính tối đa
        float distanceToInitial = Vector3.Distance(transform.position, initialPosition);
        
        if (distanceToInitial > maxChaseRadius || chaseDuration <= 0f)
        {
            // Hết thời gian Chase HOẶC ra khỏi bán kính cho phép -> Bắt đầu quay về
            targetDetected = false;
            isReturningToStayArea = true;
            agent.SetDestination(initialPosition);
            agent.speed = returnSpeed;
            SetAnimation("walk"); 
            HandleChaseMusic(false);
            return;
        }

        // 2. Thực hiện Chase
        agent.speed = runSpeed;
        float distanceToPlayer = Vector3.Distance(transform.position, player.transform.position);
        if(distanceToPlayer > agent.stoppingDistance + stoppingDistanceThreshold) 
        {
            SetAnimation("run"); 
            agent.SetDestination(player.transform.position);
        }
        else // Đã chạm tới/rất gần Player
        {
            // Dừng AI và bắt đầu quá trình quay về
            targetDetected = false; 
            isReturningToStayArea = true;
            agent.SetDestination(initialPosition);
            agent.speed = returnSpeed;
            SetAnimation("idle"); 
            HandleChaseMusic(false);
        }
    }

    public void OnTriggerEnter(Collider other)
    {
        // LOGIC ĐÁNH BẠI PLAYER
        if (playerManager != null && playerManager.isDied == false && other.CompareTag("Player"))
        {
            playerManager.isDied = true; 
            Debug.Log(other.gameObject.name);
        }
    }
    public void ReturnToStayArea()
    {
        // ... (Logic ReturnToStayArea giữ nguyên) ...
        agent.speed = returnSpeed;
        SetAnimation("walk"); 

        // Nếu đã đến gần vị trí ban đầu
        if (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance + stoppingDistanceThreshold)
        {
            // Khôi phục góc quay ban đầu mượt mà
            transform.rotation = Quaternion.Slerp(transform.rotation, initialRotation, Time.deltaTime * 5f);
            
            // Kiểm tra xem đã quay gần đúng góc quay ban đầu chưa (ngưỡng 5 độ)
            if (Quaternion.Angle(transform.rotation, initialRotation) < 5f)
            {
                isReturningToStayArea = false;
                agent.SetDestination(transform.position); // Dừng hẳn
                
                // Khởi tạo lại trạng thái di chuyển thông thường ban đầu
                InitializeMovementState(); 
                SetAnimation("idle"); 
            }
        }
    }
    
    // ... (Logic âm thanh và Gizmos giữ nguyên) ...
    // (HandleWalkSound, PlayDetectionSound, HandleChaseMusic, SetAnimation, OnDrawGizmosSelected)
    private void SetAnimation(string name)
    {
        if (animator == null || currAnimState == name) return;
        
        animator.SetTrigger(name);
        currAnimState = name;
        
        if (name == "walk" || name == "run")
        {
             HandleWalkSound(true);
        }
        else
        {
             HandleWalkSound(false);
        }
    }
    private void HandleWalkSound(bool shouldPlay)
    {
        if (audioSources.Length == 0 || audioSources[0] == null) return;

        if (shouldPlay && !isWalkSoundPlaying)
        {
            audioSources[0].loop = true;
            audioSources[0].Play();
            isWalkSoundPlaying = true;
        }
        else if (!shouldPlay && isWalkSoundPlaying)
        {
            audioSources[0].Stop();
            isWalkSoundPlaying = false;
        }
    }
    public void PlayDetectionSound()
    {
        if (audioSources.Length > 1 && audioSources[1] != null)
        {
            audioSources[1].loop = false;
            if (!audioSources[1].isPlaying)
            {
                audioSources[1].Play();
            }
        }
    }
    public void HandleChaseMusic(bool shouldPlay)
    {
        if (audioSources.Length < 3 || audioSources[2] == null) return;

        if (shouldPlay)
        {
            if (!isChaseMusicPlaying)
            {
                audioSources[2].loop = true;
                if (!audioSources[2].isPlaying)
                {
                    audioSources[2].Play();
                }
                isChaseMusicPlaying = true;
                Debug.Log("Chase Music Started by: " + gameObject.name);
            }
        }
        else 
        {
             audioSources[2].Stop();
             isChaseMusicPlaying = false; 
             Debug.Log("Chase Music Stopped by: " + gameObject.name);
        }
    }
    private void OnDrawGizmosSelected()
    {
        // ... (Logic OnDrawGizmosSelected giữ nguyên) ...
        Vector3 position = transform.position;

        // 1. VẼ VÙNG HOẠT ĐỘNG TỐI ĐA (MAX CHASE RADIUS)
        Gizmos.color = Color.yellow * 0.5f; 
        
        Vector3 centerPosition = (Application.isPlaying && initialPosition != Vector3.zero) ? initialPosition : position;
        Gizmos.DrawWireSphere(centerPosition, maxChaseRadius);
        
        Gizmos.color = Color.yellow;
        Gizmos.DrawSphere(centerPosition, 0.2f);
        
        
        // 2. VẼ TẦM NHÌN (RAYCAST RANGE)
        Gizmos.color = Color.green;
        Vector3 rayStart = position + Vector3.up;

        Vector3 forward = transform.forward;
        Vector3[] detectionDirections = new Vector3[]
        {
             forward,
             Quaternion.AngleAxis(-raycastAngle, Vector3.up) * forward,
             Quaternion.AngleAxis(raycastAngle, Vector3.up) * forward,
             Quaternion.AngleAxis(raycastAngle, transform.right) * forward,
             Quaternion.AngleAxis(-raycastAngle, transform.right) * forward
        };
        
        float currentRange = Application.isPlaying ? raycastRange : raycastRangePublic;

        foreach (Vector3 dir in detectionDirections)
        {
            Gizmos.DrawRay(rayStart, dir * currentRange);
        }
        
        // 3. VẼ LOGIC THEO TỪNG ENUM/TRẠNG THÁI ƯU TIÊN
        
        if (targetDetected && player != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawLine(position, player.transform.position);
            Gizmos.DrawWireSphere(player.transform.position, 0.5f);
            return;
        }
        
        if (isReturningToStayArea)
        {
            Gizmos.color = Color.Lerp(Color.red, Color.yellow, 0.5f); 
            Gizmos.DrawLine(position, centerPosition);
            Gizmos.DrawWireSphere(centerPosition, 0.5f);
            return;
        }
        
        // VẼ CHO CÁC TRẠNG THÁI DI CHUYỂN THÔNG THƯỜNG
        switch (currentMovementState)
        {
            case MovementState.RandomMove:
                Gizmos.color = Color.blue;
                Gizmos.DrawWireSphere(position, targetRadius); 
                
                if (agent != null && agent.hasPath)
                {
                    Gizmos.color = Color.cyan;
                    Gizmos.DrawLine(position, agent.destination);
                    Gizmos.DrawSphere(agent.destination, 0.3f);
                }
                break;

            case MovementState.Stationary:
                Gizmos.color = Color.white;
                Gizmos.DrawWireSphere(position, 0.5f); 

                Gizmos.color = Color.magenta;
                if (Application.isPlaying && targetScanRotation != Quaternion.identity) 
                {
                    Vector3 scanDirection = targetScanRotation * Vector3.forward;
                    Gizmos.DrawRay(rayStart, scanDirection * 3f); 
                }
                break;

            case MovementState.Patrol:
                Gizmos.color = Color.magenta;
                if (patrolPoints != null && patrolPoints.Length > 0)
                {
                    for (int i = 0; i < patrolPoints.Length; i++)
                    {
                        Transform currentPoint = patrolPoints[i];
                        if (currentPoint == null) continue;

                        Gizmos.DrawWireSphere(currentPoint.position, 0.4f);
                        
                        if (patrolPoints.Length > 1)
                        {
                            Transform nextPoint = patrolPoints[(i + 1) % patrolPoints.Length];
                            if (nextPoint != null)
                            {
                                Gizmos.DrawLine(currentPoint.position, nextPoint.position);
                            }
                        }
                    }
                    
                    if (agent != null && agent.hasPath && currentPatrolIndex >= 0 && currentPatrolIndex < patrolPoints.Length)
                    {
                        Gizmos.color = Color.cyan;
                        Gizmos.DrawLine(position, agent.destination);
                        Gizmos.DrawSphere(agent.destination, 0.3f);
                    }
                }
                break;
        }
    }
}