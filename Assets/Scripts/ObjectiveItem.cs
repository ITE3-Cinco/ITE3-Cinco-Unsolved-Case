// Purpose: Displays the current goal or the flashlight and artifact task list.
// Use: Attach to the HUD objective object and assign its TextMeshPro text.
// How it works: Task flags choose active or completed formatting; story scenes can supply a single primary goal.

// UnityEngine supplies GameObjects, components, transforms, time, math and physics used here.
using UnityEngine;
// TextMesh Pro supplies the text components, fonts and text-mesh information used here.
using TMPro;

public class ObjectiveItem : MonoBehaviour
{
    // objectiveText: TMP text that displays the current goal list. Assigned in the scene/prefab Inspector.
    [Header("UI Reference")]
    public TextMeshProUGUI objectiveText;

    // primaryObjective: Optional single goal for scenes without collectible objectives. Assigned in the
    // scene/prefab Inspector.
    [Tooltip("Optional single goal for scenes without collectible objectives.")]
    public string primaryObjective;

    // flashlightPickedUp: objective completion flag for flashlight pickup. Updated by CompleteFlashlightTask.
    [Header("Objectives State")]
    public bool flashlightPickedUp = false;
    // artifactsCompleted: objective completion flag for finding all artifacts. Updated by CompleteArtifactsTask.
    public bool artifactsCompleted = false;

    // activeColorHex: text color for unfinished goals. Assigned in the scene/prefab Inspector; the initializer
    // below is the default.
    [Header("Colors (HEX Codes)")]
    public string activeColorHex = "#FFFFFF";    // White for active tasks
    // completedColorHex: text color for completed goals. Assigned in the scene/prefab Inspector; the initializer
    // below is the default.
    public string completedColorHex = "#808080"; // Gray for finished tasks

    /// <summary>Draw the starting objective list when the UI begins.</summary>
    private void Start()
    {
        // Call ObjectiveItem.UpdateUI; choose a primary goal, escape goal or task list and format the assigned
        // text.
        UpdateUI();
    }

    /// <summary>Mark the flashlight task complete and redraw the list.</summary>
    public void CompleteFlashlightTask()
    {
        // Set flashlightPickedUp to true.
        flashlightPickedUp = true;
        // Call ObjectiveItem.UpdateUI; choose a primary goal, escape goal or task list and format the assigned
        // text.
        UpdateUI();
    }

    /// <summary>Mark artifact collection complete and redraw the remaining goal.</summary>
    public void CompleteArtifactsTask()
    {
        // Set artifactsCompleted to true.
        artifactsCompleted = true;
        // Call ObjectiveItem.UpdateUI; choose a primary goal, escape goal or task list and format the assigned
        // text.
        UpdateUI();
    }

    /// <summary>Choose a primary goal, escape goal or task list and format the assigned text.</summary>
    public void UpdateUI()
    {
        // If objectiveText is missing, return now; skip the rest of this method.
        if (objectiveText == null) return;

        // If not (the result of string.IsNullOrWhiteSpace using primaryObjective), run the following block.
        if (!string.IsNullOrWhiteSpace(primaryObjective))
        {
            // Display formatted text inserting {, {, { in objectiveText.text (the assigned TMP/UI text component).
            objectiveText.text = $"<color={activeColorHex}>Objectives:</color>\n<color={activeColorHex}>• {primaryObjective}</color>";
            // Return now; skip the rest of this method.
            return;
        }

        // Once evidence is complete, show the single remaining task in the holder.
        // If artifactsCompleted, run the following block.
        if (artifactsCompleted)
        {
            // Display formatted text inserting {, {, { in objectiveText.text (the assigned TMP/UI text component).
            objectiveText.text = $"<color={activeColorHex}>Objectives:</color>\n<color={activeColorHex}>• {EscapeManager.EscapeObjective}</color>";
            // Return now; skip the rest of this method.
            return;
        }

        // 1. Flashlight line formatting
        // Store formatted text inserting { when flashlightPickedUp; otherwise formatted text inserting { in local
        // flashlightLine.
        string flashlightLine = flashlightPickedUp
            ? $"<color={completedColorHex}><s>• Pick up flashlight</s></color>"
            : $"<color={activeColorHex}>• Pick up flashlight</color>";

        // 2. Artifacts line formatting
        // Store formatted text inserting { in local artifactsLine.
        string artifactsLine = $"<color={activeColorHex}>• Find all artifacts</color>";

        // 3. Render list with permanent "Objectives:" header
        // Display formatted text inserting {, {, { in objectiveText.text (the assigned TMP/UI text component).
        objectiveText.text = $"<color={activeColorHex}>Objectives:</color>\n{flashlightLine}\n{artifactsLine}";
    }
}
