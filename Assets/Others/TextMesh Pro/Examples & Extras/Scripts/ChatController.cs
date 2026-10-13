// Purpose: Appends submitted chat text with a time stamp to a scrolling display.
// Use: Assign a TMP input field, output text and scrollbar in a sample canvas.
// How it works: Input submission triggers formatting, clears the field and scrolls to the latest entry.
// Context: bundled TextMesh Pro example; these notes explain its sample behavior.

// UnityEngine supplies GameObjects, components, transforms, time, math and physics used here.
using UnityEngine;
// Unity UI supplies canvas widgets such as images, sliders and buttons.
using UnityEngine.UI;
// TextMesh Pro supplies the text components, fonts and text-mesh information used here.
using TMPro;

public class ChatController : MonoBehaviour {


    // ChatInputField: chat input field. Assigned in the scene/prefab Inspector.
    public TMP_InputField ChatInputField;

    // ChatDisplayOutput: chat display output. Assigned in the scene/prefab Inspector.
    public TMP_Text ChatDisplayOutput;

    // ChatScrollbar: chat scrollbar. Assigned in the scene/prefab Inspector.
    public Scrollbar ChatScrollbar;

    // While enabled, register the callbacks used by this example.
    void OnEnable()
    {
        // Connect AddToChatOutput as a callback on ChatInputField.onSubmit.
        ChatInputField.onSubmit.AddListener(AddToChatOutput);
    }

    // Remove the registered callbacks when the example is disabled.
    void OnDisable()
    {
        // Disconnect AddToChatOutput as a callback on ChatInputField.onSubmit.
        ChatInputField.onSubmit.RemoveListener(AddToChatOutput);
    }


    // Add a time stamp, append the message, refocus input and scroll to the latest entry.
    // newText is a string argument supplied by the code, event or Inspector callback invoking AddToChatOutput.
    void AddToChatOutput(string newText)
    {
        // Clear Input Field
        // Display string.Empty in ChatInputField.text (the assigned TMP/UI text component).
        ChatInputField.text = string.Empty;

        // Store System.DateTime.Now in local timeNow.
        var timeNow = System.DateTime.Now;

        // Store "[<#FFFF80>" + timeNow.Hour.ToString("d2") + ":" + timeNow.Minute.ToString("d2") + ":" +
        // timeNow.Second.ToString("d2") + "</color>] " + newText (calculated from the values shown) in local
        // formattedInput.
        string formattedInput = "[<#FFFF80>" + timeNow.Hour.ToString("d2") + ":" + timeNow.Minute.ToString("d2") + ":" + timeNow.Second.ToString("d2") + "</color>] " + newText;

        // If ChatDisplayOutput exists, run the following block.
        if (ChatDisplayOutput != null)
        {
            // No special formatting for first entry
            // Add line feed before each subsequent entries
            // If ChatDisplayOutput.text equals string.Empty, run the following block.
            if (ChatDisplayOutput.text == string.Empty)
                // Copy formattedInput into ChatDisplayOutput.text.
                ChatDisplayOutput.text = formattedInput;
            else
                // Add "\n" plus formattedInput to ChatDisplayOutput.text and store the updated value there.
                ChatDisplayOutput.text += "\n" + formattedInput;
        }

        // Keep Chat input field active
        // Call ChatInputField.ActivateInputField.
        ChatInputField.ActivateInputField();

        // Set the scrollbar to the bottom when next text is submitted.
        // Set ChatScrollbar.value to 0.
        ChatScrollbar.value = 0;
    }

}
