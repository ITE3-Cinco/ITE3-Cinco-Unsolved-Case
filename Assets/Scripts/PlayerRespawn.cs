using UnityEngine;

public class PlayerRespawn : MonoBehaviour
{
    [Header("Fallback Spawn")]
    [Tooltip("Used only if the player has not discovered a map checkpoint yet.")]
    public Transform defaultSpawnPoint;

    [Header("Automatic Respawn")]
    [Tooltip("The player is automatically respawned here if they fall below this height.")]
    public float killYDepth = -20f;

    private CharacterController characterController;

    private void Start()
    {
        characterController = GetComponent<CharacterController>();

        // Checkpoints belong to the current scene/run. A previous scene's map
        // must not remain active after loading a new scene.
        RespawnPoint.ResetCheckpoint();

        if (defaultSpawnPoint != null)
        {
            RespawnPoint.activeCheckpointPosition = defaultSpawnPoint.position;
            RespawnPoint.activeCheckpointRotation = defaultSpawnPoint.rotation;
        }
    }

    private void Update()
    {
        // Falling is an automatic respawn condition. There is intentionally
        // no manual R-key respawn.
        if (transform.position.y < killYDepth)
            Respawn();
    }

    public void Respawn()
    {
        Vector3 targetPosition = RespawnPoint.hasReachedCheckpoint
            ? RespawnPoint.activeCheckpointPosition
            : (defaultSpawnPoint != null ? defaultSpawnPoint.position : transform.position);

        Quaternion targetRotation = RespawnPoint.hasReachedCheckpoint
            ? RespawnPoint.activeCheckpointRotation
            : (defaultSpawnPoint != null ? defaultSpawnPoint.rotation : transform.rotation);

        if (characterController != null)
        {
            characterController.enabled = false;
            transform.SetPositionAndRotation(targetPosition, targetRotation);
            characterController.enabled = true;
        }
        else
        {
            transform.SetPositionAndRotation(targetPosition, targetRotation);
        }

        Debug.Log($"[Respawn] Teleported to: {targetPosition}");
    }
}