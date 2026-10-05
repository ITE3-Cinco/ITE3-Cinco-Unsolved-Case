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

    private readonly HashSet<Interactable> collected = new HashSet<Interactable>();
    private readonly HashSet<Interactable> registered = new HashSet<Interactable>();

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

        totalItems = registered.Count;
        collectedItems = 0;
        collected.Clear();
    }

    public void NotifyCollected(Interactable item)
    {
        if (!requiredForEscape || item == null || !registered.Contains(item))
            return;

        if (collected.Add(item))
        {
            collectedItems++;
            Debug.Log($"[CollectibleGroup] {name}: {collectedItems}/{totalItems} collected.");

            if (EscapeManager.Instance != null)
                EscapeManager.Instance.RefreshEscapeState();
        }
    }
}