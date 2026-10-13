// Purpose: Controls Renato patrols, room searches, sight and sprint-hearing detection in Level 3.
// Use: Attach with NavMeshAgent and CharacterController; assign routes, room markers and Level3Flow.
// How it works: Navigation plans routes; the solid controller moves the body so walls still block it.

// UnityEngine supplies GameObjects, components, transforms, time, math and physics used here.
using UnityEngine;
// Unity AI supplies NavMesh agents, paths and walkable-surface building/query tools.
using UnityEngine.AI;

/// <summary>Level 3 hunter using sight, optional sprint hearing and solid collision movement.
/// Complete navigation paths guide patrols, pursuit and room searches.</summary>
[RequireComponent(typeof(NavMeshAgent), typeof(CharacterController))]
public class RenatoEnemy : MonoBehaviour
{
    // Named behavior steps make patrol, room-search and pursuit decisions easier to follow.
    public enum BehaviourState { Patrol, EnterRoom, Crouch, Search, Stand, LeaveRoom, Chase, Investigate }
    [System.Serializable]
    // Each searchable room supplies a search location and an entrance to use when leaving.
    public class Room
    {
        // entrance: world-space Transform marking a room doorway. Assigned in the scene/prefab Inspector.
        public Transform entrance;
        // searchPosition: world-space Transform marking the room search location. Assigned in the scene/prefab
        // Inspector.
        public Transform searchPosition;
    }

    // player: player reference used by this script. Assigned in the scene/prefab Inspector.
    public Transform player;
    // patrolPoints: ordered patrol Transform stops. Assigned in the scene/prefab Inspector.
    public Transform[] patrolPoints;
    // rooms: room entries containing entrance and search Transforms. Assigned in the scene/prefab Inspector.
    public Room[] rooms;
    // flow: Level3Flow coordinating key, catch and escape outcomes. Assigned in the scene/prefab Inspector.
    public Level3Flow flow;
    // animations: RenatoAnimation receiving enemy behavior state. Assigned in the scene/prefab Inspector.
    public RenatoAnimation animations;
    // patrolSpeed: distance per gameplay second during ordinary Renato patrol. Assigned in the scene/prefab
    // Inspector; the initializer below is the default.
    public float patrolSpeed = 2.2f;
    // chaseSpeed: movement speed during pursuit. Assigned in the scene/prefab Inspector; the initializer below is
    // the default.
    public float chaseSpeed = 7f;
    // sightRange: maximum visual detection distance. Assigned in the scene/prefab Inspector; the initializer below
    // is the default.
    public float sightRange = 14f;
    // fieldOfView: full width of the vision cone in degrees. Assigned in the scene/prefab Inspector; the
    // initializer below is the default.
    [Range(0, 180)] public float fieldOfView = 110f;
    // catchDistance: close-contact distance setting. Assigned in the scene/prefab Inspector; the initializer below
    // is the default.
    public float catchDistance = 1f;
    // searchDuration: seconds spent looking around at a room search point. Assigned in the scene/prefab Inspector;
    // the initializer below is the default.
    public float searchDuration = 4f;
    // patrolStopsBeforeSearch: number of patrol stops between attempted room searches. Assigned in the
    // scene/prefab Inspector; the initializer below is the default.
    [Min(1)] public int patrolStopsBeforeSearch = 6;
    // investigateDuration: How long Renato investigates a last-seen position before resuming patrol. Assigned in
    // the scene/prefab Inspector; the initializer below is the default.
    [Tooltip("How long Renato investigates a last-seen position before resuming patrol.")]
    [Min(0f)] public float investigateDuration = 3f;
    // hearingRadius: If the sprinting player comes within this radius, Renato hears them and investigates even
    // without line of sight. Set to 0 to disable. Assigned in the scene/prefab Inspector; the initializer below is
    // the default.
    [Tooltip("If the sprinting player comes within this radius, Renato hears them and investigates even without line of sight. Set to 0 to disable.")]
    [Min(0f)] public float hearingRadius = 6f;
    // State can be read by other scripts; only this class can write its backing state.
    public BehaviourState State { get; private set; }

