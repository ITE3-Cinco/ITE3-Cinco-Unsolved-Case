// Purpose: Logs text-selection events to show how callbacks receive text information.
// Use: Assign TMP_TextEventHandler from the sample text object.
// How it works: Subscribe while enabled and print the selected character, sprite, word, line or link.
// Context: bundled TextMesh Pro example; these notes explain its sample behavior.

// UnityEngine supplies GameObjects, components, transforms, time, math and physics used here.
using UnityEngine;


namespace TMPro.Examples
{
    public class TMP_TextEventCheck : MonoBehaviour
    {

        // TextEventHandler: text event handler. Assigned in the scene/prefab Inspector.
        public TMP_TextEventHandler TextEventHandler;

        // m_TextComponent: text component. OnEnable retrieves it from the first TMP_Text component on
        // TextEventHandler.
        private TMP_Text m_TextComponent;

        // Find the selected text component and listen for character, sprite, word, line and link selections.
        void OnEnable()
        {
            // If TextEventHandler exists, run the following block.
            if (TextEventHandler != null)
            {
                // Get a reference to the text component
                // Store the first TMP_Text component on TextEventHandler in m_TextComponent.
                m_TextComponent = TextEventHandler.GetComponent<TMP_Text>();
                
                // Connect OnCharacterSelection as a callback on TextEventHandler.onCharacterSelection.
                TextEventHandler.onCharacterSelection.AddListener(OnCharacterSelection);
                // Connect OnSpriteSelection as a callback on TextEventHandler.onSpriteSelection.
                TextEventHandler.onSpriteSelection.AddListener(OnSpriteSelection);
                // Connect OnWordSelection as a callback on TextEventHandler.onWordSelection.
                TextEventHandler.onWordSelection.AddListener(OnWordSelection);
                // Connect OnLineSelection as a callback on TextEventHandler.onLineSelection.
                TextEventHandler.onLineSelection.AddListener(OnLineSelection);
                // Connect OnLinkSelection as a callback on TextEventHandler.onLinkSelection.
                TextEventHandler.onLinkSelection.AddListener(OnLinkSelection);
            }
        }


        // Remove the registered callbacks when the example is disabled.
        void OnDisable()
        {
            // If TextEventHandler exists, run the following block.
            if (TextEventHandler != null)
            {
                // Disconnect OnCharacterSelection as a callback on TextEventHandler.onCharacterSelection.
                TextEventHandler.onCharacterSelection.RemoveListener(OnCharacterSelection);
                // Disconnect OnSpriteSelection as a callback on TextEventHandler.onSpriteSelection.
                TextEventHandler.onSpriteSelection.RemoveListener(OnSpriteSelection);
                // Disconnect OnWordSelection as a callback on TextEventHandler.onWordSelection.
                TextEventHandler.onWordSelection.RemoveListener(OnWordSelection);
                // Disconnect OnLineSelection as a callback on TextEventHandler.onLineSelection.
                TextEventHandler.onLineSelection.RemoveListener(OnLineSelection);
                // Disconnect OnLinkSelection as a callback on TextEventHandler.onLinkSelection.
                TextEventHandler.onLinkSelection.RemoveListener(OnLinkSelection);
            }
        }


        // Log the selected character and its identifying information from the callback.
        // c is a char argument supplied by the code, event or Inspector callback invoking OnCharacterSelection.
        // index is a int argument supplied by the code, event or Inspector callback invoking OnCharacterSelection.
        void OnCharacterSelection(char c, int index)
        {
            // Write the diagnostic message to the Unity Console; it does not update game UI.
            Debug.Log("Character [" + c + "] at Index: " + index + " has been selected.");
        }

        // Log the selected sprite and its identifying information from the callback.
        // c is a char argument supplied by the code, event or Inspector callback invoking OnSpriteSelection.
        // index is a int argument supplied by the code, event or Inspector callback invoking OnSpriteSelection.
        void OnSpriteSelection(char c, int index)
        {
            // Write the diagnostic message to the Unity Console; it does not update game UI.
            Debug.Log("Sprite [" + c + "] at Index: " + index + " has been selected.");
        }

        // Log the selected word and its identifying information from the callback.
        // word is a string argument supplied by the code, event or Inspector callback invoking OnWordSelection.
        // firstCharacterIndex is a int argument supplied by the code, event or Inspector callback invoking
        // OnWordSelection.
        // length is a int argument supplied by the code, event or Inspector callback invoking OnWordSelection.
        void OnWordSelection(string word, int firstCharacterIndex, int length)
        {
            // Write the diagnostic message to the Unity Console; it does not update game UI.
            Debug.Log("Word [" + word + "] with first character index of " + firstCharacterIndex + " and length of " + length + " has been selected.");
        }

        // Log the selected line and its identifying information from the callback.
        // lineText is a string argument supplied by the code, event or Inspector callback invoking
        // OnLineSelection.
        // firstCharacterIndex is a int argument supplied by the code, event or Inspector callback invoking
        // OnLineSelection.
        // length is a int argument supplied by the code, event or Inspector callback invoking OnLineSelection.
        void OnLineSelection(string lineText, int firstCharacterIndex, int length)
        {
            // Write the diagnostic message to the Unity Console; it does not update game UI.
            Debug.Log("Line [" + lineText + "] with first character index of " + firstCharacterIndex + " and length of " + length + " has been selected.");
        }

        // Log the selected link and its identifying information from the callback.
        // linkID is a string argument supplied by the code, event or Inspector callback invoking OnLinkSelection.
        // linkText is a string argument supplied by the code, event or Inspector callback invoking
        // OnLinkSelection.
        // linkIndex is a int argument supplied by the code, event or Inspector callback invoking OnLinkSelection.
        void OnLinkSelection(string linkID, string linkText, int linkIndex)
        {
            // If m_TextComponent exists, run the following block.
            if (m_TextComponent != null)
            {
                // Store m_TextComponent.textInfo.linkInfo[linkIndex] (entry read from
                // m_TextComponent.textInfo.linkInfo) in local linkInfo.
                TMP_LinkInfo linkInfo = m_TextComponent.textInfo.linkInfo[linkIndex];
            }
            
            // Write the diagnostic message to the Unity Console; it does not update game UI.
            Debug.Log("Link Index: " + linkIndex + " with ID [" + linkID + "] and Text \"" + linkText + "\" has been selected.");
        }

    }
}
