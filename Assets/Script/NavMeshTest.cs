using UnityEngine;
using UnityEngine.AI;

public class NavMeshTest : MonoBehaviour
{
    public Transform target1; // First destination
    public Transform target2; // Second destination

    private NavMeshAgent agent;
    private bool reachedFirst = false;

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();

        if (target1 != null)
            agent.SetDestination(target1.position);
    }

    void Update()
    {
        if (agent == null) return;

        // If we haven't reached the first target yet
        if (!reachedFirst && !agent.pathPending && agent.remainingDistance <= agent.stoppingDistance)
        {
            reachedFirst = true;

            // Go to the second target
            if (target2 != null)
                agent.SetDestination(target2.position);
        }
    }
}
