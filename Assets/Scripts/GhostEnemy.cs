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
    public string movingParameter = "IsMoving";

    [Header("Lose Scene")]
    [Tooltip("Name of the scene shown after the player is caught.")]
    public string loseSceneName = "LoseScene";
    [Tooltip("Fade duration before loading the lose scene.")]
    public float loseFadeDuration = 1.5f;

    [Header("State")]
    public bool isChasing = false;

    private bool isCaught = false;

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
        if (playerTransform == null || isCaught) return;

        if (catchCooldownTimer > 0f)
            catchCooldownTimer -= Time.deltaTime;

        bool isPlayerInvisible = InvisibilityConsumable.Instance != null &&
                                 InvisibilityConsumable.Instance.isInvisible;

        float distanceToPlayer = Vector3.Distance(transform.position, playerTransform.position);

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

        UpdateAnimation(isMoving);
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

    private void UpdateAnimation(bool isMoving)
    {
        if (animator != null && !string.IsNullOrEmpty(movingParameter))
            animator.SetBool(movingParameter, isMoving);
    }

    private void CatchPlayer()
    {
        if (isCaught)
            return;

        isCaught = true;
        isChasing = false;
        catchCooldownTimer = catchCooldown;

        Debug.Log("<color=red>[Ghost]</color> Caught the player!");

        if (animator != null && !string.IsNullOrEmpty(movingParameter))
            animator.SetBool(movingParameter, false);

        PlayerAudio playerAudio = playerTransform.GetComponent<PlayerAudio>();
        if (playerAudio != null)
            playerAudio.PlayCaught();

        ScreenFadeController fadeController = FindFirstObjectByType<ScreenFadeController>();

        if (fadeController != null)
        {
            fadeController.FadeToScene(loseSceneName, loseFadeDuration);
        }
        else
        {
            Debug.LogWarning("[Ghost] No ScreenFadeController found. Loading the lose scene directly.");
            UnityEngine.SceneManagement.SceneManager.LoadScene(loseSceneName);
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, detectionRadius);
    }
}
