// Purpose: Raises events when the pointer intersects parts of a TMP text object.
// Use: Attach to sample TMP text and connect listeners to its selection events.
// How it works: Use the correct canvas camera to test pointer intersections and invoke matching callbacks.
// Context: bundled TextMesh Pro example; these notes explain its sample behavior.

// UnityEngine supplies GameObjects, components, transforms, time, math and physics used here.
using UnityEngine;
// Unity Events supplies Inspector-connectable events and listener callbacks.
using UnityEngine.Events;
// Unity EventSystem supplies pointer callbacks and event data for UI interactions.
using UnityEngine.EventSystems;
// System supplies .NET types, delegates and attributes such as Serializable.
using System;


namespace TMPro
{

    public class TMP_TextEventHandler : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        [Serializable]
        public class CharacterSelectionEvent : UnityEvent<char, int> { }

        [Serializable]
        public class SpriteSelectionEvent : UnityEvent<char, int> { }

        [Serializable]
        public class WordSelectionEvent : UnityEvent<string, int, int> { }

        [Serializable]
        public class LineSelectionEvent : UnityEvent<string, int, int> { }

        [Serializable]
        public class LinkSelectionEvent : UnityEvent<string, string, int> { }


        /// <summary>
        /// Event delegate triggered when pointer is over a character.
        /// </summary>
        // onCharacterSelection exposes its value through the getter/setter below; getter results go to the reader
        // and value comes from the writer.
        public CharacterSelectionEvent onCharacterSelection
        {
            get { return m_OnCharacterSelection; }
            set { m_OnCharacterSelection = value; }
        }
        // m_OnCharacterSelection: on character selection. Assigned in the scene/prefab Inspector; the initializer
        // below is the default.
        [SerializeField]
        private CharacterSelectionEvent m_OnCharacterSelection = new CharacterSelectionEvent();


        /// <summary>
        /// Event delegate triggered when pointer is over a sprite.
        /// </summary>
        // onSpriteSelection exposes its value through the getter/setter below; getter results go to the reader and
        // value comes from the writer.
        public SpriteSelectionEvent onSpriteSelection
        {
            get { return m_OnSpriteSelection; }
            set { m_OnSpriteSelection = value; }
        }
        // m_OnSpriteSelection: on sprite selection. Assigned in the scene/prefab Inspector; the initializer below
        // is the default.
        [SerializeField]
        private SpriteSelectionEvent m_OnSpriteSelection = new SpriteSelectionEvent();


        /// <summary>
        /// Event delegate triggered when pointer is over a word.
        /// </summary>
        // onWordSelection exposes its value through the getter/setter below; getter results go to the reader and
        // value comes from the writer.
        public WordSelectionEvent onWordSelection
        {
            get { return m_OnWordSelection; }
            set { m_OnWordSelection = value; }
        }
        // m_OnWordSelection: on word selection. Assigned in the scene/prefab Inspector; the initializer below is
        // the default.
        [SerializeField]
        private WordSelectionEvent m_OnWordSelection = new WordSelectionEvent();


        /// <summary>
        /// Event delegate triggered when pointer is over a line.
        /// </summary>
        // onLineSelection exposes its value through the getter/setter below; getter results go to the reader and
        // value comes from the writer.
        public LineSelectionEvent onLineSelection
        {
            get { return m_OnLineSelection; }
            set { m_OnLineSelection = value; }
        }
        // m_OnLineSelection: on line selection. Assigned in the scene/prefab Inspector; the initializer below is
        // the default.
        [SerializeField]
        private LineSelectionEvent m_OnLineSelection = new LineSelectionEvent();


        /// <summary>
        /// Event delegate triggered when pointer is over a link.
        /// </summary>
        // onLinkSelection exposes its value through the getter/setter below; getter results go to the reader and
        // value comes from the writer.
        public LinkSelectionEvent onLinkSelection
        {
            get { return m_OnLinkSelection; }
            set { m_OnLinkSelection = value; }
        }
        // m_OnLinkSelection: on link selection. Assigned in the scene/prefab Inspector; the initializer below is
        // the default.
        [SerializeField]
        private LinkSelectionEvent m_OnLinkSelection = new LinkSelectionEvent();



        // m_TextComponent: text component. Awake retrieves it from the first TMP_Text component on the GameObject
        // that owns this component.
        private TMP_Text m_TextComponent;

