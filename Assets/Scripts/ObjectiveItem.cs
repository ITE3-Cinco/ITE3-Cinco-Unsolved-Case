using UnityEngine;
using TMPro;

public class ObjectiveItem : MonoBehaviour
{
    [Header("UI Reference")]
    public TextMeshProUGUI objectiveText;

    [Header("Objectives State")]
    public bool flashlightPickedUp = false;
    public bool artifactsCompleted = false;

    [Header("Colors (HEX Codes)")]
    public string activeColorHex = "#FFFFFF";    // White for active tasks
    public string completedColorHex = "#808080"; // Gray for finished tasks

    private void Start()
    {
        UpdateUI();
    }

    public void CompleteFlashlightTask()
    {
        flashlightPickedUp = true;
        UpdateUI();
    }

    public void CompleteArtifactsTask()
    {
        artifactsCompleted = true;
        UpdateUI();
    }

    public void UpdateUI()
    {
        if (objectiveText == null) return;

        // Once evidence is complete, show the single remaining task in the holder.
        if (artifactsCompleted)
        {
            objectiveText.text = $"<color={activeColorHex}>Objectives:</color>\n<color={activeColorHex}>• {EscapeManager.EscapeObjective}</color>";
            return;
        }

        // 1. Flashlight line formatting
        string flashlightLine = flashlightPickedUp
            ? $"<color={completedColorHex}><s>• Pick up flashlight</s></color>"
            : $"<color={activeColorHex}>• Pick up flashlight</color>";

        // 2. Artifacts line formatting
        string artifactsLine = $"<color={activeColorHex}>• Find all artifacts</color>";

        // 3. Render list with permanent "Objectives:" header
        objectiveText.text = $"<color={activeColorHex}>Objectives:</color>\n{flashlightLine}\n{artifactsLine}";
    }
}
