// Purpose: Remembers collected items and a copy of the inventory at the latest checkpoint.
// Use: Shared static helper used by pickups, checkpoints, respawn and level escape.
// How it works: Stable item IDs survive scene reloads; saved copies restore the checkpoint state on retry.

// System.Collections.Generic supplies typed lists, sets and dictionaries for stored data.
using System.Collections.Generic;
// UnityEngine supplies GameObjects, components, transforms, time, math and physics used here.
using UnityEngine;
// Unity SceneManagement supplies scene data, scene-loaded events and scene-loading calls.
using UnityEngine.SceneManagement;

/// <summary>Session collection history and an immutable copy at the latest checkpoint.</summary>
public static class CollectionProgress
{
    // Current IDs track this run; saved IDs and inventory are independent checkpoint copies.
    // collected: stable item IDs recorded in the current run. Shared across instances; scene reload alone does not
    // reset static state.
    private static HashSet<string> collected = new HashSet<string>();
    // saved: separate set of collected IDs copied at the last checkpoint. Shared across instances; scene reload
    // alone does not reset static state.
    private static HashSet<string> saved;
    // savedInventory: copy of ItemData references carried at the last checkpoint. Shared across instances; scene
    // reload alone does not reset static state.
    private static List<ItemData> savedInventory;
    // savedSlot: zero-based selected inventory slot saved with the checkpoint. Shared across instances; scene
    // reload alone does not reset static state.
    private static int savedSlot;
    // nextScene: destination scene name permitted to retain collection progress. Shared across instances; scene
    // reload alone does not reset static state.
    private static string nextScene;

    // TotalCollected is computed when read: return number of entries in collected.
    public static int TotalCollected => collected.Count;

    /// <summary>Remember this pickup by its stable scene and hierarchy ID.</summary>
    // item is the Interactable being collected; its CollectionId gives the saved history a stable scene/hierarchy
    // key.
    // Add item stable ID cached by Interactable.CacheCollectionId to collected.
    public static void Record(Interactable item) => collected.Add(item.CollectionId);

    /// <summary>Clear collection and checkpoint history when starting a fresh session.</summary>
    public static void Reset()
    {
        // Remove all entries from collected.
        collected.Clear();
        // Clear saved to null so it no longer holds a reference.
        saved = null;
        // Clear savedInventory to null so it no longer holds a reference.
        savedInventory = null;
        // Clear nextScene to null so it no longer holds a reference.
        nextScene = null;
    }

    /// <summary>Mark the destination that may carry collection history after an escape.</summary>
    // sceneName is a string argument supplied by EscapeManager.TryTransition.
    public static void PrepareForNextLevel(string sceneName)
    {
        // Store the destination sceneName supplied by EscapeManager so BeginRun can carry collection history into
        // that level.
        nextScene = sceneName;
    }

    // Called before any Start callbacks. Only a retry or explicit escape carries history.
    /// <summary>On scene entry, restore retry history, keep allowed level progress or start fresh.</summary>
    // scenePath is a string argument supplied by RespawnPoint.BeginRun.
    // retry is a bool argument supplied by RespawnPoint.BeginRun.
    public static void BeginRun(string scenePath, bool retry)
    {
        // Store nextScene equals the result of System.IO.Path.GetFileNameWithoutExtension using scenePath in local
        // advancing.
        bool advancing = nextScene == System.IO.Path.GetFileNameWithoutExtension(scenePath);
        // Clear nextScene to null so it no longer holds a reference.
        nextScene = null;
        // If retry and saved exists, run the following block.
        if (retry && saved != null)
            // Set collected to a new hashset copied/initialized from saved.
            collected = new HashSet<string>(saved);
        else if (!advancing)
            // Call CollectionProgress.Reset; clear collection and checkpoint history when starting a fresh
            // session.
            Reset();
        // If not (retry), run the following block.
        if (!retry)
        {
            // Clear saved to null so it no longer holds a reference.
            saved = null;
            // Clear savedInventory to null so it no longer holds a reference.
            savedInventory = null;
        }
    }

    /// <summary>Copy the current collection, inventory and selected slot as the checkpoint snapshot.</summary>
    // scene is a Scene argument supplied by PlayerRespawn.Start, RespawnPoint.ActivateCheckpoint.
    public static void Save(Scene scene)
    {
        // Copy current collected IDs into a separate saved set; later pickups cannot change the checkpoint
        // snapshot.
        saved = new HashSet<string>(collected);
        // Store the result of CollectionProgress.FindInventory using scene in local inventory.
        Inventory inventory = FindInventory(scene);
        // Set savedInventory to a new list copied/initialized from inventory.items when inventory exists;
        // otherwise no reference (null).
        savedInventory = inventory != null ? new List<ItemData>(inventory.items) : null;
        // Save selectedIndex from the scene Inventory, or zero if no inventory exists, for restoration on retry.
        savedSlot = inventory != null ? inventory.selectedIndex : 0;
    }

