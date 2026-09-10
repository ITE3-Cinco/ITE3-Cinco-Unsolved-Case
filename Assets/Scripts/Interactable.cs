using UnityEngine;

public class Interactable : MonoBehaviour
{
    [Header("Prompt")]
    public string promptMessage = "Press [E] to pick up";

    [Header("Inventory Integration")]
    public ItemData itemData; // Assign ItemData for flashlight, keys, artifacts, etc.

    [Header("Artifact Settings")]
    public bool isArtifact = false; // Tick true for artifacts (increments counter & objective)

    [Header("Objective Integration")]
    [Tooltip("Optional: Drag the ObjectiveItem for the flashlight task here. If left empty, the script will find it automatically.")]
    public ObjectiveItem flashlightObjective;

    // Cached reference to the player's inventory (found lazily when BaseInteract is used)
    private Inventory playerInventory;

    // ---------------------------------------------------------------
    // MAIN ENTRY POINT (called by your interaction raycast)
    // ---------------------------------------------------------------
    public void Interact(GameObject playerObject)
    {
        Debug.Log($"[Interactable] Interact called on '{gameObject.name}'. isArtifact={isArtifact}");

        // 1. Get player & inventory references
        PlayerMovement player = playerObject.GetComponent<PlayerMovement>();
        if (player == null) player = playerObject.GetComponentInParent<PlayerMovement>();
        if (player == null) player = playerObject.GetComponentInChildren<PlayerMovement>();

        Inventory inventory = playerObject.GetComponent<Inventory>();
        if (inventory == null) inventory = playerObject.GetComponentInParent<Inventory>();
        if (inventory == null) inventory = playerObject.GetComponentInChildren<Inventory>();

        // Cache for BaseInteract use later
        if (inventory != null) playerInventory = inventory;

        // 2. Add to inventory hotbar
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

        // 3. Artifact counter (runs if add succeeded or itemData is null)
        if (isArtifact)
        {
            if (ArtifactManager.Instance != null)
            {
                ArtifactManager.Instance.CollectArtifact();
            }
            else
            {
                Debug.LogWarning("[Interactable] ArtifactManager.Instance is null! Add an ArtifactManager to the scene.");
            }
        }

        // 4. Flashlight pickup & objective logic
        bool isFlashlightPickup = (player != null && itemData != null && itemData == player.flashlightItemData);
        if (isFlashlightPickup)
        {
            player.HasFlashlight = true;

            ObjectiveItem obj = flashlightObjective;
            if (obj == null)
            {
                obj = FindAnyObjectByType<ObjectiveItem>();
            }

            if (obj != null)
            {
                obj.CompleteFlashlightTask();
                Debug.Log($"[Interactable] Notified ObjectiveItem '{obj.gameObject.name}' — flashlight task complete.");
            }
            else
            {
                Debug.LogWarning("[Interactable] Flashlight picked up, but no ObjectiveItem was found to cross out.");
            }
        }

        // 5. Notifications
        if (NotificationManager.Instance != null)
        {
            if (isFlashlightPickup)
            {
                NotificationManager.Instance.ShowNotification("Flashlight Acquired! Press [F] to toggle", 4.0f);
            }
            else if (itemData != null && itemData.itemName == "Agimat")
            {
                NotificationManager.Instance.ShowNotification(
                    "Agimat Acquired! Select slot and press [F] or [Left Click] to activate Invisibility.",
                    4.0f);
            }
            else if (itemData != null)
            {
                NotificationManager.Instance.ShowNotification("Acquired an artifact!", 3.0f);
            }
        }

        // 6. Remove 3D world object
        Destroy(gameObject);
    }

    // ---------------------------------------------------------------
    // BASE INTERACT (no arguments — uses cached playerInventory)
    // ---------------------------------------------------------------
    /// <summary>
    /// Convenience method that performs the base pickup without a player argument.
    /// Uses the cached Inventory reference from a prior Interact() call, or
    /// falls back to searching the scene if none exists yet.
    /// </summary>
    public void BaseInteract()
    {
        // Try to find the inventory if we haven't cached it yet
        if (playerInventory == null)
        {
            playerInventory = FindAnyObjectByType<Inventory>();
        }

        if (playerInventory == null)
        {
            Debug.LogWarning($"[Interactable] BaseInteract called on '{gameObject.name}' but no Inventory was found in the scene.");
            return;
        }

        if (itemData == null)
        {
            Debug.LogWarning($"[Interactable] BaseInteract called on '{gameObject.name}' but itemData is unassigned.");
            return;
        }

        bool added = playerInventory.AddItem(itemData);

        if (added)
        {
            // Custom notification logic
            if (itemData.itemName == "Agimat")
            {
                if (NotificationManager.Instance != null)
                {
                    NotificationManager.Instance.ShowNotification(
                        "Agimat Acquired! Select slot and press [F] or [Left Click] to activate Invisibility.",
                        4.0f);
                }
            }
            else if (isArtifact)
            {
                if (ArtifactManager.Instance != null)
                {
                    ArtifactManager.Instance.CollectArtifact();
                }
                else
                {
                    Debug.LogWarning("[Interactable] ArtifactManager.Instance is null! Add an ArtifactManager to the scene.");
                }
            }

            gameObject.SetActive(false);
        }
        else
        {
            Debug.LogWarning($"[Interactable] BaseInteract on '{gameObject.name}' — inventory full, pickup cancelled.");
        }
    }
}