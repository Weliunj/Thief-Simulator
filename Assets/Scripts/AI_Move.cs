using UnityEngine;
using UnityEngine.AI;

public class AI_Move_NavMesh : MonoBehaviour
{
    private Animator animator;
    private string currAnim;
    private NavMeshAgent agent;

    [Header("Movement Area")]
    public GameObject Add_Corner;
    public int X;
    public int Z;

    [Header("Settings")]
    public float radius = 1f;
    public float Speed = 2f;
    private float currentSpeed;
    private float idleTime;

    private Vector3 Target;

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        animator = GetComponentInChildren<Animator>();

        agent.updateRotation = false;
        agent.updatePosition = true;

        SetNewTarget();
    }

    void Update()
    {
        RotateToTarget();
        MoveCheck();
    }

    // ================== SET NEW TARGET ==================
    public void SetNewTarget()
    {
        Vector3 start = Add_Corner.transform.position;

        Vector3 rnd = new Vector3(
            Random.Range(start.x, start.x + X),
            transform.position.y, // lấy Y từ player để tìm trên tầng hiện tại
            Random.Range(start.z, start.z + Z)
        );

        // Tìm vị trí gần nhất trên NavMesh (có thể ở tầng khác)
        NavMeshHit hit;
        if (NavMesh.SamplePosition(rnd, out hit, 5f, NavMesh.AllAreas))
        {
            Target = hit.position;
        }
        else
        {
            // Nếu không tìm được, dùng vị trí ngẫu nhiên ban đầu
            Target = rnd;
        }

        agent.SetDestination(Target);

        // Random tốc độ
        currentSpeed = (transform.localScale.x <= 0.5f)
            ? (Random.Range(0, 100) < 20 ? Speed + 4f : Speed)
            : (Random.Range(0, 100) < 10 ? Speed + 3f : Speed);

        agent.speed = currentSpeed;

        // Random thời gian idle
        idleTime = (transform.localScale.x <= 0.5f)
            ? Random.Range(1f, 6f)
            : Random.Range(2f, 13f);
    }

    // ================== ROTATE ==================
    void RotateToTarget()
    {
        Vector3 vel = agent.velocity;

        if (vel.sqrMagnitude > 0.1f)
        {
            vel.y = 0;

            Quaternion targetRot = Quaternion.LookRotation(vel);
            transform.rotation = targetRot;
        }
    }

    // ================== MOVE CHECK ==================
    void MoveCheck()
    {
        if (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance)
        {
            ChangeAnim("idle");

            idleTime -= Time.deltaTime;
            if (idleTime <= 0)
                SetNewTarget();
        }
        else
        {
            ChangeAnim(agent.speed > Speed ? "run" : "walk");
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

        Gizmos.color = Color.red;
        Vector3 p = Add_Corner.transform.position;

        Gizmos.DrawLine(p, new Vector3(p.x + X, p.y, p.z));
        Gizmos.DrawLine(p, new Vector3(p.x, p.y, p.z + Z));
        Gizmos.DrawLine(new Vector3(p.x + X, p.y, p.z), new Vector3(p.x + X, p.y, p.z + Z));
        Gizmos.DrawLine(new Vector3(p.x, p.y, p.z + Z), new Vector3(p.x + X, p.y, p.z + Z));

        Gizmos.color = Color.green;
        Gizmos.DrawSphere(Target, 0.3f);
    }
}