    // agent: NavMeshAgent that plans movement. Awake retrieves it from the first NavMeshAgent component on this
    // GameObject.
    private NavMeshAgent agent;
    // playerController: player CharacterController used to inspect physical movement. Start retrieves it from the
    // first CharacterController component on player.
    private CharacterController playerController;
    // body: CharacterController used for solid movement. Awake retrieves it from the first CharacterController
    // component on this GameObject.
    private CharacterController body;
    // playerMovement: player movement component used by enemy detection. Start retrieves it from the first
    // PlayerMovement component on player.
    private PlayerMovement playerMovement;
    // sightHits: reused Physics hit buffer, avoiding per-frame array allocation. Initialized from a new
    // array/object initialized with the entries specified here.
    private readonly RaycastHit[] sightHits = new RaycastHit[32];
    // verticalSpeed: stored vertical body speed after gravity and grounding. Updated by MoveBody.
    private float verticalSpeed;
    // Remember the last detected player position and timing instead of following a hidden player continuously.
    // lastSeen: last detected player position used after sight is lost. Update sets it from world position of
    // player.
    private Vector3 lastSeen;
    // lastSeenAt: gameplay timestamp of the latest detection. Update sets it from Unity elapsed gameplay time.
    private float lastSeenAt;
    // stateTime: time value used by this component behavior logic. Update sets it from Unity gameplay frame
    // duration in seconds.
    private float stateTime;
    // nextPathUpdate: gameplay timestamp for the next destination-path update. Updated by Update.
    private float nextPathUpdate;
    // nextPatrolAttempt: gameplay timestamp when a failed patrol selection can be retried. Updated by PickPatrol.
    private float nextPatrolAttempt;
    // patrolIndex: zero-based selected patrol stop. Updated by PickPatrol.
    private int patrolIndex;
    // roomIndex: zero-based room selected for the enter/search/leave sequence. Updated by TryEnterRoom.
    private int roomIndex;
    // patrolStops: number of patrol arrivals used to schedule room searches. Runtime data filled by the methods
    // below; starts at the type default.
    private int patrolStops;
    // ready: whether enemy initialization completed successfully. Start sets it from whether agent is registered
    // on a walkable NavMesh.
    private bool ready;
    // hasDestination: whether a navigation destination has been accepted. Updated by GoTo.
    private bool hasDestination;
    // destination: world-space destination currently requested from navigation. GoTo sets it from world position
    // of hit.
    private Vector3 destination;
    // path: reused NavMeshPath storing a proposed complete route. Updated by Awake.
    private NavMeshPath path;

    /// <summary>Prepare navigation and collision references, leaving the agent disabled until the map is built.</summary>
    private void Awake()
    {
        // Store the first NavMeshAgent component on this GameObject in agent.
        agent = GetComponent<NavMeshAgent>();
        // Set path to a new NavMeshPath instance.
        path = new NavMeshPath();
        // Store the first CharacterController component on this GameObject in body.
        body = GetComponent<CharacterController>();
        // Set agent.updatePosition to false.
        agent.updatePosition = false;
        // GhostNavigation builds the environment before Start enables this agent.
        // Disable agent by changing its component enabled flag.
        agent.enabled = false;
    }

