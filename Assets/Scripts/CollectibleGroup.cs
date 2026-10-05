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

    private readonly HashSet<Interactable> registered = new HashSet<Interactable>();
    private readonly HashSet<int> registeredIds = new HashSet<int>();
    private readonly HashSet<int> collectedIds = new HashSet<int>();

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

        registered.Clear();

        foreach (Interactable item in items)
        {
            if (item != null)
                registered.Add(item);
        }

        // Keep stable IDs for previously registered items so destroyed
        // collectibles remain counted after they have been picked up.
        foreach (Interactable item in registered)
            registeredIds.Add(item.GetInstanceID());

        // Refresh can discover newly added collectibles, but never removes
        // previously registered ones or resets collection progress.
        totalItems = Mathf.Max(totalItems, registeredIds.Count);
        collectedItems = collectedIds.Count;
    }

    public void NotifyCollected(Interactable item)
    {
        if (!requiredForEscape || item == null || !registered.Contains(item))
            return;

        int itemId = item.GetInstanceID();

        if (collectedIds.Add(itemId))
        {
            collectedItems++;
            Debug.Log($"[CollectibleGroup] {name}: {collectedItems}/{totalItems} collected.");

            if (EscapeManager.Instance != null)
                EscapeManager.Instance.RefreshEscapeState();
        }
    }
}
