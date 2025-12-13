using UnityEngine;
using UnityEngine.AI;

public class Ai_Follow_Player : MonoBehaviour
{
    NavMeshAgent navMeshAgent;
    private Animator anim;
    private string currAnim = null;
    public GameObject Player;
    public float stoppingDistance;
    private float cdWave = 2f;
    void Start()
    {
        navMeshAgent = GetComponent<NavMeshAgent>();
        anim = GetComponent<Animator>();
    }

    // Update is called once per frame
    void Update()
    {
        if (navMeshAgent == null) return;
        if (Player == null) return; // ensure Player assigned

        // Use actual distance to player instead of comparing stoppingDistance values
        float distance = Vector3.Distance(transform.position, Player.transform.position);

        if (distance > stoppingDistance)
        {
            // update destination only when needed
            if (!navMeshAgent.hasPath || navMeshAgent.destination != Player.transform.position)
                navMeshAgent.SetDestination(Player.transform.position);
            navMeshAgent.isStopped = false;
            AnimChage("walk");
        }
        else
        {
            navMeshAgent.isStopped = true;
            
            if(cdWave > 0)
            {
                cdWave -= Time.deltaTime;
            }
            else{
                AnimChage("wave");
                Vector3 target = Player.transform.position - transform.position;
                transform.rotation = Quaternion.LookRotation(target);
                cdWave = 3f;
            }
        }
    }

    public void AnimChage(string NewAnim)
    {
        if(NewAnim != currAnim)
        {
            anim.SetTrigger(NewAnim);
            currAnim = NewAnim;
        }
    }
}