    /// <summary>Validate scene references, place Renato on navigation and begin his patrol.</summary>
    private void Start()
    {
        // If player == null or flow == null or patrolPoints is missing or length of patrolPoints equals 0, run the
        // following block.
        if (player == null || flow == null || patrolPoints == null || patrolPoints.Length == 0)
        {
            // Report the failed setup or state as a Unity Console error.
            Debug.LogError("[Renato] Assign the player, flow and patrol points.", this);
            // Set enabled to false.
            enabled = false;
            // Return now; skip the rest of this method.
            return;
        }
        // Store the first CharacterController component on player in playerController.
        playerController = player.GetComponent<CharacterController>();
        // Store the first PlayerMovement component on player in playerMovement.
        playerMovement = player.GetComponent<PlayerMovement>();
        // If not (NavMesh finds walkable ground near transform.position; write the result through the out
        // argument), run the following block.
        if (!NavMesh.SamplePosition(transform.position, out NavMeshHit spawn, 1.5f, Filter()))
        {
            // Report the failed setup or state as a Unity Console error.
            Debug.LogError("[Renato] No NavMesh at the assigned spawn.", this);
            // Set enabled to false.
            enabled = false;
            // Return now; skip the rest of this method.
            return;
        }
        // Disable body by changing its component enabled flag.
        body.enabled = false;
        // Set transform.position to world position of spawn.
        transform.position = spawn.position;
        // Enable body by changing its component enabled flag.
        body.enabled = true;
        // Enable agent by changing its component enabled flag.
        agent.enabled = true;
        // Set ready to whether agent is registered on a walkable NavMesh.
        ready = agent.isOnNavMesh;
        // If not (ready), run the following block.
        if (!ready) { enabled = false; return; }
        // Call RenatoEnemy.SetState with BehaviourState.Patrol; reset behavior timing, stop movement for
        // stationary poses and notify the animation driver.
        SetState(BehaviourState.Patrol);
        // Call RenatoEnemy.PickPatrol; try assigned patrol points, or wait to retry if none can currently be
        // reached.
        PickPatrol();
    }

    /// <summary>Stop the current path and disable enemy updates after a final outcome.</summary>
    public void Stop()
    {
        // If agent exists and enabled state of agent and agent is registered on a walkable NavMesh, run the
        // following block.
        if (agent != null && agent.enabled && agent.isOnNavMesh)
        {
            // Set agent.isStopped to true.
            agent.isStopped = true;
            // Clear agent current navigation route.
            agent.ResetPath();
            // Set agent.velocity to the zero vector (no movement or offset).
            agent.velocity = Vector3.zero;
        }
        // Set enabled to false.
        enabled = false;
    }

    /// <summary>Use the agent type and allowed areas when checking navigation positions.</summary>
    // Return a new NavMeshQueryFilter instance to the caller.
    private NavMeshQueryFilter Filter() => new NavMeshQueryFilter
    { agentTypeID = agent.agentTypeID, areaMask = agent.areaMask };

