using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>Session collection history and an immutable copy at the latest checkpoint.</summary>
public static class CollectionProgress
{
    private static HashSet<string> collected = new HashSet<string>();
    private static HashSet<string> saved;
    private static List<ItemData> savedInventory;
    private static int savedSlot;
    private static string nextScene;

    public static int TotalCollected => collected.Count;

    public static void Record(Interactable item) => collected.Add(item.CollectionId);

    public static void Reset()
    {
        collected.Clear();
        saved = null;
        savedInventory = null;
        nextScene = null;
    }

    public static void PrepareForNextLevel(string sceneName)
    {
        nextScene = sceneName;
    }

    // Called before any Start callbacks. Only a retry or explicit escape carries history.
    public static void BeginRun(string scenePath, bool retry)
    {
        bool advancing = nextScene == System.IO.Path.GetFileNameWithoutExtension(scenePath);
        nextScene = null;
        if (retry && saved != null)
            collected = new HashSet<string>(saved);
        else if (!advancing)
            Reset();
        if (!retry)
        {
            saved = null;
            savedInventory = null;
        }
    }

    public static void Save(Scene scene)
    {
        saved = new HashSet<string>(collected);
        Inventory inventory = FindInventory(scene);
        savedInventory = inventory != null ? new List<ItemData>(inventory.items) : null;
        savedSlot = inventory != null ? inventory.selectedIndex : 0;
    }

    public static void Restore(Scene scene)
    {
        // Restore trackers using the reloaded objects so their duplicate guards and
        // collectible groups retain real identities, including pickups without ItemData.
        foreach (GameObject root in scene.GetRootGameObjects())
        foreach (Interactable item in root.GetComponentsInChildren<Interactable>(true))
        {
            if (item.isCheckpoint || !collected.Contains(item.CollectionId)) continue;
            if (item.isArtifact && ArtifactManager.Instance != null)
                ArtifactManager.Instance.CollectArtifact(item);
            CollectibleGroup group = item.GetComponentInParent<CollectibleGroup>();
            if (group != null) group.NotifyCollected(item);
            item.gameObject.SetActive(false);
            Object.Destroy(item.gameObject);
        }

        Inventory inventory = FindInventory(scene);
        if (inventory != null && savedInventory != null)
        {
            inventory.items = new List<ItemData>(savedInventory);
            PlayerMovement player = inventory.GetComponentInParent<PlayerMovement>();
            if (player != null)
            {
                player.HasFlashlight = inventory.items.Contains(player.flashlightItemData);
                if (player.HasFlashlight)
                    foreach (GameObject root in scene.GetRootGameObjects())
                    foreach (ObjectiveItem objective in root.GetComponentsInChildren<ObjectiveItem>(true))
                        objective.CompleteFlashlightTask();
            }
            inventory.SelectSlot(savedSlot);
        }
        if (EscapeManager.Instance != null) EscapeManager.Instance.RefreshEscapeState();
    }

    private static Inventory FindInventory(Scene scene)
    {
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            Inventory inventory = root.GetComponentInChildren<Inventory>(true);
            if (inventory != null) return inventory;
        }
        return null;
    }
}