    /// <summary>After reloading, rebuild pickup trackers, remove saved pickups and restore inventory.</summary>
    // scene is a Scene argument supplied by PlayerRespawn.Start.
    public static void Restore(Scene scene)
    {
        // Restore trackers using the reloaded objects so their duplicate guards and
        // collectible groups retain real identities, including pickups without ItemData.
        // Process each root in the top-level GameObjects belonging to scene.
        foreach (GameObject root in scene.GetRootGameObjects())
        // Process each item in all Interactable components on or beneath root (including inactive children).
        foreach (Interactable item in root.GetComponentsInChildren<Interactable>(true))
        {
            // If item.isCheckpoint or not (collected contains item.CollectionId), skip this entry and continue
            // with the next loop iteration.
            if (item.isCheckpoint || !collected.Contains(item.CollectionId)) continue;
            // If item.isArtifact and the scene ArtifactManager registered in Awake exists, run the following
            // block.
            if (item.isArtifact && ArtifactManager.Instance != null)
                // Call ArtifactManager.CollectArtifact with item; accept a new artifact, update the display and
                // tell listeners that progress changed.
                ArtifactManager.Instance.CollectArtifact(item);
            // Store the first CollectibleGroup component on or above item in local group.
            CollectibleGroup group = item.GetComponentInParent<CollectibleGroup>();
            // If group exists, call CollectibleGroup.NotifyCollected with item; count a registered item once, then
            // ask the escape manager to check its requirements.
            if (group != null) group.NotifyCollected(item);
            // Deactivate item.gameObject.
            item.gameObject.SetActive(false);
            // Schedule GameObject owning item for destruction at Unity end-of-frame cleanup.
            Object.Destroy(item.gameObject);
        }

        // Restore the saved inventory separately so later consumption cannot change the checkpoint copy.
        // Store the result of CollectionProgress.FindInventory using scene in local inventory.
        Inventory inventory = FindInventory(scene);
        // If inventory exists and savedInventory exists, run the following block.
        if (inventory != null && savedInventory != null)
        {
            // Set inventory.items to a new list copied/initialized from savedInventory.
            inventory.items = new List<ItemData>(savedInventory);
            // Store the first PlayerMovement component on or above inventory in local player.
            PlayerMovement player = inventory.GetComponentInParent<PlayerMovement>();
            // If player exists, run the following block.
            if (player != null)
            {
                // Store whether inventory.items contains player.flashlightItemData in player.HasFlashlight.
                player.HasFlashlight = inventory.items.Contains(player.flashlightItemData);
                // If player.HasFlashlight, run the following block.
                if (player.HasFlashlight)
                    // Process each root in the top-level GameObjects belonging to scene.
                    foreach (GameObject root in scene.GetRootGameObjects())
                    // Process each objective in all ObjectiveItem components on or beneath root (including
                    // inactive children).
                    foreach (ObjectiveItem objective in root.GetComponentsInChildren<ObjectiveItem>(true))
                        // Call objective.CompleteFlashlightTask.
                        objective.CompleteFlashlightTask();
            }
            // Call Inventory.SelectSlot with savedSlot; validate a slot, highlight it and tell PlayerMovement
            // which item is selected.
            inventory.SelectSlot(savedSlot);
        }
        // If the scene EscapeManager registered in Awake exists, call EscapeManager.RefreshEscapeState; unlock
        // escape only when every required group and optional artifact total is complete.
        if (EscapeManager.Instance != null) EscapeManager.Instance.RefreshEscapeState();
    }

    /// <summary>Search only the supplied scene for the player inventory, including inactive children.</summary>
    // scene is a Scene argument supplied by CollectionProgress.Restore, CollectionProgress.Save.
    private static Inventory FindInventory(Scene scene)
    {
        // Process each root in the top-level GameObjects belonging to scene.
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            // Store the first Inventory component on or beneath root (including inactive children) in local
            // inventory.
            Inventory inventory = root.GetComponentInChildren<Inventory>(true);
            // If inventory exists, return inventory to the method caller.
            if (inventory != null) return inventory;
        }
        // Return no reference (null) to the method caller.
        return null;
    }
}
