using UnityEngine;

public class AI_Move : MonoBehaviour
{
    private Animator animator;
    private string currAnim;
    private Rigidbody rb;

    [Header("Movement Area")]
    public GameObject Add_Corner;   // Góc gốc để tính phạm vi
    public int X;                   // Chiều rộng tối đa
    public int Z;                   // Chiều dài tối đa
    public float radius = 1f;

    [Header("Speed Settings")]
    public float Speed = 2f;
    private float currentSpeed;
    private float idleTime;

    [Header("Raycast")]
    public LayerMask LayerCol;

    private Vector3 Target;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        animator = GetComponentInChildren<Animator>();
        SetNewTarget();
    }

    void Update()
    {
        CheckObstacle();
        RotateToTarget();
        Move();
    }

    // ================== SET NEW TARGET ==================
    public void SetNewTarget()
    {
        Vector3 start = Add_Corner.transform.position;

        Target = new Vector3(
            Random.Range(start.x, start.x + X),
            transform.position.y,
            Random.Range(start.z, start.z + Z)
        );

        currentSpeed = (transform.localScale.x <= 0.5f) ? (Random.Range(0, 100) < 20) ? Speed + 4f : Speed : (Random.Range(0, 100) < 10) ? Speed + 3f : Speed;
        idleTime = (transform.localScale.x <= 0.5f) ? Random.Range(1f, 6f) : Random.Range(2f, 13f); 
    }

    // ================== OBSTACLE CHECK ==================
    private void CheckObstacle()
    {
        Vector3 rayStart = transform.position + Vector3.up;
        if (Physics.Raycast(rayStart, transform.forward, 1.5f, LayerCol))
        {
            SetNewTarget();
        }
    }

    // ================== ROTATION ==================
    private void RotateToTarget()
    {
        Vector3 dir = Target - transform.position;
        dir.y = 0;

        if (dir.sqrMagnitude < 0.01f)
            return;

        transform.rotation = Quaternion.LookRotation(dir);
    }

    // ================== MOVE ==================
    private void Move()
    {
        float distance = Vector3.Distance(transform.position, Target);

        if (distance > radius)
        {
            ChangeAnim(currentSpeed > Speed ? "run" : "walk");
            rb.linearVelocity = transform.forward * currentSpeed;
        }
        else
        {
            rb.linearVelocity = Vector3.zero;
            ChangeAnim("idle");

            idleTime -= Time.deltaTime;
            if (idleTime <= 0)
                SetNewTarget();
        }
    }

    // ================== ANIM ==================
    private void ChangeAnim(string name)
    {
        if (currAnim == name) return;

        currAnim = name;
        animator.SetTrigger(currAnim);
    }

    // ================== GIZMOS ==================
    private void OnDrawGizmos()
    {
        if (Add_Corner == null) return;

        // Vùng di chuyển
        Gizmos.color = Color.red;
        Vector3 p = Add_Corner.transform.position;

        // 4 cạnh (hình chữ nhật)
        Gizmos.DrawLine(p, new Vector3(p.x + X, p.y, p.z));
        Gizmos.DrawLine(p, new Vector3(p.x, p.y, p.z + Z));
        Gizmos.DrawLine(new Vector3(p.x + X, p.y, p.z), new Vector3(p.x + X, p.y, p.z + Z));
        Gizmos.DrawLine(new Vector3(p.x, p.y, p.z + Z), new Vector3(p.x + X, p.y, p.z + Z));

        // Vẽ target
        Gizmos.color = Color.green;
        Gizmos.DrawSphere(Target, radius);

        // Raycast
        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(transform.position + Vector3.up,
                        transform.position + Vector3.up + transform.forward * 1.5f);
    }
}

