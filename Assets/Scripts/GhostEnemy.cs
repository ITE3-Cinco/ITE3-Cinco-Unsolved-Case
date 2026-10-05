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

        Collider col = GetComponent<Collider>();

        if (col != null)
            col.isTrigger = true;

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
            return false;

        Transform targetWaypoint = patrolWaypoints[currentWaypointIndex];

        if (targetWaypoint == null)
            return false;

        transform.position = Vector3.MoveTowards(
            transform.position,
            targetWaypoint.position,
            moveSpeed * Time.deltaTime
        );

        Vector3 direction = (targetWaypoint.position - transform.position).normalized;

        if (direction != Vector3.zero)
        {
            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                Quaternion.LookRotation(direction),
                Time.deltaTime * 5f
            );
        }

        if (Vector3.Distance(transform.position, targetWaypoint.position) < 0.3f)
            currentWaypointIndex = (currentWaypointIndex + 1) % patrolWaypoints.Length;

        return true;
    }

    private bool ChasePlayer()
    {
        if (playerTransform == null)
            return false;

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
