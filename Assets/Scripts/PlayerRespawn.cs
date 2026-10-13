// Purpose: Places the player at a saved checkpoint on retry or after falling out of the level.
// Use: Attach to the player; optionally assign a fallback spawn and the minimum safe height.
// How it works: Scene entry decides whether to restore progress; teleporting temporarily disables collision movement.

// UnityEngine supplies GameObjects, components, transforms, time, math and physics used here.
using UnityEngine;

[DefaultExecutionOrder(100)]
public class PlayerRespawn : MonoBehaviour
{
    // defaultSpawnPoint: Used before discovering a map checkpoint. If empty, the player's starting pose is used.
    // Assigned in the scene/prefab Inspector.
    [Header("Fallback Spawn")]
    [Tooltip("Used before discovering a map checkpoint. If empty, the player's starting pose is used.")]
    public Transform defaultSpawnPoint;

    // killYDepth: The player automatically respawns if they fall below this height. Assigned in the scene/prefab
    // Inspector; the initializer below is the default.
    [Header("Automatic Respawn")]
    [Tooltip("The player automatically respawns if they fall below this height.")]
    public float killYDepth = -20f;

    // characterController: CharacterController used to disable collision movement during respawn. Awake retrieves
    // it from the first CharacterController component on this GameObject.
    private CharacterController characterController;
    // movement: PlayerMovement reset after teleport so old vertical velocity is cleared. Awake retrieves it from
    // the first PlayerMovement component on this GameObject.
    private PlayerMovement movement;
    // startingPosition: player world position cached in Awake. Awake sets it from world position of transform.
    private Vector3 startingPosition;
    // startingRotation: player world rotation cached in Awake. Awake sets it from world rotation of transform.
    private Quaternion startingRotation;
    // resumeCheckpoint: whether RespawnPoint.BeginRun accepted a same-scene retry. Updated by Awake.
    private bool resumeCheckpoint;

    /// <summary>Save the starting pose, decide whether this is a checkpoint retry and cache all pickup IDs.</summary>
    private void Awake()
    {
        // Retrieve this player GameObject CharacterController and cache it for temporary disabling during
        // teleport.
        characterController = GetComponent<CharacterController>();
        // Store the first PlayerMovement component on this GameObject in movement.
        movement = GetComponent<PlayerMovement>();
        // Set startingPosition to world position of transform.
        startingPosition = transform.position;
        // Set startingRotation to world rotation of transform.
        startingRotation = transform.rotation;
        // Store RespawnPoint.BeginRun result for the asset path of this GameObject scene in resumeCheckpoint.
        resumeCheckpoint = RespawnPoint.BeginRun(gameObject.scene.path);
        // Inactive pickups also need identities before any sibling is removed.
        // Process each root in the top-level GameObjects belonging to the scene containing this GameObject.
        foreach (GameObject root in gameObject.scene.GetRootGameObjects())
        // Process each item in all Interactable components on or beneath root (including inactive children).
        foreach (Interactable item in root.GetComponentsInChildren<Interactable>(true))
            // Set _ to item stable ID cached by Interactable.CacheCollectionId.
            _ = item.CollectionId;
    }

    /// <summary>Restore the retry snapshot and respawn, or save the level entrance as the baseline.</summary>
    private void Start()
    {
        // If resumeCheckpoint, run the following block.
        if (resumeCheckpoint)
        {
            // Pass this player owning scene to CollectionProgress.Restore; saved pickups are removed and inventory
            // is rebuilt from the checkpoint copy.
            CollectionProgress.Restore(gameObject.scene);
            // Call PlayerRespawn.Respawn; choose checkpoint or fallback pose, teleport safely and clear stored
            // movement velocity.
            Respawn();
        }
        else
        {
            // The level entrance is the retry baseline, including carried history.
            // Pass this object owning scene to CollectionProgress.Save; it copies collected IDs, inventory items
            // and selection into a checkpoint snapshot.
            CollectionProgress.Save(gameObject.scene);
        }
    }

    /// <summary>Respawn if the player falls below the configured kill height.</summary>
    private void Update()
    {
        // If transform.position Y component is below killYDepth, run the following block.
        if (transform.position.y < killYDepth)
            // Call PlayerRespawn.Respawn; choose checkpoint or fallback pose, teleport safely and clear stored
            // movement velocity.
            Respawn();
    }

    /// <summary>Choose checkpoint or fallback pose, teleport safely and clear stored movement velocity.</summary>
    public void Respawn()
    {
        // Store the world spawn position saved by RespawnPoint.ActivateCheckpoint when RespawnPoint shared
        // checkpoint-discovered flag; otherwise (defaultSpawnPoint.position when defaultSpawnPoint != null;
        // otherwise startingPosition) in local targetPosition.
        Vector3 targetPosition = RespawnPoint.hasReachedCheckpoint
            ? RespawnPoint.activeCheckpointPosition
            : (defaultSpawnPoint != null ? defaultSpawnPoint.position : startingPosition);

        // Store the rotation saved by RespawnPoint.ActivateCheckpoint when RespawnPoint shared
        // checkpoint-discovered flag; otherwise (defaultSpawnPoint.rotation when defaultSpawnPoint != null;
        // otherwise startingRotation) in local targetRotation.
        Quaternion targetRotation = RespawnPoint.hasReachedCheckpoint
            ? RespawnPoint.activeCheckpointRotation
            : (defaultSpawnPoint != null ? defaultSpawnPoint.rotation : startingRotation);

        // Disable the controller while teleporting, then restore its previous enabled state.
        // Store characterController exists and enabled state of characterController in local controllerWasEnabled.
        bool controllerWasEnabled = characterController != null && characterController.enabled;
        // If controllerWasEnabled, run the following block.
        if (controllerWasEnabled)
            // Disable characterController by changing its component enabled flag.
            characterController.enabled = false;

        // Place transform at targetPosition with rotation targetRotation.
        transform.SetPositionAndRotation(targetPosition, targetRotation);
        // If movement exists, run the following block.
        if (movement != null)
            // Call PlayerMovement.ResetVelocity; clear stored falling or jumping motion after the player is
            // teleported.
            movement.ResetVelocity();

        // If controllerWasEnabled, run the following block.
        if (controllerWasEnabled)
            // Enable characterController by changing its component enabled flag.
            characterController.enabled = true;

        // Write the diagnostic message to the Unity Console; it does not update game UI.
        Debug.Log($"[Respawn] Teleported to: {targetPosition}");
    }
}
