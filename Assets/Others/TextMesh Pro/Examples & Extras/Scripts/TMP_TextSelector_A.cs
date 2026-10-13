// Purpose: Demonstrates selecting and coloring parts of world-space text with the pointer.
// Use: Attach to sample TextMeshPro text with pointer event support.
// How it works: Intersection checks choose characters, words or links and change their vertex colors.
// Context: bundled TextMesh Pro example; these notes explain its sample behavior.

// UnityEngine supplies GameObjects, components, transforms, time, math and physics used here.
using UnityEngine;
// Unity EventSystem supplies pointer callbacks and event data for UI interactions.
using UnityEngine.EventSystems;
// System.Collections supplies IEnumerator so coroutines can pause and resume across frames.
using System.Collections;


namespace TMPro.Examples
{

    public class TMP_TextSelector_A : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        // m_TextMeshPro: text mesh pro. Awake retrieves it from the first TextMeshPro component on the GameObject
        // that owns this component.
        private TextMeshPro m_TextMeshPro;

        // m_Camera: camera. Awake retrieves it from Unity first enabled camera tagged MainCamera.
        private Camera m_Camera;

        // m_isHoveringObject: is hovering object. Updated by LateUpdate, OnPointerEnter, OnPointerExit.
        private bool m_isHoveringObject;
        // m_selectedLink: selected link. LateUpdate sets it from linkIndex.
        private int m_selectedLink = -1;
        // m_lastCharIndex: last char index. LateUpdate sets it from charIndex.
        private int m_lastCharIndex = -1;
        // m_lastWordIndex: last word index. LateUpdate sets it from wordIndex.
        private int m_lastWordIndex = -1;

        // Before the example starts, cache its components and prepare the starting display.
        void Awake()
        {
            // Store the first TextMeshPro component on the GameObject that owns this component in m_TextMeshPro.
            m_TextMeshPro = gameObject.GetComponent<TextMeshPro>();
            // Set m_Camera to Unity first enabled camera tagged MainCamera.
            m_Camera = Camera.main;

            // Force generation of the text object so we have valid data to work with. This is needed since LateUpdate() will be called before the text object has a chance to generated when entering play mode.
            // Regenerate m_TextMeshPro text mesh now so character and vertex information is available.
            m_TextMeshPro.ForceMeshUpdate();
        }