    /// <summary>Check contact and detection, remember sightings, then advance patrol, pursuit or room-search states.</summary>
    private void Update()
    {
        // If !ready or flow.Finished or Unity gameplay clock multiplier (0 freezes gameplay time) is at most 0f or
        // PauseManager.IsGamePaused, return now; skip the rest of this method.
        if (!ready || flow.Finished || Time.timeScale <= 0f || PauseManager.IsGamePaused) return;
        // Add Unity gameplay frame duration in seconds to stateTime and store the updated value there.
        stateTime += Time.deltaTime;

        // --- Catch check (distance + clear sight) ---
        // Store playerController.bounds.center - transform.position (calculated from the values shown) when
        // playerController exists; otherwise world position of player minus world position of transform in local
        // offset.
        Vector3 offset = playerController != null
            ? playerController.bounds.center - transform.position
            : player.position - transform.position;
        // Store squared length of offset (avoids a square root) is at most Mathf.Max(sightRange, catchDistance +
        // agent.height) * Mathf.Max(sightRange, catchDistance + agent.height) (calculated from the values shown)
        // and the result of RenatoEnemy.HasClearSight in local clearSight.
        bool clearSight = offset.sqrMagnitude <= Mathf.Max(sightRange, catchDistance + agent.height) *
            Mathf.Max(sightRange, catchDistance + agent.height) && HasClearSight();
        // If clearSight and new Vector2(offset.x, offset.z).magnitude is at most catchDistance and the absolute
        // value of offset.y is below agent.height, run the following block.
        if (clearSight && new Vector2(offset.x, offset.z).magnitude <= catchDistance &&
            Mathf.Abs(offset.y) < agent.height)
        {
            // Clear agent current navigation route.
            agent.ResetPath();
            // Call Level3Flow.Caught; commit a loss once, play caught audio, focus Renato face and show the lose
            // screen.
            flow.Caught();
            // Return now; skip the rest of this method.
            return;
        }

        // --- Sight detection ---
        // Store clearSight and offset.magnitude is at most sightRange and the angle in degrees between
        // transform.forward and new Vector3(offset.x, 0f, offset.z) is at most fieldOfView multiplied by 0.5f in
        // local seen.
        bool seen = clearSight && offset.magnitude <= sightRange &&
            Vector3.Angle(transform.forward, new Vector3(offset.x, 0f, offset.z)) <= fieldOfView * 0.5f;

        // A visible player updates the last-known location and immediately switches Renato to chase.
        // If seen, run the following block.
        if (seen)
        {
            // Track the player's live feet position while in sight.
            // Set lastSeen to playerController.bounds.center - Vector3.up * playerController.bounds.extents.y
            // (calculated from the values shown) when playerController exists; otherwise world position of player.
            lastSeen = playerController != null
                ? playerController.bounds.center - Vector3.up * playerController.bounds.extents.y
                : player.position;
            // Set lastSeenAt to Unity elapsed gameplay time.
            lastSeenAt = Time.time;
            // If State differs from BehaviourState.Chase, call RenatoEnemy.SetState with BehaviourState.Chase;
            // reset behavior timing, stop movement for stationary poses and notify the animation driver.
            if (State != BehaviourState.Chase) SetState(BehaviourState.Chase);
        }

        // --- Hearing detection (sprinting player) ---
        // If not (seen) and State differs from BehaviourState.Chase and hearingRadius is above 0, run the
        // following block.
        if (!seen && State != BehaviourState.Chase && hearingRadius > 0f)
        {
            // Keep playerMovement cached by this component in local pm.
            PlayerMovement pm = playerMovement;
            // Store pm != null and pm.currentStamina < pm.maxStamina and UnityEngine.InputSystem.Keyboard.current
            // exists and leftShift key on UnityEngine.InputSystem.Keyboard.current is held down in local
            // playerSprinting.
            bool playerSprinting = pm != null && pm.currentStamina < pm.maxStamina &&
                                   UnityEngine.InputSystem.Keyboard.current != null &&
                                   UnityEngine.InputSystem.Keyboard.current.leftShiftKey.isPressed;
            // If playerSprinting and length of offset is at most hearingRadius, run the following block.
            if (playerSprinting && offset.magnitude <= hearingRadius)
            {
                // Set lastSeen to world position of player.
                lastSeen = player.position;
                // Set lastSeenAt to Unity elapsed gameplay time.
                lastSeenAt = Time.time;
                // If State differs from BehaviourState.Investigate, run the following block.
                if (State != BehaviourState.Investigate)
                {
                    // Call RenatoEnemy.SetState with BehaviourState.Investigate; reset behavior timing, stop
                    // movement for stationary poses and notify the animation driver.
                    SetState(BehaviourState.Investigate);
                    // If not (the result of RenatoEnemy.GoTo using lastSeen, patrolSpeed), call
                    // RenatoEnemy.ResumePatrol; return to patrol state and select the next reachable stop.
                    if (!GoTo(lastSeen, patrolSpeed)) ResumePatrol();
                }
            }
        }

        // --- Chase state ---
        // During pursuit, refresh the route at most every 0.2 seconds; lost sight leads to investigation.
        // If State equals BehaviourState.Chase, run the following block.
        if (State == BehaviourState.Chase)
        {
            // If Unity elapsed gameplay time is at least nextPathUpdate, run the following block.
            if (Time.time >= nextPathUpdate)
            {
                // Recalculate nextPathUpdate from Unity elapsed gameplay time plus 0.2.
                nextPathUpdate = Time.time + 0.2f;
                // While the player is visible, chase their live position.
                // After losing sight, continue toward the last known position.
                // Keep lastSeen cached by this component in local target.
                Vector3 target = lastSeen;
                // If not (the result of RenatoEnemy.GoTo using target, chaseSpeed), clear agent current navigation
                // route.
                if (!GoTo(target, chaseSpeed)) agent.ResetPath();
            }
            // If not (seen) and Unity elapsed gameplay time minus lastSeenAt is at least 2, run the following
            // block.
            if (!seen && Time.time - lastSeenAt >= 2f)
            {
                // Call RenatoEnemy.SetState with BehaviourState.Investigate; reset behavior timing, stop movement
                // for stationary poses and notify the animation driver.
                SetState(BehaviourState.Investigate);
                // If not (the result of RenatoEnemy.GoTo using lastSeen, patrolSpeed), call
                // RenatoEnemy.ResumePatrol; return to patrol state and select the next reachable stop.
                if (!GoTo(lastSeen, patrolSpeed)) ResumePatrol();
            }
            // Return now; skip the rest of this method.
            return;
        }

        // --- Other states ---
        // Room searching follows enter, crouch, look around, stand and leave before patrol resumes.
        // Choose the matching branch using State.
        switch (State)
        {
            case BehaviourState.Patrol:
                // If (Arrived() || !agent.hasPath or stateTime > 25f) and Unity elapsed gameplay time is at least
                // nextPatrolAttempt, run the following block.
                if ((Arrived() || !agent.hasPath || stateTime > 25f) && Time.time >= nextPatrolAttempt)
                {
                    // If ++patrolStops % Mathf.Max(1, patrolStopsBeforeSearch) (calculated from the values shown)
                    // equals 0 and the result of RenatoEnemy.TryEnterRoom, leave this loop or switch case so no
                    // later statements in that block run.
                    if (++patrolStops % Mathf.Max(1, patrolStopsBeforeSearch) == 0 && TryEnterRoom()) break;
                    // Call RenatoEnemy.PickPatrol; try assigned patrol points, or wait to retry if none can
                    // currently be reached.
                    PickPatrol();
                }
                // Leave this loop or switch case so no later statements in that block run.
                break;
            case BehaviourState.EnterRoom:
                // If the result of RenatoEnemy.Arrived, call RenatoEnemy.SetState with BehaviourState.Crouch;
                // reset behavior timing, stop movement for stationary poses and notify the animation driver.
                if (Arrived()) SetState(BehaviourState.Crouch);
                // Leave this loop or switch case so no later statements in that block run.
                break;
            case BehaviourState.Crouch:
                // If stateTime is at least StandDuration, call RenatoEnemy.SetState with BehaviourState.Search;
                // reset behavior timing, stop movement for stationary poses and notify the animation driver.
                if (stateTime >= StandDuration) SetState(BehaviourState.Search);
                // Leave this loop or switch case so no later statements in that block run.
                break;
            case BehaviourState.Search:
                // If stateTime is at least searchDuration, call RenatoEnemy.SetState with BehaviourState.Stand;
                // reset behavior timing, stop movement for stationary poses and notify the animation driver.
                if (stateTime >= searchDuration) SetState(BehaviourState.Stand);
                // Leave this loop or switch case so no later statements in that block run.
                break;
            case BehaviourState.Stand:
                // If stateTime is at least StandDuration, run the following block.
                if (stateTime >= StandDuration)
                {
                    // Call RenatoEnemy.SetState with BehaviourState.LeaveRoom; reset behavior timing, stop
                    // movement for stationary poses and notify the animation driver.
                    SetState(BehaviourState.LeaveRoom);
                    // If not (the result of RenatoEnemy.GoTo using rooms[roomIndex].entrance.position,
                    // patrolSpeed), call RenatoEnemy.ResumePatrol; return to patrol state and select the next
                    // reachable stop.
                    if (!GoTo(rooms[roomIndex].entrance.position, patrolSpeed)) ResumePatrol();
                }
                // Leave this loop or switch case so no later statements in that block run.
                break;
            case BehaviourState.LeaveRoom:
                // If the result of RenatoEnemy.Arrived, call RenatoEnemy.ResumePatrol; return to patrol state and
                // select the next reachable stop.
                if (Arrived()) ResumePatrol();
                // Leave this loop or switch case so no later statements in that block run.
                break;
            case BehaviourState.Investigate:
                // Exit when arrived OR after investigateDuration seconds (handles blocked paths).
                // If (Arrived() and stateTime >= investigateDuration) or stateTime is at least investigateDuration
                // plus 5f, run the following block.
                if ((Arrived() && stateTime >= investigateDuration) || stateTime >= investigateDuration + 5f)
                {
                    // If not (the result of RenatoEnemy.TryEnterRoom), call RenatoEnemy.ResumePatrol; return to
                    // patrol state and select the next reachable stop.
                    if (!TryEnterRoom()) ResumePatrol();
                }
                // Leave this loop or switch case so no later statements in that block run.
                break;
        }

        // A blocked route must never start a search animation in the corridor.
        // If (State == BehaviourState.EnterRoom || State == BehaviourState.LeaveRoom or State ==
        // BehaviourState.Investigate) and ((!agent.pathPending && agent.hasPath && agent.pathStatus !=
        // NavMeshPathStatus.PathComplete) or stateTime > 25f), run the following block.
        if ((State == BehaviourState.EnterRoom || State == BehaviourState.LeaveRoom || State == BehaviourState.Investigate) &&
            ((!agent.pathPending && agent.hasPath && agent.pathStatus != NavMeshPathStatus.PathComplete) || stateTime > 25f))
            // Call RenatoEnemy.ResumePatrol; return to patrol state and select the next reachable stop.
            ResumePatrol();
    }

