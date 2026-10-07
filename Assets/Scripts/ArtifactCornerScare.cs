using System.Collections;
using UnityEngine;
using UnityEngine.AI;

/// <summary>A visual-only, once-per-run encounter armed by artifact progress.</summary>
public class ArtifactCornerScare : MonoBehaviour
{
    [Header("References")]
    public ArtifactManager artifactManager;
    [Tooltip("The existing White Lady. A visual copy is used; its patrol is not interrupted.")]
    public GhostEnemy ghost;
    public Transform player;
    public Camera playerCamera;
    public Transform cornerSpawn;
    [Tooltip("Place beyond a wall. The apparition deliberately ignores collision and navigation.")]
    public Transform wallExit;

    [Header("Trigger")]
    [Min(1)] public int requiredArtifacts = 2;
    [Min(0.1f)] public float activationDistance = 12f;
    [Tooltip("Solid geometry used to check that the corner is visible.")]
    public LayerMask visibilityMask = Physics.DefaultRaycastLayers;

    [Header("Scare")]
    [Min(0f)] public float holdDuration = 1.2f;
    [Min(0.1f)] public float exitSpeed = 2.5f;
    [Tooltip("Optional sound played at the apparition when it appears.")]
    public AudioClip scareAudio;
    [Range(0f, 1f)] public float audioVolume = 0.8f;

    public bool IsArmed { get; private set; }
    public bool HasPlayed { get; private set; }
    private GameObject apparition;
    private ArtifactManager subscribedManager;

    private void OnEnable()
    {
        Subscribe();
    }

    private void Start()
    {
        // Instance and Camera.main may not have been available during OnEnable.
        Subscribe();
        if (playerCamera == null)
            playerCamera = Camera.main;
        if (player == null && ghost != null)
            player = ghost.playerTransform;

        if (artifactManager == null || ghost == null || player == null ||
            playerCamera == null || cornerSpawn == null || wallExit == null)
        {
            Debug.LogWarning("[ArtifactCornerScare] Assign the manager, ghost, player, camera, spawn and exit.", this);
            enabled = false;
        }
    }

    private void Subscribe()
    {
        if (artifactManager == null)
            artifactManager = ArtifactManager.Instance;
        if (artifactManager == null || subscribedManager == artifactManager)
            return;

        subscribedManager = artifactManager;
        subscribedManager.ArtifactCountChanged += OnArtifactCountChanged;
        OnArtifactCountChanged(subscribedManager.artifactsCollected);
    }

    private void OnArtifactCountChanged(int count)
    {
        if (!HasPlayed && count >= requiredArtifacts)
            IsArmed = true;
    }

    private void Update()
    {
        if (!IsArmed || HasPlayed || Time.timeScale <= 0f || PauseManager.IsGamePaused ||
            ghost == null || player == null || playerCamera == null || cornerSpawn == null || wallExit == null)
            return;

        if (Vector3.Distance(player.position, cornerSpawn.position) > activationDistance)
            return;

        Vector3 viewport = playerCamera.WorldToViewportPoint(cornerSpawn.position);
        if (viewport.z <= 0f || viewport.x < 0.1f || viewport.x > 0.9f ||
            viewport.y < 0.1f || viewport.y > 0.9f)
            return;

        if (Physics.Linecast(playerCamera.transform.position, cornerSpawn.position,
            visibilityMask, QueryTriggerInteraction.Ignore))
            return;

        HasPlayed = true;
        IsArmed = false;
        StartCoroutine(PlayScare());
    }

