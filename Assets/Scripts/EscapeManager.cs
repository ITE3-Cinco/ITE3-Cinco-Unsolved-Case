using UnityEngine;

public class EscapeManager : MonoBehaviour
{
    public static EscapeManager Instance;

    [Header("Required Collectible Groups")]
    [Tooltip("Assign parent objects containing the items required to unlock the escape door.")]
    public CollectibleGroup[] requiredGroups;

    [Header("Optional Existing Artifact Requirement")]
    public bool requireAllArtifacts = false;

    public bool IsEscapeUnlocked { get; private set; }

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }

    private void Start()
    {
        RefreshEscapeState();
    }

    public void RefreshEscapeState()
    {
        bool groupsComplete = true;

        if (requiredGroups != null)
        {
            foreach (CollectibleGroup group in requiredGroups)
            {
                if (group != null && !group.IsComplete)
                {
                    groupsComplete = false;
                    break;
                }
            }
        }

        bool artifactsComplete = true;
        if (requireAllArtifacts)
        {
            artifactsComplete = ArtifactManager.Instance != null &&
                                ArtifactManager.Instance.artifactsCollected >= ArtifactManager.Instance.totalArtifacts;
        }

        IsEscapeUnlocked = groupsComplete && artifactsComplete;
        Debug.Log($"[EscapeManager] Escape unlocked: {IsEscapeUnlocked}");
    }

    public bool CanEscape()
    {
        RefreshEscapeState();
        return IsEscapeUnlocked;
    }
}