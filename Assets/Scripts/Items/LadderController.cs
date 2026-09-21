using StarterAssets;
using UnityEngine;

/// <summary>
/// Quản lý chức năng leo trèo của Thang (Ladder).
/// Triển khai IInteractable:
/// - Khi tâm ngắm rọi vào thang (điểm A hoặc điểm B), nút InteractBtn trên Mobile sẽ tự động hiện lên.
/// - Bấm nút InteractBtn (hoặc phím E/F) để bám thang leo LÊN hoặc leo XUỐNG.
/// - Cơ chế Di chuyển theo Ray (climbProgress 0 -> 1): Giúp leo mượt mà trên MỌI góc thang nghiêng, không bị kẹt va chạm Collider.
/// - Tự động bỏ qua va chạm (IgnoreCollision) giữa Player và Thang khi đang bám.
/// - Hỗ trợ Tuột thang nhanh (Slide Down) khi giữ Crouch / Sprint / kéo mạnh cần xuống.
/// - Chống spam nhảy (Jump Cooldown) và đẩy lùi an toàn khi nhảy rời thang.
/// </summary>
public class LadderController : MonoBehaviour, IInteractable
{
    [Header("📍 Climb Points (Điểm chân thang và đỉnh thang)")]
    [Tooltip("Điểm bắt đầu ở chân thang (A)")]
    public Transform pointA;

    [Tooltip("Điểm kết thúc ở đỉnh thang (B)")]
    public Transform pointB;

    [Header("⚙️ Climb Settings")]
    [Tooltip("Tốc độ di chuyển trèo thang cơ bản (m/s)")]
    [Range(0.5f, 5f)]
    public float climbSpeed = 2.0f;

    [Tooltip("Hệ số tốc độ Animation khi trèo LÊN")]
    [Range(0.2f, 5f)]
    public float animClimbUpSpeed = 1.0f;

    [Tooltip("Hệ số tốc độ Animation khi trèo XUỐNG")]
    [Range(0.2f, 5f)]
    public float animClimbDownSpeed = 1.0f;

    [Header("🦘 Jump Off Settings")]
    [Tooltip("Lực đẩy lùi ra sau khi bấm nút nhảy để rời khỏi thang")]
    [Range(0.5f, 8f)]
    public float jumpOffPushForce = 3.0f;

    [Tooltip("Lực nâng nhẹ lên trên khi nhảy rời khỏi thang")]
    [Range(0.0f, 4f)]
    public float jumpOffUpForce = 1.2f;

    [Tooltip("Thời gian chờ trước khi có thể bám lại thang sau khi nhảy thoát ra (tránh spam nhảy)")]
    public float reClimbCooldown = 0.45f;

    [Tooltip("Khoảng cách bước về phía trước khi leo lên tới đỉnh thang")]
    public float topExitForwardOffset = 0.6f;

    [Tooltip("Khoảng cách tối đa để bấm phím bắt đầu leo")]
    public float interactDistance = 2.5f;

    [Tooltip("Phím bấm thủ công để bắt đầu leo thang (Desktop)")]
    public KeyCode climbKey = KeyCode.F;

    [Tooltip("Vị trí bám khi bắt đầu leo xuống từ đỉnh thang (0.0 - 1.0, ví dụ 0.90 để bám thấp hơn đỉnh một chút tránh bám vào hư không)")]
    [Range(0.7f, 1.0f)]
    public float topAttachProgress = 0.90f;

    [Tooltip("Vị trí bám khi bắt đầu leo lên từ chân thang (0.0 - 1.0, 0.0 = ngay tại vị trí bottomPoint đã kéo ngang người)")]
    [Range(-0.7f, 1.0f)]
    public float bottomAttachProgress = 0.0f;

    [Tooltip("Đảo ngược 180 độ hướng mặt thang khi trèo (Bật để quay ngược hướng bám thang)")]
    public bool invertFacingDirection = true;

    [Header("⚙️ Ladder Permissions")]
    [Tooltip("Cho phép leo thang hay không. Nếu đang leo mà canClimb = false thì người chơi sẽ lập tức buông tay rơi tự do")]
    public bool canClimb = true;

