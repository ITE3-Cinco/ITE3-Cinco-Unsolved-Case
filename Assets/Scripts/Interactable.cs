using UnityEngine;

public class Interactable : MonoBehaviour
{
    public string promptMessage = "Press [E] to pick up";

    [Header("Inventory Integration")]
    public ItemData itemData; // Assign ItemData for flashlight, keys, artifacts, etc.

    [Header("Checkpoint Settings")]
    [Tooltip("Tick this for a map that acts as a checkpoint. The player must interact with the map to activate its respawn location.")]
    public bool isCheckpoint = false;

    [Tooltip("Optional. If empty, the RespawnPoint on this same GameObject is used.")]
    public RespawnPoint checkpoint;

    [Header("Artifact Settings")]
    public bool isArtifact = false; // Tick true for artifacts (increments counter & objective)

    [Header("Objective Integration")]
    [Tooltip("Optional: Drag the ObjectiveItem for the flashlight task here. If left empty, the script will find it automatically.")]
    public ObjectiveItem flashlightObjective;

    public virtual void Interact(GameObject playerObject)
    {
        Debug.Log($"[Interactable] Interact called on '{gameObject.name}'. isArtifact={isArtifact}, isCheckpoint={isCheckpoint}");

        // ---------------------------------------------------------------
        // 1. CHECKPOINT MAP
        // ---------------------------------------------------------------
        // Checkpoint maps are not inventory items and are not destroyed.
        // Interacting with the map explicitly activates its respawn point.
        if (isCheckpoint)
        {
            RespawnPoint checkpointPoint = checkpoint;

            if (checkpointPoint == null)
                checkpointPoint = GetComponent<RespawnPoint>();

            if (checkpointPoint != null)
            {
                checkpointPoint.ActivateCheckpoint();
            }
            else
            {
                Debug.LogWarning($"[Interactable] '{gameObject.name}' is marked as a checkpoint, but no RespawnPoint is assigned or attached.");
            }

            return;
        }

        // ---------------------------------------------------------------
        // 2. GET PLAYER & INVENTORY REFERENCES
        // ---------------------------------------------------------------
        PlayerMovement player = playerObject.GetComponent<PlayerMovement>();
        if (player == null) player = playerObject.GetComponentInParent<PlayerMovement>();
        if (player == null) player = playerObject.GetComponentInChildren<PlayerMovement>();

        Inventory inventory = playerObject.GetComponent<Inventory>();
        if (inventory == null) inventory = playerObject.GetComponentInParent<Inventory>();
        if (inventory == null) inventory = playerObject.GetComponentInChildren<Inventory>();

        // ---------------------------------------------------------------
        // 3. ADD TO INVENTORY HOTBAR
        // ---------------------------------------------------------------
        bool added = false;
        if (inventory != null && itemData != null)
        {
            added = inventory.AddItem(itemData);
            Debug.Log($"[Interactable] Tried to add '{(itemData != null ? itemData.name : "NULL")}' to inventory. Success: {added}");
        }
        else if (itemData == null)
        {
            Debug.LogWarning($"[Interactable] ItemData is unassigned on '{gameObject.name}'. It will not appear in the hotbar.");
        }

        // If inventory is full, stop here so the 3D item isn't consumed/destroyed
        if (!added && itemData != null)
        {
            Debug.LogWarning("[Interactable] Could not add item to inventory (inventory full). Pickup cancelled.");
            return;
        }

        // ---------------------------------------------------------------
        // 4. ARTIFACT COUNTER (Runs if inventory add succeeded or itemData is null)
        // ---------------------------------------------------------------
        if (isArtifact)
        {
            if (ArtifactManager.Instance != null)
            {
                ArtifactManager.Instance.CollectArtifact(this);
            }
            else
            {
                Debug.LogWarning("[Interactable] ArtifactManager.Instance is null! Add an ArtifactManager to the scene.");
            }
        }

        // ---------------------------------------------------------------
        // 5. FLASHLIGHT PICKUP & OBJECTIVE LOGIC
        // ---------------------------------------------------------------
        bool isFlashlightPickup = (player != null && itemData != null && itemData == player.flashlightItemData);
        if (isFlashlightPickup)
        {
            player.HasFlashlight = true;

            ObjectiveItem obj = flashlightObjective;
            if (obj == null)
            {
                // If the flashlightObjective field is not assigned, try to find it in the scene automatically
                obj = FindAnyObjectByType<ObjectiveItem>();
            }

            if (obj != null)
            {
                obj.CompleteFlashlightTask();
                Debug.Log($"[Interactable] Notified ObjectiveItem '{obj.gameObject.name}' -> flashlight task complete.");
            }
            else
            {
                Debug.LogWarning("[Interactable] Flashlight picked up, but no ObjectiveItem was found to cross out.");
            }
        }

        // ---------------------------------------------------------------
        // 6. NOTIFICATIONS
        // ---------------------------------------------------------------
        if (NotificationManager.Instance != null)
        {
            if (isFlashlightPickup)
            {
                NotificationManager.Instance.ShowNotification("Flashlight Acquired! Press [F] to toggle", 4.0f);
            }
            else if (itemData != null && itemData.isAgimat)
            {
                NotificationManager.Instance.ShowNotification("Agimat acquired! Press [E] to use when it glows.", 4f);
            }
            else if (isArtifact)
            {
                bool complete = ArtifactManager.Instance != null &&
                    ArtifactManager.Instance.artifactsCollected >= ArtifactManager.Instance.totalArtifacts;
                NotificationManager.Instance.ShowNotification(complete ? EscapeManager.EscapeObjective : "Acquired an artifact!", 5f);
            }
        }

        // ---------------------------------------------------------------
        // 7. ESCAPE/COLLECTIBLE GROUP TRACKING
        // ---------------------------------------------------------------
        // If this item is inside a CollectibleGroup parent, tell the group
        // that this specific required item has been collected.
        CollectibleGroup collectibleGroup = GetComponentInParent<CollectibleGroup>();
        if (collectibleGroup != null)
        {
            collectibleGroup.NotifyCollected(this);
        }

        // ---------------------------------------------------------------
        // 8. REMOVE 3D WORLD OBJECT
        // ---------------------------------------------------------------
        Destroy(gameObject);
    }
}