        // m_Camera: camera. Awake retrieves it from Unity first enabled camera tagged MainCamera.
        private Camera m_Camera;
        // m_Canvas: canvas. Awake retrieves it from the first Canvas component on or above the GameObject that
        // owns this component.
        private Canvas m_Canvas;

        // m_selectedLink: selected link. LateUpdate sets it from linkIndex.
        private int m_selectedLink = -1;
        // m_lastCharIndex: last char index. LateUpdate sets it from charIndex.
        private int m_lastCharIndex = -1;
        // m_lastWordIndex: last word index. LateUpdate sets it from wordIndex.
        private int m_lastWordIndex = -1;
        // m_lastLineIndex: last line index. LateUpdate sets it from lineIndex.
        private int m_lastLineIndex = -1;

        // Before the example starts, cache its components and prepare the starting display.
        void Awake()
        {
            // Get a reference to the text component.
            // Store the first TMP_Text component on the GameObject that owns this component in m_TextComponent.
            m_TextComponent = gameObject.GetComponent<TMP_Text>();

            // Get a reference to the camera rendering the text taking into consideration the text component type.
            // If runtime type information for m_TextComponent equals the runtime type TextMeshProUGUI, run the
            // following block.
            if (m_TextComponent.GetType() == typeof(TextMeshProUGUI))
            {
                // Store the first Canvas component on or above the GameObject that owns this component in
                // m_Canvas.
                m_Canvas = gameObject.GetComponentInParent<Canvas>();
                // If m_Canvas exists, run the following block.
                if (m_Canvas != null)
                {
                    // If m_Canvas.renderMode equals RenderMode.ScreenSpaceOverlay, run the following block.
                    if (m_Canvas.renderMode == RenderMode.ScreenSpaceOverlay)
                        // Clear m_Camera to null so it no longer holds a reference.
                        m_Camera = null;
                    else
                        // Set m_Camera to m_Canvas.worldCamera.
                        m_Camera = m_Canvas.worldCamera;
                }
            }
            else
            {
                // Set m_Camera to Unity first enabled camera tagged MainCamera.
                m_Camera = Camera.main;
            }
        }


