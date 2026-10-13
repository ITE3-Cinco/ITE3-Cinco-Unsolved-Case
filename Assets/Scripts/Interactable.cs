// Purpose: Handles item pickup, checkpoint maps and related objective updates.
// Use: Attach to world objects with colliders; PlayerInteraction calls Interact when E is pressed.
// How it works: Checkpoint maps save progress; normal pickups enter inventory, notify trackers and disappear.

// UnityEngine supplies GameObjects, components, transforms, time, math and physics used here.
using UnityEngine;

public class Interactable : MonoBehaviour
{
    // collectionId: cached scene-and-hierarchy identity used to restore this pickup across retry. Updated by
    // CacheCollectionId.
    private string collectionId;
    // CollectionId exposes its value through the getter/setter below; getter results go to the reader and value
    // comes from the writer.
    public string CollectionId
    {
        get
        {
            // If collectionId is missing, call Interactable.CacheCollectionId; combine scene path, parent names
            // and sibling indices to identify this pickup across retries.
            if (collectionId == null) CacheCollectionId();
            // Return collectionId to the method caller.
            return collectionId;
        }
    }

    /// <summary>Cache a stable item ID before any pickup changes the hierarchy.</summary>
    // Call Interactable.CacheCollectionId; combine scene path, parent names and sibling indices to identify this
    // pickup across retries.
    protected virtual void Awake() => CacheCollectionId();

    /// <summary>Combine scene path, parent names and sibling indices to identify this pickup across retries.</summary>
    private void CacheCollectionId()
    {
        // If collectionId exists, return now; skip the rest of this method.
        if (collectionId != null) return;
        // Cache before pickups are destroyed and sibling indices can change.
        // Store "" in local path.
        string path = "";
        // Repeat this block while node exists; start from Transform node = transform.
        for (Transform node = transform; node != null; node = node.parent)
            // Set path to "/" + node.GetSiblingIndex() + ":" + node.name + path (calculated from the values
            // shown).
            path = "/" + node.GetSiblingIndex() + ":" + node.name + path;
        // Join Unity owning-scene path with the cached hierarchy path; CollectionProgress uses this stable ID on
        // retry.
        collectionId = gameObject.scene.path + path;
    }

    // promptMessage: message read by PlayerInteraction and copied into the interaction prompt. Assigned in the
    // scene/prefab Inspector; the initializer below is the default.
    public string promptMessage = "Press [E] to pick up";

    // itemData: ItemData asset offered by this pickup to Inventory.AddItem. Assigned in the scene/prefab
    // Inspector.
    [Header("Inventory Integration")]
    public ItemData itemData; // Assign ItemData for flashlight, keys, artifacts, etc.

    // isCheckpoint: Tick this for a map that acts as a checkpoint. The player must interact with the map to
    // activate its respawn location. Assigned in the scene/prefab Inspector; the initializer below is the default.
    [Header("Checkpoint Settings")]
    [Tooltip("Tick this for a map that acts as a checkpoint. The player must interact with the map to activate its respawn location.")]
    public bool isCheckpoint = false;

    // checkpoint: Optional. If empty, the RespawnPoint on this same GameObject is used. Assigned in the
    // scene/prefab Inspector.
    [Tooltip("Optional. If empty, the RespawnPoint on this same GameObject is used.")]
    public RespawnPoint checkpoint;

    // isArtifact: whether pickup updates the artifact tracker. Assigned in the scene/prefab Inspector; the
    // initializer below is the default.
    [Header("Artifact Settings")]
    public bool isArtifact = false; // Tick true for artifacts (increments counter & objective)

    // flashlightObjective: Optional: Drag the ObjectiveItem for the flashlight task here. If left empty, the
    // script will find it automatically. Assigned in the scene/prefab Inspector.
    [Header("Objective Integration")]
    [Tooltip("Optional: Drag the ObjectiveItem for the flashlight task here. If left empty, the script will find it automatically.")]
    public ObjectiveItem flashlightObjective;