        // Check hovered text elements, update their highlight and restore earlier selections as needed.
        void LateUpdate()
        {
            // Set m_isHoveringObject to false.
            m_isHoveringObject = false;

            // If the result of TMP_TextUtilities.IsIntersectingRectTransform using m_TextMeshPro.rectTransform,
            // Input.mousePosition, Unity first enabled camera tagged MainCamera, run the following block.
            if (TMP_TextUtilities.IsIntersectingRectTransform(m_TextMeshPro.rectTransform, Input.mousePosition, Camera.main))
            {
                // Set m_isHoveringObject to true.
                m_isHoveringObject = true;
            }

            // If m_isHoveringObject, run the following block.
            if (m_isHoveringObject)
            {
                #region Example of Character Selection
                // Store the result of TMP_TextUtilities.FindIntersectingCharacter using m_TextMeshPro,
                // Input.mousePosition, Unity first enabled camera tagged MainCamera, true in local charIndex.
                int charIndex = TMP_TextUtilities.FindIntersectingCharacter(m_TextMeshPro, Input.mousePosition, Camera.main, true);
                // If charIndex differs from -1 and charIndex differs from m_lastCharIndex and
                // (Input.GetKey(KeyCode.LeftShift) or Input.GetKey(KeyCode.RightShift)), run the following block.
                if (charIndex != -1 && charIndex != m_lastCharIndex && (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift)))
                {
                    //Debug.Log("[" + m_TextMeshPro.textInfo.characterInfo[charIndex].character + "] has been selected.");

                    // Copy charIndex into m_lastCharIndex.
                    m_lastCharIndex = charIndex;

                    // Store m_TextMeshPro.textInfo.characterInfo[charIndex].materialReferenceIndex in local
                    // meshIndex.
                    int meshIndex = m_TextMeshPro.textInfo.characterInfo[charIndex].materialReferenceIndex;

                    // Store m_TextMeshPro.textInfo.characterInfo[charIndex].vertexIndex in local vertexIndex.
                    int vertexIndex = m_TextMeshPro.textInfo.characterInfo[charIndex].vertexIndex;

                    // Store a Color32 built from Unity random value between 0 and 255 (integer upper bound
                    // excluded) viewed as byte, Unity random value between 0 and 255 (integer upper bound
                    // excluded) viewed as byte, Unity random value between 0 and 255 (integer upper bound
                    // excluded) viewed as byte, 255 in local c.
                    Color32 c = new Color32((byte)Random.Range(0, 255), (byte)Random.Range(0, 255), (byte)Random.Range(0, 255), 255);

                    // Store m_TextMeshPro.textInfo.meshInfo[meshIndex] colors32 array from its mesh data in local
                    // vertexColors.
                    Color32[] vertexColors = m_TextMeshPro.textInfo.meshInfo[meshIndex].colors32;

                    // Copy c into vertexColors[vertexIndex + 0].
                    vertexColors[vertexIndex + 0] = c;
                    // Copy c into vertexColors[vertexIndex + 1].
                    vertexColors[vertexIndex + 1] = c;
                    // Copy c into vertexColors[vertexIndex + 2].
                    vertexColors[vertexIndex + 2] = c;
                    // Copy c into vertexColors[vertexIndex + 3].
                    vertexColors[vertexIndex + 3] = c;

                    //m_TextMeshPro.mesh.colors32 = vertexColors;
                    // Copy vertexColors into m_TextMeshPro.textInfo.meshInfo[meshIndex].mesh.colors32.
                    m_TextMeshPro.textInfo.meshInfo[meshIndex].mesh.colors32 = vertexColors;
                }
                #endregion

                #region Example of Link Handling
                // Check if mouse intersects with any links.
                // Store the result of TMP_TextUtilities.FindIntersectingLink using m_TextMeshPro,
                // Input.mousePosition, m_Camera in local linkIndex.
                int linkIndex = TMP_TextUtilities.FindIntersectingLink(m_TextMeshPro, Input.mousePosition, m_Camera);

                // Clear previous link selection if one existed.
                // If (linkIndex == -1 and m_selectedLink != -1) or linkIndex differs from m_selectedLink, run the
                // following block.
                if ((linkIndex == -1 && m_selectedLink != -1) || linkIndex != m_selectedLink)
                {
                    //m_TextPopup_RectTransform.gameObject.SetActive(false);
                    // Set m_selectedLink to negative 1.
                    m_selectedLink = -1;
                }

                // Handle new Link selection.
                // If linkIndex differs from negative 1 and linkIndex differs from m_selectedLink, run the
                // following block.
                if (linkIndex != -1 && linkIndex != m_selectedLink)
                {
                    // Copy linkIndex into m_selectedLink.
                    m_selectedLink = linkIndex;

                    // Store m_TextMeshPro.textInfo.linkInfo[linkIndex] (entry read from
                    // m_TextMeshPro.textInfo.linkInfo) in local linkInfo.
                    TMP_LinkInfo linkInfo = m_TextMeshPro.textInfo.linkInfo[linkIndex];

                    // The following provides an example of how to access the link properties.
                    //Debug.Log("Link ID: \"" + linkInfo.GetLinkID() + "\"   Link Text: \"" + linkInfo.GetLinkText() + "\""); // Example of how to retrieve the Link ID and Link Text.

                    // Declare local worldPointInRectangle to hold Vector3 data supplied later in this method.
                    Vector3 worldPointInRectangle;

                    // Call RectTransformUtility.ScreenPointToWorldPointInRectangle using
                    // m_TextMeshPro.rectTransform, Input.mousePosition, m_Camera, worldPointInRectangle.
                    RectTransformUtility.ScreenPointToWorldPointInRectangle(m_TextMeshPro.rectTransform, Input.mousePosition, m_Camera, out worldPointInRectangle);

                    // Choose the matching branch using the result of linkInfo.GetLinkID.
                    switch (linkInfo.GetLinkID())
                    {
                        case "id_01": // 100041637: // id_01
                                      //m_TextPopup_RectTransform.position = worldPointInRectangle;
                                      //m_TextPopup_RectTransform.gameObject.SetActive(true);
                                      //m_TextPopup_TMPComponent.text = k_LinkText + " ID 01";
                            // Leave this loop or switch case so no later statements in that block run.
                            break;
                        case "id_02": // 100041638: // id_02
                                      //m_TextPopup_RectTransform.position = worldPointInRectangle;
                                      //m_TextPopup_RectTransform.gameObject.SetActive(true);
                                      //m_TextPopup_TMPComponent.text = k_LinkText + " ID 02";
                            // Leave this loop or switch case so no later statements in that block run.
                            break;
                    }
                }
                #endregion


                #region Example of Word Selection
                // Check if Mouse intersects any words and if so assign a random color to that word.
                // Store the result of TMP_TextUtilities.FindIntersectingWord using m_TextMeshPro,
                // Input.mousePosition, Unity first enabled camera tagged MainCamera in local wordIndex.
                int wordIndex = TMP_TextUtilities.FindIntersectingWord(m_TextMeshPro, Input.mousePosition, Camera.main);
                // If wordIndex differs from negative 1 and wordIndex differs from m_lastWordIndex, run the
                // following block.
                if (wordIndex != -1 && wordIndex != m_lastWordIndex)
                {
                    // Copy wordIndex into m_lastWordIndex.
                    m_lastWordIndex = wordIndex;

                    // Store m_TextMeshPro.textInfo.wordInfo[wordIndex] (entry read from
                    // m_TextMeshPro.textInfo.wordInfo) in local wInfo.
                    TMP_WordInfo wInfo = m_TextMeshPro.textInfo.wordInfo[wordIndex];

                    // Store m_TextMeshPro.textInfo.characterInfo[wInfo.firstCharacterIndex].bottomLeft converted
                    // from m_TextMeshPro.transform local coordinates into world coordinates in local wordPOS.
                    Vector3 wordPOS = m_TextMeshPro.transform.TransformPoint(m_TextMeshPro.textInfo.characterInfo[wInfo.firstCharacterIndex].bottomLeft);
                    // Store the result of Camera.main.WorldToScreenPoint using wordPOS in wordPOS.
                    wordPOS = Camera.main.WorldToScreenPoint(wordPOS);

                    //Debug.Log("Mouse Position: " + Input.mousePosition.ToString("f3") + "  Word Position: " + wordPOS.ToString("f3"));

                    // Store m_TextMeshPro.textInfo.meshInfo[0] colors32 array from its mesh data in local
                    // vertexColors.
                    Color32[] vertexColors = m_TextMeshPro.textInfo.meshInfo[0].colors32;

                    // Store a Color32 built from Unity random value between 0 and 255 (integer upper bound
                    // excluded) viewed as byte, Unity random value between 0 and 255 (integer upper bound
                    // excluded) viewed as byte, Unity random value between 0 and 255 (integer upper bound
                    // excluded) viewed as byte, 255 in local c.
                    Color32 c = new Color32((byte)Random.Range(0, 255), (byte)Random.Range(0, 255), (byte)Random.Range(0, 255), 255);
                    // Repeat this block while i is below wInfo.characterCount; start from int i = 0.
                    for (int i = 0; i < wInfo.characterCount; i++)
                    {
                        // Store m_TextMeshPro.textInfo.characterInfo[wInfo.firstCharacterIndex + i].vertexIndex in
                        // local vertexIndex.
                        int vertexIndex = m_TextMeshPro.textInfo.characterInfo[wInfo.firstCharacterIndex + i].vertexIndex;

                        // Copy c into vertexColors[vertexIndex + 0].
                        vertexColors[vertexIndex + 0] = c;
                        // Copy c into vertexColors[vertexIndex + 1].
                        vertexColors[vertexIndex + 1] = c;
                        // Copy c into vertexColors[vertexIndex + 2].
                        vertexColors[vertexIndex + 2] = c;
                        // Copy c into vertexColors[vertexIndex + 3].
                        vertexColors[vertexIndex + 3] = c;
                    }

                    // Copy vertexColors into m_TextMeshPro.mesh.colors32.
                    m_TextMeshPro.mesh.colors32 = vertexColors;
                }
                #endregion
            }
        }


        // Record that the pointer has entered this text object.
        // Unity EventSystem supplies eventData with the pointer position, button and selected UI event
        // information.
        public void OnPointerEnter(PointerEventData eventData)
        {
            // Write the diagnostic message to the Unity Console; it does not update game UI.
            Debug.Log("OnPointerEnter()");
            // Set m_isHoveringObject to true.
            m_isHoveringObject = true;
        }


        // Record that the pointer has left this text object.
        // Unity EventSystem supplies eventData with the pointer position, button and selected UI event
        // information.
        public void OnPointerExit(PointerEventData eventData)
        {
            // Write the diagnostic message to the Unity Console; it does not update game UI.
            Debug.Log("OnPointerExit()");
            // Set m_isHoveringObject to false.
            m_isHoveringObject = false;
        }

    }
}
