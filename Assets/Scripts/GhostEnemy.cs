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

    [Header("Catch Settings")]
    public float catchCooldown = 1.5f;
    private float catchCooldownTimer = 0f;

    [Header("Animation")]
    public Animator animator;
    public string chasingParameter = "IsChasing";

    [Header("State")]
    public bool isChasing = false;

    private void Start()
    {
        if (playerTransform == null)
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null) playerTransform = playerObj.transform;
        }

        Collider col = GetComponent<Collider>();
        if (col != null) col.isTrigger = true;
    }

    private void Update()
    {
        if (playerTransform == null) return;

        if (catchCooldownTimer > 0f)
            catchCooldownTimer -= Time.deltaTime;

        bool isPlayerInvisible = InvisibilityConsumable.Instance != null && InvisibilityConsumable.Instance.isInvisible;
        float distanceToPlayer = Vector3.Distance(transform.position, playerTransform.position);

        isChasing = !isPlayerInvisible && distanceToPlayer <= detectionRadius;
        UpdateAnimation();

        if (isChasing)
        {
            ChasePlayer();

            if (distanceToPlayer <= catchDistance && catchCooldownTimer <= 0f)
                CatchPlayer();
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
        transform.position = Vector3.MoveTowards(transform.position, targetWaypoint.position, moveSpeed * Time.deltaTime);

        Vector3 direction = (targetWaypoint.position - transform.position).normalized;
        if (direction != Vector3.zero)
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(direction), Time.deltaTime * 5f);

        if (Vector3.Distance(transform.position, targetWaypoint.position) < 0.3f)
            currentWaypointIndex = (currentWaypointIndex + 1) % patrolWaypoints.Length;
    }

    private void ChasePlayer()
    {
        transform.position = Vector3.MoveTowards(transform.position, playerTransform.position, moveSpeed * Time.deltaTime);

        Vector3 direction = (playerTransform.position - transform.position).normalized;
        if (direction != Vector3.zero)
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(direction), Time.deltaTime * 5f);
    }

    private void UpdateAnimation()
    {
        if (animator != null && !string.IsNullOrEmpty(chasingParameter))
            animator.SetBool(chasingParameter, isChasing);
    }

    private void CatchPlayer()
    {
        Debug.Log("<color=red>[Ghost]</color> Caught the player!");

        if (animator != null)
            animator.SetBool(chasingParameter, false);

        GhostAudio ghostAudio = GetComponent<GhostAudio>();
        if (ghostAudio != null)
            ghostAudio.enabled = false;

        // Prevent repeated catches immediately after the respawn.
        catchCooldownTimer = catchCooldown;
        isChasing = false;

        PlayerRespawn respawnSystem = playerTransform.GetComponent<PlayerRespawn>();
        if (respawnSystem != null)
            respawnSystem.Respawn();
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, detectionRadius);
    }
}