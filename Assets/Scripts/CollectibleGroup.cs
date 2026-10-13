// Purpose: Tracks the required pickups beneath one parent object.
// Use: Attach to a parent containing Interactable children and assign it to EscapeManager.
// How it works: Two sets remember registered items and collected items even after pickups disappear.

// System.Collections.Generic supplies typed lists, sets and dictionaries for stored data.
using System.Collections.Generic;
// UnityEngine supplies GameObjects, components, transforms, time, math and physics used here.
using UnityEngine;

public class CollectibleGroup : MonoBehaviour
{
    // requiredForEscape: All Interactable children of this object count as required collectibles. Assigned in the
    // scene/prefab Inspector; the initializer below is the default.
    [Header("Collection Group")]
    [Tooltip("All Interactable children of this object count as required collectibles.")]
    public bool requiredForEscape = true;

    // totalItems: number of registered items in this collectible group. RefreshGroup sets it from number of
    // entries in registered.
    [Header("Progress")]
    [SerializeField] private int totalItems;
    // collectedItems: number of distinct group items collected. RefreshGroup sets it from number of entries in
    // collected.
    [SerializeField] private int collectedItems;

    // Keep references to all items ever registered/collected. Unity object
    // references remain valid as identity even after the world object is destroyed.
    // registered: set remembering all children registered for this collectible group. Initialized from a new
    // hashset with no entries.
    private readonly HashSet<Interactable> registered = new HashSet<Interactable>();
    // collected: set remembering collected item identities. Initialized from a new hashset with no entries.
    private readonly HashSet<Interactable> collected = new HashSet<Interactable>();

    // Expose progress to EscapeManager; optional or empty groups do not block escape.
    // TotalItems is computed when read: return totalItems.
    public int TotalItems => totalItems;
    // CollectedItems is computed when read: return collectedItems.
    public int CollectedItems => collectedItems;
    // IsComplete is computed when read: return not (requiredForEscape) or totalItems equals 0 or collectedItems is
    // at least totalItems.
    public bool IsComplete => !requiredForEscape || totalItems == 0 || collectedItems >= totalItems;

    /// <summary>Count the group children before pickups begin.</summary>
    private void Awake()
    {
        // Call CollectibleGroup.RefreshGroup; register existing children while keeping collection history from
        // destroyed pickups.
        RefreshGroup();
    }

    /// <summary>Register existing children while keeping collection history from destroyed pickups.</summary>
    public void RefreshGroup()
    {
        // Store all Interactable components on or beneath this GameObject (including inactive children) in local
        // items.
        Interactable[] items = GetComponentsInChildren<Interactable>(true);

        // Add currently existing children without clearing previous progress.
        // Previously collected objects may already have been destroyed, but
        // their references remain in the HashSet and continue counting.
        // Process each item in items.
        foreach (Interactable item in items)
        {
            // If item exists, run the following block.
            if (item != null)
                // Add item to registered.
                registered.Add(item);
        }

        // Set totalItems to number of entries in registered.
        totalItems = registered.Count;
        // Set collectedItems to number of entries in collected.
        collectedItems = collected.Count;
    }

    /// <summary>Count a registered item once, then ask the escape manager to check its requirements.</summary>
    // item is a Interactable argument supplied by CollectionProgress.Restore, Interactable.Interact.
    public void NotifyCollected(Interactable item)
    {
        // Stop this method early when not (requiredForEscape) or item is missing or not (registered contains
        // item).
        if (!requiredForEscape || item == null || !registered.Contains(item))
            // Return now; skip the rest of this method.
            return;

        // If collected accepts item as a new entry (a set rejects duplicates), run the following block.
        if (collected.Add(item))
        {
            // Set collectedItems to number of entries in collected.
            collectedItems = collected.Count;
            // Write the diagnostic message to the Unity Console; it does not update game UI.
            Debug.Log($"[CollectibleGroup] {name}: {collectedItems}/{totalItems} collected.");

            // If the scene EscapeManager registered in Awake exists, run the following block.
            if (EscapeManager.Instance != null)
                // Call EscapeManager.RefreshEscapeState; unlock escape only when every required group and optional
                // artifact total is complete.
                EscapeManager.Instance.RefreshEscapeState();
        }
    }
}
