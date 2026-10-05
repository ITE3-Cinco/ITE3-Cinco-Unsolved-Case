using UnityEngine;

public class PlayerRespawn : MonoBehaviour
{
    [Header("Fallback Spawn")]
    public Transform defaultSpawnPoint;

    [Header("Testing Controls")]
    public KeyCode debugRespawnKey = KeyCode.R;
    public float killYDepth = -20f;

    private CharacterController characterController;

    private void Start()
    {
        characterController = GetComponent<CharacterController>();

        // The lobby is the fallback spawn until a checkpoint is reached.
        if (!RespawnPoint.hasReachedCheckpoint && defaultSpawnPoint != null)
        {
            RespawnPoint.activeCheckpointPosition = defaultSpawnPoint.position;
        }
    }

    private void Update()
    {
        if (Input.GetKeyDown(debugRespawnKey))
            Respawn();

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