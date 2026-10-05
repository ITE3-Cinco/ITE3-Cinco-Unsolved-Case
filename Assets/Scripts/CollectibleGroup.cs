using System.Collections.Generic;
using UnityEngine;

public class CollectibleGroup : MonoBehaviour
{
    [Header("Collection Group")]
    [Tooltip("All Interactable children of this object count as required collectibles.")]
    public bool requiredForEscape = true;

    [Header("Progress")]
    [SerializeField] private int totalItems;
    [SerializeField] private int collectedItems;

    // Keep references to all items ever registered/collected. Unity object
    // references remain valid as identity even after the world object is destroyed.
    private readonly HashSet<Interactable> registered = new HashSet<Interactable>();
    private readonly HashSet<Interactable> collected = new HashSet<Interactable>();

    public int TotalItems => totalItems;
    public int CollectedItems => collectedItems;
    public bool IsComplete => !requiredForEscape || totalItems == 0 || collectedItems >= totalItems;

    private void Awake()
    {
        RefreshGroup();
    }

    public void RefreshGroup()
    {
        Interactable[] items = GetComponentsInChildren<Interactable>(true);

        // Add currently existing children without clearing previous progress.
        // Previously collected objects may already have been destroyed, but
        // their references remain in the HashSet and continue counting.
        foreach (Interactable item in items)
        {
            if (item != null)
                registered.Add(item);
        }

        totalItems = registered.Count;
        collectedItems = collected.Count;
    }

    public void NotifyCollected(Interactable item)
    {
        if (!requiredForEscape || item == null || !registered.Contains(item))
            return;

        if (collected.Add(item))
        {
            collectedItems = collected.Count;
            Debug.Log($"[CollectibleGroup] {name}: {collectedItems}/{totalItems} collected.");

            if (EscapeManager.Instance != null)
                EscapeManager.Instance.RefreshEscapeState();
        }
    }
}
