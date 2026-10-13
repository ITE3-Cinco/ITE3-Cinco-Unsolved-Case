// Purpose: Counts unique artifacts and updates the collection objective.
// Use: Attach to a scene manager and assign the counter text and objective UI.
// How it works: Pickups call CollectArtifact; a set prevents counting the same object twice.

// System.Collections.Generic supplies typed lists, sets and dictionaries for stored data.
using System.Collections.Generic;
// UnityEngine supplies GameObjects, components, transforms, time, math and physics used here.
using UnityEngine;
// TextMesh Pro supplies the text components, fonts and text-mesh information used here.
using TMPro;

public class ArtifactManager : MonoBehaviour
{
    // Instance lets pickups find this scene tracker; the event lets other systems react to progress.
    // ArtifactManager.Instance is the shared reference set to this component during initialization; other scripts
    // read it to find this manager.
    public static ArtifactManager Instance;

    // ArtifactCountChanged lets other components subscribe a callback; Invoke later supplies its event arguments
    // to those listeners.
    public event System.Action<int> ArtifactCountChanged;

    // artifactsCollected: number of distinct pickups accepted by ArtifactManager. CollectArtifact sets it from
    // number of entries in collectedArtifacts.
    [Header("Artifact Tracker Settings")]
    public int artifactsCollected = 0;
    // totalArtifacts: required count before artifact completion. Assigned in the scene/prefab Inspector; the
    // initializer below is the default.
    public int totalArtifacts = 3;

    // collectedMessage: pickup message copied into notifications before all artifacts are found. Assigned in the
    // scene/prefab Inspector; the initializer below is the default.
    [Header("Pickup Messages")]
    public string collectedMessage = "Acquired an artifact!";
    // completedMessage: notification text used once artifact collection is complete. Assigned in the scene/prefab
    // Inspector; the initializer below is the default.
    public string completedMessage = EscapeManager.EscapeObjective;

    // artifactCounterText: HUD text receiving the collected/required counter. Assigned in the scene/prefab
    // Inspector.
    [Header("UI References")]
    public TextMeshProUGUI artifactCounterText; // Option icon counter (e.g., 0/3)
    // artifactObjective: ObjectiveItem notified when all artifacts are collected. Assigned in the scene/prefab
    // Inspector.
    public ObjectiveItem artifactObjective;    // Drag your ObjectiveItem script here

    // A HashSet stores each pickup only once, so repeated calls cannot increase the count.
    // collectedArtifacts: set of already-counted Interactable objects, preventing duplicate collection.
    // Initialized from a new hashset with no entries.
    private readonly HashSet<Interactable> collectedArtifacts = new HashSet<Interactable>();

    /// <summary>Make this the shared scene tracker; remove a duplicate manager object.</summary>
    private void Awake()
    {
        // If Instance is missing, run the following block.
        if (Instance == null)
        {
            // Register this component in the class shared Instance field so other scripts can locate it without
            // repeated scene searches.
            Instance = this;
        }
        else
        {
            // Schedule the GameObject that owns this component for destruction at Unity end-of-frame cleanup.
            Destroy(gameObject);
        }
    }

    /// <summary>Clear the shared reference when this manager is destroyed.</summary>
    private void OnDestroy()
    {
        // If Instance equals this, run the following block.
        if (Instance == this)
            // Clear this class shared Instance reference so callers cannot reuse the removed component.
            Instance = null;
    }

    /// <summary>Show the starting artifact count when the scene begins.</summary>
    private void Start()
    {
        // Call ArtifactManager.UpdateUI; write the collected and required counts into the assigned UI text.
        UpdateUI();
    }

    /// <summary>Accept a new artifact, update the display and tell listeners that progress changed.</summary>
    // artifact is the Interactable supplied by a successful pickup or checkpoint restoration; its object identity
    // prevents duplicate counting.
    public void CollectArtifact(Interactable artifact)
    {
        // If artifact is missing, run the following block.
        if (artifact == null)
        {
            // Report the missing/invalid setup or state as a Unity Console warning.
            Debug.LogWarning("[ArtifactManager] CollectArtifact called with a null artifact.");
            // Return now; skip the rest of this method.
            return;
        }

        // If not (collectedArtifacts accepts artifact as a new entry (a set rejects duplicates)), run the
        // following block.
        if (!collectedArtifacts.Add(artifact))
        {
            // Write the diagnostic message to the Unity Console; it does not update game UI.
            Debug.Log($"[ArtifactManager] Ignored duplicate collection for '{artifact.gameObject.name}'.");
            // Return now; skip the rest of this method.
            return;
        }

        // Update local UI before listeners react to the new artifact count.
        // Copy the HashSet unique-pickup count into artifactsCollected; duplicates cannot increase this value.
        artifactsCollected = collectedArtifacts.Count;
        // Call ArtifactManager.UpdateUI; write the collected and required counts into the assigned UI text.
        UpdateUI();
        // Send the new artifactsCollected count to subscribed scare, escape and story handlers; ?. does nothing if
        // no listener exists.
        ArtifactCountChanged?.Invoke(artifactsCollected);

        // Write the diagnostic message to the Unity Console; it does not update game UI.
        Debug.Log($"Artifact Collected! Progress: {artifactsCollected}/{totalArtifacts}");

        // If artifactsCollected is at least totalArtifacts, run the following block.
        if (artifactsCollected >= totalArtifacts)
        {
            // Call ArtifactManager.OnAllArtifactsCollected; mark the artifact objective complete once the required
            // total is reached.
            OnAllArtifactsCollected();
        }
    }

    /// <summary>Write the collected and required counts into the assigned UI text.</summary>
    private void UpdateUI()
    {
        // If artifactCounterText exists, run the following block.
        if (artifactCounterText != null)
        {
            // Display formatted text inserting {, { in artifactCounterText.text (the assigned TMP/UI text
            // component).
            artifactCounterText.text = $"{artifactsCollected}/{totalArtifacts}";
        }
    }

    /// <summary>Mark the artifact objective complete once the required total is reached.</summary>
    private void OnAllArtifactsCollected()
    {
        // Call CompleteArtifactsTask() to cross out "Find all artifacts"
        // If artifactObjective exists, run the following block.
        if (artifactObjective != null)
        {
            // Call ObjectiveItem.CompleteArtifactsTask; mark artifact collection complete and redraw the remaining
            // goal.
            artifactObjective.CompleteArtifactsTask();
        }

        // Write the diagnostic message to the Unity Console; it does not update game UI.
        Debug.Log("All artifacts collected! Objective updated.");
    }
}