using ithappy.Animals_FREE;
using StarterAssets;
using UnityEngine;
using UnityEngine.AI;

public class AI_Move_NavMesh : MonoBehaviour
{
    // Enum mới để quản lý các kiểu di chuyển của AI
    public enum MovementState
    {
        RandomMove,  // Di chuyển ngẫu nhiên
        Stationary,  // Đứng im
        Patrol       // Tuần tra giữa các điểm
    }

    // =========================================================================
    [Header("🤖 AI Core Components")]
    private Animator animator;
    private string currAnimState;
    private NavMeshAgent agent;
    private ThirdPersonController player; 
    public PlayerManager playerManager;

    [Header("🏃 Movement Settings")]
    public float walkSpeed = 1.5f;   
    public float runSpeed = 3f;      
    public float stoppingDistanceThreshold = 0.5f; 

    [Header("👀 Detection & Chase")]
    public float raycastRangePublic = 15f; 
    private float raycastRange = 15f;
    public float raycastAngle = 30f;     
    public Vector2 chaseDurationPublic = new Vector2(5f, 10f); 
    [HideInInspector] public bool targetDetected = false;
    [HideInInspector] public float chaseDuration = 0f;

    [Header("🚶 Normal Movement States")]
    public MovementState currentMovementState = MovementState.RandomMove; 
    public float targetRadius = 20f; 
    public Vector2 minMaxIdleTime = new Vector2(2f, 5f); 
    private float IdleTime = 0f;
    
    [Header("🔎 Stationary Scan")]
    public float scanDuration = 1f; // Thời gian duy trì góc nhìn khi quét
    private float scanTimer = 0f;
    private Quaternion targetScanRotation; // Góc quay mục tiêu khi đang quét
    
    [Header("📍 Patrol Points (Chỉ dùng cho chế độ Patrol)")]
    public Transform[] patrolPoints; 
    private int currentPatrolIndex = 0; 

    [Header("🏡 AI Stay Area")]
    public float maxChaseRadius = 30f;  
    public float returnSpeed = 2f;      
    private Vector3 initialPosition;     
    private Quaternion initialRotation;  
    private bool isReturningToStayArea = false; 

    [Header("🎨 Appearance Settings")] // Header mới để quản lý hình thức
    public Material[] availableMaterials; // Mảng chứa các vật liệu bạn muốn chọn
    private Renderer aiRenderer;         // Component Renderer của GameObject AI
    // =========================================================================

    void Start()
    {
        // 1. Lấy Component
        agent = GetComponent<NavMeshAgent>();
        animator = GetComponent<Animator>();
        player = FindAnyObjectByType<ThirdPersonController>(); 

        // LẤY RENDERER VÀ GÁN MATERIAL NGẪU NHIÊN
        aiRenderer = GetComponentInChildren<Renderer>(); // Tìm Renderer trên GameObject hoặc con
        if (aiRenderer == null)
        {
            Debug.LogWarning("Renderer component không được tìm thấy trên AI.");
        }

        if (availableMaterials.Length > 0 && aiRenderer != null)
        {
            // Chọn ngẫu nhiên một chỉ mục trong mảng
            int randomIndex = Random.Range(0, availableMaterials.Length);
            
            // Gán material đã chọn cho Renderer
            aiRenderer.material = availableMaterials[randomIndex];
            Debug.Log($"Đã gán Material: {availableMaterials[randomIndex].name}");
        }
        else if (aiRenderer != null)
        {
            Debug.LogWarning("Mảng Materials rỗng! Không thể gán material ngẫu nhiên.");
        }


        if (agent == null)
        {
            Debug.LogError("NavMeshAgent component không được tìm thấy.");
            enabled = false;
            return;
        }

        if (player == null)
        {
             Debug.LogWarning("Không tìm thấy ThirdPersonController (Player). AI sẽ không thể Chase.");
        }
        
        // Thiết lập ban đầu
        agent.speed = walkSpeed;
        
        // LƯU VỊ TRÍ & GÓC QUAY BAN ĐẦU
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
            // Nếu phát hiện Player, ngắt trạng thái quay về (nếu đang ở đó)
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

        // Cập nhật Animation cho việc di chuyển thông thường (walk/idle)
        UpdateNormalMoveAnimation();
    }

    // =========================================================================
    //                            CÁC HÀM XỬ LÝ TRẠNG THÁI
    // =========================================================================

    private void InitializeMovementState()
    {
        IdleTime = Random.Range(minMaxIdleTime.x, minMaxIdleTime.y);
        agent.speed = walkSpeed;

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
        else if (currentMovementState == MovementState.Stationary)
        {
            targetScanRotation = initialRotation; // Bắt đầu bằng góc ban đầu
            SetRandomScanRotation();
        }
    }
    
