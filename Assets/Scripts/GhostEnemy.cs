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
    [Tooltip("Visual reference only. Catching the player is handled by the Rigidbody-free catch check.")]
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
    [Tooltip("Distance used for the Rigidbody-free physical catch check.")]
    public float catchRadius = 0.9f;
    public float catchCooldown = 1.5f;
    private float catchCooldownTimer = 0f;

    [Header("Lose UI")]
    [Tooltip("Controller for the existing Lose Canvas shown when the ghost catches the player. If empty, one is found automatically, including inactive UI objects.")]
    public LoseSceneController loseSceneController;

    [Header("State")]
    public bool isChasing = false;

    private bool isCaught = false;
    private bool hasStartedMoving = false;

    [Header("Checkpoint Patrol / Agimat")]
    [Min(1f)] public float checkpointPatrolRadius = 18f;
    [Min(1f)] public float playerSafetyDistance = 12f;
    [Min(0f)] public float checkpointGraceDuration = 6f;
    private float safeUntil;
    private float repelledUntil;
    private bool checkpointPatrol;
    private Vector3 retreatTarget;
    private float patrolHeight;

    private void OnEnable() => RespawnPoint.CheckpointSaved += UpdateCheckpointPatrol;
    private void OnDisable() => RespawnPoint.CheckpointSaved -= UpdateCheckpointPatrol;

    public void UpdateCheckpointPatrol(Vector3 checkpointPosition)
    {
        automaticPatrolCenter = checkpointPosition;
        automaticPatrolCenter.y = patrolHeight;
        checkpointPatrol = true;
        hasStartedMoving = true;
        hasAutomaticPatrolTarget = false;
        safeUntil = Time.time + checkpointGraceDuration;
        isChasing = false;
        if (navAgent != null && navAgent.isOnNavMesh) navAgent.ResetPath();
        SetNewAutomaticPatrolTarget();
        // Move into the new patrol region without spawning beside the player.
        if (hasAutomaticPatrolTarget)
        {
            if (navAgent != null && navAgent.isOnNavMesh)
                navAgent.Warp(automaticPatrolTarget);
            else
                transform.position = automaticPatrolTarget;
        }
    }

    public void Repel(float duration)
    {
        repelledUntil = Mathf.Max(repelledUntil, Time.time + duration);
        isChasing = false;
        PickRetreatTarget();
    }

    private void PickRetreatTarget()
    {
        if (playerTransform == null) return;
        Vector3 away = transform.position - playerTransform.position;
        away.y = 0f;
        if (away.sqrMagnitude < 0.01f) away = -playerTransform.forward;
        retreatTarget = transform.position + away.normalized * playerSafetyDistance;
        if (navAgent != null && navAgent.isOnNavMesh &&
            NavMesh.SamplePosition(retreatTarget, out NavMeshHit hit, 5f, navAgent.areaMask))
            retreatTarget = hit.position;
    }

    private bool Retreat()
    {
        if (Vector3.Distance(transform.position, retreatTarget) < 1f ||
            Vector3.Dot(retreatTarget - transform.position, transform.position - playerTransform.position) <= 0f)
            PickRetreatTarget();
        return MoveTo(retreatTarget, moveSpeed * 1.4f);
    }

    private bool MoveTo(Vector3 destination, float speed)
    {
        if (navAgent != null && navAgent.isOnNavMesh)
        {
            navAgent.speed = speed;
            navAgent.SetDestination(destination);
        }
        else
        {
            destination.y = transform.position.y;
            transform.position = Vector3.MoveTowards(transform.position, destination, speed * Time.deltaTime);
        }
        return true;
    }

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
            // Keep the ghost upright.
            navAgent.updateUpAxis = false;
        }

        automaticPatrolCenter = transform.position;
        patrolHeight = transform.position.y;
        hasStartedMoving = !waitForPlayerBeforePatrolling;
        SetNewAutomaticPatrolTarget();
        SetMovingAnimation(false);
        if (RespawnPoint.hasReachedCheckpoint)
            UpdateCheckpointPatrol(RespawnPoint.activeCheckpointPosition);
    }

    private void Update()
    {
        if (playerTransform == null || isCaught || Time.timeScale <= 0f || PauseManager.IsGamePaused)
            return;

        if (catchCooldownTimer > 0f)
            catchCooldownTimer -= Time.deltaTime;

        float distanceToPlayer = Vector3.Distance(transform.position, playerTransform.position);

        if (Time.time < repelledUntil || IsPlayerInvisible())
        {
            isChasing = false;
            SetMovingAnimation(Retreat());
            return;
        }
        if (Time.time < safeUntil)
        {
            isChasing = false;
            SetMovingAnimation(distanceToPlayer < playerSafetyDistance ? Retreat() : AutomaticPatrol());
            return;
        }

        // Rigidbody-free catch system.
        // The detection radius only controls chasing. This separate, small
        // catch radius represents actual physical contact with the Ghost.
        if (catchCooldownTimer <= 0f &&
            !isCaught &&
            !IsPlayerInvisible() &&
            distanceToPlayer <= GetCatchDistance())
        {
            TryCatchPlayerByDistance();
            if (isCaught)
                return;
        }

        if (!hasStartedMoving)
        {
            isChasing = false;
            SetMovingAnimation(false);

            if (distanceToPlayer <= spawnIdleDistance)
                hasStartedMoving = true;
            else
                return;
        }

        bool isPlayerInvisible = IsPlayerInvisible();

        // Detection radius only controls whether the ghost chases.
        // Catching is handled separately by the small catch-radius check.
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

    private bool IsPlayerInvisible()
    {
        return InvisibilityConsumable.Instance != null &&
               InvisibilityConsumable.Instance.isInvisible;
    }

    private float GetCatchDistance()
    {
        if (catchRadius > 0f)
            return catchRadius;

        return catchDistance;
    }

    private void TryCatchPlayerByDistance()
    {
        if (playerTransform == null)
            return;

        CharacterController playerController = playerTransform.GetComponent<CharacterController>();

        if (playerController != null && !playerController.enabled)
            return;

        Debug.Log("[Ghost] Player entered the physical catch radius. Showing Lose Canvas.");
        CatchPlayer();
    }

    private bool Patrol()
    {
        if (checkpointPatrol || patrolWaypoints == null || patrolWaypoints.Length == 0)
            return (checkpointPatrol || useAutomaticPatrol) && AutomaticPatrol();

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

        if (!hasAutomaticPatrolTarget) return false;

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
        hasAutomaticPatrolTarget = false;
        float patrolRadius = checkpointPatrol ? checkpointPatrolRadius : automaticPatrolRadius;
        for (int attempt = 0; attempt < 32; attempt++)
        {
            Vector2 offset = Random.insideUnitCircle.normalized * Random.Range(patrolRadius * 0.55f, patrolRadius);
            Vector3 candidate = automaticPatrolCenter + new Vector3(offset.x, 0f, offset.y);
            if (navAgent != null && navAgent.isOnNavMesh)
            {
                if (!NavMesh.SamplePosition(candidate, out NavMeshHit hit, 3f, navAgent.areaMask)) continue;
                candidate = hit.position;
                NavMeshPath path = new NavMeshPath();
                if (!navAgent.CalculatePath(candidate, path) || path.status != NavMeshPathStatus.PathComplete) continue;
            }
            else if (checkpointPatrol)
            {
                // PoliceHQ has no baked NavMesh: only choose clear points above a floor.
                if (!Physics.Raycast(candidate + Vector3.up, Vector3.down, out RaycastHit floor,
                    5f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore) || floor.normal.y < 0.7f)
                    continue;
                if (Physics.CheckSphere(candidate, 0.6f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
                    continue;
            }
            Vector3 playerPosition = playerTransform != null ? playerTransform.position : automaticPatrolCenter;
            playerPosition.y = candidate.y;
            if (checkpointPatrol && (Vector3.Distance(candidate, playerPosition) < playerSafetyDistance ||
                Vector3.Distance(candidate, automaticPatrolCenter) < playerSafetyDistance)) continue;
            automaticPatrolTarget = candidate;
            hasAutomaticPatrolTarget = true;
            return;
        }
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

    private void CatchPlayer()
    {
        if (isCaught || IsPlayerInvisible() || Time.time < safeUntil || Time.time < repelledUntil)
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
        Gizmos.DrawWireSphere(transform.position, GetCatchDistance());
    }
}
