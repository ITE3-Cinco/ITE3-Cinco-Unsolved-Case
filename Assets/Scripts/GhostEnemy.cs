// Purpose: Controls White Lady patrols, pursuit, retreat and catching the player.
// Use: Attach with a NavMeshAgent; assign the player, optional waypoints and lose UI.
// How it works: Each frame chooses behavior from story progress, protection, distance and clear sight.

// UnityEngine supplies GameObjects, components, transforms, time, math and physics used here.
using UnityEngine;
// Unity AI supplies NavMesh agents, paths and walkable-surface building/query tools.
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class GhostEnemy : MonoBehaviour
{
    // moveSpeed: normal patrol movement speed. Assigned in the scene/prefab Inspector; the initializer below is
    // the default.
    [Header("Movement Settings")]
    public float moveSpeed = 3f;
    // chaseSpeed: Speed used when actively chasing the player. Set higher than moveSpeed for a threatening
    // pursuit. Assigned in the scene/prefab Inspector; the initializer below is the default.
    [Tooltip("Speed used when actively chasing the player. Set higher than moveSpeed for a threatening pursuit.")]
    public float chaseSpeed = 5f;
    // patrolWaypoints: ordered Transform stops for the ghost patrol. Assigned in the scene/prefab Inspector.
    public Transform[] patrolWaypoints;
    // Runtime patrol state remembers the current route, random target and navigation readiness.
    // currentWaypointIndex: zero-based patrol waypoint selected for the current route. Updated by Patrol.
    private int currentWaypointIndex = 0;
    // automaticPatrolCenter: world-space center used when patrol waypoints are absent. UpdateCheckpointPatrol sets
    // it from checkpointPosition.
    private Vector3 automaticPatrolCenter;
    // automaticPatrolTarget: chosen world-space destination for automatic patrol. SetNewAutomaticPatrolTarget sets
    // it from candidate.
    private Vector3 automaticPatrolTarget;
    // hasAutomaticPatrolTarget: whether an automatic patrol destination has been selected. Updated by
    // UpdateCheckpointPatrol, AutomaticPatrol, SetNewAutomaticPatrolTarget.
    private bool hasAutomaticPatrolTarget = false;
    // navAgent: NavMeshAgent cached for route planning and movement. Awake retrieves it from the first
    // NavMeshAgent component on this GameObject.
    private NavMeshAgent navAgent;
    // navigationReady: whether startup found usable navigation for this enemy. Start sets it from whether navAgent
    // is registered on a walkable NavMesh.
    private bool navigationReady;
    // previousPosition: previous-frame world position used to measure actual movement. Start sets it from world
    // position of transform.
    private Vector3 previousPosition;
    // nextWaypointRetry: gameplay timestamp when another waypoint attempt is allowed. Updated by Patrol.
    private float nextWaypointRetry;
    // waypointPathActive: whether a waypoint navigation route is currently active. Updated by MoveTo, Patrol,
    // AutomaticPatrol and related helpers.
    private bool waypointPathActive;
    // artifactsBeforeHunt: Patrol peacefully until this many artifacts have been collected. Assigned in the
    // scene/prefab Inspector; the initializer below is the default.
    [Tooltip("Patrol peacefully until this many artifacts have been collected.")]
    [Min(1)] public int artifactsBeforeHunt = 1;
    // requireNavigation: Keep enabled for corridor enemies. A missing NavMesh stops movement instead of passing
    // through walls. Assigned in the scene/prefab Inspector; the initializer below is the default.
    [Tooltip("Keep enabled for corridor enemies. A missing NavMesh stops movement instead of passing through walls.")]
    public bool requireNavigation = true;
    // navMeshPlacementDistance: Maximum distance used to place the ghost on the level NavMesh at startup. Assigned
    // in the scene/prefab Inspector; the initializer below is the default.
    [Tooltip("Maximum distance used to place the ghost on the level NavMesh at startup.")]
    public float navMeshPlacementDistance = 4f;

    // detectionRadius: maximum detection distance before sight checks. Assigned in the scene/prefab Inspector; the
    // initializer below is the default.
    [Header("Detection Settings")]
    public float detectionRadius = 8f;
    // requireLineOfSight: When enabled, the ghost only begins chasing if it has a clear line of sight to the
    // player (no walls in between). Assigned in the scene/prefab Inspector; the initializer below is the default.
    [Tooltip("When enabled, the ghost only begins chasing if it has a clear line of sight to the player (no walls in between).")]
    public bool requireLineOfSight = true;
    // lineOfSightMask: Layer mask used for line-of-sight raycasts. Should include walls/environment but NOT the
    // player or ghost. Assigned in the scene/prefab Inspector; the initializer below is the default.
    [Tooltip("Layer mask used for line-of-sight raycasts. Should include walls/environment but NOT the player or ghost.")]
    public LayerMask lineOfSightMask = Physics.DefaultRaycastLayers;
    // losePlayerCooldown: How many seconds the ghost keeps chasing after losing sight or detection range of the
    // player. Assigned in the scene/prefab Inspector; the initializer below is the default.
    [Tooltip("How many seconds the ghost keeps chasing after losing sight or detection range of the player.")]
    [Min(0f)] public float losePlayerCooldown = 3f;
    // losePlayerTimer: remaining pursuit time after detection is lost. Update sets it from losePlayerCooldown.
    private float losePlayerTimer = 0f;
    // catchDistance: Visual reference only. Catching the player is handled by the Rigidbody-free catch check.
    // Assigned in the scene/prefab Inspector; the initializer below is the default.
    [Tooltip("Visual reference only. Catching the player is handled by the Rigidbody-free catch check.")]
    public float catchDistance = 1.2f;
    // playerTransform: player Transform used for detection and pursuit. Start sets it from Transform of playerObj.
    public Transform playerTransform;

    // useAutomaticPatrol: If no patrol waypoints are assigned, the ghost automatically patrols around its starting
    // position. Assigned in the scene/prefab Inspector; the initializer below is the default.
    [Header("Automatic Patrol Fallback")]
    [Tooltip("If no patrol waypoints are assigned, the ghost automatically patrols around its starting position.")]
    public bool useAutomaticPatrol = true;
    // automaticPatrolRadius: radius around the fallback patrol center for random destinations. Assigned in the
    // scene/prefab Inspector; the initializer below is the default.
    public float automaticPatrolRadius = 6f;
    // automaticPatrolPointTolerance: arrival tolerance for the random patrol destination. Assigned in the
    // scene/prefab Inspector; the initializer below is the default.
    public float automaticPatrolPointTolerance = 0.75f;
    // waitForPlayerBeforePatrolling: If disabled, the ghost starts patrolling immediately instead of waiting for
    // the player to approach. Assigned in the scene/prefab Inspector; the initializer below is the default.
    [Tooltip("If disabled, the ghost starts patrolling immediately instead of waiting for the player to approach.")]
    public bool waitForPlayerBeforePatrolling = false;

    // spawnIdleDistance: The ghost stays in the idle animation at the spawn point until the player gets within
    // this distance. Assigned in the scene/prefab Inspector; the initializer below is the default.
    [Header("Spawn / Animation")]
    [Tooltip("The ghost stays in the idle animation at the spawn point until the player gets within this distance.")]
    public float spawnIdleDistance = 8f;
    // animator: Animator receiving poses or parameter changes. Start retrieves it from the first Animator
    // component on or beneath this GameObject. Inspector assignment may supply its initial value.
    public Animator animator;
    // movingParameter: Animator parameter name receiving the movement flag. Assigned in the scene/prefab
    // Inspector; the initializer below is the default.
    public string movingParameter = "IsMoving";
    // screamParameter: Animator trigger name used for the catch scream. Assigned in the scene/prefab Inspector;
    // the initializer below is the default.
    public string screamParameter = "Scream";

    // catchRadius: Distance used for the Rigidbody-free physical catch check. Assigned in the scene/prefab
    // Inspector; the initializer below is the default.
    [Header("Catch Settings")]
    [Tooltip("Distance used for the Rigidbody-free physical catch check.")]
    public float catchRadius = 1.2f;
    // catchCooldown: gameplay seconds between allowed catch attempts. Assigned in the scene/prefab Inspector; the
    // initializer below is the default.
    public float catchCooldown = 1.5f;
    // catchCooldownTimer: remaining catch cooldown checked before another attempt. Update sets it from Unity
    // gameplay frame duration in seconds.
    private float catchCooldownTimer = 0f;

    // loseSceneController: Controller for the existing Lose Canvas shown when the ghost catches the player. If
    // empty, one is found automatically, including inactive UI objects. Start retrieves it from a scene
    // LoseSceneController component (including inactive objects). Inspector assignment may supply its initial
    // value.
    [Header("Lose UI")]
    [Tooltip("Controller for the existing Lose Canvas shown when the ghost catches the player. If empty, one is found automatically, including inactive UI objects.")]
    public LoseSceneController loseSceneController;

    // clueStory: Level2NoteNpc story state used to gate the ghost hunt. Assigned in the scene/prefab Inspector.
    [Header("Optional Level 2 Story")]
    public Level2NoteNpc clueStory;
    // storyCompleted: permanent scene-run flag that makes the ghost non-hostile. Updated by BecomeNonHostile.
    private bool storyCompleted;
    // IsHostile is computed when read: return not (storyCompleted) and (clueStory == null or (clueStory.State ==
    // Level2NoteNpc.StoryState.FindingClues && !clueStory.AllCluesCollected)).
    public bool IsHostile => !storyCompleted && (clueStory == null ||
        (clueStory.State == Level2NoteNpc.StoryState.FindingClues && !clueStory.AllCluesCollected));

    /// <summary>Permanent for this scene run; only a fresh scene can restore the hunt.</summary>
    // After the clue story is complete, stop hunting and hide this enemy for the rest of the run.
    public void BecomeNonHostile()
    {
        // Set storyCompleted to true.
        storyCompleted = true;
        // Call GhostEnemy.StopPursuit; clear chase state, stop the navigation route and switch to idle animation.
        StopPursuit();
        // If navAgent exists, disable navAgent by changing its component enabled flag.
        if (navAgent != null) navAgent.enabled = false;
        // Set navigationReady to false.
        navigationReady = false;
        // Disappear without destroying the existing enemy or creating another woman.
        // This also prevents her appearing beside the NPC during the identity reveal.
        // Process each visual in all Renderer components on or beneath this GameObject (including inactive
        // children).
        foreach (Renderer visual in GetComponentsInChildren<Renderer>(true)) visual.enabled = false;
        // Process each body in all Collider components on or beneath this GameObject (including inactive
        // children).
        foreach (Collider body in GetComponentsInChildren<Collider>(true)) body.enabled = false;
    }

    /// <summary>Clear chase state, stop the navigation route and switch to idle animation.</summary>
    private void StopPursuit()
    {
        // Set isChasing to false.
        isChasing = false;
        // Set losePlayerTimer to 0.
        losePlayerTimer = 0f;
        // Set intendedMoving to false.
        intendedMoving = false;
        // If navAgent exists and enabled state of navAgent and navAgent is registered on a walkable NavMesh, run
        // the following block.
        if (navAgent != null && navAgent.enabled && navAgent.isOnNavMesh)
        {
            // Clear navAgent current navigation route.
            navAgent.ResetPath();
            // Set navAgent.velocity to the zero vector (no movement or offset).
            navAgent.velocity = Vector3.zero;
        }
        // Call GhostEnemy.ApplyMovingAnimation with false; forward the movement state to the animation helper.
        ApplyMovingAnimation(false);
    }

    // isChasing: pursuit flag read by animation, audio and protection code. Updated by StopPursuit,
    // UpdateCheckpointPatrol, Repel and related helpers.
    [Header("State")]
    public bool isChasing = false;

    // isCaught: guard preventing repeated processing of a player catch. Updated by CatchPlayer.
    private bool isCaught = false;
    // hasStartedMoving: whether spawn-idle behavior has ended. Updated by UpdateCheckpointPatrol, Start, Update.
    private bool hasStartedMoving = false;
    // Tracks the intended moving state set in Update so LateUpdate does not override it.
    // intendedMoving: requested movement flag sent to animation. Update sets it from isMoving.
    private bool intendedMoving = false;

    // checkpointPatrolRadius: radius for patrol destinations near the saved checkpoint. Assigned in the
    // scene/prefab Inspector; the initializer below is the default.
    [Header("Checkpoint Patrol / Agimat")]
    [Min(1f)] public float checkpointPatrolRadius = 18f;
    // playerSafetyDistance: minimum clearance from the player when selecting safe patrol/retreat points. Assigned
    // in the scene/prefab Inspector; the initializer below is the default.
    [Min(1f)] public float playerSafetyDistance = 12f;
    // checkpointGraceDuration: gameplay seconds of safety after saving a checkpoint. Assigned in the scene/prefab
    // Inspector; the initializer below is the default.
    [Min(0f)] public float checkpointGraceDuration = 6f;
    // These timestamps and targets track checkpoint safety and agimat retreat.
    // safeUntil: gameplay timestamp when checkpoint safety ends. Updated by UpdateCheckpointPatrol.
    private float safeUntil;
    // repelledUntil: gameplay timestamp when agimat retreat ends. Updated by Repel.
    private float repelledUntil;
    // checkpointPatrol: whether the patrol center follows the saved checkpoint. Updated by UpdateCheckpointPatrol.
    private bool checkpointPatrol;
    // retreatTarget: world-space destination selected while repelled. PickRetreatTarget sets it from candidate.
    private Vector3 retreatTarget;
    // hasRetreatTarget: whether a retreat destination has been selected. Updated by PickRetreatTarget.
    private bool hasRetreatTarget;
    // nextRetreatSelection: gameplay timestamp when retreat selection may be retried. Updated by
    // PickRetreatTarget.
    private float nextRetreatSelection;
    // patrolHeight: reference height used to keep random patrol points near the correct floor. Start sets it from
    // transform.position Y component.
    private float patrolHeight;

    /// <summary>Listen for saved checkpoints while this enemy is active.</summary>
    // Subscribe UpdateCheckpointPatrol to RespawnPoint.CheckpointSaved so the callback runs when that event is
    // raised.
    private void OnEnable() => RespawnPoint.CheckpointSaved += UpdateCheckpointPatrol;
    /// <summary>Stop listening for checkpoint events when this enemy is disabled.</summary>
    // Unsubscribe UpdateCheckpointPatrol from RespawnPoint.CheckpointSaved so the callback stops running when that
    // event is raised.
    private void OnDisable() => RespawnPoint.CheckpointSaved -= UpdateCheckpointPatrol;

    /// <summary>Move patrol focus to the new checkpoint and grant a short player safety period.</summary>
    // checkpointPosition arrives from RespawnPoint.CheckpointSaved (or saved state at startup) as a world-space
    // patrol center.
    public void UpdateCheckpointPatrol(Vector3 checkpointPosition)
    {
        // If not (IsHostile), return now; skip the rest of this method.
        if (!IsHostile) return;
        // Copy checkpointPosition (argument from the caller) into automaticPatrolCenter.
        automaticPatrolCenter = checkpointPosition;
        // Update the vertical Y component automaticPatrolCenter.y from checkpointPosition Y component when
        // navigationReady; otherwise patrolHeight.
        automaticPatrolCenter.y = navigationReady ? checkpointPosition.y : patrolHeight;
        // Set checkpointPatrol to true.
        checkpointPatrol = true;
        // Set hasStartedMoving to true.
        hasStartedMoving = true;
        // Set hasAutomaticPatrolTarget to false.
        hasAutomaticPatrolTarget = false;
        // If requireNavigation and not (navigationReady), return now; skip the rest of this method.
        if (requireNavigation && !navigationReady) return;
        // Recalculate safeUntil from Unity elapsed gameplay time plus checkpointGraceDuration.
        safeUntil = Time.time + checkpointGraceDuration;
        // Set isChasing to false.
        isChasing = false;
        // Set losePlayerTimer to 0.
        losePlayerTimer = 0f;
        // If navigationReady and navAgent exists and navAgent is registered on a walkable NavMesh, clear navAgent
        // current navigation route.
        if (navigationReady && navAgent != null && navAgent.isOnNavMesh) navAgent.ResetPath();
        // Call GhostEnemy.SetNewAutomaticPatrolTarget; try random points within the patrol region and accept a
        // reachable, safe candidate.
        SetNewAutomaticPatrolTarget();
        // Move into the new patrol region without spawning beside the player.
        // If hasAutomaticPatrolTarget, run the following block.
        if (hasAutomaticPatrolTarget)
        {
            // If navigationReady and navAgent exists and navAgent is registered on a walkable NavMesh, run the
            // following block.
            if (navigationReady && navAgent != null && navAgent.isOnNavMesh)
                // Reposition navAgent NavMeshAgent at automaticPatrolTarget.
                navAgent.Warp(automaticPatrolTarget);
            else
                // Copy automaticPatrolTarget cached by this component into transform.position.
                transform.position = automaticPatrolTarget;
        }
    }

    /// <summary>Extend the retreat period when the player activates agimat protection.</summary>
    // duration comes from InvisibilityConsumable during agimat use and controls the temporary retreat deadline.
    public void Repel(float duration)
    {
        // If not (IsHostile), return now; skip the rest of this method.
        if (!IsHostile) return;
        // Store the largest of repelledUntil, Unity elapsed gameplay time plus duration in repelledUntil.
        repelledUntil = Mathf.Max(repelledUntil, Time.time + duration);
        // Set isChasing to false.
        isChasing = false;
        // Set losePlayerTimer to 0.
        losePlayerTimer = 0f;
        // Call GhostEnemy.PickRetreatTarget; try reachable points and choose one farther away from the player.
        PickRetreatTarget();
    }

    /// <summary>Try reachable points and choose one farther away from the player.</summary>
    private void PickRetreatTarget()
    {
        // If playerTransform is missing, return now; skip the rest of this method.
        if (playerTransform == null) return;
        // Store world position of transform minus world position of playerTransform in local away.
        Vector3 away = transform.position - playerTransform.position;
        // Update the vertical Y component away.y from 0.
        away.y = 0f;
        // If squared length of away (avoids a square root) is below 0.01, set away to negative playerTransform
        // forward axis expressed in world coordinates.
        if (away.sqrMagnitude < 0.01f) away = -playerTransform.forward;
        // Set hasRetreatTarget to false.
        hasRetreatTarget = false;
        // Recalculate nextRetreatSelection from Unity elapsed gameplay time plus 0.5.
        nextRetreatSelection = Time.time + 0.5f;
        // Store length of away in local bestDistance.
        float bestDistance = away.magnitude;
        // Repeat this block while attempt is below 16; start from int attempt = 0.
        for (int attempt = 0; attempt < 16; attempt++)
        {
            // Store Quaternion.Euler(0f, attempt * 22.5f, 0f) * away.normalized (calculated from the values shown)
            // in local direction.
            Vector3 direction = Quaternion.Euler(0f, attempt * 22.5f, 0f) * away.normalized;
            // Store transform.position + direction * playerSafetyDistance (calculated from the values shown) in
            // local candidate.
            Vector3 candidate = transform.position + direction * playerSafetyDistance;
            // If navigationReady, subtract navAgent.baseOffset from candidate.y and store the updated value there.
            if (navigationReady) candidate.y -= navAgent.baseOffset;
            // If navigationReady and navAgent is registered on a walkable NavMesh, run the following block.
            if (navigationReady && navAgent.isOnNavMesh)
            {
                // Store a new NavMeshQueryFilter instance in local filter.
                NavMeshQueryFilter filter = new NavMeshQueryFilter
                { agentTypeID = navAgent.agentTypeID, areaMask = navAgent.areaMask };
                // If not (NavMesh finds walkable ground near candidate; write the result through the out
                // argument), skip this entry and continue with the next loop iteration.
                if (!NavMesh.SamplePosition(candidate, out NavMeshHit hit, 3f, filter)) continue;
                // Set candidate to world position of hit.
                candidate = hit.position;
                // Store a new NavMeshPath instance in local path.
                NavMeshPath path = new NavMeshPath();
                // If not (navAgent found a navigation route using the supplied target and path object) or
                // path.status differs from NavMeshPathStatus.PathComplete, skip this entry and continue with the
                // next loop iteration.
                if (!navAgent.CalculatePath(candidate, path) || path.status != NavMeshPathStatus.PathComplete) continue;
            }
            else if (requireNavigation) return;
            // Store candidate minus world position of playerTransform in local offset.
            Vector3 offset = candidate - playerTransform.position;
            // Update the vertical Y component offset.y from 0.
            offset.y = 0f;
            // If length of offset is at most bestDistance, skip this entry and continue with the next loop
            // iteration.
            if (offset.magnitude <= bestDistance) continue;
            // Set bestDistance to length of offset.
            bestDistance = offset.magnitude;
            // Copy candidate into retreatTarget.
            retreatTarget = candidate;
            // Set hasRetreatTarget to true.
            hasRetreatTarget = true;
        }
        // If not (hasRetreatTarget) and navigationReady, clear navAgent current navigation route.
        if (!hasRetreatTarget && navigationReady) navAgent.ResetPath();
    }

    /// <summary>Refresh the retreat target when needed and move away at an increased patrol speed.</summary>
    private bool Retreat()
    {
        // If Unity elapsed gameplay time is at least nextRetreatSelection and (!hasRetreatTarget or
        // (navigationReady && !navAgent.pathPending && navAgent.remainingDistance < 1f)), run the following block.
        if (Time.time >= nextRetreatSelection && (!hasRetreatTarget ||
            (navigationReady && !navAgent.pathPending && navAgent.remainingDistance < 1f)))
            // Call GhostEnemy.PickRetreatTarget; try reachable points and choose one farther away from the player.
            PickRetreatTarget();
        // Return hasRetreatTarget and the result of GhostEnemy.MoveTo using retreatTarget, moveSpeed * 1.4f to the
        // method caller.
        return hasRetreatTarget && MoveTo(retreatTarget, moveSpeed * 1.4f);
    }

    /// <summary>Send a destination to navigation, or move directly when using the optional fallback.</summary>
    // destination is a Vector3 argument supplied by GhostEnemy.Retreat.
    // speed is a float argument supplied by GhostEnemy.Retreat.
    private bool MoveTo(Vector3 destination, float speed)
    {
        // Set waypointPathActive to false.
        waypointPathActive = false;
        // If navigationReady and navAgent exists and navAgent is registered on a walkable NavMesh, run the
        // following block.
        if (navigationReady && navAgent != null && navAgent.isOnNavMesh)
        {
            // Copy speed (argument from the caller) into navAgent.speed.
            navAgent.speed = speed;
            // Ask navAgent NavMeshAgent to plan a route to destination.
            navAgent.SetDestination(destination);
        }
        else
        {
            // Update the vertical Y component destination.y from transform.position Y component.
            destination.y = transform.position.y;
            // Store world position of transform moved toward destination by at most speed multiplied by Unity
            // gameplay frame duration in seconds this step in transform.position.
            transform.position = Vector3.MoveTowards(transform.position, destination, speed * Time.deltaTime);
        }
        // Return true to the method caller.
        return true;
    }

    /// <summary>Keep the agent disabled until the environment navigation surface is ready.</summary>
    private void Awake()
    {
        // Store the first NavMeshAgent component on this GameObject in navAgent.
        navAgent = GetComponent<NavMeshAgent>();
        // The scene serializes the agent disabled to avoid registration before the build.
        // Disable navAgent by changing its component enabled flag.
        navAgent.enabled = false;
    }

    /// <summary>Find scene references, place the agent on walkable ground and initialize patrol and animation.</summary>
    private void Start()
    {
        // If storyCompleted or (clueStory != null and clueStory.AllCluesCollected), run the following block.
        if (storyCompleted || (clueStory != null && clueStory.AllCluesCollected))
        {
            // Call GhostEnemy.BecomeNonHostile; permanent for this scene run; only a fresh scene can restore the
            // hunt.
            BecomeNonHostile();
            // Return now; skip the rest of this method.
            return;
        }
        // If playerTransform is missing, run the following block.
        if (playerTransform == null)
        {
            // Store the scene GameObject tagged "Player" in local playerObj.
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            // If playerObj exists, run the following block.
            if (playerObj != null)
                // Set playerTransform to Transform of playerObj.
                playerTransform = playerObj.transform;
        }

        // If loseSceneController is missing, run the following block.
        if (loseSceneController == null)
            // Store a scene LoseSceneController component (including inactive objects) in loseSceneController.
            loseSceneController = FindAnyObjectByType<LoseSceneController>(FindObjectsInactive.Include);

        // Copy moveSpeed from this component into navAgent.speed.
        navAgent.speed = moveSpeed;
        // Set navAgent.stoppingDistance to 0.1.
        navAgent.stoppingDistance = 0.1f;
        // Set navAgent.updateRotation to true.
        navAgent.updateRotation = true;
        // Set navAgent.updateUpAxis to false.
        navAgent.updateUpAxis = false;
        // Store a rotation built from angles 0, transform.eulerAngles Y component, 0 in degrees in
        // transform.rotation.
        transform.rotation = Quaternion.Euler(0f, transform.eulerAngles.y, 0f);
        // Store a new NavMeshQueryFilter instance in local filter.
        NavMeshQueryFilter filter = new NavMeshQueryFilter
        {
            agentTypeID = navAgent.agentTypeID,
            areaMask = navAgent.areaMask
        };
        // Store transform.position - Vector3.up * navAgent.baseOffset (calculated from the values shown) in local
        // feet.
        Vector3 feet = transform.position - Vector3.up * navAgent.baseOffset;
        // If NavMesh finds walkable ground near feet; write the result through the out argument, run the following
        // block.
        if (NavMesh.SamplePosition(feet, out NavMeshHit spawn, navMeshPlacementDistance, filter))
        {
            // Set transform.position to spawn.position + Vector3.up * navAgent.baseOffset (calculated from the
            // values shown).
            transform.position = spawn.position + Vector3.up * navAgent.baseOffset;
            // Enable navAgent by changing its component enabled flag.
            navAgent.enabled = true;
            // Set navigationReady to whether navAgent is registered on a walkable NavMesh.
            navigationReady = navAgent.isOnNavMesh;
        }
        // If not (navigationReady), run the following block.
        if (!navigationReady)
            // Report the failed setup or state as a Unity Console error.
            Debug.LogError("[Ghost] No walkable NavMesh near the ghost spawn. Check the environment surface and spawn placement.", this);

        // Resolve the animator from GhostAnimation if present; fall back to any child Animator.
        // Store the first GhostAnimation component on this GameObject in local animations.
        GhostAnimation animations = GetComponent<GhostAnimation>();
        // If animations exists and animations.Animator exists, set animator to animations.Animator.
        if (animations != null && animations.Animator != null) animator = animations.Animator;
        // If animator is missing, store the first Animator component on or beneath this GameObject in animator.
        if (animator == null) animator = GetComponentInChildren<Animator>();
        // If animator exists, set animator.applyRootMotion to false.
        if (animator != null) animator.applyRootMotion = false;
        // Set previousPosition to world position of transform.
        previousPosition = transform.position;
        // Set automaticPatrolCenter to transform.position - Vector3.up * navAgent.baseOffset (calculated from the
        // values shown) when navigationReady; otherwise world position of transform.
        automaticPatrolCenter = navigationReady ? transform.position - Vector3.up * navAgent.baseOffset : transform.position;
        // Set patrolHeight to transform.position Y component.
        patrolHeight = transform.position.y;
        // Set hasStartedMoving to not (waitForPlayerBeforePatrolling).
        hasStartedMoving = !waitForPlayerBeforePatrolling;
        // Call GhostEnemy.SetNewAutomaticPatrolTarget; try random points within the patrol region and accept a
        // reachable, safe candidate.
        SetNewAutomaticPatrolTarget();
        // Call GhostEnemy.ApplyMovingAnimation with false; forward the movement state to the animation helper.
        ApplyMovingAnimation(false);
        // If RespawnPoint shared checkpoint-discovered flag, run the following block.
        if (RespawnPoint.hasReachedCheckpoint)
            // Call GhostEnemy.UpdateCheckpointPatrol with the world spawn position saved by
            // RespawnPoint.ActivateCheckpoint; move patrol focus to the new checkpoint and grant a short player
            // safety period.
            UpdateCheckpointPatrol(RespawnPoint.activeCheckpointPosition);
    }

    /// <summary>Each frame, check contact, protection and detection, then choose retreat, patrol or chase.</summary>
    private void Update()
    {
        // If not (IsHostile), run the following block.
        if (!IsHostile)
        {
            // If not (storyCompleted) and clueStory exists and clueStory Note completion check from Level2NoteNpc,
            // run the following block.
            if (!storyCompleted && clueStory != null && clueStory.AllCluesCollected)
                // Call GhostEnemy.BecomeNonHostile; permanent for this scene run; only a fresh scene can restore
                // the hunt.
                BecomeNonHostile();
            /// <summary>Clear chase state, stop the navigation route and switch to idle animation.</summary>
            else StopPursuit();
            // Return now; skip the rest of this method.
            return;
        }
        // Stop this method early when playerTransform == null or isCaught or Unity gameplay clock multiplier (0
        // freezes gameplay time) is at most 0f or PauseManager.IsGamePaused.
        if (playerTransform == null || isCaught ||
            Time.timeScale <= 0f || PauseManager.IsGamePaused)
            // Return now; skip the rest of this method.
            return;

        // Count down the catch delay and compare horizontal distance and height to avoid catching across floors.
        // If catchCooldownTimer is above 0, run the following block.
        if (catchCooldownTimer > 0f)
            // Subtract Unity gameplay frame duration in seconds from catchCooldownTimer and store the updated
            // value there.
            catchCooldownTimer -= Time.deltaTime;

        // Store world position of playerTransform minus world position of transform in local playerOffset.
        Vector3 playerOffset = playerTransform.position - transform.position;
        // Store length of new Vector2(playerOffset.x, playerOffset.z) in local distanceToPlayer.
        float distanceToPlayer = new Vector2(playerOffset.x, playerOffset.z).magnitude;
        // Store the absolute value of playerOffset.y is at most navAgent.height in local sameFloor.
        bool sameFloor = Mathf.Abs(playerOffset.y) <= navAgent.height;

        // Contact remains dangerous during patrol, even before hunting unlocks
        // or if navigation failed. Safety effects are checked by CatchPlayer.
        // If catchCooldownTimer <= 0f and sameFloor and distanceToPlayer is at most GetCatchDistance() and the
        // result of GhostEnemy.HasLineOfSight, run the following block.
        if (catchCooldownTimer <= 0f && sameFloor &&
            distanceToPlayer <= GetCatchDistance() && HasLineOfSight())
        {
            // Call GhostEnemy.TryCatchPlayerByDistance; after a close-contact check, reject protection and
            // disabled player controllers before catching.
            TryCatchPlayerByDistance();
            // If isCaught, return now; skip the rest of this method.
            if (isCaught) return;
        }
        // If requireNavigation and not (navigationReady), return now; skip the rest of this method.
        if (requireNavigation && !navigationReady) return;

        // Agimat protection makes the ghost retreat; checkpoint grace keeps the player safe for a short time.
        // If Unity elapsed gameplay time is below repelledUntil or the result of GhostEnemy.IsPlayerInvisible, run
        // the following block.
        if (Time.time < repelledUntil || IsPlayerInvisible())
        {
            // Set isChasing to false.
            isChasing = false;
            // Set losePlayerTimer to 0.
            losePlayerTimer = 0f;
            // Store the result of GhostEnemy.Retreat in intendedMoving.
            intendedMoving = Retreat();
            // Return now; skip the rest of this method.
            return;
        }
        // If Unity elapsed gameplay time is below safeUntil, run the following block.
        if (Time.time < safeUntil)
        {
            // Set isChasing to false.
            isChasing = false;
            // Set losePlayerTimer to 0.
            losePlayerTimer = 0f;
            // Set intendedMoving to the result of GhostEnemy.Retreat when distanceToPlayer is below
            // playerSafetyDistance; otherwise the result of GhostEnemy.AutomaticPatrol.
            intendedMoving = distanceToPlayer < playerSafetyDistance ? Retreat() : AutomaticPatrol();
            // Return now; skip the rest of this method.
            return;
        }

        // The Note quest or enough artifacts enables hunting; until then the ghost follows patrol behavior.
        // Store clueStory exists or (ArtifactManager.Instance != null and
        // ArtifactManager.Instance.artifactsCollected >= Mathf.Max(1, artifactsBeforeHunt)) in local huntUnlocked.
        bool huntUnlocked = clueStory != null || (ArtifactManager.Instance != null &&
            ArtifactManager.Instance.artifactsCollected >= Mathf.Max(1, artifactsBeforeHunt));
        // If not (huntUnlocked), run the following block.
        if (!huntUnlocked)
        {
            // Set isChasing to false.
            isChasing = false;
            // Set losePlayerTimer to 0.
            losePlayerTimer = 0f;
            // Store the result of GhostEnemy.Patrol in intendedMoving.
            intendedMoving = Patrol();
            // Return now; skip the rest of this method.
            return;
        }

        // If configured to wait at spawn, stay idle until the player comes close enough.
        // If not (hasStartedMoving), run the following block.
        if (!hasStartedMoving)
        {
            // Set isChasing to false.
            isChasing = false;
            // Set intendedMoving to false.
            intendedMoving = false;

            // If distanceToPlayer is at most spawnIdleDistance, run the following block.
            if (distanceToPlayer <= spawnIdleDistance)
                // Set hasStartedMoving to true.
                hasStartedMoving = true;
            else
                // Return now; skip the rest of this method.
                return;
        }

        // Store the result of GhostEnemy.IsPlayerInvisible in local isPlayerInvisible.
        bool isPlayerInvisible = IsPlayerInvisible();

        // Determine whether the ghost can currently detect the player.
        // Store sameFloor and not (isPlayerInvisible) and distanceToPlayer is at most detectionRadius in local
        // playerDetected.
        bool playerDetected = sameFloor && !isPlayerInvisible && distanceToPlayer <= detectionRadius;

        // Apply line-of-sight check: only start chasing if there is a clear sightline.
        // If playerDetected and requireLineOfSight, run the following block.
        if (playerDetected && requireLineOfSight)
            // Store the result of GhostEnemy.HasLineOfSight in playerDetected.
            playerDetected = HasLineOfSight();

        // Persist the chase for losePlayerCooldown seconds after losing detection.
        // If playerDetected, run the following block.
        if (playerDetected)
        {
            // Set isChasing to true.
            isChasing = true;
            // Copy losePlayerCooldown from this component into losePlayerTimer.
            losePlayerTimer = losePlayerCooldown;
        }
        else if (isChasing)
        {
            // Subtract Unity gameplay frame duration in seconds from losePlayerTimer and store the updated value
            // there.
            losePlayerTimer -= Time.deltaTime;
            // If losePlayerTimer is at most 0, run the following block.
            if (losePlayerTimer <= 0f)
            {
                // Set isChasing to false.
                isChasing = false;
                // Set losePlayerTimer to 0.
                losePlayerTimer = 0f;
            }
        }

        // Declare local isMoving to hold bool data supplied later in this method.
        bool isMoving;

        // If isChasing, run the following block.
        if (isChasing)
            // Store the result of GhostEnemy.ChasePlayer in isMoving.
            isMoving = ChasePlayer();
        else
            // Store the result of GhostEnemy.Patrol in isMoving.
            isMoving = Patrol();

        // Copy isMoving into intendedMoving.
        intendedMoving = isMoving;

        // Keep rotation flat — NavMeshAgent handles yaw; prevent pitch/roll drift.
        // Store transform.eulerAngles in local euler.
        Vector3 euler = transform.eulerAngles;
        // Set transform.eulerAngles to a Vector3 built from 0, euler Y component, 0.
        transform.eulerAngles = new Vector3(0f, euler.y, 0f);
    }

    // Reuse a fixed hit buffer for sight checks instead of allocating one every frame.
    // sightHits: reused Physics hit buffer, avoiding per-frame array allocation. Initialized from a new
    // array/object initialized with the entries specified here.
    private readonly RaycastHit[] sightHits = new RaycastHit[32];
    // nextChasePathUpdate: gameplay timestamp for the next pursuit-route refresh. Updated by ChasePlayer.
    private float nextChasePathUpdate;

    /// <summary>Raycast toward the player and reject solid obstacles, ignoring both character hierarchies.</summary>
    private bool HasLineOfSight()
    {
        // If playerTransform is missing, return false to the method caller.
        if (playerTransform == null) return false;

        // Cast from the ghost's approximate eye level to the player's center.
        // Store transform.position + Vector3.up * (navAgent.height * 0.85f - navAgent.baseOffset) (calculated from
        // the values shown) in local origin.
        Vector3 origin = transform.position + Vector3.up * (navAgent.height * 0.85f - navAgent.baseOffset);
        // Store the first CharacterController component on playerTransform in local controller.
        CharacterController controller = playerTransform.GetComponent<CharacterController>();
        // Store controller.bounds center when controller exists; otherwise world position of playerTransform plus
        // the world-up direction (positive Y) in local target.
        Vector3 target = controller != null ? controller.bounds.center : playerTransform.position + Vector3.up;
        // Store target minus origin in local direction.
        Vector3 direction = target - origin;

        // The ghost can share Default with walls; filter by hierarchy, not layer.
        // Store the number of Physics ray hits written to the supplied reusable hit buffer in local count.
        int count = Physics.RaycastNonAlloc(origin, direction.normalized, sightHits,
            direction.magnitude, lineOfSightMask, QueryTriggerInteraction.Ignore);
        // If the hit buffer fills, treat sight as blocked because additional obstacles may be missing.
        // If count equals length of sightHits, return false to the method caller.
        if (count == sightHits.Length) return false;
        // Repeat this block while i is below count; start from int i = 0.
        for (int i = 0; i < count; i++)
        {
            // Store sightHits[i] (entry read from sightHits) in local hit.
            RaycastHit hit = sightHits[i];
            // If hit.transform belongs to the Transform on this component GameObject Transform hierarchy or
            // hit.transform belongs to playerTransform Transform hierarchy, skip this entry and continue with the
            // next loop iteration.
            if (hit.transform.IsChildOf(transform) || hit.transform.IsChildOf(playerTransform)) continue;
            // Return false to the method caller.
            return false;
        }
        // Return true to the method caller.
        return true;
    }

    /// <summary>Read the shared agimat protection flag before detecting or catching the player.</summary>
    private bool IsPlayerInvisible()
    {
        // Return the player protection controller registered in Awake exists and InvisibilityConsumable.Instance
        // protection flag from InvisibilityConsumable to the method caller.
        return InvisibilityConsumable.Instance != null &&
               InvisibilityConsumable.Instance.isInvisible;
    }

    /// <summary>Use the configured catch radius, falling back to the older catch distance value.</summary>
    private float GetCatchDistance()
    {
        // If catchRadius is above 0, run the following block.
        if (catchRadius > 0f)
            // Return catchRadius to the method caller.
            return catchRadius;

        // Return catchDistance to the method caller.
        return catchDistance;
    }

    /// <summary>After a close-contact check, reject protection and disabled player controllers before catching.</summary>
    private void TryCatchPlayerByDistance()
    {
        // Stop this method early when !IsHostile || playerTransform == null or IsPlayerInvisible() or Unity
        // elapsed gameplay time is below safeUntil or Unity elapsed gameplay time is below repelledUntil.
        if (!IsHostile || playerTransform == null || IsPlayerInvisible() || Time.time < safeUntil || Time.time < repelledUntil)
            // Return now; skip the rest of this method.
            return;

        // Store the first CharacterController component on playerTransform in local playerController.
        CharacterController playerController = playerTransform.GetComponent<CharacterController>();

        // Stop this method early when playerController exists and not (enabled state of playerController).
        if (playerController != null && !playerController.enabled)
            // Return now; skip the rest of this method.
            return;

        // Write the diagnostic message to the Unity Console; it does not update game UI.
        Debug.Log("[Ghost] Player entered the physical catch radius. Showing Lose Canvas.");
        // Call GhostEnemy.CatchPlayer; commit one catch, stop pursuit and player controls, play the scare and open
        // the lose UI.
        CatchPlayer();
    }

    /// <summary>Follow reachable waypoints in order, or use automatic patrol when no route is assigned.</summary>
    private bool Patrol()
    {
        // If checkpointPatrol or patrolWaypoints is missing or length of patrolWaypoints equals 0, run the
        // following block.
        if (checkpointPatrol || patrolWaypoints == null || patrolWaypoints.Length == 0)
            // Return (checkpointPatrol or useAutomaticPatrol) and the result of GhostEnemy.AutomaticPatrol to the
            // method caller.
            return (checkpointPatrol || useAutomaticPatrol) && AutomaticPatrol();

        // If navigationReady and navAgent exists and navAgent is registered on a walkable NavMesh, run the
        // following block.
        if (navigationReady && navAgent != null && navAgent.isOnNavMesh)
        {
            // If navAgent is still calculating its route, return true to the method caller.
            if (navAgent.pathPending) return true;
            // If navAgent.hasPath && navAgent.pathStatus == NavMeshPathStatus.PathComplete and
            // navAgent.remainingDistance > Mathf.Max(navAgent.stoppingDistance + 0.1f, 0.3f) and not (isChasing)
            // and waypointPathActive, run the following block.
            if (navAgent.hasPath && navAgent.pathStatus == NavMeshPathStatus.PathComplete &&
                navAgent.remainingDistance > Mathf.Max(navAgent.stoppingDistance + 0.1f, 0.3f) &&
                !isChasing && waypointPathActive)
                // Return true to the method caller.
                return true;

            // If waypointPathActive, run the following block.
            if (waypointPathActive)
                // Recalculate currentWaypointIndex from (currentWaypointIndex + 1) % patrolWaypoints.Length
                // (calculated from the values shown).
                currentWaypointIndex = (currentWaypointIndex + 1) % patrolWaypoints.Length;
            // Set waypointPathActive to false.
            waypointPathActive = false;
            // If Unity elapsed gameplay time is below nextWaypointRetry, return false to the method caller.
            if (Time.time < nextWaypointRetry) return false;

            // Store a new NavMeshQueryFilter instance in local filter.
            NavMeshQueryFilter filter = new NavMeshQueryFilter
            { agentTypeID = navAgent.agentTypeID, areaMask = navAgent.areaMask };
            // Try each waypoint until a full navigation route is found; skip null or unreachable stops.
            // Repeat this block while attempt is below length of patrolWaypoints; start from int attempt = 0.
            for (int attempt = 0; attempt < patrolWaypoints.Length; attempt++)
            {
                // Store patrolWaypoints[currentWaypointIndex] (entry read from patrolWaypoints) in local waypoint.
                Transform waypoint = patrolWaypoints[currentWaypointIndex];
                // Store a new NavMeshPath instance in local path.
                NavMeshPath path = new NavMeshPath();
                // If waypoint != null and NavMesh.SamplePosition(waypoint.position, out NavMeshHit hit, 2f,
                // filter) and navAgent found a navigation route using the supplied target and path object and
                // path.status equals NavMeshPathStatus.PathComplete, run the following block.
                if (waypoint != null && NavMesh.SamplePosition(waypoint.position,
                    out NavMeshHit hit, 2f, filter) && navAgent.CalculatePath(hit.position, path) &&
                    path.status == NavMeshPathStatus.PathComplete)
                {
                    // Copy moveSpeed from this component into navAgent.speed.
                    navAgent.speed = moveSpeed;
                    // Store whether navAgent accepted the path from path in waypointPathActive.
                    waypointPathActive = navAgent.SetPath(path);
                    // Return waypointPathActive to the method caller.
                    return waypointPathActive;
                }
                // Recalculate currentWaypointIndex from (currentWaypointIndex + 1) % patrolWaypoints.Length
                // (calculated from the values shown).
                currentWaypointIndex = (currentWaypointIndex + 1) % patrolWaypoints.Length;
            }
            // Clear navAgent current navigation route.
            navAgent.ResetPath();
            // Recalculate nextWaypointRetry from Unity elapsed gameplay time plus 1.
            nextWaypointRetry = Time.time + 1f;
            // Return false to the method caller.
            return false;
        }

        // Store patrolWaypoints[currentWaypointIndex] (entry read from patrolWaypoints) in local wpFallback.
        Transform wpFallback = patrolWaypoints[currentWaypointIndex];
        // If wpFallback is missing, return false to the method caller.
        if (wpFallback == null) return false;

        // Store world position of transform moved toward world position of wpFallback by at most moveSpeed
        // multiplied by Unity gameplay frame duration in seconds this step in transform.position.
        transform.position = Vector3.MoveTowards(
            transform.position,
            wpFallback.position,
            moveSpeed * Time.deltaTime
        );

        // Store (wpFallback.position - transform.position) scaled to unit length (zero stays zero) in local dir.
        Vector3 dir = (wpFallback.position - transform.position).normalized;
        // If dir differs from the zero vector (no movement or offset), run the following block.
        if (dir != Vector3.zero)
        {
            // Store a rotation blend from world rotation of transform to a rotation facing direction dir using
            // fraction Unity gameplay frame duration in seconds multiplied by 5f in transform.rotation.
            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                Quaternion.LookRotation(dir),
                Time.deltaTime * 5f
            );
        }

        // If the distance between transform.position and wpFallback.position is below 0.3, run the following
        // block.
        if (Vector3.Distance(transform.position, wpFallback.position) < 0.3f)
            // Recalculate currentWaypointIndex from (currentWaypointIndex + 1) % patrolWaypoints.Length
            // (calculated from the values shown).
            currentWaypointIndex = (currentWaypointIndex + 1) % patrolWaypoints.Length;

        // Return true to the method caller.
        return true;
    }

    /// <summary>Move toward a random patrol destination and replace it on arrival or when unsafe.</summary>
    private bool AutomaticPatrol()
    {
        // Set waypointPathActive to false.
        waypointPathActive = false;
        // If hasAutomaticPatrolTarget and checkpointPatrol and playerTransform exists and the distance between
        // automaticPatrolTarget and playerTransform.position is below playerSafetyDistance, run the following
        // block.
        if (hasAutomaticPatrolTarget && checkpointPatrol && playerTransform != null &&
            Vector3.Distance(automaticPatrolTarget, playerTransform.position) < playerSafetyDistance)
            // Set hasAutomaticPatrolTarget to false.
            hasAutomaticPatrolTarget = false;
        // If not (hasAutomaticPatrolTarget), run the following block.
        if (!hasAutomaticPatrolTarget)
            // Call GhostEnemy.SetNewAutomaticPatrolTarget; try random points within the patrol region and accept a
            // reachable, safe candidate.
            SetNewAutomaticPatrolTarget();

        // If not (hasAutomaticPatrolTarget), return false to the method caller.
        if (!hasAutomaticPatrolTarget) return false;

        // If navigationReady and navAgent exists and navAgent is registered on a walkable NavMesh, run the
        // following block.
        if (navigationReady && navAgent != null && navAgent.isOnNavMesh)
        {
            // Copy moveSpeed from this component into navAgent.speed.
            navAgent.speed = moveSpeed;
            // Ask navAgent NavMeshAgent to plan a route to automaticPatrolTarget.
            navAgent.SetDestination(automaticPatrolTarget);

            // If not (navAgent is still calculating its route) and navAgent distance remaining along its route is
            // at most automaticPatrolPointTolerance, run the following block.
            if (!navAgent.pathPending && navAgent.remainingDistance <= automaticPatrolPointTolerance)
                // Call GhostEnemy.SetNewAutomaticPatrolTarget; try random points within the patrol region and
                // accept a reachable, safe candidate.
                SetNewAutomaticPatrolTarget();

            // Return true to the method caller.
            return true;
        }

        // Store world position of transform moved toward automaticPatrolTarget by at most moveSpeed multiplied by
        // Unity gameplay frame duration in seconds this step in transform.position.
        transform.position = Vector3.MoveTowards(
            transform.position,
            automaticPatrolTarget,
            moveSpeed * Time.deltaTime
        );

        // Store automaticPatrolTarget minus world position of transform in local direction.
        Vector3 direction = automaticPatrolTarget - transform.position;
        // Update the vertical Y component direction.y from 0.
        direction.y = 0f;

        // If squared length of direction (avoids a square root) is above 0.01, run the following block.
        if (direction.sqrMagnitude > 0.01f)
        {
            // Store a rotation blend from world rotation of transform to a rotation facing direction
            // direction.normalized using fraction Unity gameplay frame duration in seconds multiplied by 5f in
            // transform.rotation.
            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                Quaternion.LookRotation(direction.normalized),
                Time.deltaTime * 5f
            );
        }

        // If the distance between transform.position and automaticPatrolTarget is at most
        // automaticPatrolPointTolerance, run the following block.
        if (Vector3.Distance(transform.position, automaticPatrolTarget) <= automaticPatrolPointTolerance)
            // Call GhostEnemy.SetNewAutomaticPatrolTarget; try random points within the patrol region and accept a
            // reachable, safe candidate.
            SetNewAutomaticPatrolTarget();

        // Return true to the method caller.
        return true;
    }

    /// <summary>Try random points within the patrol region and accept a reachable, safe candidate.</summary>
    private void SetNewAutomaticPatrolTarget()
    {
        // Set hasAutomaticPatrolTarget to false.
        hasAutomaticPatrolTarget = false;
        // Store checkpointPatrolRadius when checkpointPatrol; otherwise automaticPatrolRadius in local
        // patrolRadius.
        float patrolRadius = checkpointPatrol ? checkpointPatrolRadius : automaticPatrolRadius;
        // Limit random target attempts so a blocked patrol region cannot keep this frame running forever.
        // Repeat this block while attempt is below 32; start from int attempt = 0.
        for (int attempt = 0; attempt < 32; attempt++)
        {
            // Store Random.insideUnitCircle.normalized * Random.Range(patrolRadius * 0.55f, patrolRadius)
            // (calculated from the values shown) in local offset.
            Vector2 offset = Random.insideUnitCircle.normalized * Random.Range(patrolRadius * 0.55f, patrolRadius);
            // Store automaticPatrolCenter + new Vector3(offset.x, 0f, offset.y) (calculated from the values shown)
            // in local candidate.
            Vector3 candidate = automaticPatrolCenter + new Vector3(offset.x, 0f, offset.y);
            // If navigationReady and navAgent exists and navAgent is registered on a walkable NavMesh, run the
            // following block.
            if (navigationReady && navAgent != null && navAgent.isOnNavMesh)
            {
                // If not (NavMesh finds walkable ground near candidate; write the result through the out
                // argument), skip this entry and continue with the next loop iteration.
                if (!NavMesh.SamplePosition(candidate, out NavMeshHit hit, 3f, navAgent.areaMask)) continue;
                // Set candidate to world position of hit.
                candidate = hit.position;
                // Store a new NavMeshPath instance in local path.
                NavMeshPath path = new NavMeshPath();
                // If not (navAgent found a navigation route using the supplied target and path object) or
                // path.status differs from NavMeshPathStatus.PathComplete, skip this entry and continue with the
                // next loop iteration.
                if (!navAgent.CalculatePath(candidate, path) || path.status != NavMeshPathStatus.PathComplete) continue;
            }
            else if (requireNavigation)
            {
                // Return now; skip the rest of this method.
                return;
            }
            else if (checkpointPatrol)
            {
                // PoliceHQ has no baked NavMesh: only choose clear points above a floor.
                // Skip this loop entry when not (the ray hits a collider within the specified range/layers; store
                // collider, point and distance in RaycastHit floor) or floor.normal Y component is below 0.7.
                if (!Physics.Raycast(candidate + Vector3.up, Vector3.down, out RaycastHit floor,
                    5f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore) || floor.normal.y < 0.7f)
                    // Skip this entry and continue with the next loop iteration.
                    continue;
                // Skip this loop entry when the result of Physics.CheckSphere using candidate, 0.6, Unity default
                // layers for raycast collision queries, QueryTriggerInteraction.Ignore.
                if (Physics.CheckSphere(candidate, 0.6f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
                    // Skip this entry and continue with the next loop iteration.
                    continue;
            }
            // Store world position of playerTransform when playerTransform exists; otherwise automaticPatrolCenter
            // in local playerPosition.
            Vector3 playerPosition = playerTransform != null ? playerTransform.position : automaticPatrolCenter;
            // Update the vertical Y component playerPosition.y from candidate Y component.
            playerPosition.y = candidate.y;
            // Skip this loop entry when checkpointPatrol and (Vector3.Distance(candidate, playerPosition) <
            // playerSafetyDistance or Vector3.Distance(candidate, automaticPatrolCenter) < playerSafetyDistance).
            if (checkpointPatrol && (Vector3.Distance(candidate, playerPosition) < playerSafetyDistance ||
                Vector3.Distance(candidate, automaticPatrolCenter) < playerSafetyDistance)) continue;
            // Copy candidate into automaticPatrolTarget.
            automaticPatrolTarget = candidate;
            // Set hasAutomaticPatrolTarget to true.
            hasAutomaticPatrolTarget = true;
            // Return now; skip the rest of this method.
            return;
        }
    }

    /// <summary>Follow the player at chase speed, refreshing navigation destinations at short intervals.</summary>
    private bool ChasePlayer()
    {
        // Set waypointPathActive to false.
        waypointPathActive = false;
        // If playerTransform is missing, run the following block.
        if (playerTransform == null)
            // Return false to the method caller.
            return false;

        // Store chaseSpeed when chaseSpeed is above moveSpeed; otherwise moveSpeed in local speed.
        float speed = chaseSpeed > moveSpeed ? chaseSpeed : moveSpeed;

        // If navigationReady and navAgent exists and navAgent is registered on a walkable NavMesh, run the
        // following block.
        if (navigationReady && navAgent != null && navAgent.isOnNavMesh)
        {
            // Copy speed into navAgent.speed.
            navAgent.speed = speed;
            // If Unity elapsed gameplay time is at least nextChasePathUpdate, run the following block.
            if (Time.time >= nextChasePathUpdate)
            {
                // Recalculate nextChasePathUpdate from Unity elapsed gameplay time plus 0.2.
                nextChasePathUpdate = Time.time + 0.2f;
                // The player's transform can be above the floor or just outside
                // agent clearance near a wall. Target the nearest walkable point.
                // Store a new NavMeshQueryFilter instance in local filter.
                NavMeshQueryFilter filter = new NavMeshQueryFilter
                { agentTypeID = navAgent.agentTypeID, areaMask = navAgent.areaMask };
                // If NavMesh finds walkable ground near world position of playerTransform; write the result
                // through the out argument, run the following block.
                if (NavMesh.SamplePosition(playerTransform.position, out NavMeshHit hit, 2f, filter))
                    // Ask navAgent NavMeshAgent to plan a route to world position of hit.
                    navAgent.SetDestination(hit.position);
            }
            // Return true to the method caller.
            return true;
        }

        // Store world position of transform moved toward world position of playerTransform by at most speed
        // multiplied by Unity gameplay frame duration in seconds this step in transform.position.
        transform.position = Vector3.MoveTowards(
            transform.position,
            playerTransform.position,
            speed * Time.deltaTime
        );

        // Store (playerTransform.position - transform.position) scaled to unit length (zero stays zero) in local
        // direction.
        Vector3 direction = (playerTransform.position - transform.position).normalized;
        // If direction differs from the zero vector (no movement or offset), run the following block.
        if (direction != Vector3.zero)
        {
            // Store a rotation blend from world rotation of transform to a rotation facing direction direction
            // using fraction Unity gameplay frame duration in seconds multiplied by 5f in transform.rotation.
            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                Quaternion.LookRotation(direction),
                Time.deltaTime * 5f
            );
        }

        // Return true to the method caller.
        return true;
    }

    /// <summary>Use actual movement after navigation to decide whether the walk animation should play.</summary>
    private void LateUpdate()
    {
        // If Unity gameplay clock multiplier (0 freezes gameplay time) is at most 0 or PauseManager.IsGamePaused,
        // return now; skip the rest of this method.
        if (Time.timeScale <= 0f || PauseManager.IsGamePaused) return;
        // Store (transform.position - previousPosition).magnitude / Mathf.Max(Time.deltaTime, 0.001f) (calculated
        // from the values shown) in local speed.
        float speed = (transform.position - previousPosition).magnitude / Mathf.Max(Time.deltaTime, 0.001f);
        // Call GhostEnemy.ApplyMovingAnimation with intendedMoving and speed > 0.01f and not (isCaught); forward
        // the movement state to the animation helper.
        ApplyMovingAnimation(intendedMoving && speed > 0.01f && !isCaught);
        // Set previousPosition to world position of transform.
        previousPosition = transform.position;
    }

    /// <summary>Forward the movement state to the animation helper.</summary>
    // moving is a bool argument supplied by GhostEnemy.CatchPlayer, GhostEnemy.LateUpdate,
    // GhostEnemy.SetMovingAnimation or another caller.
    private void ApplyMovingAnimation(bool moving)
    {
        // Call GhostAnimation.SetMoving with animator, movingParameter, moving; use this custom driver when
        // present, otherwise set a matching Animator bool parameter.
        GhostAnimation.SetMoving(animator, movingParameter, moving);
    }

    // Legacy name kept for external callers (e.g. ArtifactCornerScare).
    /// <summary>Keep the intended movement flag and visible animation in sync for this helper.</summary>
    // moving is a bool argument supplied by the code, event or Inspector callback invoking SetMovingAnimation.
    private void SetMovingAnimation(bool moving)
    {
        // Copy moving (argument from the caller) into intendedMoving.
        intendedMoving = moving;
        // Call GhostEnemy.ApplyMovingAnimation with moving; forward the movement state to the animation helper.
        ApplyMovingAnimation(moving);
    }

    /// <summary>Commit one catch, stop pursuit and player controls, play the scare and open the lose UI.</summary>
    private void CatchPlayer()
    {
        // Stop this method early when !IsHostile || isCaught or IsPlayerInvisible() or Unity elapsed gameplay time
        // is below safeUntil or Unity elapsed gameplay time is below repelledUntil.
        if (!IsHostile || isCaught || IsPlayerInvisible() || Time.time < safeUntil || Time.time < repelledUntil)
            // Return now; skip the rest of this method.
            return;

        // Set the caught guard before opening UI so another frame cannot trigger a second loss.
        // Set isCaught to true.
        isCaught = true;
        // Set isChasing to false.
        isChasing = false;
        // Set losePlayerTimer to 0.
        losePlayerTimer = 0f;
        // Copy catchCooldown from this component into catchCooldownTimer.
        catchCooldownTimer = catchCooldown;

        // Write the diagnostic message to the Unity Console; it does not update game UI.
        Debug.Log("<color=red>[Ghost]</color> Caught the player!");

        // Set intendedMoving to false.
        intendedMoving = false;
        // Call GhostEnemy.ApplyMovingAnimation with false; forward the movement state to the animation helper.
        ApplyMovingAnimation(false);
        // Call GhostAnimation.PlayScream with animator, screamParameter; restart the custom scream and return its
        // duration, or trigger a regular Animator scream.
        GhostAnimation.PlayScream(animator, screamParameter);
        // If animator exists, set animator.updateMode to AnimatorUpdateMode.UnscaledTime.
        if (animator != null) animator.updateMode = AnimatorUpdateMode.UnscaledTime;
        // If navigationReady, clear navAgent current navigation route.
        if (navigationReady) navAgent.ResetPath();

        // Disable player controls so they cannot move or look during the lose screen.
        // If playerTransform exists, run the following block.
        if (playerTransform != null)
        {
            // Store the first PlayerMovement component on playerTransform in local movement.
            PlayerMovement movement = playerTransform.GetComponent<PlayerMovement>();
            // If movement exists, disable movement by changing its component enabled flag.
            if (movement != null) movement.enabled = false;

            // Store the first MouseLook component on or beneath playerTransform in local mouseLook.
            MouseLook mouseLook = playerTransform.GetComponentInChildren<MouseLook>();
            // If mouseLook exists, disable mouseLook by changing its component enabled flag.
            if (mouseLook != null) mouseLook.enabled = false;

            // Store the first PlayerAudio component on playerTransform in local playerAudio.
            PlayerAudio playerAudio = playerTransform.GetComponent<PlayerAudio>();
            // If playerAudio exists, run the following block.
            if (playerAudio != null)
                // Call PlayerAudio.PlayCaught; play the assigned caught clip, or report that it needs an Inspector
                // assignment.
                playerAudio.PlayCaught();
        }

        // If loseSceneController is missing, run the following block.
        if (loseSceneController == null)
            // Store a scene LoseSceneController component (including inactive objects) in loseSceneController.
            loseSceneController = FindAnyObjectByType<LoseSceneController>(FindObjectsInactive.Include);

        // If loseSceneController exists, run the following block.
        if (loseSceneController != null)
        {
            // Write the diagnostic message to the Unity Console; it does not update game UI.
            Debug.Log("[Ghost] LoseSceneController found. Calling ShowLoseScreen().");
            // Call LoseSceneController.ShowLoseScreen; hide gameplay UI, reveal the lose panel, freeze gameplay
            // and allow pointer input.
            loseSceneController.ShowLoseScreen();
        }
        else
        {
            // Report the failed setup or state as a Unity Console error.
            Debug.LogError("[Ghost] No LoseSceneController found in the scene, including inactive objects.");
        }
    }

    /// <summary>Draw detection and catch ranges in the Editor to help tune enemy placement.</summary>
    private void OnDrawGizmosSelected()
    {
        // Set Gizmos.color to Color.yellow.
        Gizmos.color = Color.yellow;
        // Call Gizmos.DrawWireSphere using world position of transform, detectionRadius.
        Gizmos.DrawWireSphere(transform.position, detectionRadius);
        // Set Gizmos.color to Color.red.
        Gizmos.color = Color.red;
        // Call Gizmos.DrawWireSphere using world position of transform, the result of GhostEnemy.GetCatchDistance.
        Gizmos.DrawWireSphere(transform.position, GetCatchDistance());
        // Draw chase speed indicator as a cyan ring.
        // Set Gizmos.color to Color.cyan.
        Gizmos.color = Color.cyan;
        // Call Gizmos.DrawWireSphere using world position of transform, chaseSpeed multiplied by 0.25.
        Gizmos.DrawWireSphere(transform.position, chaseSpeed * 0.25f);
    }
}
