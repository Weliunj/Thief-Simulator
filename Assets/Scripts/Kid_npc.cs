using ithappy.Animals_FREE;
using StarterAssets;
using UnityEngine;
using UnityEngine.AI;

public class Kid : MonoBehaviour
{
    // Các Component cần thiết
    private Animator animator;
    private string currAnimState;
    private NavMeshAgent agent;
    private ThirdPersonController player;

    public float walkSpeed = 1.5f; // Tốc độ đi bộ
    public float runSpeed = 3f;  // Tốc độ chạy

    // Biến để quản lý trạng thái
    public float targetRadius = 20f; // Bán kính tìm kiếm điểm đến ngẫu nhiên
    public float stoppingDistanceThreshold = 0.5f; // Ngưỡng dừng để chuyển hoạt ảnh từ đi sang đứng
    
    private float IdleTime = 0f;
    public Vector2 minMaxIdleTime = new Vector2(2f, 5f); // Thời gian đứng yên ngẫu nhiên (Min/Max)

    public float raycastRangePublic = 15f; // Khoảng cách raycast để phát hiện mục tiêu
    private float raycastRange = 15f;

    public float raycastAngle = 30f; // Góc raycast để phát hiện mục tiêu
    private bool targetDetected = false;

    public Vector2 callDurationPublic = new Vector2(5f, 10f); // Thời gian theo đuổi mục tiêu
    private float callDuration = 0f;  
    public float callRanger = 20f;

    void Start()
    {
        // 1. Lấy Component
        agent = GetComponent<NavMeshAgent>();
        animator = GetComponent<Animator>();
        player = FindAnyObjectByType<ThirdPersonController>();
        // Kiểm tra xem các component có tồn tại không
        if (agent == null)
        {
            Debug.LogError("NavMeshAgent component không được tìm thấy trên GameObject này.");
            enabled = false; // Tắt script nếu thiếu
            return;
        }

        if (animator == null)
        {
            Debug.LogWarning("Animator component không được tìm thấy.");
        }

        // Tắt tính năng tự động xoay của NavMeshAgent để Animator có thể kiểm soát hướng
        // (Tùy chọn, nếu bạn muốn Animator xoay nhân vật)
        // agent.updateRotation = false; 

        // Bắt đầu di chuyển ngay lập tức
        SetRandomDestination();
        IdleTime = Random.Range(2f, 5f);
    }

