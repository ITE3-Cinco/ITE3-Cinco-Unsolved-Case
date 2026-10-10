using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class ArtifactManager : MonoBehaviour
{
    public static ArtifactManager Instance;

    public event System.Action<int> ArtifactCountChanged;

    [Header("Artifact Tracker Settings")]
    public int artifactsCollected = 0;
    public int totalArtifacts = 3;

    [Header("Pickup Messages")]
    public string collectedMessage = "Acquired an artifact!";
    public string completedMessage = EscapeManager.EscapeObjective;

    [Header("UI References")]
    public TextMeshProUGUI artifactCounterText; // Option icon counter (e.g., 0/3)
    public ObjectiveItem artifactObjective;    // Drag your ObjectiveItem script here

    private readonly HashSet<Interactable> collectedArtifacts = new HashSet<Interactable>();

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    private void Start()
    {
        UpdateUI();
    }

    public void CollectArtifact(Interactable artifact)
    {
        if (artifact == null)
        {
            Debug.LogWarning("[ArtifactManager] CollectArtifact called with a null artifact.");
            return;
        }

        if (!collectedArtifacts.Add(artifact))
        {
            Debug.Log($"[ArtifactManager] Ignored duplicate collection for '{artifact.gameObject.name}'.");
            return;
        }

        artifactsCollected = collectedArtifacts.Count;
        UpdateUI();
        ArtifactCountChanged?.Invoke(artifactsCollected);

        Debug.Log($"Artifact Collected! Progress: {artifactsCollected}/{totalArtifacts}");

        if (artifactsCollected >= totalArtifacts)
        {
            OnAllArtifactsCollected();
        }
    }

    private void UpdateUI()
    {
        if (artifactCounterText != null)
        {
            artifactCounterText.text = $"{artifactsCollected}/{totalArtifacts}";
        }
    }

    private void OnAllArtifactsCollected()
    {
        // Call CompleteArtifactsTask() to cross out "Find all artifacts"
        if (artifactObjective != null)
        {
            artifactObjective.CompleteArtifactsTask();
        }

        Debug.Log("All artifacts collected! Objective updated.");
    }
}