    /// <summary>Process the interaction in order: checkpoint or inventory, objectives, notifications and removal.</summary>
    // playerObject comes from PlayerInteraction as transform.root.gameObject; component searches retrieve the
    // player and its inventory.
    public virtual void Interact(GameObject playerObject)
    {
        // Write the diagnostic message to the Unity Console; it does not update game UI.
        Debug.Log($"[Interactable] Interact called on '{gameObject.name}'. isArtifact={isArtifact}, isCheckpoint={isCheckpoint}");

        // ---------------------------------------------------------------
        // 1. CHECKPOINT MAP
        // ---------------------------------------------------------------
        // Checkpoint maps are not inventory items and are not destroyed.
        // Interacting with the map explicitly activates its respawn point.
        // If isCheckpoint, run the following block.
        if (isCheckpoint)
        {
            // Keep checkpoint from this component in local checkpointPoint.
            RespawnPoint checkpointPoint = checkpoint;

            // If checkpointPoint is missing, run the following block.
            if (checkpointPoint == null)
                // Store the first RespawnPoint component on this GameObject in checkpointPoint.
                checkpointPoint = GetComponent<RespawnPoint>();

            // If checkpointPoint exists, run the following block.
            if (checkpointPoint != null)
            {
                // Call RespawnPoint.ActivateCheckpoint; save this map spawn and current progress, then notify the
                // ghost and HUD.
                checkpointPoint.ActivateCheckpoint();
            }
            else
            {
                // Report the missing/invalid setup or state as a Unity Console warning.
                Debug.LogWarning($"[Interactable] '{gameObject.name}' is marked as a checkpoint, but no RespawnPoint is assigned or attached.");
            }

            // Return now; skip the rest of this method.
            return;
        }

        // ---------------------------------------------------------------
        // 2. GET PLAYER & INVENTORY REFERENCES
        // ---------------------------------------------------------------
        // Store the first PlayerMovement component on playerObject in local player.
        PlayerMovement player = playerObject.GetComponent<PlayerMovement>();
        // If player is missing, store the first PlayerMovement component on or above playerObject in player.
        if (player == null) player = playerObject.GetComponentInParent<PlayerMovement>();
        // If player is missing, store the first PlayerMovement component on or beneath playerObject in player.
        if (player == null) player = playerObject.GetComponentInChildren<PlayerMovement>();

        // Store the first Inventory component on playerObject in local inventory.
        Inventory inventory = playerObject.GetComponent<Inventory>();
        // If inventory is missing, store the first Inventory component on or above playerObject in inventory.
        if (inventory == null) inventory = playerObject.GetComponentInParent<Inventory>();
        // If inventory is missing, store the first Inventory component on or beneath playerObject in inventory.
        if (inventory == null) inventory = playerObject.GetComponentInChildren<Inventory>();

        // ---------------------------------------------------------------
        // 3. ADD TO INVENTORY HOTBAR
        // ---------------------------------------------------------------
        // Initialize added to false.
        bool added = false;
        // If inventory exists and itemData exists, run the following block.
        if (inventory != null && itemData != null)
        {
            // Store the result of Inventory.AddItem using itemData in added.
            added = inventory.AddItem(itemData);
            // Write the diagnostic message to the Unity Console; it does not update game UI.
            Debug.Log($"[Interactable] Tried to add '{(itemData != null ? itemData.name : "NULL")}' to inventory. Success: {added}");
        }
        else if (itemData == null)
        {
            // Report the missing/invalid setup or state as a Unity Console warning.
            Debug.LogWarning($"[Interactable] ItemData is unassigned on '{gameObject.name}'. It will not appear in the hotbar.");
        }

        // If inventory is full, stop here so the 3D item isn't consumed/destroyed
        // If not (added) and itemData exists, run the following block.
        if (!added && itemData != null)
        {
            // Report the missing/invalid setup or state as a Unity Console warning.
            Debug.LogWarning("[Interactable] Could not add item to inventory (inventory full). Pickup cancelled.");
            // Return now; skip the rest of this method.
            return;
        }

        // ---------------------------------------------------------------
        // 4. ARTIFACT COUNTER (Runs if inventory add succeeded or itemData is null)
        // ---------------------------------------------------------------
        // If isArtifact, run the following block.
        if (isArtifact)
        {
            // If the scene ArtifactManager registered in Awake exists, run the following block.
            if (ArtifactManager.Instance != null)
            {
                // Call ArtifactManager.CollectArtifact with this; accept a new artifact, update the display and
                // tell listeners that progress changed.
                ArtifactManager.Instance.CollectArtifact(this);
            }
            else
            {
                // Report the missing/invalid setup or state as a Unity Console warning.
                Debug.LogWarning("[Interactable] ArtifactManager.Instance is null! Add an ArtifactManager to the scene.");
            }
        }

        // ---------------------------------------------------------------
        // 5. FLASHLIGHT PICKUP & OBJECTIVE LOGIC
        // ---------------------------------------------------------------
        // Store (player != null and itemData != null and itemData equals player.flashlightItemData) in local
        // isFlashlightPickup.
        bool isFlashlightPickup = (player != null && itemData != null && itemData == player.flashlightItemData);
        // If isFlashlightPickup, run the following block.
        if (isFlashlightPickup)
        {
            // Write ownership through PlayerMovement.HasFlashlight, whose setter updates the player hasFlashlight
            // field.
            player.HasFlashlight = true;

            // Keep flashlightObjective from this component in local obj.
            ObjectiveItem obj = flashlightObjective;
            // If obj is missing, run the following block.
            if (obj == null)
            {
                // If the flashlightObjective field is not assigned, try to find it in the scene automatically
                // Store a scene ObjectiveItem component in obj.
                obj = FindAnyObjectByType<ObjectiveItem>();
            }

            // If obj exists, run the following block.
            if (obj != null)
            {
                // Call ObjectiveItem.CompleteFlashlightTask; mark the flashlight task complete and redraw the
                // list.
                obj.CompleteFlashlightTask();
                // Write the diagnostic message to the Unity Console; it does not update game UI.
                Debug.Log($"[Interactable] Notified ObjectiveItem '{obj.gameObject.name}' -> flashlight task complete.");
            }
            else
            {
                // Report the missing/invalid setup or state as a Unity Console warning.
                Debug.LogWarning("[Interactable] Flashlight picked up, but no ObjectiveItem was found to cross out.");
            }
        }

        // ---------------------------------------------------------------
        // 6. NOTIFICATIONS
        // ---------------------------------------------------------------
        // If the scene NotificationManager registered in Awake exists, run the following block.
        if (NotificationManager.Instance != null)
        {
            // If isFlashlightPickup, run the following block.
            if (isFlashlightPickup)
            {
                // Call NotificationManager.ShowNotification with "Flashlight Acquired! Press [F] to toggle", 4.0;
                // show a new message and restart its hide timer so it receives its full display time.
                NotificationManager.Instance.ShowNotification("Flashlight Acquired! Press [F] to toggle", 4.0f);
            }
            else if (itemData != null && itemData.isAgimat)
            {
                // Call NotificationManager.ShowNotification with "Agimat acquired! Press [E] to use when it
                // glows.", 4; show a new message and restart its hide timer so it receives its full display time.
                NotificationManager.Instance.ShowNotification("Agimat acquired! Press [E] to use when it glows.", 4f);
            }
            else if (isArtifact)
            {
                // Store the scene ArtifactManager registered in Awake exists and unique pickup count stored by
                // ArtifactManager.Instance ArtifactManager is at least required artifact count configured on
                // ArtifactManager.Instance in local complete.
                bool complete = ArtifactManager.Instance != null &&
                    ArtifactManager.Instance.artifactsCollected >= ArtifactManager.Instance.totalArtifacts;
                // Store the scene ArtifactManager registered in Awake in local tracker.
                ArtifactManager tracker = ArtifactManager.Instance;
                // Store (tracker.completedMessage when complete; otherwise tracker.collectedMessage) when tracker
                // exists; otherwise "Acquired an artifact!" in local message.
                string message = tracker != null
                    ? (complete ? tracker.completedMessage : tracker.collectedMessage)
                    : "Acquired an artifact!";
                // Call NotificationManager.ShowNotification with message, 5; show a new message and restart its
                // hide timer so it receives its full display time.
                NotificationManager.Instance.ShowNotification(message, 5f);
            }
        }

        // ---------------------------------------------------------------
        // 7. ESCAPE/COLLECTIBLE GROUP TRACKING
        // ---------------------------------------------------------------
        // If this item is inside a CollectibleGroup parent, tell the group
        // that this specific required item has been collected.
        // Store the first CollectibleGroup component on or above this GameObject in local collectibleGroup.
        CollectibleGroup collectibleGroup = GetComponentInParent<CollectibleGroup>();
        // If collectibleGroup exists, run the following block.
        if (collectibleGroup != null)
        {
            // Call CollectibleGroup.NotifyCollected with this; count a registered item once, then ask the escape
            // manager to check its requirements.
            collectibleGroup.NotifyCollected(this);
        }

        // ---------------------------------------------------------------
        // 8. REMOVE 3D WORLD OBJECT
        // ---------------------------------------------------------------
        // Call CollectionProgress.Record with this; remember this pickup by its stable scene and hierarchy ID.
        CollectionProgress.Record(this);
        // Deactivate gameObject.
        gameObject.SetActive(false);
        // Schedule the GameObject that owns this component for destruction at Unity end-of-frame cleanup.
        Destroy(gameObject);
    }
}