        // Test the pointer against characters, sprites, words, lines and links, then send selection events.
        void LateUpdate()
        {
            // If the result of TMP_TextUtilities.IsIntersectingRectTransform using m_TextComponent.rectTransform,
            // Input.mousePosition, m_Camera, run the following block.
            if (TMP_TextUtilities.IsIntersectingRectTransform(m_TextComponent.rectTransform, Input.mousePosition, m_Camera))
            {
                #region Nearest Character
                /*int charIndex = TMP_TextUtilities.FindNearestCharacterOnLine(m_TextComponent, Input.mousePosition, 0, m_Camera, false);
                if (charIndex != -1 && charIndex != m_lastCharIndex)
                {
                    m_lastCharIndex = charIndex;
                }*/
                #endregion


                #region Example of Character or Sprite Selection
                // Store the result of TMP_TextUtilities.FindIntersectingCharacter using m_TextComponent,
                // Input.mousePosition, m_Camera, true in local charIndex.
                int charIndex = TMP_TextUtilities.FindIntersectingCharacter(m_TextComponent, Input.mousePosition, m_Camera, true);
                // If charIndex differs from negative 1 and charIndex differs from m_lastCharIndex, run the
                // following block.
                if (charIndex != -1 && charIndex != m_lastCharIndex)
                {
                    // Copy charIndex into m_lastCharIndex.
                    m_lastCharIndex = charIndex;

                    // Store m_TextComponent.textInfo.characterInfo[charIndex].elementType in local elementType.
                    TMP_TextElementType elementType = m_TextComponent.textInfo.characterInfo[charIndex].elementType;

                    // Send event to any event listeners depending on whether it is a character or sprite.
                    // If elementType equals TMP_TextElementType.Character, run the following block.
                    if (elementType == TMP_TextElementType.Character)
                        // Call TMP_TextEventHandler.SendOnCharacterSelection with
                        // m_TextComponent.textInfo.characterInfo[charIndex].character, charIndex; invoke
                        // registered listeners with the selected character information.
                        SendOnCharacterSelection(m_TextComponent.textInfo.characterInfo[charIndex].character, charIndex);
                    else if (elementType == TMP_TextElementType.Sprite)
                        // Call TMP_TextEventHandler.SendOnSpriteSelection with
                        // m_TextComponent.textInfo.characterInfo[charIndex].character, charIndex; invoke
                        // registered listeners with the selected sprite information.
                        SendOnSpriteSelection(m_TextComponent.textInfo.characterInfo[charIndex].character, charIndex);
                }
                #endregion


                #region Example of Word Selection
                // Check if Mouse intersects any words and if so assign a random color to that word.
                // Store the result of TMP_TextUtilities.FindIntersectingWord using m_TextComponent,
                // Input.mousePosition, m_Camera in local wordIndex.
                int wordIndex = TMP_TextUtilities.FindIntersectingWord(m_TextComponent, Input.mousePosition, m_Camera);
                // If wordIndex differs from negative 1 and wordIndex differs from m_lastWordIndex, run the
                // following block.
                if (wordIndex != -1 && wordIndex != m_lastWordIndex)
                {
                    // Copy wordIndex into m_lastWordIndex.
                    m_lastWordIndex = wordIndex;

                    // Get the information about the selected word.
                    // Store m_TextComponent.textInfo.wordInfo[wordIndex] (entry read from
                    // m_TextComponent.textInfo.wordInfo) in local wInfo.
                    TMP_WordInfo wInfo = m_TextComponent.textInfo.wordInfo[wordIndex];

                    // Send the event to any listeners.
                    // Call TMP_TextEventHandler.SendOnWordSelection with the result of wInfo.GetWord,
                    // wInfo.firstCharacterIndex, wInfo.characterCount; invoke registered listeners with the
                    // selected word information.
                    SendOnWordSelection(wInfo.GetWord(), wInfo.firstCharacterIndex, wInfo.characterCount);
                }
                #endregion


                #region Example of Line Selection
                // Check if Mouse intersects any words and if so assign a random color to that word.
                // Store the result of TMP_TextUtilities.FindIntersectingLine using m_TextComponent,
                // Input.mousePosition, m_Camera in local lineIndex.
                int lineIndex = TMP_TextUtilities.FindIntersectingLine(m_TextComponent, Input.mousePosition, m_Camera);
                // If lineIndex differs from negative 1 and lineIndex differs from m_lastLineIndex, run the
                // following block.
                if (lineIndex != -1 && lineIndex != m_lastLineIndex)
                {
                    // Copy lineIndex into m_lastLineIndex.
                    m_lastLineIndex = lineIndex;

                    // Get the information about the selected word.
                    // Store m_TextComponent.textInfo.lineInfo[lineIndex] (entry read from
                    // m_TextComponent.textInfo.lineInfo) in local lineInfo.
                    TMP_LineInfo lineInfo = m_TextComponent.textInfo.lineInfo[lineIndex];

                    // Send the event to any listeners.
                    // Store a new array/object initialized with the entries specified here in local buffer.
                    char[] buffer = new char[lineInfo.characterCount];
                    // Repeat this block while i is below lineInfo.characterCount and i is below length of
                    // m_TextComponent.textInfo.characterInfo; start from int i = 0.
                    for (int i = 0; i < lineInfo.characterCount && i < m_TextComponent.textInfo.characterInfo.Length; i++)
                    {
                        // Set buffer[i] to m_TextComponent.textInfo.characterInfo[i +
                        // lineInfo.firstCharacterIndex].character.
                        buffer[i] = m_TextComponent.textInfo.characterInfo[i + lineInfo.firstCharacterIndex].character;
                    }

                    // Store a new string initialized with buffer in local lineText.
                    string lineText = new string(buffer);
                    // Call TMP_TextEventHandler.SendOnLineSelection with lineText, lineInfo.firstCharacterIndex,
                    // lineInfo.characterCount; invoke registered listeners with the selected line information.
                    SendOnLineSelection(lineText, lineInfo.firstCharacterIndex, lineInfo.characterCount);
                }
                #endregion


                #region Example of Link Handling
                // Check if mouse intersects with any links.
                // Store the result of TMP_TextUtilities.FindIntersectingLink using m_TextComponent,
                // Input.mousePosition, m_Camera in local linkIndex.
                int linkIndex = TMP_TextUtilities.FindIntersectingLink(m_TextComponent, Input.mousePosition, m_Camera);

                // Handle new Link selection.
                // If linkIndex differs from negative 1 and linkIndex differs from m_selectedLink, run the
                // following block.
                if (linkIndex != -1 && linkIndex != m_selectedLink)
                {
                    // Copy linkIndex into m_selectedLink.
                    m_selectedLink = linkIndex;

                    // Get information about the link.
                    // Store m_TextComponent.textInfo.linkInfo[linkIndex] (entry read from
                    // m_TextComponent.textInfo.linkInfo) in local linkInfo.
                    TMP_LinkInfo linkInfo = m_TextComponent.textInfo.linkInfo[linkIndex];

                    // Send the event to any listeners.
                    // Call TMP_TextEventHandler.SendOnLinkSelection with the result of linkInfo.GetLinkID, the
                    // result of linkInfo.GetLinkText, linkIndex; invoke registered listeners with the selected
                    // link information.
                    SendOnLinkSelection(linkInfo.GetLinkID(), linkInfo.GetLinkText(), linkIndex);
                }
                #endregion
            }
            else
            {
                // Reset all selections given we are hovering outside the text container bounds.
                // Set m_selectedLink to negative 1.
                m_selectedLink = -1;
                // Set m_lastCharIndex to negative 1.
                m_lastCharIndex = -1;
                // Set m_lastWordIndex to negative 1.
                m_lastWordIndex = -1;
                // Set m_lastLineIndex to negative 1.
                m_lastLineIndex = -1;
            }
        }


