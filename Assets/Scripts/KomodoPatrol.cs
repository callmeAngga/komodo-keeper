using UnityEngine;
using UnityEngine.AI;
using System.Collections;

public class KomodoPatrol : MonoBehaviour
{
    private NavMeshAgent agent;

    [Header("Patrol Settings")]
    [Tooltip("Radius area untuk patroli")]
    public float patrolRadius = 10.0f;

    [Tooltip("Waktu tunggu sebelum pindah ke titik berikutnya")]
    public float waitTime = 3.0f;

    [Tooltip("Posisi tengah area patroli (jika kosong, akan menggunakan posisi awal)")]
    public Transform centerPoint;

    private bool isWaiting = false;
    private Vector3 startPosition;

    [Header("Animation")]
    [Tooltip("Reference ke Animator komodo jika menggunakan animasi")]
    public Animator animator;
    [Tooltip("Nama parameter Animator untuk status bergerak")]
    public string moveAnimParam = "IsMoving";

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();

        startPosition = centerPoint != null ? centerPoint.position : transform.position;

        StartCoroutine(PatrolRoutine());
    }

    void Update()
    {
        if (animator != null)
        {
            animator.SetBool(moveAnimParam, agent.velocity.magnitude > 0.1f);
        }

        EnsureAgentOnNavMesh();
    }

    IEnumerator PatrolRoutine()
    {
        while (true)
        {
            if (!isWaiting)
            {
                Vector3 randomPoint = GetRandomPointOnNavMesh();

                if (randomPoint != Vector3.zero)
                {

                    agent.SetDestination(randomPoint);


                    while (agent.pathPending || agent.remainingDistance > agent.stoppingDistance)
                    {
                        yield return null;
                    }

                    isWaiting = true;

                    yield return new WaitForSeconds(waitTime);

                    isWaiting = false;
                }
            }
            yield return null;
        }
    }

    Vector3 GetRandomPointOnNavMesh()
    {
        Vector3 randomDirection = Random.insideUnitSphere * patrolRadius;
        randomDirection += startPosition;

        NavMeshHit hit;
        if (NavMesh.SamplePosition(randomDirection, out hit, patrolRadius, NavMesh.AllAreas))
        {
            Debug.DrawRay(hit.position, Vector3.up * 2, Color.blue, 2.0f);

            return hit.position;
        }

        return Vector3.zero;
    }

    void EnsureAgentOnNavMesh()
    {
        if (!agent.isOnNavMesh)
        {
            NavMeshHit hit;
            if (NavMesh.SamplePosition(transform.position, out hit, 10.0f, NavMesh.AllAreas))
            {
                agent.Warp(hit.position);
                Debug.LogWarning("Komodo reset ke NavMesh position");
            }
        }
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Vector3 center = Application.isPlaying ? startPosition :
                         (centerPoint != null ? centerPoint.position : transform.position);
        Gizmos.DrawWireSphere(center, patrolRadius);
    }
}