    [Header("🎯 Trạng thái")]
    public bool isClimbing = false;

    private PlayerController playerController;
    private StarterAssetsInputs starterInputs;
    private MobileActionButtons mobileActions;
    private CharacterController characterController;
    private Camera mainCam;

    private bool hasClimbSpeedParam = false;
    private bool checkedAnimatorParams = false;
    private float climbNormalizedTime = 0f;
    private float climbProgress = 0f; // 0 = Chân thang (A), 1 = Đỉnh thang (B)
    private float reClimbCooldownTimer = 0f;

    void Awake()
    {
        EnsureUpright();
    }

    void Start()
    {
        EnsureUpright();
        InitializePoints();
        FindPlayerReferences();
    }

    void OnEnable()
    {
        EnsureUpright();
    }

    /// <summary>
    /// Đảm bảo thang luôn đứng thẳng 100%, khóa góc nghiêng X và Z để thang không bao giờ bị đổ ngã
    /// </summary>
    public void EnsureUpright()
    {
        Vector3 angles = transform.eulerAngles;
        transform.eulerAngles = new Vector3(0f, angles.y, 0f);

        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
            if (!rb.isKinematic)
            {
                rb.angularVelocity = Vector3.zero;
            }
        }
    }

    void OnDisable()
    {
        if (isClimbing)
        {
            StopClimbing();
        }
    }

    private void FindPlayerReferences()
    {
        if (playerController == null)
        {
            playerController = FindFirstObjectByType<PlayerController>();
        }
        if (playerController != null)
        {
            if (starterInputs == null)
            {
                starterInputs = playerController.GetComponent<StarterAssetsInputs>();
            }
            if (characterController == null)
            {
                characterController = playerController.GetComponent<CharacterController>();
            }
            if (!checkedAnimatorParams && playerController._animator != null)
            {
                hasClimbSpeedParam = HasAnimatorParameter(playerController._animator, "ClimbSpeed");
                checkedAnimatorParams = true;
            }
        }
        if (mobileActions == null)
        {
            mobileActions = FindFirstObjectByType<MobileActionButtons>();
        }
        if (mainCam == null)
        {
            mainCam = Camera.main;
            if (mainCam == null) mainCam = FindFirstObjectByType<Camera>();
        }
    }

    private bool HasAnimatorParameter(Animator anim, string paramName)
    {
        if (anim == null) return false;
        foreach (AnimatorControllerParameter param in anim.parameters)
        {
            if (param.name == paramName) return true;
        }
        return false;
    }

    /// <summary>
    /// Tự động tìm hoặc tạo điểm A (chân thang) và B (đỉnh thang) nếu chưa gán
    /// </summary>
    public void InitializePoints()
    {
        if (pointA == null)
        {
            Transform foundA = transform.Find("A") ?? transform.Find("PointA") ?? transform.Find("Bottom");
            if (foundA != null) pointA = foundA;
            else
            {
                GameObject aObj = new GameObject("PointA");
                aObj.transform.SetParent(transform, false);
                aObj.transform.localPosition = new Vector3(0, 0.2f, 0.1f);
                pointA = aObj.transform;
            }
        }

        if (pointB == null)
        {
            Transform foundB = transform.Find("B") ?? transform.Find("PointB") ?? transform.Find("Top");
            if (foundB != null) pointB = foundB;
            else
            {
                GameObject bObj = new GameObject("PointB");
                bObj.transform.SetParent(transform, false);
                bObj.transform.localPosition = new Vector3(0, 2.5f, 0.1f);
                pointB = bObj.transform;
            }
        }

        // Đảm bảo PointA và PointB có Trigger Collider để tâm ngắm Raycast từ cả trên nóc nhà lẫn dưới đất đều bắt trúng dễ dàng
        EnsureTriggerCollider(pointA.gameObject, 0.45f);
        EnsureTriggerCollider(pointB.gameObject, 0.45f);
    }

    private void EnsureTriggerCollider(GameObject obj, float radius)
    {
        if (obj == null) return;
        Collider col = obj.GetComponent<Collider>();
        if (col == null)
        {
            SphereCollider sc = obj.AddComponent<SphereCollider>();
            sc.isTrigger = true;
            sc.radius = radius;
        }
    }

    void Update()
    {
        if (reClimbCooldownTimer > 0f)
        {
            reClimbCooldownTimer -= Time.deltaTime;
        }

        if (playerController == null)
        {
            FindPlayerReferences();
            if (playerController == null) return;
        }

        // 1. Kiểm tra bấm phím leo thủ công khi đứng gần (Fallback nếu không dùng Raycast)
        if (!isClimbing && !playerController.isClimbingLadder && reClimbCooldownTimer <= 0f)
        {
            CheckClimbInput();
        }

        // 2. Xử lý toàn bộ quá trình tương tác khi đang leo thang
        if (isClimbing)
        {
            if (!canClimb || (playerController != null && !playerController.canClimb))
            {
                FallOffLadder();
                return;
            }
            PerformClimbing();
        }
    }

    /// <summary>
    /// Kiểm tra người chơi có đứng gần chân/đỉnh thang và bấm phím leo (F / Mobile Interact) không
    /// </summary>
    private void CheckClimbInput()
    {
        if (pointA == null || pointB == null || playerController == null) return;

        float distToA = Vector3.Distance(playerController.transform.position, pointA.position);
        float distToB = Vector3.Distance(playerController.transform.position, pointB.position);

        bool nearA = distToA <= interactDistance;
        bool nearB = distToB <= interactDistance;

        if (!nearA && !nearB) return;

        // Chỉ bắt đầu leo khi người chơi CHỦ ĐỘNG BẤM PHÍM (F hoặc nút Interact trên Mobile)
        bool manualClimbPressed = Input.GetKeyDown(climbKey);
        if (mobileActions != null && mobileActions.interactPressed)
        {
            manualClimbPressed = true;
        }

        if (manualClimbPressed)
        {
            Transform startPoint = (distToB < distToA) ? pointB : pointA;
            StartClimbing(startPoint);
        }
    }

    /// <summary>
    /// Bắt đầu gắn người chơi vào thang
    /// </summary>
    public void StartClimbing(Transform startPoint)
    {
        if (!canClimb || playerController == null || pointA == null || pointB == null || reClimbCooldownTimer > 0f) return;
        if (playerController != null && !playerController.canClimb) return;

        FindPlayerReferences();
        isClimbing = true;
        playerController.isClimbingLadder = true;

        // Tắt hiển thị model 3D đang cầm trên Hotbar (nếu có) để 2 tay rảnh bám thang leo
        if (playerController.hotbarManager != null)
        {
            playerController.hotbarManager.DeselectAll();
        }
        else if (playerController.heldItem != null)
        {
            foreach (var item in playerController.heldItem)
            {
                if (item != null) item.SetActive(false);
            }
        }

        // Tạm thời vô hiệu hoá va chạm giữa Player và Thang để leo thang nghiêng không bị kẹt
        SetLadderCollisionsIgnored(true);

        // Đảm bảo thứ tự điểm chân A (thấp) và đỉnh B (cao)
        Transform bottomPoint = (pointA.position.y <= pointB.position.y) ? pointA : pointB;
        Transform topPoint = (pointA.position.y <= pointB.position.y) ? pointB : pointA;

        if (startPoint == topPoint)
        {
            climbProgress = topAttachProgress;
            climbNormalizedTime = 0.90f;
        }
        else
        {
            climbProgress = bottomAttachProgress;
            climbNormalizedTime = 0.15f; // Frame tay chân bám chắc vào bậc thang
        }

        // Đặt vị trí chính xác trên ray thang
        Vector3 targetPos = Vector3.Lerp(bottomPoint.position, topPoint.position, climbProgress);

        if (characterController != null) characterController.enabled = false;
        playerController.transform.position = targetPos;
        if (characterController != null) characterController.enabled = true;

        // Hướng mặt thang bám leo (Đảo ngược theo invertFacingDirection)
        Vector3 baseFacing = invertFacingDirection ? transform.forward : -transform.forward;
        Vector3 toLadder = (transform.position - playerController.transform.position);
        toLadder.y = 0f;

        Vector3 faceDir = baseFacing;
        if (toLadder.sqrMagnitude > 0.001f)
        {
            // Nếu người chơi tiếp cận từ phía đối diện, đảo hướng nhìn để luôn nhìn vào mặt thang phía mình
            if (Vector3.Dot(toLadder, baseFacing) > 0f)
            {
                faceDir = baseFacing;
            }
            else
            {
                faceDir = -baseFacing;
            }
        }
        faceDir.y = 0f;

        if (faceDir.sqrMagnitude < 0.001f)
        {
            faceDir = (toLadder.sqrMagnitude > 0.001f) ? toLadder.normalized : baseFacing;
        }

        Quaternion lookRot = Quaternion.LookRotation(faceDir);
        playerController.transform.rotation = lookRot;
        playerController.SetLadderCameraFacing(lookRot.eulerAngles.y);

        // Kích hoạt ngay lập tức tư thế bám thang (Climb Idle Pose)
        if (playerController._animator != null)
        {
            playerController._animator.SetBool("Climb", true);
            playerController._animator.Play("Climb", 0, climbNormalizedTime);
            playerController._animator.Update(0f);
            playerController._animator.speed = 0f;
        }
    }

    /// <summary>
    /// Xử lý di chuyển theo Ray, animation và nhảy thoát khỏi thang
    /// </summary>
    private void PerformClimbing()
    {
        // 1. Kiểm tra bấm nút Nhảy để THOÁT KHỎI THANG (Space hoặc Jump Mobile)
        bool jumpPressed = false;
        if (starterInputs != null && starterInputs.jump)
        {
            jumpPressed = true;
            starterInputs.jump = false; // Reset cờ input
        }
        else if (Input.GetKeyDown(KeyCode.Space) || (mobileActions != null && mobileActions.jumpPressed))
        {
            jumpPressed = true;
        }

        if (jumpPressed)
        {
            JumpOffLadder();
            return;
        }

        // 2. Đọc tín hiệu di chuyển dọc (W / S hoặc Analog Joystick)
        float verticalInput = 0f;
        if (starterInputs != null)
        {
            verticalInput = starterInputs.move.y;
        }
        else
        {
            verticalInput = Input.GetAxisRaw("Vertical");
        }

        // Đảm bảo thứ tự độ cao A (dưới) và B (trên)
        Transform bottomPoint = (pointA.position.y <= pointB.position.y) ? pointA : pointB;
        Transform topPoint = (pointA.position.y <= pointB.position.y) ? pointB : pointA;

        float ladderLength = Vector3.Distance(bottomPoint.position, topPoint.position);
        if (ladderLength < 0.1f) ladderLength = 0.1f;

        // 3. Xử lý Trèo LÊN (W / Joystick Up)
        if (verticalInput > 0.1f)
        {
            float delta = (climbSpeed * verticalInput * Time.deltaTime) / ladderLength;
            climbProgress = Mathf.Clamp01(climbProgress + delta);

            // Cập nhật vị trí trực tiếp theo ray thang
            Vector3 railPos = Vector3.Lerp(bottomPoint.position, topPoint.position, climbProgress);
            SetPlayerPosition(railPos);

            // Cập nhật tốc độ Animation tiến xuôi
            if (playerController._animator != null)
            {
                if (hasClimbSpeedParam)
                {
                    playerController._animator.speed = 1.0f;
                    playerController._animator.SetFloat("ClimbSpeed", animClimbUpSpeed * Mathf.Abs(verticalInput));
                }
                else
                {
                    playerController._animator.speed = animClimbUpSpeed * Mathf.Abs(verticalInput);
                    AnimatorStateInfo stateInfo = playerController._animator.GetCurrentAnimatorStateInfo(0);
                    climbNormalizedTime = stateInfo.normalizedTime % 1.0f;
                }
            }

            // Kiểm tra chạm đỉnh thang B
            if (climbProgress >= 0.98f)
            {
                ExitTopLadder(topPoint);
                return;
            }
        }
        // 4. Xử lý Trèo XUỐNG (S / Joystick Down)
        else if (verticalInput < -0.1f)
        {
            float delta = (climbSpeed * Mathf.Abs(verticalInput) * Time.deltaTime) / ladderLength;
            climbProgress = Mathf.Clamp01(climbProgress - delta);

            // Cập nhật vị trí trực tiếp theo ray thang
            Vector3 railPos = Vector3.Lerp(bottomPoint.position, topPoint.position, climbProgress);
            SetPlayerPosition(railPos);

            // Cập nhật Animation tua ngược (Không gán Animator.speed âm để tránh lỗi Unity)
            if (playerController._animator != null)
            {
                if (hasClimbSpeedParam)
                {
                    playerController._animator.speed = 1.0f;
                    playerController._animator.SetFloat("ClimbSpeed", -animClimbDownSpeed * Mathf.Abs(verticalInput));
                }
                else
                {
                    playerController._animator.speed = 0f;
                    climbNormalizedTime -= Time.deltaTime * animClimbDownSpeed * Mathf.Abs(verticalInput);
                    if (climbNormalizedTime < 0f) climbNormalizedTime += 1.0f;

                    playerController._animator.Play("Climb", 0, climbNormalizedTime);
                }
            }

            // Kiểm tra chạm chân thang A
            if (climbProgress <= 0.02f)
            {
                StopClimbing();
                return;
            }
        }
        // 5. ĐỨNG YÊN trên thang (khi nhả phím)
        else
        {
            if (playerController._animator != null)
            {
                playerController._animator.speed = 0f; // Đóng băng animation tại frame hiện tại
                if (hasClimbSpeedParam)
                {
                    playerController._animator.SetFloat("ClimbSpeed", 0f);
                }
            }
        }
    }

    private void SetPlayerPosition(Vector3 pos)
    {
        if (characterController != null)
        {
            characterController.enabled = false;
            playerController.transform.position = pos;
            characterController.enabled = true;
        }
        else
        {
            playerController.transform.position = pos;
        }
    }

    /// <summary>
    /// Nhảy thoát khỏi thang: huỷ trạng thái leo, bật lùi ra phía sau lưng và kích hoạt cooldown chống spam
    /// </summary>
    public void JumpOffLadder()
    {
        if (playerController == null) return;

        // Đẩy lùi về phía sau lưng người chơi (xa thang) và 1 lực nâng nhẹ
        Vector3 pushVector = -playerController.transform.forward * jumpOffPushForce + Vector3.up * jumpOffUpForce;

        StopClimbing();

        // Kích hoạt cooldown không cho bám lại thang ngay lập tức (chống spam nhảy)
        reClimbCooldownTimer = reClimbCooldown;

        if (characterController != null && characterController.enabled)
        {
            characterController.Move(pushVector * 0.2f);
        }
        else
        {
            playerController.transform.position += pushVector * 0.2f;
        }
    }

    /// <summary>
    /// Bước ra khỏi đỉnh thang khi trèo lên tới nóc/sàn
    /// </summary>
    private void ExitTopLadder(Transform topPoint)
    {
        if (characterController != null) characterController.enabled = false;

        // Hướng bước ra khỏi đỉnh thang: bước về phía trước theo hướng mặt nhân vật đang bám thang (tiến lên sàn/nóc)
        Vector3 defaultStep = invertFacingDirection ? transform.forward : -transform.forward;
        Vector3 forwardStep = (playerController != null) ? playerController.transform.forward : defaultStep;
        forwardStep.y = 0f;
        if (forwardStep.sqrMagnitude > 0.001f)
        {
            forwardStep.Normalize();
        }
        else
        {
            forwardStep = defaultStep;
            forwardStep.y = 0f;
        }

        Vector3 exitPos = topPoint.position + forwardStep * topExitForwardOffset + Vector3.up * 0.1f;
        playerController.transform.position = exitPos;

        if (characterController != null) characterController.enabled = true;

        StopClimbing();
    }

    /// <summary>
    /// Buông tay rơi tự do khỏi thang (khi canClimb bị tắt hoặc bị tác động ngoại cảnh)
    /// </summary>
    public void FallOffLadder()
    {
        StopClimbing();
        reClimbCooldownTimer = reClimbCooldown;
    }

    /// <summary>
    /// Kết thúc trạng thái leo thang, hồi phục tốc độ Animator và va chạm
    /// </summary>
    public void StopClimbing()
    {
        isClimbing = false;
        SetLadderCollisionsIgnored(false);

        if (playerController != null)
        {
            playerController.isClimbingLadder = false;
            if (playerController._animator != null)
            {
                playerController._animator.speed = 1.0f; // Khôi phục tốc độ animation bình thường
                playerController._animator.SetBool("Climb", false);
                if (hasClimbSpeedParam)
                {
                    playerController._animator.SetFloat("ClimbSpeed", 1.0f);
                }
            }
        }
    }

    /// <summary>
    /// Bật/tắt bỏ qua va chạm giữa Player và Collider của Thang khi đang bám
    /// </summary>
    private void SetLadderCollisionsIgnored(bool ignore)
    {
        if (playerController == null) return;

        Collider[] ladderColliders = GetComponentsInChildren<Collider>();
        Collider[] playerColliders = playerController.GetComponentsInChildren<Collider>();

        foreach (var lc in ladderColliders)
        {
            if (lc == null) continue;
            foreach (var pc in playerColliders)
            {
                if (pc == null) continue;
                Physics.IgnoreCollision(pc, lc, ignore);
            }
        }
    }

    // =========================================================================
    //                        IINTERACTABLE IMPLEMENTATION
    // =========================================================================

    public string GetInteractableName() => "Ladder";

    public string GetActionPrompt()
    {
        if (playerController == null) playerController = FindFirstObjectByType<PlayerController>();
        if (playerController != null && pointA != null && pointB != null)
        {
            float distToA = Vector3.Distance(playerController.transform.position, pointA.position);
            float distToB = Vector3.Distance(playerController.transform.position, pointB.position);
            return (distToB < distToA) ? "Climb Down" : "Climb Up";
        }
        return "Climb";
    }

    public int GetPrice() => 0;

    public int GetWeight() => 0;

    /// <summary>
    /// False: Báo hiệu đây là đối tượng tương tác đặc biệt -> Kích hoạt nút InteractBtn trên Mobile
    /// </summary>
    public bool IsLootItem() => false;

    public bool CanInteract(PlayerController player, out string failReason)
    {
        failReason = "";
        if (!canClimb || (player != null && !player.canClimb))
        {
            failReason = "Cannot climb";
            return false;
        }
        if (isClimbing)
        {
            failReason = "Already climbing";
            return false;
        }
        if (reClimbCooldownTimer > 0f)
        {
            failReason = "";
            return false;
        }
        return true;
    }

    public string GetDescription() => "A sturdy ladder for climbing up and down.";

    public Sprite GetIcon() => null;

    public ItemRarity GetRarity() => ItemRarity.Common;

    public void Interact(PlayerController player)
    {
        if (playerController == null) playerController = player;
        if (pointA == null || pointB == null || reClimbCooldownTimer > 0f) return;

        // Xác định leo lên hay leo xuống dựa vào điểm gần người chơi hơn
        float distToA = Vector3.Distance(player.transform.position, pointA.position);
        float distToB = Vector3.Distance(player.transform.position, pointB.position);

        Transform startPoint = (distToB < distToA) ? pointB : pointA;
        StartClimbing(startPoint);
    }

    private void OnDrawGizmos()
    {
        if (pointA == null || pointB == null) return;

        Gizmos.color = Color.green;
        Gizmos.DrawLine(pointA.position, pointB.position);

        Gizmos.color = Color.cyan;
        Gizmos.DrawSphere(pointA.position, 0.08f);

        Gizmos.color = Color.magenta;
        Gizmos.DrawSphere(pointB.position, 0.08f);

        // Vẽ hướng bước ra ở đỉnh thang (hướng bước tới phía trước theo mặt thang)
        Gizmos.color = Color.yellow;
        Vector3 gizmoStep = invertFacingDirection ? transform.forward : -transform.forward;
        Gizmos.DrawRay(pointB.position, gizmoStep * topExitForwardOffset);
    }
}