    void Update()
    {
        if (agent == null || !agent.enabled) return;

        // 2. Kiểm tra trạng thái NavMeshAgent
        // Agent.hasPath là true nếu nó đang tính toán hoặc di chuyển đến đích
        
        RayCastHitTarget();
        if(targetDetected)  { return; }
        // Kiểm tra xem AI đã gần đến đích chưa (dựa trên stoppingDistance đã được thiết lập)
        if (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance + stoppingDistanceThreshold )
        {
            // Đã đến đích hoặc gần đến đích
            // Chuyển sang hoạt ảnh Đứng (Set IsWalking = false)
            SetAnimation("idle");
            
            if(IdleTime > 0f)
            {
                IdleTime -= Time.deltaTime;
                return; // Chờ cho đến khi hết thời gian đứng yên
            }
            else
            {
                // 3. Hết thời gian chờ -> Thiết lập mục tiêu mới
                IdleTime = Random.Range(minMaxIdleTime.x, minMaxIdleTime.y);
            }

            // Đặt mục tiêu mới sau một khoảng thời gian (có thể dùng Invoke hoặc Coroutine để trễ)
            // Hiện tại, code này đặt mục tiêu mới ngay lập tức
            SetRandomDestination();
        }
        else
        {
            // Đang trên đường đi
            // Chuyển sang hoạt ảnh Đi (Set IsWalking = true)
            agent.speed = walkSpeed;
            SetAnimation("walk");
        }
    }
    public void RayCastHitTarget()
    {
        if(player.Crouching) { raycastRange = raycastRangePublic / 2f; }
        else { raycastRange = raycastRangePublic; }

        Vector3 rayStart = transform.position + Vector3.up;
        var center = Physics.Raycast(rayStart, transform.forward, out RaycastHit hit, raycastRange);
        
        Vector3 leftDirection = Quaternion.AngleAxis(-raycastAngle, Vector3.up) * transform.forward;
        var left = Physics.Raycast(rayStart, leftDirection, out RaycastHit hitLeft, raycastRange);
        Vector3 rightDirection = Quaternion.AngleAxis(raycastAngle, Vector3.up) * transform.forward;
        var right = Physics.Raycast(rayStart, rightDirection, out RaycastHit hitRight, raycastRange);

        Vector3 upDirection = Quaternion.AngleAxis(raycastAngle, Vector3.right) * transform.forward;
        var up = Physics.Raycast(rayStart, upDirection, out RaycastHit hitUp, raycastRange);
        Vector3 downDirection = Quaternion.AngleAxis(-raycastAngle, Vector3.right) * transform.forward;
        var down = Physics.Raycast(rayStart, downDirection, out RaycastHit hitDown, raycastRange);

        if(center)
        {
            Debug.DrawLine(rayStart, hit.point, Color.red);
            if(hit.collider.CompareTag("Player"))
            {
                targetDetected = true;
                callDuration = Random.Range(callDurationPublic.x, callDurationPublic.y); // Reset thời gian theo đuổi
            }
        }
        else
        {
            Debug.DrawLine(rayStart, rayStart + transform.forward * raycastRange, Color.green);
        }
        if(left)
        {
            Debug.DrawLine(rayStart, hitLeft.point, Color.red);
            if(hitLeft.collider.CompareTag("Player"))
            {
                targetDetected = true;
                callDuration = Random.Range(callDurationPublic.x, callDurationPublic.y)  ; // Reset thời gian theo đuổi
            }
        }
        else
        {
            Debug.DrawLine(rayStart, rayStart + leftDirection * raycastRange, Color.green);
        }
        if(right)
        {
            Debug.DrawLine(rayStart, hitRight.point, Color.red);
            if(hitRight.collider.CompareTag("Player"))
            {
                targetDetected = true;
                callDuration = Random.Range(callDurationPublic.x, callDurationPublic.y); // Reset thời gian theo đuổi
            }
        }
        else
        {
            Debug.DrawLine(rayStart, rayStart + rightDirection * raycastRange, Color.green);
        }
        if(up)
        {
            Debug.DrawLine(rayStart, hitUp.point, Color.red);
            if(hitUp.collider.CompareTag("Player"))
            {
                targetDetected = true;
                callDuration = Random.Range(callDurationPublic.x, callDurationPublic.y); // Reset thời gian theo đuổi
            }
        }
        else
        {
            Debug.DrawLine(rayStart, rayStart + upDirection * raycastRange, Color.green);
        }
        if(down)
        {
            Debug.DrawLine(rayStart, hitDown.point, Color.red);
            if(hitDown.collider.CompareTag("Player"))
            {
                targetDetected = true;
                callDuration = Random.Range(callDurationPublic.x, callDurationPublic.y); // Reset thời gian theo đuổi
            }
        }
        else
        {
            Debug.DrawLine(rayStart, rayStart + downDirection * raycastRange, Color.green);
        }
        Call();
    }
    public void Call()
    {
        if(callDuration > 0f)
        {
            callDuration -= Time.deltaTime;
        }

        if(targetDetected && callDuration > 0f)
        {
            agent.speed = runSpeed;
            SetAnimation("run");
            if (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance + stoppingDistanceThreshold )
            {
                SetRandomDestination();
            }
            Collider[] colliders = Physics.OverlapSphere(transform.position, callRanger);
                foreach (var collider in colliders)
                {
                    if(collider.CompareTag("adult"))
                    {
                        AI_Move_NavMesh adultNpc = collider.GetComponent<AI_Move_NavMesh>();
                        if(adultNpc != null)
                        {
                            adultNpc.targetDetected = true;
                            adultNpc.chaseDuration = Random.Range(
                                adultNpc.chaseDurationPublic.x, 
                                adultNpc.chaseDurationPublic.y);
                        }
                    }
                }
            
        }
        else if(callDuration <= 0f)
        {
            targetDetected = false;     //idle 2 giay sau do lai di nhu npc
        }
    }
    private void SetAnimation(string name)
    {
        if (currAnimState == name)
        {
            return; // Tránh gọi lại nếu trạng thái không thay đổi
        }
        animator.SetTrigger(name);
        currAnimState = name;
    }

    // Hàm tìm và thiết lập điểm đến ngẫu nhiên
    private void SetRandomDestination()
    {
        Vector3 randomPoint;
        
        // Sử dụng hàm tiện ích để tìm điểm ngẫu nhiên trên NavMesh
        if (GetRandomPoint(transform.position, targetRadius, out randomPoint))
        {
            // Thiết lập đích đến cho NavMeshAgent
            agent.SetDestination(randomPoint);
            Debug.Log($"AI đang di chuyển đến vị trí ngẫu nhiên: {randomPoint}");
        }
        else
        {
            Debug.LogWarning("Không thể tìm thấy vị trí ngẫu nhiên hợp lệ trên NavMesh.");
        }
    }

    // Hàm hỗ trợ tìm kiếm điểm ngẫu nhiên trên NavMesh (Tương tự như giải thích trước)
    private bool GetRandomPoint(Vector3 center, float range, out Vector3 result)
    {
        Vector3 randomDirection = Random.insideUnitSphere * range;
        randomDirection += center;
        
        NavMeshHit hit;
        // Kiểm tra xem điểm ngẫu nhiên có nằm trên NavMesh không
        if (NavMesh.SamplePosition(randomDirection, out hit, range, NavMesh.AllAreas))
        {
            result = hit.position;
            return true;
        }

        result = Vector3.zero;
        return false;
    }

    public void OnDrawGizmos()
    {
        if(targetDetected && callDuration > 0f)
        {
            Gizmos.DrawWireSphere(transform.position, callRanger);
        }
    }
}
    

    