        // Record that the pointer has entered this text object.
        // Unity EventSystem supplies eventData with the pointer position, button and selected UI event
        // information.
        public void OnPointerEnter(PointerEventData eventData)
        {
            //Debug.Log("OnPointerEnter()");
        }


        // Record that the pointer has left this text object.
        // Unity EventSystem supplies eventData with the pointer position, button and selected UI event
        // information.
        public void OnPointerExit(PointerEventData eventData)
        {
            //Debug.Log("OnPointerExit()");
        }


        // Invoke registered listeners with the selected character information.
        // character is a char argument supplied by TMP_TextEventHandler.LateUpdate.
        // characterIndex is a int argument supplied by TMP_TextEventHandler.LateUpdate.
        private void SendOnCharacterSelection(char character, int characterIndex)
        {
            // If onCharacterSelection exists, run the following block.
            if (onCharacterSelection != null)
                // Notify onCharacterSelection listeners with character, characterIndex.
                onCharacterSelection.Invoke(character, characterIndex);
        }

        // Invoke registered listeners with the selected sprite information.
        // character is a char argument supplied by TMP_TextEventHandler.LateUpdate.
        // characterIndex is a int argument supplied by TMP_TextEventHandler.LateUpdate.
        private void SendOnSpriteSelection(char character, int characterIndex)
        {
            // If onSpriteSelection exists, run the following block.
            if (onSpriteSelection != null)
                // Notify onSpriteSelection listeners with character, characterIndex.
                onSpriteSelection.Invoke(character, characterIndex);
        }

        // Invoke registered listeners with the selected word information.
        // word is a string argument supplied by TMP_TextEventHandler.LateUpdate.
        // charIndex is a int argument supplied by TMP_TextEventHandler.LateUpdate.
        // length is a int argument supplied by TMP_TextEventHandler.LateUpdate.
        private void SendOnWordSelection(string word, int charIndex, int length)
        {
            // If onWordSelection exists, run the following block.
            if (onWordSelection != null)
                // Notify onWordSelection listeners with word, charIndex, length.
                onWordSelection.Invoke(word, charIndex, length);
        }

        // Invoke registered listeners with the selected line information.
        // line is a string argument supplied by TMP_TextEventHandler.LateUpdate.
        // charIndex is a int argument supplied by TMP_TextEventHandler.LateUpdate.
        // length is a int argument supplied by TMP_TextEventHandler.LateUpdate.
        private void SendOnLineSelection(string line, int charIndex, int length)
        {
            // If onLineSelection exists, run the following block.
            if (onLineSelection != null)
                // Notify onLineSelection listeners with line, charIndex, length.
                onLineSelection.Invoke(line, charIndex, length);
        }

        // Invoke registered listeners with the selected link information.
        // linkID is a string argument supplied by TMP_TextEventHandler.LateUpdate.
        // linkText is a string argument supplied by TMP_TextEventHandler.LateUpdate.
        // linkIndex is a int argument supplied by TMP_TextEventHandler.LateUpdate.
        private void SendOnLinkSelection(string linkID, string linkText, int linkIndex)
        {
            // If onLinkSelection exists, run the following block.
            if (onLinkSelection != null)
                // Notify onLinkSelection listeners with linkID, linkText, linkIndex.
                onLinkSelection.Invoke(linkID, linkText, linkIndex);
        }

    }
}
