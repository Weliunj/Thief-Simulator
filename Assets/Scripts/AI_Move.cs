/*
using UnityEngine;

public class AI_Move : MonoBehaviour
{
    private Animator animator;
    private string currAnim;

    public GameObject Sub_corner;
    public GameObject Add_Corner;
    public int X;
    public int Z;
    public Vector3 Target;
    public float radius;
    public float Speed;
    RaycastHit2D vatcan;
    Rigidbody rb;
    Vector3 direc;
    Vector3 rayStart;
    public LayerMask lm;
    private float currentSpeed = 2f;
    float idletime;
    void Start()
    {
        rb = GetComponent<Rigidbody>();
        animator = GetComponentInChildren<Animator>();
        Target = new Vector3(UnityEngine.Random.Range(Add_Corner.transform.position.x + 2, Add_Corner.transform.position.x + X), transform.position.y,
                            UnityEngine.Random.Range(Add_Corner.transform.position.z + 2, Add_Corner.transform.position.z + Z));
        idletime = Random.Range(3, 20);
    }


    void Update()
    {

        TargetM();
        Direc();
        Move();
    }
    public void SetnewTarget()
    {
        Target = new Vector3(UnityEngine.Random.Range(Add_Corner.transform.position.x + 2, Add_Corner.transform.position.x + X), transform.position.y,
                        UnityEngine.Random.Range(Add_Corner.transform.position.z + 2, Add_Corner.transform.position.z + Z));
        // Decision Logic
        int sprintChance = Random.Range(1, 101);
        if (sprintChance <= 10)
        {
            currentSpeed = Speed + 3f; // Sprint speed
        }
        else
        {
            currentSpeed = Speed; // Walk speed
        }

        // Reset idle timer
        idletime = Random.Range(3f, 13f); // Use floats for time
    }
    public void TargetM()
    {
        rayStart = transform.position + Vector3.up ;

        bool vatcan = Physics.Raycast(rayStart, transform.forward, 1.5f, lm);
        if(  vatcan )
        {
            SetnewTarget();
        }
    }
    public void Direc()
    {
        /*
            Quaternion.LookRotation()    Xoay Ngay Lập Tức
            Quaternion.Slerp()           Xoay Có Delay
        

        direc = Target - transform.position;
        direc.y = 0;

        if (direc.sqrMagnitude < 0.01f)
            return;

        Quaternion targetRotation = Quaternion.LookRotation(direc.normalized);
        transform.rotation = targetRotation;
    }
    public void Move()
    {

        if (Vector3.Distance(transform.position, Target) > radius)
        {
            if (currentSpeed > Speed) // Check if the AI is in sprint mode
            {
                ChangeAnim("run");
            }
            else
            {
                ChangeAnim("walk");
            }

            // FIX: Use rb.velocity instead of rb.linearVelocity
            rb.linearVelocity = transform.forward * currentSpeed;
        }
        else
        {
            rb.linearVelocity = Vector3.zero;
            ChangeAnim("idle");
            if (idletime > 0)
            {
                idletime -= Time.deltaTime;
            }
            else
            {
                SetnewTarget();
            }
            
        }
        
        
    }
    public void ChangeAnim(string name)
    {
        if(currAnim != name)
        {
            currAnim = name;
            animator.SetTrigger(currAnim);
        }
    }
    private void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        //Add
        Gizmos.DrawLine(Add_Corner.transform.position, new Vector3(X + Add_Corner.transform.position.x, Add_Corner.transform.position.y, Add_Corner.transform.position.z));
        Gizmos.DrawLine(Add_Corner.transform.position, new Vector3(Add_Corner.transform.position.x, Add_Corner.transform.position.y, Z + Add_Corner.transform.position.z));
        //Sub
        Gizmos.DrawLine(Sub_corner.transform.position, new Vector3((X * -1) + Sub_corner.transform.position.x, Sub_corner.transform.position.y, Sub_corner.transform.position.z));
        Gizmos.DrawLine(Sub_corner.transform.position, new Vector3(Sub_corner.transform.position.x, Sub_corner.transform.position.y, (-1*Z) + Sub_corner.transform.position.z));
    
        Gizmos.color = Color.green;
        Gizmos.DrawLine(new Vector3(transform.position.x, transform.position.y, transform.position.z), new Vector3(Target.x, transform.position.y, Target.z));
        Gizmos.DrawSphere(Target, radius);
        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(new Vector3(transform.position.x, transform.position.y+1, transform.position.z), rayStart + transform.forward * 1.5f);
    }
}
*/
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