    // --- Hàm thiết lập Góc Quét Ngẫu nhiên ---
    private void SetRandomScanRotation()
    {
        scanTimer = scanDuration; // Reset thời gian quét

        // Chọn ngẫu nhiên 1 trong 3 góc: Trái (-45), Giữa (0), Phải (+45) so với góc ban đầu (initialRotation)
        float[] angles = { -45f, 0f, 45f };
        float randomAngle = angles[Random.Range(0, angles.Length)];

        // Tính toán góc quay mới dựa trên góc quay ban đầu (chỉ sử dụng trục Y)
        Quaternion initialYRotation = Quaternion.Euler(0, initialRotation.eulerAngles.y, 0);
        targetScanRotation = initialYRotation * Quaternion.Euler(0, randomAngle, 0);
    }


    // --- Xử lý Trạng thái Di chuyển Ngẫu nhiên ---
    private void HandleRandomMove()
    {
        if (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance + stoppingDistanceThreshold )
        {
            if(IdleTime > 0f)
            {
                IdleTime -= Time.deltaTime;
                return; 
            }
            else
            {
                IdleTime = Random.Range(minMaxIdleTime.x, minMaxIdleTime.y);
                SetRandomDestination();
            }
        }
    }

    // --- Xử lý Trạng thái Đứng Im (VÀ QUÉT) ---
    private void HandleStationary()
    {
        // Đảm bảo agent đã dừng
        if (agent.hasPath)
        {
            agent.SetDestination(transform.position);
        }

        SetAnimation("idle");
        
        // Chỉ quét khi không đang đuổi và không đang quay về
        if (!targetDetected && !isReturningToStayArea)
        {
            // Quay AI về góc quay mục tiêu (targetScanRotation)
            transform.rotation = Quaternion.Slerp(transform.rotation, targetScanRotation, Time.deltaTime * 3f);
            
            // Cập nhật Scan Timer
            scanTimer -= Time.deltaTime;

            if (scanTimer <= 0f)
            {
                // Hết thời gian scan -> Thiết lập góc quét mới
                SetRandomScanRotation();
            }
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
                return;
            }
            else
            {
                IdleTime = Random.Range(minMaxIdleTime.x, minMaxIdleTime.y);
                currentPatrolIndex = (currentPatrolIndex + 1) % patrolPoints.Length;
                agent.SetDestination(patrolPoints[currentPatrolIndex].position);
            }
        }
    }
    
    // --- Cập nhật Animation Di chuyển Thường ---
    private void UpdateNormalMoveAnimation()
    {
        if (agent.hasPath && !agent.pathPending && agent.remainingDistance > agent.stoppingDistance + stoppingDistanceThreshold)
        {
            SetAnimation("walk");
        }
        else
        {
            SetAnimation("idle");
        }
    }

    // --- Hàm tìm và thiết lập điểm đến ngẫu nhiên ---
    private void SetRandomDestination()
    {
        Vector3 randomPoint;
        if (GetRandomPoint(transform.position, targetRadius, out randomPoint))
        {
            agent.SetDestination(randomPoint);
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

    // --- Hàm thiết lập Animation ---
    private void SetAnimation(string name)
    {
        if (animator == null || currAnimState == name)
        {
            return;
        }
        animator.SetTrigger(name);
        currAnimState = name;
    }

    // =========================================================================
    //                            PHÁT HIỆN, CHASE & QUAY VỀ
    // =========================================================================

    public void RayCastHitTarget()
    {
        if (player == null) return; 
        
        // Giảm tầm nhìn nếu Player đang Crouch
        raycastRange = (player != null && player.Crouching) ? raycastRangePublic / 2f : raycastRangePublic;

        Vector3 rayStart = transform.position + Vector3.up;
        Vector3 forward = transform.forward;

        // Các hướng raycast
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
                if (hit.collider.CompareTag("Player"))
                {
                    if (!targetDetected)
                    {
                        chaseDuration = Random.Range(chaseDurationPublic.x, chaseDurationPublic.y);
                        targetDetected = true;
                    }
                    break;
                }
            }
            else
            {
                Debug.DrawLine(rayStart, rayStart + dir * raycastRange, Color.green);
            }
        }
        
        // Kiểm tra điều kiện mất dấu và hết thời gian Chase
        if (targetDetected  && chaseDuration <= 0f)
        {
            targetDetected = false;
            isReturningToStayArea = true;
            agent.SetDestination(initialPosition);
            agent.speed = returnSpeed;
        }
    }

    public void Chase()
    {
        if (player == null) return;
        
        // 1. Kiểm tra thời gian Chase & Bán kính tối đa
        if(chaseDuration > 0f)
        {
            chaseDuration -= Time.deltaTime;
        }
        
        float distanceToInitial = Vector3.Distance(transform.position, initialPosition);
        
        if (distanceToInitial > maxChaseRadius || chaseDuration <= 0f)
        {
            // Hết thời gian Chase HOẶC ra khỏi bán kính cho phép -> Bắt đầu quay về
            targetDetected = false;
            isReturningToStayArea = true;
            agent.SetDestination(initialPosition);
            agent.speed = returnSpeed;
            SetAnimation("walk"); 
            return;
        }

        // 2. Thực hiện Chase
        agent.speed = runSpeed;
        float distanceToPlayer = Vector3.Distance(transform.position, player.transform.position);

        if(distanceToPlayer > 1f) 
        {
            SetAnimation("run");
            agent.SetDestination(player.transform.position);
        }
        else // Đã chạm tới/rất gần Player
        {
            // LOGIC ĐÁNH BẠI PLAYER
            if (playerManager.isDied == false)
            {
                playerManager.isDied = true; 
            }

            // Dừng AI và bắt đầu quá trình quay về
            targetDetected = false; 
            isReturningToStayArea = true;
            agent.SetDestination(initialPosition);
            agent.speed = returnSpeed;
            SetAnimation("idle");
        }
    }

    // --- Xử lý Trạng thái Quay về Khu vực Stay ---
    public void ReturnToStayArea()
    {
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
                IdleTime = Random.Range(minMaxIdleTime.x, minMaxIdleTime.y); // Bắt đầu thời gian idle
                
                // Kích hoạt quét nếu trạng thái là Stationary
                if (currentMovementState == MovementState.Stationary)
                {
                    SetRandomScanRotation();
                }
                
                // Khởi tạo lại trạng thái di chuyển thông thường ban đầu
                InitializeMovementState(); 
            }
        }
    }

    // =========================================================================
    //                                   GIZMOS
    // =========================================================================

    private void OnDrawGizmosSelected()
    {
        // Lấy vị trí hiện tại của AI
        Vector3 position = transform.position;

        // 1. VẼ VÙNG HOẠT ĐỘNG TỐI ĐA (MAX CHASE RADIUS)
        Gizmos.color = Color.yellow * 0.5f; 
        
        Vector3 centerPosition = (Application.isPlaying && initialPosition != Vector3.zero) ? initialPosition : position;
        Gizmos.DrawWireSphere(centerPosition, maxChaseRadius);
        
        // Đánh dấu Vị trí Quay về Ban đầu (Initial Position)
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
            // Màu Đỏ: Đang Chase Player
            Gizmos.color = Color.red;
            Gizmos.DrawLine(position, player.transform.position);
            Gizmos.DrawWireSphere(player.transform.position, 0.5f);
            return;
        }
        
        if (isReturningToStayArea)
        {
            // Màu Cam: Đang quay về vùng Stay
            Gizmos.color = Color.Lerp(Color.red, Color.yellow, 0.5f); 
            Gizmos.DrawLine(position, centerPosition);
            Gizmos.DrawWireSphere(centerPosition, 0.5f);
            return;
        }
        
        // VẼ CHO CÁC TRẠNG THÁI DI CHUYỂN THÔNG THƯỜNG
        switch (currentMovementState)
        {
            case MovementState.RandomMove:
                // Màu Xanh dương: Vùng tìm kiếm điểm ngẫu nhiên
                Gizmos.color = Color.blue;
                Gizmos.DrawWireSphere(position, targetRadius); 
                
                // Vẽ đường đến điểm đích (nếu đang di chuyển)
                if (agent != null && agent.hasPath)
                {
                    Gizmos.color = Color.cyan;
                    Gizmos.DrawLine(position, agent.destination);
                    Gizmos.DrawSphere(agent.destination, 0.3f);
                }
                break;

            case MovementState.Stationary:
                // Màu Trắng: Đang đứng yên
                Gizmos.color = Color.white;
                Gizmos.DrawWireSphere(position, 0.5f); 

                // Mới: Vẽ góc quét mục tiêu
                Gizmos.color = Color.magenta;
                // Chỉ vẽ nếu targetScanRotation đã được khởi tạo
                if (Application.isPlaying && targetScanRotation != Quaternion.identity) 
                {
                    Vector3 scanDirection = targetScanRotation * Vector3.forward;
                    Gizmos.DrawRay(rayStart, scanDirection * 3f); 
                }
                break;

            case MovementState.Patrol:
                // Màu Tím: Đường tuần tra
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