    private IEnumerator PlayScare()
    {
        // Clone while inactive so the copy cannot start patrol/catch behaviour.
        apparition = new GameObject("White Lady Apparition (visual only)");
        apparition.SetActive(false);
        apparition.transform.position = cornerSpawn.position;
        GameObject visual = Instantiate(ghost.gameObject, apparition.transform, false);
        visual.name = "White Lady Visual";
        visual.transform.localPosition = Vector3.zero;
        // Preserve the source's model orientation, without its current yaw.
        visual.transform.localRotation = Quaternion.Inverse(
            Quaternion.Euler(0f, ghost.transform.eulerAngles.y, 0f)) * ghost.transform.rotation;
        visual.transform.localScale = ghost.transform.lossyScale;

        foreach (MonoBehaviour behaviour in visual.GetComponentsInChildren<MonoBehaviour>(true))
            if (behaviour != null)
                behaviour.enabled = false;
        foreach (NavMeshAgent agent in visual.GetComponentsInChildren<NavMeshAgent>(true))
            agent.enabled = false;
        foreach (Collider collider in visual.GetComponentsInChildren<Collider>(true))
            collider.enabled = false;
        foreach (Rigidbody body in visual.GetComponentsInChildren<Rigidbody>(true))
        {
            body.isKinematic = true;
            body.detectCollisions = false;
        }
        foreach (AudioSource source in visual.GetComponentsInChildren<AudioSource>(true))
        {
            source.playOnAwake = false;
            source.enabled = false;
        }
        foreach (Animator animator in visual.GetComponentsInChildren<Animator>(true))
        {
            animator.applyRootMotion = false;
            // The current static model has no Animator. A rigged replacement
            // can reuse the patrol controller's optional movement parameter.
            SetMoving(animator, false);
        }
        foreach (Transform child in visual.GetComponentsInChildren<Transform>(true))
            child.gameObject.tag = "Untagged";

        Face(player.position - cornerSpawn.position);
        visual.SetActive(true);
        apparition.SetActive(true);
        if (scareAudio != null)
        {
            AudioSource source = apparition.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.spatialBlend = 1f;
            source.volume = audioVolume;
            source.clip = scareAudio;
            source.Play();
        }

        yield return new WaitForSeconds(Mathf.Max(0f, holdDuration));

        if (apparition == null || wallExit == null)
        {
            ClearApparition();
            yield break;
        }
        foreach (Animator animator in visual.GetComponentsInChildren<Animator>(true))
            SetMoving(animator, true);

        Vector3 destination = wallExit.position;
        Face(destination - apparition.transform.position);
        while ((apparition.transform.position - destination).sqrMagnitude > 0.0001f)
        {
            // Scaled time freezes the sequence with pause and the lose screen.
            apparition.transform.position = Vector3.MoveTowards(apparition.transform.position,
                destination, Mathf.Max(0.1f, exitSpeed) * Time.deltaTime);
            yield return null;
        }
        ClearApparition();
    }

    private void SetMoving(Animator animator, bool moving)
    {
        if (ghost == null || animator.runtimeAnimatorController == null)
            return;
        foreach (AnimatorControllerParameter parameter in animator.parameters)
        {
            if (parameter.type == AnimatorControllerParameterType.Bool && parameter.name == ghost.movingParameter)
                animator.SetBool(parameter.nameHash, moving);
        }
    }

    private void Face(Vector3 direction)
    {
        direction.y = 0f;
        if (direction.sqrMagnitude > 0.001f)
            apparition.transform.rotation = Quaternion.LookRotation(direction);
    }

    private void OnDisable()
    {
        if (subscribedManager != null)
            subscribedManager.ArtifactCountChanged -= OnArtifactCountChanged;
        subscribedManager = null;
        StopAllCoroutines();
        ClearApparition();
    }

    private void ClearApparition()
    {
        if (apparition != null)
            Destroy(apparition);
        apparition = null;
    }

    private void OnDrawGizmosSelected()
    {
        if (cornerSpawn == null || wallExit == null)
            return;
        Gizmos.color = Color.magenta;
        Gizmos.DrawWireSphere(cornerSpawn.position, 0.5f);
        Gizmos.DrawLine(cornerSpawn.position, wallExit.position);
        Gizmos.DrawWireSphere(wallExit.position, 0.3f);
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(cornerSpawn.position, activationDistance);
    }
}
