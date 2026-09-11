using UnityEngine;

public class GhostEnemy : MonoBehaviour
{
    [Header("Movement Settings")]
    public float moveSpeed = 3f;
    public Transform[] patrolWaypoints;
    private int currentWaypointIndex = 0;

    [Header("Detection Settings")]
    public float detectionRadius = 8f;
    public float catchDistance = 1.2f;
    public Transform playerTransform;

    [Header("State")]
    public bool isChasing = false;

    private void Start()
    {
        // Auto-find Player if not assigned
        if (playerTransform == null)
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null) playerTransform = playerObj.transform;
        }

        // Make colliders triggers so the ghost passes through walls/objects
        Collider col = GetComponent<Collider>();
        if (col != null) col.isTrigger = true;
    }

    private void Update()
    {
        if (playerTransform == null) return;

        // Check if player activated the Agimat
        bool isPlayerInvisible = InvisibilityConsumable.Instance != null && InvisibilityConsumable.Instance.isInvisible;

        float distanceToPlayer = Vector3.Distance(transform.position, playerTransform.position);

        // 1. Detection Logic
        if (!isPlayerInvisible && distanceToPlayer <= detectionRadius)
        {
            isChasing = true;
        }
        else
        {
            isChasing = false;
        }

        // 2. Behavior Logic
        if (isChasing)
        {
            ChasePlayer();

            // Catch player trigger
            if (distanceToPlayer <= catchDistance)
            {
                CatchPlayer();
            }
        }
        else
        {
            Patrol();
        }
    }

    private void Patrol()
    {
        if (patrolWaypoints.Length == 0) return;

        Transform targetWaypoint = patrolWaypoints[currentWaypointIndex];

        // Direct position movement (ignores wall collisions)
        transform.position = Vector3.MoveTowards(transform.position, targetWaypoint.position, moveSpeed * Time.deltaTime);

        // Face movement direction
        Vector3 direction = (targetWaypoint.position - transform.position).normalized;
        if (direction != Vector3.zero)
        {
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(direction), Time.deltaTime * 5f);
        }

        // Cycle to next waypoint when reached
        if (Vector3.Distance(transform.position, targetWaypoint.position) < 0.3f)
        {
            currentWaypointIndex = (currentWaypointIndex + 1) % patrolWaypoints.Length;
        }
    }

    private void ChasePlayer()
    {
        // Directly pursue player through walls
        transform.position = Vector3.MoveTowards(transform.position, playerTransform.position, moveSpeed * Time.deltaTime);

        // Rotate toward player
        Vector3 direction = (playerTransform.position - transform.position).normalized;
        if (direction != Vector3.zero)
        {
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(direction), Time.deltaTime * 5f);
        }
    }

    private void CatchPlayer()
    {
        Debug.Log("<color=red>[Ghost]</color> Caught the player!");

        // Trigger player respawn
        PlayerRespawn respawnSystem = playerTransform.GetComponent<PlayerRespawn>();
        if (respawnSystem != null)
        {
            respawnSystem.Respawn();
        }
    }

    private void OnDrawGizmosSelected()
    {
        // Visual radius in Scene view
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, detectionRadius);
    }
}