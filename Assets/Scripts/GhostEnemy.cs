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
    [Tooltip("Visual reference only. Catching the player is handled by the ghost trigger collision.")]
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
    [Tooltip("Controller for the existing Lose Canvas shown when the ghost catches the player. If empty, one is found automatically, including inactive UI objects.")]
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
            loseSceneController = FindAnyObjectByType<LoseSceneController>(FindObjectsInactive.Include);

        navAgent = GetComponent<NavMeshAgent>();
        if (navAgent != null)
        {
            navAgent.speed = moveSpeed;
            navAgent.stoppingDistance = 0.1f;
            navAgent.updateRotation = true;
            // Keep the ghost upright — do NOT let the NavMeshAgent tilt it
            // to match the surface normal.
            navAgent.updateUpAxis = false;
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

        // Detection radius only controls whether the ghost chases.
        // The lose screen is triggered exclusively by physical collision.
        isChasing = !isPlayerInvisible && distanceToPlayer <= detectionRadius;

        bool isMoving;

        if (isChasing)
            isMoving = ChasePlayer();
        else
            isMoving = Patrol();

        SetMovingAnimation(isMoving);

        Vector3 euler = transform.eulerAngles;
        transform.eulerAngles = new Vector3(0f, euler.y, 0f);
    }

    private bool Patrol()
    {
        if (patrolWaypoints == null || patrolWaypoints.Length == 0)
            return useAutomaticPatrol && AutomaticPatrol();

        if (navAgent != null && navAgent.isOnNavMesh)
        {
            Transform wpNav = patrolWaypoints[currentWaypointIndex];
            if (wpNav == null) return false;

            navAgent.speed = moveSpeed;
            navAgent.SetDestination(wpNav.position);

            if (!navAgent.pathPending && navAgent.remainingDistance <= Mathf.Max(navAgent.stoppingDistance + 0.1f, 0.3f))
                currentWaypointIndex = (currentWaypointIndex + 1) % patrolWaypoints.Length;

            return true;
        }

        Transform wpFallback = patrolWaypoints[currentWaypointIndex];
        if (wpFallback == null) return false;

        transform.position = Vector3.MoveTowards(
            transform.position,
            wpFallback.position,
            moveSpeed * Time.deltaTime
        );

        Vector3 dir = (wpFallback.position - transform.position).normalized;
        if (dir != Vector3.zero)
        {
            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                Quaternion.LookRotation(dir),
                Time.deltaTime * 5f
            );
        }

        if (Vector3.Distance(transform.position, wpFallback.position) < 0.3f)
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

    /// <summary>
    /// Lose screen is ONLY triggered here — by actual physical collision,
    /// not by the detection radius.
    /// </summary>
    private void OnTriggerEnter(Collider other)
    {
        TryCatchPlayer(other);
    }

    private void OnCollisionEnter(Collision collision)
    {
        TryCatchPlayer(collision.collider);
    }

    private void TryCatchPlayer(Collider other)
    {
        if (isCaught || catchCooldownTimer > 0f || other == null)
            return;

        bool isPlayer =
            other.CompareTag("Player") ||
            (other.transform.root != null && other.transform.root.CompareTag("Player"));

        if (!isPlayer)
            return;

        Debug.Log("[Ghost] Player collision detected. Showing Lose Canvas.");
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
            loseSceneController = FindAnyObjectByType<LoseSceneController>(FindObjectsInactive.Include);

        if (loseSceneController != null)
        {
            Debug.Log("[Ghost] LoseSceneController found. Calling ShowLoseScreen().");
            loseSceneController.ShowLoseScreen();
        }
        else
        {
            Debug.LogError("[Ghost] No LoseSceneController found in the scene, including inactive objects.");
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRadius);
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, catchDistance);
    }
}
