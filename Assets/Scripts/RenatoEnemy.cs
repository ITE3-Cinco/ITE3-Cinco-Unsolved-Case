using UnityEngine;
using UnityEngine.AI;

/// <summary>Level 3's visual-only hunter. Uses the same complete-path and hierarchy-filtered
/// sight checks as GhostEnemy, without its artifact/checkpoint/Agimat rules.</summary>
[RequireComponent(typeof(NavMeshAgent), typeof(CharacterController))]
public class RenatoEnemy : MonoBehaviour
{
    public enum BehaviourState { Patrol, EnterRoom, Crouch, Search, Stand, LeaveRoom, Chase, Investigate }
    [System.Serializable]
    public class Room
    {
        public Transform entrance;
        public Transform searchPosition;
    }

    public Transform player;
    public Transform[] patrolPoints;
    public Room[] rooms;
    public Level3Flow flow;
    public RenatoAnimation animations;
    public float patrolSpeed = 2.2f;
    public float chaseSpeed = 7f;
    public float sightRange = 14f;
    [Range(0, 180)] public float fieldOfView = 110f;
    public float catchDistance = 1f;
    public float searchDuration = 4f;
    public BehaviourState State { get; private set; }

    private NavMeshAgent agent;
    private CharacterController playerController;
    private CharacterController body;
    private float verticalSpeed;
    private Vector3 lastSeen;
    private float lastSeenAt;
    private float stateTime;
    private float nextPathUpdate;
    private float nextPatrolAttempt;
    private int patrolIndex;
    private int roomIndex;
    private int patrolStops;
    private bool ready;
    private bool hasDestination;
    private Vector3 destination;
    private NavMeshPath path;

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        path = new NavMeshPath();
        body = GetComponent<CharacterController>();
        agent.updatePosition = false;
        // GhostNavigation builds the environment before Start enables this agent.
        agent.enabled = false;
    }

    private void Start()
    {
        if (player == null || flow == null || patrolPoints == null || patrolPoints.Length == 0)
        {
            Debug.LogError("[Renato] Assign the player, flow and patrol points.", this);
            enabled = false;
            return;
        }
        playerController = player.GetComponent<CharacterController>();
        if (!NavMesh.SamplePosition(transform.position, out NavMeshHit spawn, 1.5f, Filter()))
        {
            Debug.LogError("[Renato] No NavMesh at the assigned spawn.", this);
            enabled = false;
            return;
        }
        body.enabled = false;
        transform.position = spawn.position;
        body.enabled = true;
        agent.enabled = true;
        ready = agent.isOnNavMesh;
        if (!ready) { enabled = false; return; }
        SetState(BehaviourState.Patrol);
        PickPatrol();
    }

    public void Stop()
    {
        if (agent != null && agent.enabled && agent.isOnNavMesh)
        {
            agent.isStopped = true;
            agent.ResetPath();
            agent.velocity = Vector3.zero;
        }
        enabled = false;
    }

    private NavMeshQueryFilter Filter() => new NavMeshQueryFilter
    { agentTypeID = agent.agentTypeID, areaMask = agent.areaMask };

    private void Update()
    {
        if (!ready || flow.Finished || Time.timeScale <= 0f || PauseManager.IsGamePaused) return;
        stateTime += Time.deltaTime;
        bool clearSight = HasClearSight();
        Vector3 offset = playerController != null ? playerController.bounds.center - transform.position : player.position - transform.position;
        if (clearSight && new Vector2(offset.x, offset.z).magnitude <= catchDistance && Mathf.Abs(offset.y) < agent.height)
        {
            agent.ResetPath();
            flow.Caught();
            return;
        }
        bool seen = clearSight && offset.magnitude <= sightRange &&
            Vector3.Angle(transform.forward, new Vector3(offset.x, 0f, offset.z)) <= fieldOfView * 0.5f;
        if (seen)
        {
            // Only sight may update this location. Sounds never feed this AI.
            lastSeen = playerController != null ? playerController.bounds.center - Vector3.up * playerController.bounds.extents.y : player.position;
            lastSeenAt = Time.time;
            if (State != BehaviourState.Chase) SetState(BehaviourState.Chase);
        }
        if (State == BehaviourState.Chase)
        {
            if (Time.time >= nextPathUpdate)
            {
                nextPathUpdate = Time.time + 0.2f;
                if (!GoTo(lastSeen, chaseSpeed)) agent.ResetPath();
            }
            if (!seen && Time.time - lastSeenAt >= 2f)
            {
                SetState(BehaviourState.Investigate);
                if (!GoTo(lastSeen, patrolSpeed)) ResumePatrol();
            }
            return;
        }
        switch (State)
        {
            case BehaviourState.Patrol:
                if ((Arrived() || !agent.hasPath || stateTime > 25f) && Time.time >= nextPatrolAttempt)
                {
                    if (++patrolStops % 2 == 0 && TryEnterRoom()) break;
                    PickPatrol();
                }
                break;
            case BehaviourState.EnterRoom:
                if (Arrived()) SetState(BehaviourState.Crouch);
                break;
            case BehaviourState.Crouch:
                if (stateTime >= StandDuration) SetState(BehaviourState.Search);
                break;
            case BehaviourState.Search:
                if (stateTime >= searchDuration) SetState(BehaviourState.Stand);
                break;
            case BehaviourState.Stand:
                if (stateTime >= StandDuration)
                {
                    SetState(BehaviourState.LeaveRoom);
                    if (!GoTo(rooms[roomIndex].entrance.position, patrolSpeed)) ResumePatrol();
                }
                break;
            case BehaviourState.LeaveRoom:
                if (Arrived()) ResumePatrol();
                break;
            case BehaviourState.Investigate:
                if (Arrived() && stateTime >= 3f)
                {
                    if (!TryEnterRoom()) ResumePatrol();
                }
                break;
        }
        // A blocked route must never start a search animation in the corridor.
        if ((State == BehaviourState.EnterRoom || State == BehaviourState.LeaveRoom || State == BehaviourState.Investigate) &&
            ((!agent.pathPending && agent.hasPath && agent.pathStatus != NavMeshPathStatus.PathComplete) || stateTime > 25f))
            ResumePatrol();
    }

    private void LateUpdate()
    {
        if (!ready || flow.Finished || Time.timeScale <= 0f || PauseManager.IsGamePaused) return;
        MoveBody(agent.isStopped ? Vector3.zero : agent.desiredVelocity, Time.deltaTime);
        // Keep path simulation at the position physics actually allowed. Never warp through
        // an obstacle to catch up with the agent, even if a collider was absent from the bake.
        agent.nextPosition = transform.position;
    }

    private void MoveBody(Vector3 velocity, float deltaTime)
    {
        verticalSpeed = body.isGrounded ? -2f : Mathf.Max(verticalSpeed + Physics.gravity.y * deltaTime, -20f);
        velocity.y = verticalSpeed;
        body.Move(velocity * deltaTime);
    }

    public float StandDuration => animations != null ? animations.StandDuration : 3f;

    public bool HasClearSight()
    {
        if (player == null) return false;
        Vector3 target = playerController != null ? playerController.bounds.center : player.position;
        Vector3 origin = transform.position + Vector3.up * 1.5f;
        Vector3 direction = target - origin;
        if (direction.sqrMagnitude > sightRange * sightRange) return false;
        foreach (RaycastHit hit in Physics.RaycastAll(origin, direction.normalized, direction.magnitude,
            Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
        {
            if (hit.transform.IsChildOf(transform) || hit.transform.IsChildOf(player)) continue;
            return false;
        }
        return true;
    }

    private bool GoTo(Vector3 point, float speed)
    {
        if (!NavMesh.SamplePosition(point, out NavMeshHit hit, 0.75f, Filter()) ||
            !agent.CalculatePath(hit.position, path) || path.status != NavMeshPathStatus.PathComplete) return false;
        agent.isStopped = false;
        agent.speed = speed;
        hasDestination = agent.SetPath(path);
        destination = hit.position;
        return hasDestination;
    }

    private bool Arrived() => hasDestination && !agent.pathPending &&
        Vector3.Distance(transform.position, destination) <= Mathf.Max(agent.stoppingDistance + 0.1f, 0.35f);

    private void SetState(BehaviourState state)
    {
        State = state;
        stateTime = 0f;
        bool stationary = state == BehaviourState.Crouch || state == BehaviourState.Search || state == BehaviourState.Stand;
        agent.isStopped = stationary;
        if (stationary) agent.ResetPath();
        if (animations != null) animations.SetState(state);
    }

    private bool TryEnterRoom()
    {
        if (rooms == null || rooms.Length == 0) return false;
        for (int i = 0; i < rooms.Length; i++)
        {
            roomIndex = (roomIndex + 1) % rooms.Length;
            Room room = rooms[roomIndex];
            if (room == null || room.entrance == null || room.searchPosition == null) continue;
            if (!GoTo(room.searchPosition.position, patrolSpeed)) continue;
            SetState(BehaviourState.EnterRoom);
            return true;
        }
        return false;
    }

    private void ResumePatrol() { SetState(BehaviourState.Patrol); PickPatrol(); }

    private void PickPatrol()
    {
        stateTime = 0f;
        nextPatrolAttempt = Time.time + 1f;
        for (int i = 0; i < patrolPoints.Length; i++)
        {
            patrolIndex = (patrolIndex + 1) % patrolPoints.Length;
            if (patrolPoints[patrolIndex] != null && GoTo(patrolPoints[patrolIndex].position, patrolSpeed)) return;
        }
        // Retry without passing through geometry if no point is currently reachable.
        agent.ResetPath();
        Debug.LogWarning("[Renato] No reachable patrol point. Check the Level 3 NavMesh.", this);
    }
}