    /// <summary>Move the solid body along the planned direction and synchronize navigation with its real position.</summary>
    private void LateUpdate()
    {
        // If !ready or flow.Finished or Unity gameplay clock multiplier (0 freezes gameplay time) is at most 0f or
        // PauseManager.IsGamePaused, return now; skip the rest of this method.
        if (!ready || flow.Finished || Time.timeScale <= 0f || PauseManager.IsGamePaused) return;
        // Call RenatoEnemy.MoveBody with the zero vector (no movement or offset) when agent.isStopped; otherwise
        // agent planned velocity from NavMesh path following, Unity gameplay frame duration in seconds; add
        // gravity and let CharacterController collision limit the requested movement.
        MoveBody(agent.isStopped ? Vector3.zero : agent.desiredVelocity, Time.deltaTime);
        // Keep path simulation at the position physics actually allowed. Never warp through
        // an obstacle to catch up with the agent, even if a collider was absent from the bake.
        // Copy the physically allowed world position into NavMeshAgent simulation so the route planner cannot
        // drift through walls.
        agent.nextPosition = transform.position;
    }

    /// <summary>Add gravity and let CharacterController collision limit the requested movement.</summary>
    // velocity is a Vector3 argument supplied by RenatoEnemy.LateUpdate.
    // deltaTime is a float argument supplied by RenatoEnemy.LateUpdate.
    private void MoveBody(Vector3 velocity, float deltaTime)
    {
        // Set verticalSpeed to negative 2 when body floor-contact result from its last Move; otherwise the largest
        // of verticalSpeed + Physics.gravity.y * deltaTime, -20f.
        verticalSpeed = body.isGrounded ? -2f : Mathf.Max(verticalSpeed + Physics.gravity.y * deltaTime, -20f);
        // Copy verticalSpeed cached by this component into velocity.y.
        velocity.y = verticalSpeed;
        // Move body with displacement velocity * deltaTime while resolving collisions.
        body.Move(velocity * deltaTime);
    }

