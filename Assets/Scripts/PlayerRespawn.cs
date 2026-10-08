using UnityEngine;

public class PlayerRespawn : MonoBehaviour
{
    [Header("Fallback Spawn")]
    [Tooltip("Used before discovering a map checkpoint. If empty, the player's starting pose is used.")]
    public Transform defaultSpawnPoint;

    [Header("Automatic Respawn")]
    [Tooltip("The player automatically respawns if they fall below this height.")]
    public float killYDepth = -20f;

    private CharacterController characterController;
    private PlayerMovement movement;
    private Vector3 startingPosition;
    private Quaternion startingRotation;
    private bool resumeCheckpoint;

    private void Awake()
    {
        characterController = GetComponent<CharacterController>();
        movement = GetComponent<PlayerMovement>();
        startingPosition = transform.position;
        startingRotation = transform.rotation;
        resumeCheckpoint = RespawnPoint.BeginRun(gameObject.scene.path);
    }

    private void Start()
    {
        if (resumeCheckpoint)
            Respawn();
    }

    private void Update()
    {
        if (transform.position.y < killYDepth)
            Respawn();
    }

    public void Respawn()
    {
        Vector3 targetPosition = RespawnPoint.hasReachedCheckpoint
            ? RespawnPoint.activeCheckpointPosition
            : (defaultSpawnPoint != null ? defaultSpawnPoint.position : startingPosition);

        Quaternion targetRotation = RespawnPoint.hasReachedCheckpoint
            ? RespawnPoint.activeCheckpointRotation
            : (defaultSpawnPoint != null ? defaultSpawnPoint.rotation : startingRotation);

        bool controllerWasEnabled = characterController != null && characterController.enabled;
        if (controllerWasEnabled)
            characterController.enabled = false;

        transform.SetPositionAndRotation(targetPosition, targetRotation);
        if (movement != null)
            movement.ResetVelocity();

        if (controllerWasEnabled)
            characterController.enabled = true;

        Debug.Log($"[Respawn] Teleported to: {targetPosition}");
    }
}
