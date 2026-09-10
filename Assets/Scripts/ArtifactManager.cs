using UnityEngine;
using TMPro;

public class ArtifactManager : MonoBehaviour
{
    public static ArtifactManager Instance;

    [Header("Artifact Tracker Settings")]
    public int artifactsCollected = 0;
    public int totalArtifacts = 3;

    [Header("UI References")]
    public TextMeshProUGUI artifactCounterText; // Option icon counter (e.g., 0/3)
    public ObjectiveItem artifactObjective;    // Drag your ObjectiveItem script here

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

    private void Start()
    {
        UpdateUI();
    }

    public void CollectArtifact()
    {
        artifactsCollected++;
        UpdateUI();

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