    // StandDuration is computed when read: return animations.StandDuration when animations exists; otherwise 3.
    public float StandDuration => animations != null ? animations.StandDuration : 3f;

    /// <summary>
    /// Returns true when there is an unobstructed line of sight from Renato's eye level to the player.
    /// Reuses a hit buffer; a saturated query conservatively blocks sight.
    /// </summary>
    // Raycast from Renato eyes to the player, ignoring character colliders and rejecting obstacles.
    public bool HasClearSight()
    {
        // If player is missing, return false to the method caller.
        if (player == null) return false;
        // Store playerController.bounds center when playerController exists; otherwise world position of player in
        // local target.
        Vector3 target = playerController != null ? playerController.bounds.center : player.position;
        // Store world position of transform plus the world-up direction (positive Y) multiplied by 1.5 in local
        // origin.
        Vector3 origin = transform.position + Vector3.up * 1.5f;

        // Store the number of Physics ray hits written to the supplied reusable hit buffer in local count.
        int count = Physics.RaycastNonAlloc(origin, (target - origin).normalized, sightHits,
            Vector3.Distance(origin, target), Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
        // If count equals length of sightHits, return false to the method caller.
        if (count == sightHits.Length) return false;
        // Repeat this block while i is below count; start from int i = 0.
        for (int i = 0; i < count; i++)
        {
            // Store sightHits[i] (entry read from sightHits) in local hit.
            RaycastHit hit = sightHits[i];
            // If hit.transform belongs to the Transform on this component GameObject Transform hierarchy or
            // hit.transform belongs to player Transform hierarchy, skip this entry and continue with the next loop
            // iteration.
            if (hit.transform.IsChildOf(transform) || hit.transform.IsChildOf(player)) continue;
            // Return false to the method caller.
            return false;
        }
        // Return true to the method caller.
        return true;
    }

    /// <summary>Accept a nearby navigation point only when a complete route can reach it.</summary>
    // point is a Vector3 argument supplied by RenatoEnemy.PickPatrol, RenatoEnemy.TryEnterRoom,
    // RenatoEnemy.Update.
    // speed is a float argument supplied by RenatoEnemy.PickPatrol, RenatoEnemy.TryEnterRoom, RenatoEnemy.Update.
    private bool GoTo(Vector3 point, float speed)
    {
        // If not (NavMesh.SamplePosition(point, out NavMeshHit hit, 0.75f, Filter())) or not
        // (agent.CalculatePath(hit.position, path)) or path.status differs from NavMeshPathStatus.PathComplete,
        // run the following block.
        if (!NavMesh.SamplePosition(point, out NavMeshHit hit, 0.75f, Filter()) ||
            !agent.CalculatePath(hit.position, path) || path.status != NavMeshPathStatus.PathComplete) return false;
        // Set agent.isStopped to false.
        agent.isStopped = false;
        // Copy speed (argument from the caller) into agent.speed.
        agent.speed = speed;
        // Store whether agent accepted the path from path in hasDestination.
        hasDestination = agent.SetPath(path);
        // Set destination to world position of hit.
        destination = hit.position;
        // Return hasDestination to the method caller.
        return hasDestination;
    }

    /// <summary>Confirm the body has reached the current destination after path calculation finishes.</summary>
    // Return hasDestination and not (agent.pathPending) and the distance between transform.position and
    // destination is at most the largest of agent.stoppingDistance + 0.1f, 0.35f to the caller.
    private bool Arrived() => hasDestination && !agent.pathPending &&
        Vector3.Distance(transform.position, destination) <= Mathf.Max(agent.stoppingDistance + 0.1f, 0.35f);

    /// <summary>Reset behavior timing, stop movement for stationary poses and notify the animation driver.</summary>
    // state is a BehaviourState argument supplied by RenatoEnemy.ResumePatrol, RenatoEnemy.Start,
    // RenatoEnemy.TryEnterRoom or another caller.
    private void SetState(BehaviourState state)
    {
        // Copy state (argument from the caller) into State.
        State = state;
        // Set stateTime to 0.
        stateTime = 0f;
        // Store state equals BehaviourState.Crouch or state equals BehaviourState.Search or state equals
        // BehaviourState.Stand in local stationary.
        bool stationary = state == BehaviourState.Crouch || state == BehaviourState.Search || state == BehaviourState.Stand;
        // Copy stationary into agent.isStopped.
        agent.isStopped = stationary;
        // If stationary, clear agent current navigation route.
        if (stationary) agent.ResetPath();
        // If animations exists, call RenatoAnimation.SetState with state; receive a new enemy behavior and restart
        // the timer for its animation sequence.
        if (animations != null) animations.SetState(state);
    }

    /// <summary>Try room search positions in order and enter a room only with a complete route.</summary>
    private bool TryEnterRoom()
    {
        // If rooms is missing or length of rooms equals 0, return false to the method caller.
        if (rooms == null || rooms.Length == 0) return false;
        // Repeat this block while i is below length of rooms; start from int i = 0.
        for (int i = 0; i < rooms.Length; i++)
        {
            // Recalculate roomIndex from (roomIndex plus 1) modulo (remainder after dividing by) length of rooms.
            roomIndex = (roomIndex + 1) % rooms.Length;
            // Store rooms[roomIndex] (entry read from rooms) in local room.
            Room room = rooms[roomIndex];
            // If room is missing or room.entrance is missing or room.searchPosition is missing, skip this entry
            // and continue with the next loop iteration.
            if (room == null || room.entrance == null || room.searchPosition == null) continue;
            // If not (the result of RenatoEnemy.GoTo using room.searchPosition.position, patrolSpeed), skip this
            // entry and continue with the next loop iteration.
            if (!GoTo(room.searchPosition.position, patrolSpeed)) continue;
            // Call RenatoEnemy.SetState with BehaviourState.EnterRoom; reset behavior timing, stop movement for
            // stationary poses and notify the animation driver.
            SetState(BehaviourState.EnterRoom);
            // Return true to the method caller.
            return true;
        }
        // Return false to the method caller.
        return false;
    }

    /// <summary>Return to patrol state and select the next reachable stop.</summary>
    // Call RenatoEnemy.SetState with BehaviourState.Patrol; reset behavior timing, stop movement for stationary
    // poses and notify the animation driver. Call RenatoEnemy.PickPatrol; try assigned patrol points, or wait to
    // retry if none can currently be reached.
    private void ResumePatrol() { SetState(BehaviourState.Patrol); PickPatrol(); }

    /// <summary>Try assigned patrol points, or wait to retry if none can currently be reached.</summary>
    private void PickPatrol()
    {
        // Set stateTime to 0.
        stateTime = 0f;
        // Recalculate nextPatrolAttempt from Unity elapsed gameplay time plus 1.
        nextPatrolAttempt = Time.time + 1f;
        // Repeat this block while i is below length of patrolPoints; start from int i = 0.
        for (int i = 0; i < patrolPoints.Length; i++)
        {
            // Recalculate patrolIndex from (patrolIndex plus 1) modulo (remainder after dividing by) length of
            // patrolPoints.
            patrolIndex = (patrolIndex + 1) % patrolPoints.Length;
            // If patrolPoints[patrolIndex] (entry read from patrolPoints) exists and the result of
            // RenatoEnemy.GoTo using patrolPoints[patrolIndex].position, patrolSpeed, return now; skip the rest of
            // this method.
            if (patrolPoints[patrolIndex] != null && GoTo(patrolPoints[patrolIndex].position, patrolSpeed)) return;
        }
        // Retry without passing through geometry if no point is currently reachable.
        // Clear agent current navigation route.
        agent.ResetPath();
        // Report the missing/invalid setup or state as a Unity Console warning.
        Debug.LogWarning("[Renato] No reachable patrol point. Check the Level 3 NavMesh.", this);
    }

    /// <summary>Draw sight, hearing and catch distances in the Editor for tuning.</summary>
    private void OnDrawGizmosSelected()
    {
        // Set Gizmos.color to Color.yellow.
        Gizmos.color = Color.yellow;
        // Call Gizmos.DrawWireSphere using world position of transform, sightRange.
        Gizmos.DrawWireSphere(transform.position, sightRange);
        // If hearingRadius is above 0, run the following block.
        if (hearingRadius > 0f)
        {
            // Set Gizmos.color to Color.cyan.
            Gizmos.color = Color.cyan;
            // Call Gizmos.DrawWireSphere using world position of transform, hearingRadius.
            Gizmos.DrawWireSphere(transform.position, hearingRadius);
        }
        // Set Gizmos.color to Color.red.
        Gizmos.color = Color.red;
        // Call Gizmos.DrawWireSphere using world position of transform, catchDistance.
        Gizmos.DrawWireSphere(transform.position, catchDistance);
    }
}
