using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class GhostEnemy : MonoBehaviour
{
    [Header("Movement Settings")]
    public float moveSpeed = 3f;
    [Tooltip("Speed used when actively chasing the player. Set higher than moveSpeed for a threatening pursuit.")]
    public float chaseSpeed = 5f;
    public Transform[] patrolWaypoints;
    private int currentWaypointIndex = 0;
    private Vector3 automaticPatrolCenter;
    private Vector3 automaticPatrolTarget;
    private bool hasAutomaticPatrolTarget = false;
    private NavMeshAgent navAgent;
    private bool navigationReady;
    private Vector3 previousPosition;
    private float nextWaypointRetry;
    private bool waypointPathActive;
    [Tooltip("Patrol peacefully until this many artifacts have been collected.")]
    [Min(1)] public int artifactsBeforeHunt = 1;
    [Tooltip("Keep enabled for corridor enemies. A missing NavMesh stops movement instead of passing through walls.")]
    public bool requireNavigation = true;
    [Tooltip("Maximum distance used to place the ghost on the level NavMesh at startup.")]
    public float navMeshPlacementDistance = 4f;

    [Header("Detection Settings")]
    public float detectionRadius = 8f;
    [Tooltip("When enabled, the ghost only begins chasing if it has a clear line of sight to the player (no walls in between).")]
    public bool requireLineOfSight = true;
    [Tooltip("Layer mask used for line-of-sight raycasts. Should include walls/environment but NOT the player or ghost.")]
    public LayerMask lineOfSightMask = Physics.DefaultRaycastLayers;
    [Tooltip("How many seconds the ghost keeps chasing after losing sight or detection range of the player.")]
    [Min(0f)] public float losePlayerCooldown = 3f;
    private float losePlayerTimer = 0f;
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
    public string screamParameter = "Scream";

    [Header("Catch Settings")]
    [Tooltip("Distance used for the Rigidbody-free physical catch check.")]
    public float catchRadius = 1.2f;
    public float catchCooldown = 1.5f;
    private float catchCooldownTimer = 0f;

    [Header("Lose UI")]
    [Tooltip("Controller for the existing Lose Canvas shown when the ghost catches the player. If empty, one is found automatically, including inactive UI objects.")]
    public LoseSceneController loseSceneController;

    [Header("Optional Level 2 Story")]
    public Level2NoteNpc clueStory;
    private bool storyCompleted;
    public bool IsHostile => !storyCompleted && (clueStory == null ||
        (clueStory.State == Level2NoteNpc.StoryState.FindingClues && !clueStory.AllCluesCollected));

    /// <summary>Permanent for this scene run; only a fresh scene can restore the hunt.</summary>
    public void BecomeNonHostile()
    {
        storyCompleted = true;
        StopPursuit();
        if (navAgent != null) navAgent.enabled = false;
        navigationReady = false;
        // Disappear without destroying the existing enemy or creating another woman.
        // This also prevents her appearing beside the NPC during the identity reveal.
        foreach (Renderer visual in GetComponentsInChildren<Renderer>(true)) visual.enabled = false;
        foreach (Collider body in GetComponentsInChildren<Collider>(true)) body.enabled = false;
    }

    private void StopPursuit()
    {
        isChasing = false;
        losePlayerTimer = 0f;
        intendedMoving = false;
        if (navAgent != null && navAgent.enabled && navAgent.isOnNavMesh)
        {
            navAgent.ResetPath();
            navAgent.velocity = Vector3.zero;
        }
        ApplyMovingAnimation(false);
    }

    [Header("State")]
    public bool isChasing = false;

    private bool isCaught = false;
    private bool hasStartedMoving = false;
    // Tracks the intended moving state set in Update so LateUpdate does not override it.
    private bool intendedMoving = false;

    [Header("Checkpoint Patrol / Agimat")]
    [Min(1f)] public float checkpointPatrolRadius = 18f;
    [Min(1f)] public float playerSafetyDistance = 12f;
    [Min(0f)] public float checkpointGraceDuration = 6f;
    private float safeUntil;
    private float repelledUntil;
    private bool checkpointPatrol;
    private Vector3 retreatTarget;
    private bool hasRetreatTarget;
    private float nextRetreatSelection;
    private float patrolHeight;

    private void OnEnable() => RespawnPoint.CheckpointSaved += UpdateCheckpointPatrol;
    private void OnDisable() => RespawnPoint.CheckpointSaved -= UpdateCheckpointPatrol;

    public void UpdateCheckpointPatrol(Vector3 checkpointPosition)
    {
        if (!IsHostile) return;
        automaticPatrolCenter = checkpointPosition;
        automaticPatrolCenter.y = navigationReady ? checkpointPosition.y : patrolHeight;
        checkpointPatrol = true;
        hasStartedMoving = true;
        hasAutomaticPatrolTarget = false;
        if (requireNavigation && !navigationReady) return;
        safeUntil = Time.time + checkpointGraceDuration;
        isChasing = false;
        losePlayerTimer = 0f;
        if (navigationReady && navAgent != null && navAgent.isOnNavMesh) navAgent.ResetPath();
        SetNewAutomaticPatrolTarget();
        // Move into the new patrol region without spawning beside the player.
        if (hasAutomaticPatrolTarget)
        {
            if (navigationReady && navAgent != null && navAgent.isOnNavMesh)
                navAgent.Warp(automaticPatrolTarget);
            else
                transform.position = automaticPatrolTarget;
        }
    }

    public void Repel(float duration)
    {
        if (!IsHostile) return;
        repelledUntil = Mathf.Max(repelledUntil, Time.time + duration);
        isChasing = false;
        losePlayerTimer = 0f;
        PickRetreatTarget();
    }

    private void PickRetreatTarget()
    {
        if (playerTransform == null) return;
        Vector3 away = transform.position - playerTransform.position;
        away.y = 0f;
        if (away.sqrMagnitude < 0.01f) away = -playerTransform.forward;
        hasRetreatTarget = false;
        nextRetreatSelection = Time.time + 0.5f;
        float bestDistance = away.magnitude;
        for (int attempt = 0; attempt < 16; attempt++)
        {
            Vector3 direction = Quaternion.Euler(0f, attempt * 22.5f, 0f) * away.normalized;
            Vector3 candidate = transform.position + direction * playerSafetyDistance;
            if (navigationReady) candidate.y -= navAgent.baseOffset;
            if (navigationReady && navAgent.isOnNavMesh)
            {
                NavMeshQueryFilter filter = new NavMeshQueryFilter
                { agentTypeID = navAgent.agentTypeID, areaMask = navAgent.areaMask };
                if (!NavMesh.SamplePosition(candidate, out NavMeshHit hit, 3f, filter)) continue;
                candidate = hit.position;
                NavMeshPath path = new NavMeshPath();
                if (!navAgent.CalculatePath(candidate, path) || path.status != NavMeshPathStatus.PathComplete) continue;
            }
            else if (requireNavigation) return;
            Vector3 offset = candidate - playerTransform.position;
            offset.y = 0f;
            if (offset.magnitude <= bestDistance) continue;
            bestDistance = offset.magnitude;
            retreatTarget = candidate;
            hasRetreatTarget = true;
        }
        if (!hasRetreatTarget && navigationReady) navAgent.ResetPath();
    }

    private bool Retreat()
    {
        if (Time.time >= nextRetreatSelection && (!hasRetreatTarget ||
            (navigationReady && !navAgent.pathPending && navAgent.remainingDistance < 1f)))
            PickRetreatTarget();
        return hasRetreatTarget && MoveTo(retreatTarget, moveSpeed * 1.4f);
    }

    private bool MoveTo(Vector3 destination, float speed)
    {
        waypointPathActive = false;
        if (navigationReady && navAgent != null && navAgent.isOnNavMesh)
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

    private void Awake()
    {
        navAgent = GetComponent<NavMeshAgent>();
        // The scene serializes the agent disabled to avoid registration before the build.
        navAgent.enabled = false;
    }

    private void Start()
    {
        if (storyCompleted || (clueStory != null && clueStory.AllCluesCollected))
        {
            BecomeNonHostile();
            return;
        }
        if (playerTransform == null)
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null)
                playerTransform = playerObj.transform;
        }

        if (loseSceneController == null)
            loseSceneController = FindAnyObjectByType<LoseSceneController>(FindObjectsInactive.Include);

        navAgent.speed = moveSpeed;
        navAgent.stoppingDistance = 0.1f;
        navAgent.updateRotation = true;
        navAgent.updateUpAxis = false;
        transform.rotation = Quaternion.Euler(0f, transform.eulerAngles.y, 0f);
        NavMeshQueryFilter filter = new NavMeshQueryFilter
        {
            agentTypeID = navAgent.agentTypeID,
            areaMask = navAgent.areaMask
        };
        Vector3 feet = transform.position - Vector3.up * navAgent.baseOffset;
        if (NavMesh.SamplePosition(feet, out NavMeshHit spawn, navMeshPlacementDistance, filter))
        {
            transform.position = spawn.position + Vector3.up * navAgent.baseOffset;
            navAgent.enabled = true;
            navigationReady = navAgent.isOnNavMesh;
        }
        if (!navigationReady)
            Debug.LogError("[Ghost] No walkable NavMesh near the ghost spawn. Check the environment surface and spawn placement.", this);

        // Resolve the animator from GhostAnimation if present; fall back to any child Animator.
        GhostAnimation animations = GetComponent<GhostAnimation>();
        if (animations != null && animations.Animator != null) animator = animations.Animator;
        if (animator == null) animator = GetComponentInChildren<Animator>();
        if (animator != null) animator.applyRootMotion = false;
        previousPosition = transform.position;
        automaticPatrolCenter = navigationReady ? transform.position - Vector3.up * navAgent.baseOffset : transform.position;
        patrolHeight = transform.position.y;
        hasStartedMoving = !waitForPlayerBeforePatrolling;
        SetNewAutomaticPatrolTarget();
        ApplyMovingAnimation(false);
        if (RespawnPoint.hasReachedCheckpoint)
            UpdateCheckpointPatrol(RespawnPoint.activeCheckpointPosition);
    }

    private void Update()
    {
        if (!IsHostile)
        {
            if (!storyCompleted && clueStory != null && clueStory.AllCluesCollected)
                BecomeNonHostile();
            else StopPursuit();
            return;
        }
        if (playerTransform == null || isCaught ||
            Time.timeScale <= 0f || PauseManager.IsGamePaused)
            return;

        if (catchCooldownTimer > 0f)
            catchCooldownTimer -= Time.deltaTime;

        Vector3 playerOffset = playerTransform.position - transform.position;
        float distanceToPlayer = new Vector2(playerOffset.x, playerOffset.z).magnitude;
        bool sameFloor = Mathf.Abs(playerOffset.y) <= navAgent.height;

        // Contact remains dangerous during patrol, even before hunting unlocks
        // or if navigation failed. Safety effects are checked by CatchPlayer.
        if (catchCooldownTimer <= 0f && sameFloor &&
            distanceToPlayer <= GetCatchDistance() && HasLineOfSight())
        {
            TryCatchPlayerByDistance();
            if (isCaught) return;
        }
        if (requireNavigation && !navigationReady) return;

        if (Time.time < repelledUntil || IsPlayerInvisible())
        {
            isChasing = false;
            losePlayerTimer = 0f;
            intendedMoving = Retreat();
            return;
        }
        if (Time.time < safeUntil)
        {
            isChasing = false;
            losePlayerTimer = 0f;
            intendedMoving = distanceToPlayer < playerSafetyDistance ? Retreat() : AutomaticPatrol();
            return;
        }

        bool huntUnlocked = clueStory != null || (ArtifactManager.Instance != null &&
            ArtifactManager.Instance.artifactsCollected >= Mathf.Max(1, artifactsBeforeHunt));
        if (!huntUnlocked)
        {
            isChasing = false;
            losePlayerTimer = 0f;
            intendedMoving = Patrol();
            return;
        }

        if (!hasStartedMoving)
        {
            isChasing = false;
            intendedMoving = false;

            if (distanceToPlayer <= spawnIdleDistance)
                hasStartedMoving = true;
            else
                return;
        }

        bool isPlayerInvisible = IsPlayerInvisible();

        // Determine whether the ghost can currently detect the player.
        bool playerDetected = sameFloor && !isPlayerInvisible && distanceToPlayer <= detectionRadius;

        // Apply line-of-sight check: only start chasing if there is a clear sightline.
        if (playerDetected && requireLineOfSight)
            playerDetected = HasLineOfSight();

        // Persist the chase for losePlayerCooldown seconds after losing detection.
        if (playerDetected)
        {
            isChasing = true;
            losePlayerTimer = losePlayerCooldown;
        }
        else if (isChasing)
        {
            losePlayerTimer -= Time.deltaTime;
            if (losePlayerTimer <= 0f)
            {
                isChasing = false;
                losePlayerTimer = 0f;
            }
        }

        bool isMoving;

        if (isChasing)
            isMoving = ChasePlayer();
        else
            isMoving = Patrol();

        intendedMoving = isMoving;

        // Keep rotation flat — NavMeshAgent handles yaw; prevent pitch/roll drift.
        Vector3 euler = transform.eulerAngles;
        transform.eulerAngles = new Vector3(0f, euler.y, 0f);
    }

    /// <summary>Returns true when there is an unobstructed line of sight from the ghost to the player.</summary>
    private readonly RaycastHit[] sightHits = new RaycastHit[32];
    private float nextChasePathUpdate;

    private bool HasLineOfSight()
    {
        if (playerTransform == null) return false;

        // Cast from the ghost's approximate eye level to the player's center.
        Vector3 origin = transform.position + Vector3.up * (navAgent.height * 0.85f - navAgent.baseOffset);
        CharacterController controller = playerTransform.GetComponent<CharacterController>();
        Vector3 target = controller != null ? controller.bounds.center : playerTransform.position + Vector3.up;
        Vector3 direction = target - origin;

        // The ghost can share Default with walls; filter by hierarchy, not layer.
        int count = Physics.RaycastNonAlloc(origin, direction.normalized, sightHits,
            direction.magnitude, lineOfSightMask, QueryTriggerInteraction.Ignore);
        if (count == sightHits.Length) return false;
        for (int i = 0; i < count; i++)
        {
            RaycastHit hit = sightHits[i];
            if (hit.transform.IsChildOf(transform) || hit.transform.IsChildOf(playerTransform)) continue;
            return false;
        }
        return true;
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
        if (!IsHostile || playerTransform == null || IsPlayerInvisible() || Time.time < safeUntil || Time.time < repelledUntil)
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

        if (navigationReady && navAgent != null && navAgent.isOnNavMesh)
        {
            if (navAgent.pathPending) return true;
            if (navAgent.hasPath && navAgent.pathStatus == NavMeshPathStatus.PathComplete &&
                navAgent.remainingDistance > Mathf.Max(navAgent.stoppingDistance + 0.1f, 0.3f) &&
                !isChasing && waypointPathActive)
                return true;

            if (waypointPathActive)
                currentWaypointIndex = (currentWaypointIndex + 1) % patrolWaypoints.Length;
            waypointPathActive = false;
            if (Time.time < nextWaypointRetry) return false;

            NavMeshQueryFilter filter = new NavMeshQueryFilter
            { agentTypeID = navAgent.agentTypeID, areaMask = navAgent.areaMask };
            for (int attempt = 0; attempt < patrolWaypoints.Length; attempt++)
            {
                Transform waypoint = patrolWaypoints[currentWaypointIndex];
                NavMeshPath path = new NavMeshPath();
                if (waypoint != null && NavMesh.SamplePosition(waypoint.position,
                    out NavMeshHit hit, 2f, filter) && navAgent.CalculatePath(hit.position, path) &&
                    path.status == NavMeshPathStatus.PathComplete)
                {
                    navAgent.speed = moveSpeed;
                    waypointPathActive = navAgent.SetPath(path);
                    return waypointPathActive;
                }
                currentWaypointIndex = (currentWaypointIndex + 1) % patrolWaypoints.Length;
            }
            navAgent.ResetPath();
            nextWaypointRetry = Time.time + 1f;
            return false;
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
        waypointPathActive = false;
        if (hasAutomaticPatrolTarget && checkpointPatrol && playerTransform != null &&
            Vector3.Distance(automaticPatrolTarget, playerTransform.position) < playerSafetyDistance)
            hasAutomaticPatrolTarget = false;
        if (!hasAutomaticPatrolTarget)
            SetNewAutomaticPatrolTarget();

        if (!hasAutomaticPatrolTarget) return false;

        if (navigationReady && navAgent != null && navAgent.isOnNavMesh)
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
            if (navigationReady && navAgent != null && navAgent.isOnNavMesh)
            {
                if (!NavMesh.SamplePosition(candidate, out NavMeshHit hit, 3f, navAgent.areaMask)) continue;
                candidate = hit.position;
                NavMeshPath path = new NavMeshPath();
                if (!navAgent.CalculatePath(candidate, path) || path.status != NavMeshPathStatus.PathComplete) continue;
            }
            else if (requireNavigation)
            {
                return;
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
        waypointPathActive = false;
        if (playerTransform == null)
            return false;

        float speed = chaseSpeed > moveSpeed ? chaseSpeed : moveSpeed;

        if (navigationReady && navAgent != null && navAgent.isOnNavMesh)
        {
            navAgent.speed = speed;
            if (Time.time >= nextChasePathUpdate)
            {
                nextChasePathUpdate = Time.time + 0.2f;
                // The player's transform can be above the floor or just outside
                // agent clearance near a wall. Target the nearest walkable point.
                NavMeshQueryFilter filter = new NavMeshQueryFilter
                { agentTypeID = navAgent.agentTypeID, areaMask = navAgent.areaMask };
                if (NavMesh.SamplePosition(playerTransform.position, out NavMeshHit hit, 2f, filter))
                    navAgent.SetDestination(hit.position);
            }
            return true;
        }

        transform.position = Vector3.MoveTowards(
            transform.position,
            playerTransform.position,
            speed * Time.deltaTime
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

    private void LateUpdate()
    {
        if (Time.timeScale <= 0f || PauseManager.IsGamePaused) return;
        float speed = (transform.position - previousPosition).magnitude / Mathf.Max(Time.deltaTime, 0.001f);
        ApplyMovingAnimation(intendedMoving && speed > 0.01f && !isCaught);
        previousPosition = transform.position;
    }

    private void ApplyMovingAnimation(bool moving)
    {
        GhostAnimation.SetMoving(animator, movingParameter, moving);
    }

    // Legacy name kept for external callers (e.g. ArtifactCornerScare).
    private void SetMovingAnimation(bool moving)
    {
        intendedMoving = moving;
        ApplyMovingAnimation(moving);
    }

    private void CatchPlayer()
    {
        if (!IsHostile || isCaught || IsPlayerInvisible() || Time.time < safeUntil || Time.time < repelledUntil)
            return;

        isCaught = true;
        isChasing = false;
        losePlayerTimer = 0f;
        catchCooldownTimer = catchCooldown;

        Debug.Log("<color=red>[Ghost]</color> Caught the player!");

        intendedMoving = false;
        ApplyMovingAnimation(false);
        GhostAnimation.PlayScream(animator, screamParameter);
        if (animator != null) animator.updateMode = AnimatorUpdateMode.UnscaledTime;
        if (navigationReady) navAgent.ResetPath();

        // Disable player controls so they cannot move or look during the lose screen.
        if (playerTransform != null)
        {
            PlayerMovement movement = playerTransform.GetComponent<PlayerMovement>();
            if (movement != null) movement.enabled = false;

            MouseLook mouseLook = playerTransform.GetComponentInChildren<MouseLook>();
            if (mouseLook != null) mouseLook.enabled = false;

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
        // Draw chase speed indicator as a cyan ring.
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, chaseSpeed * 0.25f);
    }
}
