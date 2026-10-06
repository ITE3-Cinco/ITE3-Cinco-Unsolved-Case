using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class GhostEnemy : MonoBehaviour
{
    [Header("Movement Settings")]
    public float moveSpeed = 3f;
    public Transform[] patrolWaypoints;
    private int currentWaypointIndex = 0;
    private Vector3 automaticPatrolCenter;
    private Vector3 automaticPatrolTarget;
    private bool hasAutomaticPatrolTarget = false;
    private NavMeshAgent navAgent;

    [Header("Detection Settings")]
    public float detectionRadius = 8f;
    public float catchDistance = 1.2f;
    public Transform playerTransform;

    [Header("Automatic Patrol Fallback")]
    [Tooltip("If no patrol waypoints are assigned, the ghost automatically patrols around its starting position.")]
    public bool useAutomaticPatrol = true;
    public float automaticPatrolRadius = 6f;
    public float automaticPatrolPointTolerance = 0.75f;
    [Tooltip("If disabled, the ghost starts patrolling immediately instead of waiting for the player to approach.")]
    public bool waitForPlayerBeforePatrolling = false;

    [Header("Spawn / Animation")]
    [Tooltip("The ghost stays in the idle animation at the spawn point until the player gets within this distance.")]
    public float spawnIdleDistance = 8f;
    public Animator animator;
    public string movingParameter = "IsMoving";

    [Header("Catch Settings")]
    public float catchCooldown = 1.5f;
    private float catchCooldownTimer = 0f;

    [Header("Lose UI")]
    [Tooltip("Controller for the Lose Canvas shown when the ghost catches the player. If empty, one is found automatically.")]
    public LoseSceneController loseSceneController;

    [Header("State")]
    public bool isChasing = false;

    private bool isCaught = false;
    private bool hasStartedMoving = false;

    private void Start()
    {
        if (playerTransform == null)
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");

            if (playerObj != null)
                playerTransform = playerObj.transform;
        }

        if (loseSceneController == null)
            loseSceneController = FindAnyObjectByType<LoseSceneController>();

        navAgent = GetComponent<NavMeshAgent>();
        if (navAgent != null)
        {
            navAgent.speed = moveSpeed;
            navAgent.stoppingDistance = 0.1f;
            navAgent.updateRotation = true;
            navAgent.updateUpAxis = true;
        }

        Collider col = GetComponent<Collider>();

        if (col != null)
            col.isTrigger = true;

        automaticPatrolCenter = transform.position;
        hasStartedMoving = !waitForPlayerBeforePatrolling;
        SetNewAutomaticPatrolTarget();
        SetMovingAnimation(false);
    }

    private void Update()
    {
        if (playerTransform == null || isCaught)
            return;

        if (catchCooldownTimer > 0f)
            catchCooldownTimer -= Time.deltaTime;

        float distanceToPlayer = Vector3.Distance(transform.position, playerTransform.position);

        // The ghost begins in her idle spawn state.
        // Once the player gets close enough, the idle state ends and movement begins.
        if (!hasStartedMoving)
        {
            isChasing = false;
            SetMovingAnimation(false);

            if (distanceToPlayer <= spawnIdleDistance)
                hasStartedMoving = true;
            else
                return;
        }

        bool isPlayerInvisible =
            InvisibilityConsumable.Instance != null &&
            InvisibilityConsumable.Instance.isInvisible;

        isChasing = !isPlayerInvisible && distanceToPlayer <= detectionRadius;

        bool isMoving;

        if (isChasing)
        {
            isMoving = ChasePlayer();

            if (distanceToPlayer <= catchDistance && catchCooldownTimer <= 0f)
                CatchPlayer();
        }
        else
        {
            isMoving = Patrol();
        }

        SetMovingAnimation(isMoving);
    }

    private bool Patrol()
    {
        if (patrolWaypoints == null || patrolWaypoints.Length == 0)
            return useAutomaticPatrol && AutomaticPatrol();

        if (navAgent != null && navAgent.isOnNavMesh)
        {
            Transform targetWaypoint = patrolWaypoints[currentWaypointIndex];
            if (targetWaypoint == null)
                return false;

            navAgent.speed = moveSpeed;
            navAgent.SetDestination(targetWaypoint.position);

            if (!navAgent.pathPending && navAgent.remainingDistance <= Mathf.Max(navAgent.stoppingDistance + 0.1f, 0.3f))
                currentWaypointIndex = (currentWaypointIndex + 1) % patrolWaypoints.Length;

            return true;
        }

        Transform waypointTarget = patrolWaypoints[currentWaypointIndex];

        if (waypointTarget == null)
            return false;

        transform.position = Vector3.MoveTowards(
            transform.position,
            waypointTarget.position,
            moveSpeed * Time.deltaTime
        );

        Vector3 direction = (waypointTarget.position - transform.position).normalized;

        if (direction != Vector3.zero)
        {
            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                Quaternion.LookRotation(direction),
                Time.deltaTime * 5f
            );
        }

        if (Vector3.Distance(transform.position, waypointTarget.position) < 0.3f)
            currentWaypointIndex = (currentWaypointIndex + 1) % patrolWaypoints.Length;

        return true;
    }

    private bool AutomaticPatrol()
    {
        if (!hasAutomaticPatrolTarget)
            SetNewAutomaticPatrolTarget();

        if (navAgent != null && navAgent.isOnNavMesh)
        {
            navAgent.speed = moveSpeed;
            navAgent.SetDestination(automaticPatrolTarget);

            if (!navAgent.pathPending && navAgent.remainingDistance <= automaticPatrolPointTolerance)
                SetNewAutomaticPatrolTarget();

            return true;
        }

        transform.position = Vector3.MoveTowards(
            transform.position,
            automaticPatrolTarget,
            moveSpeed * Time.deltaTime
        );

        Vector3 direction = automaticPatrolTarget - transform.position;
        direction.y = 0f;

        if (direction.sqrMagnitude > 0.01f)
        {
            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                Quaternion.LookRotation(direction.normalized),
                Time.deltaTime * 5f
            );
        }

        if (Vector3.Distance(transform.position, automaticPatrolTarget) <= automaticPatrolPointTolerance)
            SetNewAutomaticPatrolTarget();

        return true;
    }

    private void SetNewAutomaticPatrolTarget()
    {
        float angle = Random.Range(0f, Mathf.PI * 2f);
        float radius = Random.Range(automaticPatrolRadius * 0.55f, automaticPatrolRadius);

        Vector3 candidate = automaticPatrolCenter + new Vector3(
            Mathf.Cos(angle) * radius,
            0f,
            Mathf.Sin(angle) * radius
        );

        if (NavMesh.SamplePosition(candidate, out NavMeshHit hit, automaticPatrolRadius, NavMesh.AllAreas))
            automaticPatrolTarget = hit.position;
        else
            automaticPatrolTarget = candidate;

        hasAutomaticPatrolTarget = true;
    }

    private bool ChasePlayer()
    {
        if (playerTransform == null)
            return false;

        if (navAgent != null && navAgent.isOnNavMesh)
        {
            navAgent.speed = moveSpeed;
            navAgent.SetDestination(playerTransform.position);
            return true;
        }

        transform.position = Vector3.MoveTowards(
            transform.position,
            playerTransform.position,
            moveSpeed * Time.deltaTime
        );

        Vector3 direction = (playerTransform.position - transform.position).normalized;

        if (direction != Vector3.zero)
        {
            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                Quaternion.LookRotation(direction),
                Time.deltaTime * 5f
            );
        }

        return true;
    }

    private void SetMovingAnimation(bool moving)
    {
        if (animator != null && !string.IsNullOrEmpty(movingParameter))
            animator.SetBool(movingParameter, moving);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (isCaught || catchCooldownTimer > 0f)
            return;

        if (other.CompareTag("Player") || other.transform.root.CompareTag("Player"))
            CatchPlayer();
    }

    private void CatchPlayer()
    {
        if (isCaught)
            return;

        isCaught = true;
        isChasing = false;
        catchCooldownTimer = catchCooldown;

        Debug.Log("<color=red>[Ghost]</color> Caught the player!");

        SetMovingAnimation(false);

        if (playerTransform != null)
        {
            PlayerAudio playerAudio = playerTransform.GetComponent<PlayerAudio>();

            if (playerAudio != null)
                playerAudio.PlayCaught();
        }

        if (loseSceneController == null)
            loseSceneController = FindAnyObjectByType<LoseSceneController>();

        if (loseSceneController != null)
        {
            loseSceneController.ShowLoseScreen();
        }
        else
        {
            Debug.LogError("[Ghost] No LoseSceneController was found. Assign the existing Lose Canvas controller to GhostEnemy.");
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, detectionRadius);
    }
}
