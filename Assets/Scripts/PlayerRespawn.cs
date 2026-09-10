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

        // Set default position if no checkpoint has been hit yet
        if (!RespawnPoint.hasReachedCheckpoint && defaultSpawnPoint != null)
        {
            RespawnPoint.activeCheckpointPosition = defaultSpawnPoint.position;
        }
    }

    private void Update()
    {
        if (Input.GetKeyDown(debugRespawnKey))
        {
            Respawn();
        }

        if (transform.position.y < killYDepth)
        {
            Respawn();
        }
    }

    public void Respawn()
    {
        // 1. Target the reached checkpoint first, fallback to default transform
        Vector3 targetPosition = RespawnPoint.hasReachedCheckpoint
            ? RespawnPoint.activeCheckpointPosition
            : (defaultSpawnPoint != null ? defaultSpawnPoint.position : transform.position);

        // 2. Disable CharacterController temporarily to allow direct transform movement
        if (characterController != null)
        {
            characterController.enabled = false;
            transform.position = targetPosition;
            characterController.enabled = true;
        }
        else
        {
            transform.position = targetPosition;
        }

        Debug.Log($"[Respawn] Teleported to: {targetPosition}");
    }
}