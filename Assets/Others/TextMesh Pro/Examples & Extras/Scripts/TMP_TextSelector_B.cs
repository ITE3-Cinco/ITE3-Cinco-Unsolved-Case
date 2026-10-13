// Purpose: Demonstrates highlighting canvas text and showing selection popups.
// Use: Attach to TextMeshProUGUI and assign the sample popup prefab.
// How it works: Cache original mesh data, test pointer intersections and restore highlights when selection changes.
// Context: bundled TextMesh Pro example; these notes explain its sample behavior.

// UnityEngine supplies GameObjects, components, transforms, time, math and physics used here.
using UnityEngine;
// Unity Events supplies Inspector-connectable events and listener callbacks.
using UnityEngine.Events;
// Unity EventSystem supplies pointer callbacks and event data for UI interactions.
using UnityEngine.EventSystems;
// System.Collections supplies IEnumerator so coroutines can pause and resume across frames.
using System.Collections;
// System.Collections.Generic supplies typed lists, sets and dictionaries for stored data.
using System.Collections.Generic;


#pragma warning disable 0618 // Disabled warning due to SetVertices being deprecated until new release with SetMesh() is available.

namespace TMPro.Examples
{

    public class TMP_TextSelector_B : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler, IPointerUpHandler
    {
        // TextPopup_Prefab_01: text popup prefab 01. Assigned in the scene/prefab Inspector.
        public RectTransform TextPopup_Prefab_01;

        // m_TextPopup_RectTransform: text popup rect transform. Updated by Awake.
        private RectTransform m_TextPopup_RectTransform;
        // m_TextPopup_TMPComponent: text popup tmpcomponent. Awake retrieves it from the first TextMeshProUGUI
        // component on or beneath m_TextPopup_RectTransform.
        private TextMeshProUGUI m_TextPopup_TMPComponent;
        // k_LinkText: k link text. Fixed script constant; callers cannot change it.
        private const string k_LinkText = "You have selected link <#ffff00>";
        // k_WordText: k word text. Fixed script constant; callers cannot change it.
        private const string k_WordText = "Word Index: <#ffff00>";


        // m_TextMeshPro: text mesh pro. Awake retrieves it from the first TextMeshProUGUI component on the
        // GameObject that owns this component.
        private TextMeshProUGUI m_TextMeshPro;
        // m_Canvas: canvas. Awake retrieves it from the first Canvas component on or above the GameObject that
        // owns this component.
        private Canvas m_Canvas;
        // m_Camera: camera. Awake sets it from m_Canvas.worldCamera.
        private Camera m_Camera;

        // Flags
        // isHoveringObject: is hovering object. Updated by OnPointerEnter, OnPointerExit.
        private bool isHoveringObject;
        // m_selectedWord: selected word. LateUpdate sets it from wordIndex.
        private int m_selectedWord = -1;
        // m_selectedLink: selected link. LateUpdate sets it from linkIndex.
        private int m_selectedLink = -1;
        // m_lastIndex: last index. LateUpdate sets it from charIndex.
        private int m_lastIndex = -1;

        // m_matrix: matrix. Updated by LateUpdate.
        private Matrix4x4 m_matrix;

        // m_cachedMeshInfoVertexData: cached mesh info vertex data. Updated by ON_TEXT_CHANGED.
        private TMP_MeshInfo[] m_cachedMeshInfoVertexData;

        // Before the example starts, cache its components and prepare the starting display.
        void Awake()
        {
            // Store the first TextMeshProUGUI component on the GameObject that owns this component in
            // m_TextMeshPro.
            m_TextMeshPro = gameObject.GetComponent<TextMeshProUGUI>();


            // Store the first Canvas component on or above the GameObject that owns this component in m_Canvas.
            m_Canvas = gameObject.GetComponentInParent<Canvas>();

            // Get a reference to the camera if Canvas Render Mode is not ScreenSpace Overlay.
            // If m_Canvas.renderMode equals RenderMode.ScreenSpaceOverlay, run the following block.
            if (m_Canvas.renderMode == RenderMode.ScreenSpaceOverlay)
                // Clear m_Camera to null so it no longer holds a reference.
                m_Camera = null;
            else
                // Set m_Camera to m_Canvas.worldCamera.
                m_Camera = m_Canvas.worldCamera;

            // Create pop-up text object which is used to show the link information.
            // Set m_TextPopup_RectTransform to Instantiate(TextPopup_Prefab_01) as RectTransform.
            m_TextPopup_RectTransform = Instantiate(TextPopup_Prefab_01) as RectTransform;
            // Parent m_TextPopup_RectTransform beneath Transform of m_Canvas; keep its local coordinates.
            m_TextPopup_RectTransform.SetParent(m_Canvas.transform, false);
            // Store the first TextMeshProUGUI component on or beneath m_TextPopup_RectTransform in
            // m_TextPopup_TMPComponent.
            m_TextPopup_TMPComponent = m_TextPopup_RectTransform.GetComponentInChildren<TextMeshProUGUI>();
            // Deactivate m_TextPopup_RectTransform.gameObject.
            m_TextPopup_RectTransform.gameObject.SetActive(false);
        }


        // While enabled, register the callbacks used by this example.
        void OnEnable()
        {
            // Subscribe to event fired when text object has been regenerated.
            // Subscribe ON_TEXT_CHANGED to TMP text regeneration events.
            TMPro_EventManager.TEXT_CHANGED_EVENT.Add(ON_TEXT_CHANGED);
        }

        // Remove the registered callbacks when the example is disabled.
        void OnDisable()
        {
            // UnSubscribe to event fired when text object has been regenerated.
            // Unsubscribe ON_TEXT_CHANGED from TMP text regeneration events.
            TMPro_EventManager.TEXT_CHANGED_EVENT.Remove(ON_TEXT_CHANGED);
        }


        // Mark text geometry for refreshing when a text-change event is received.
        // obj is a Object argument supplied by the code, event or Inspector callback invoking ON_TEXT_CHANGED.
        void ON_TEXT_CHANGED(Object obj)
        {
            // If obj equals m_TextMeshPro, run the following block.
            if (obj == m_TextMeshPro)
            {
                // Update cached vertex data.
                // Store a copy of m_TextMeshPro.textInfo generated mesh arrays, used as the original effect
                // baseline in m_cachedMeshInfoVertexData.
                m_cachedMeshInfoVertexData = m_TextMeshPro.textInfo.CopyMeshInfoVertexData();
            }
        }


        // Check hovered text elements, update their highlight and restore earlier selections as needed.
        void LateUpdate()
        {
            // If isHoveringObject, run the following block.
            if (isHoveringObject)
            {
                // Check if Mouse Intersects any of the characters. If so, assign a random color.
                #region Handle Character Selection
                // Store the result of TMP_TextUtilities.FindIntersectingCharacter using m_TextMeshPro,
                // Input.mousePosition, m_Camera, true in local charIndex.
                int charIndex = TMP_TextUtilities.FindIntersectingCharacter(m_TextMeshPro, Input.mousePosition, m_Camera, true);

                // Undo Swap and Vertex Attribute changes.
                // If charIndex equals negative 1 or charIndex differs from m_lastIndex, run the following block.
                if (charIndex == -1 || charIndex != m_lastIndex)
                {
                    // Call TMP_TextSelector_B.RestoreCachedVertexAttributes with m_lastIndex; restore the original
                    // character mesh data when removing a highlight.
                    RestoreCachedVertexAttributes(m_lastIndex);
                    // Set m_lastIndex to negative 1.
                    m_lastIndex = -1;
                }

                // If charIndex differs from -1 and charIndex differs from m_lastIndex and
                // (Input.GetKey(KeyCode.LeftShift) or Input.GetKey(KeyCode.RightShift)), run the following block.
                if (charIndex != -1 && charIndex != m_lastIndex && (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift)))
                {
                    // Copy charIndex into m_lastIndex.
                    m_lastIndex = charIndex;

                    // Get the index of the material / sub text object used by this character.
                    // Store m_TextMeshPro.textInfo.characterInfo[charIndex].materialReferenceIndex in local
                    // materialIndex.
                    int materialIndex = m_TextMeshPro.textInfo.characterInfo[charIndex].materialReferenceIndex;

                    // Get the index of the first vertex of the selected character.
                    // Store m_TextMeshPro.textInfo.characterInfo[charIndex].vertexIndex in local vertexIndex.
                    int vertexIndex = m_TextMeshPro.textInfo.characterInfo[charIndex].vertexIndex;

                    // Get a reference to the vertices array.
                    // Store m_TextMeshPro.textInfo.meshInfo[materialIndex] vertices array from its mesh data in
                    // local vertices.
                    Vector3[] vertices = m_TextMeshPro.textInfo.meshInfo[materialIndex].vertices;

                    // Determine the center point of the character.
                    // Store (vertices[vertexIndex + 0] + vertices[vertexIndex + 2]) / 2 (calculated from the
                    // values shown) in local charMidBasline.
                    Vector2 charMidBasline = (vertices[vertexIndex + 0] + vertices[vertexIndex + 2]) / 2;

                    // Need to translate all 4 vertices of the character to aligned with middle of character / baseline.
                    // This is needed so the matrix TRS is applied at the origin for each character.
                    // Keep charMidBasline in local offset.
                    Vector3 offset = charMidBasline;

                    // Translate the character to the middle baseline.
                    // Set vertices[vertexIndex + 0] to vertices[vertexIndex + 0] (entry read from vertices) minus
                    // offset.
                    vertices[vertexIndex + 0] = vertices[vertexIndex + 0] - offset;
                    // Set vertices[vertexIndex + 1] to vertices[vertexIndex + 1] (entry read from vertices) minus
                    // offset.
                    vertices[vertexIndex + 1] = vertices[vertexIndex + 1] - offset;
                    // Set vertices[vertexIndex + 2] to vertices[vertexIndex + 2] (entry read from vertices) minus
                    // offset.
                    vertices[vertexIndex + 2] = vertices[vertexIndex + 2] - offset;
                    // Set vertices[vertexIndex + 3] to vertices[vertexIndex + 3] (entry read from vertices) minus
                    // offset.
                    vertices[vertexIndex + 3] = vertices[vertexIndex + 3] - offset;

                    // Initialize zoomFactor to 1.5.
                    float zoomFactor = 1.5f;

                    // Setup the Matrix for the scale change.
                    // Store a transform matrix from position the zero vector (no movement or offset), rotation the
                    // identity rotation (no rotation) and scale Vector3.one * zoomFactor in m_matrix.
                    m_matrix = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, Vector3.one * zoomFactor);

                    // Apply Matrix operation on the given character.
                    // Store the result of m_matrix.MultiplyPoint3x4 using vertices[vertexIndex + 0] (entry read
                    // from vertices) in vertices[vertexIndex + 0].
                    vertices[vertexIndex + 0] = m_matrix.MultiplyPoint3x4(vertices[vertexIndex + 0]);
                    // Store the result of m_matrix.MultiplyPoint3x4 using vertices[vertexIndex + 1] (entry read
                    // from vertices) in vertices[vertexIndex + 1].
                    vertices[vertexIndex + 1] = m_matrix.MultiplyPoint3x4(vertices[vertexIndex + 1]);
                    // Store the result of m_matrix.MultiplyPoint3x4 using vertices[vertexIndex + 2] (entry read
                    // from vertices) in vertices[vertexIndex + 2].
                    vertices[vertexIndex + 2] = m_matrix.MultiplyPoint3x4(vertices[vertexIndex + 2]);
                    // Store the result of m_matrix.MultiplyPoint3x4 using vertices[vertexIndex + 3] (entry read
                    // from vertices) in vertices[vertexIndex + 3].
                    vertices[vertexIndex + 3] = m_matrix.MultiplyPoint3x4(vertices[vertexIndex + 3]);

                    // Translate the character back to its original position.
                    // Set vertices[vertexIndex + 0] to vertices[vertexIndex + 0] (entry read from vertices) plus
                    // offset.
                    vertices[vertexIndex + 0] = vertices[vertexIndex + 0] + offset;
                    // Set vertices[vertexIndex + 1] to vertices[vertexIndex + 1] (entry read from vertices) plus
                    // offset.
                    vertices[vertexIndex + 1] = vertices[vertexIndex + 1] + offset;
                    // Set vertices[vertexIndex + 2] to vertices[vertexIndex + 2] (entry read from vertices) plus
                    // offset.
                    vertices[vertexIndex + 2] = vertices[vertexIndex + 2] + offset;
                    // Set vertices[vertexIndex + 3] to vertices[vertexIndex + 3] (entry read from vertices) plus
                    // offset.
                    vertices[vertexIndex + 3] = vertices[vertexIndex + 3] + offset;

                    // Change Vertex Colors of the highlighted character
                    // Store a Color32 built from 255, 255, 192, 255 in local c.
                    Color32 c = new Color32(255, 255, 192, 255);

                    // Get a reference to the vertex color
                    // Store m_TextMeshPro.textInfo.meshInfo[materialIndex] colors32 array from its mesh data in
                    // local vertexColors.
                    Color32[] vertexColors = m_TextMeshPro.textInfo.meshInfo[materialIndex].colors32;

                    // Copy c into vertexColors[vertexIndex + 0].
                    vertexColors[vertexIndex + 0] = c;
                    // Copy c into vertexColors[vertexIndex + 1].
                    vertexColors[vertexIndex + 1] = c;
                    // Copy c into vertexColors[vertexIndex + 2].
                    vertexColors[vertexIndex + 2] = c;
                    // Copy c into vertexColors[vertexIndex + 3].
                    vertexColors[vertexIndex + 3] = c;


                    // Get a reference to the meshInfo of the selected character.
                    // Store m_TextMeshPro.textInfo.meshInfo[materialIndex] (entry read from
                    // m_TextMeshPro.textInfo.meshInfo) in local meshInfo.
                    TMP_MeshInfo meshInfo = m_TextMeshPro.textInfo.meshInfo[materialIndex];

                    // Get the index of the last character's vertex attributes.
                    // Store length of vertices minus 4 in local lastVertexIndex.
                    int lastVertexIndex = vertices.Length - 4;

                    // Swap the current character's vertex attributes with those of the last element in the vertex attribute arrays.
                    // We do this to make sure this character is rendered last and over other characters.
                    // Call meshInfo.SwapVertexData using vertexIndex, lastVertexIndex.
                    meshInfo.SwapVertexData(vertexIndex, lastVertexIndex);

                    // Need to update the appropriate
                    // Upload changed TMP_VertexDataUpdateFlags.All from the TMP mesh arrays to m_TextMeshPro.
                    m_TextMeshPro.UpdateVertexData(TMP_VertexDataUpdateFlags.All);
                }
                #endregion


                #region Word Selection Handling
                //Check if Mouse intersects any words and if so assign a random color to that word.
                // Store the result of TMP_TextUtilities.FindIntersectingWord using m_TextMeshPro,
                // Input.mousePosition, m_Camera in local wordIndex.
                int wordIndex = TMP_TextUtilities.FindIntersectingWord(m_TextMeshPro, Input.mousePosition, m_Camera);

                // Clear previous word selection.
                // If m_TextPopup_RectTransform exists and m_selectedWord differs from -1 and (wordIndex == -1 or
                // wordIndex != m_selectedWord), run the following block.
                if (m_TextPopup_RectTransform != null && m_selectedWord != -1 && (wordIndex == -1 || wordIndex != m_selectedWord))
                {
                    // Store m_TextMeshPro.textInfo.wordInfo[m_selectedWord] (entry read from
                    // m_TextMeshPro.textInfo.wordInfo) in local wInfo.
                    TMP_WordInfo wInfo = m_TextMeshPro.textInfo.wordInfo[m_selectedWord];

                    // Iterate through each of the characters of the word.
                    // Repeat this block while i is below wInfo.characterCount; start from int i = 0.
                    for (int i = 0; i < wInfo.characterCount; i++)
                    {
                        // Store wInfo.firstCharacterIndex plus i in local characterIndex.
                        int characterIndex = wInfo.firstCharacterIndex + i;

                        // Get the index of the material / sub text object used by this character.
                        // Store m_TextMeshPro.textInfo.characterInfo[characterIndex].materialReferenceIndex in
                        // local meshIndex.
                        int meshIndex = m_TextMeshPro.textInfo.characterInfo[characterIndex].materialReferenceIndex;

                        // Get the index of the first vertex of this character.
                        // Store m_TextMeshPro.textInfo.characterInfo[characterIndex].vertexIndex in local
                        // vertexIndex.
                        int vertexIndex = m_TextMeshPro.textInfo.characterInfo[characterIndex].vertexIndex;

                        // Get a reference to the vertex color
                        // Store m_TextMeshPro.textInfo.meshInfo[meshIndex] colors32 array from its mesh data in
                        // local vertexColors.
                        Color32[] vertexColors = m_TextMeshPro.textInfo.meshInfo[meshIndex].colors32;

                        // Store the result of vertexColors[vertexIndex + 0].Tint using 1.33333 in local c.
                        Color32 c = vertexColors[vertexIndex + 0].Tint(1.33333f);

                        // Copy c into vertexColors[vertexIndex + 0].
                        vertexColors[vertexIndex + 0] = c;
                        // Copy c into vertexColors[vertexIndex + 1].
                        vertexColors[vertexIndex + 1] = c;
                        // Copy c into vertexColors[vertexIndex + 2].
                        vertexColors[vertexIndex + 2] = c;
                        // Copy c into vertexColors[vertexIndex + 3].
                        vertexColors[vertexIndex + 3] = c;
                    }

                    // Update Geometry
                    // Upload changed TMP_VertexDataUpdateFlags.All from the TMP mesh arrays to m_TextMeshPro.
                    m_TextMeshPro.UpdateVertexData(TMP_VertexDataUpdateFlags.All);

                    // Set m_selectedWord to negative 1.
                    m_selectedWord = -1;
                }


                // Word Selection Handling
                // If wordIndex differs from -1 and wordIndex differs from m_selectedWord and not
                // ((Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift))), run the following
                // block.
                if (wordIndex != -1 && wordIndex != m_selectedWord && !(Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift)))
                {
                    // Copy wordIndex into m_selectedWord.
                    m_selectedWord = wordIndex;

                    // Store m_TextMeshPro.textInfo.wordInfo[wordIndex] (entry read from
                    // m_TextMeshPro.textInfo.wordInfo) in local wInfo.
                    TMP_WordInfo wInfo = m_TextMeshPro.textInfo.wordInfo[wordIndex];

                    // Iterate through each of the characters of the word.
                    // Repeat this block while i is below wInfo.characterCount; start from int i = 0.
                    for (int i = 0; i < wInfo.characterCount; i++)
                    {
                        // Store wInfo.firstCharacterIndex plus i in local characterIndex.
                        int characterIndex = wInfo.firstCharacterIndex + i;

                        // Get the index of the material / sub text object used by this character.
                        // Store m_TextMeshPro.textInfo.characterInfo[characterIndex].materialReferenceIndex in
                        // local meshIndex.
                        int meshIndex = m_TextMeshPro.textInfo.characterInfo[characterIndex].materialReferenceIndex;

                        // Store m_TextMeshPro.textInfo.characterInfo[characterIndex].vertexIndex in local
                        // vertexIndex.
                        int vertexIndex = m_TextMeshPro.textInfo.characterInfo[characterIndex].vertexIndex;

                        // Get a reference to the vertex color
                        // Store m_TextMeshPro.textInfo.meshInfo[meshIndex] colors32 array from its mesh data in
                        // local vertexColors.
                        Color32[] vertexColors = m_TextMeshPro.textInfo.meshInfo[meshIndex].colors32;

                        // Store the result of vertexColors[vertexIndex + 0].Tint using 0.75 in local c.
                        Color32 c = vertexColors[vertexIndex + 0].Tint(0.75f);

                        // Copy c into vertexColors[vertexIndex + 0].
                        vertexColors[vertexIndex + 0] = c;
                        // Copy c into vertexColors[vertexIndex + 1].
                        vertexColors[vertexIndex + 1] = c;
                        // Copy c into vertexColors[vertexIndex + 2].
                        vertexColors[vertexIndex + 2] = c;
                        // Copy c into vertexColors[vertexIndex + 3].
                        vertexColors[vertexIndex + 3] = c;
                    }

                    // Update Geometry
                    // Upload changed TMP_VertexDataUpdateFlags.All from the TMP mesh arrays to m_TextMeshPro.
                    m_TextMeshPro.UpdateVertexData(TMP_VertexDataUpdateFlags.All);

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
                    // Deactivate m_TextPopup_RectTransform.gameObject.
                    m_TextPopup_RectTransform.gameObject.SetActive(false);
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

                    // Debug.Log("Link ID: \"" + linkInfo.GetLinkID() + "\"   Link Text: \"" + linkInfo.GetLinkText() + "\""); // Example of how to retrieve the Link ID and Link Text.

                    // Declare local worldPointInRectangle to hold Vector3 data supplied later in this method.
                    Vector3 worldPointInRectangle;
                    // Call RectTransformUtility.ScreenPointToWorldPointInRectangle using
                    // m_TextMeshPro.rectTransform, Input.mousePosition, m_Camera, worldPointInRectangle.
                    RectTransformUtility.ScreenPointToWorldPointInRectangle(m_TextMeshPro.rectTransform, Input.mousePosition, m_Camera, out worldPointInRectangle);

                    // Choose the matching branch using the result of linkInfo.GetLinkID.
                    switch (linkInfo.GetLinkID())
                    {
                        case "id_01": // 100041637: // id_01
                            // Copy worldPointInRectangle into m_TextPopup_RectTransform.position.
                            m_TextPopup_RectTransform.position = worldPointInRectangle;
                            // Activate m_TextPopup_RectTransform.gameObject.
                            m_TextPopup_RectTransform.gameObject.SetActive(true);
                            // Display k_LinkText plus " ID 01" in m_TextPopup_TMPComponent.text (the assigned
                            // TMP/UI text component).
                            m_TextPopup_TMPComponent.text = k_LinkText + " ID 01";
                            // Leave this loop or switch case so no later statements in that block run.
                            break;
                        case "id_02": // 100041638: // id_02
                            // Copy worldPointInRectangle into m_TextPopup_RectTransform.position.
                            m_TextPopup_RectTransform.position = worldPointInRectangle;
                            // Activate m_TextPopup_RectTransform.gameObject.
                            m_TextPopup_RectTransform.gameObject.SetActive(true);
                            // Display k_LinkText plus " ID 02" in m_TextPopup_TMPComponent.text (the assigned
                            // TMP/UI text component).
                            m_TextPopup_TMPComponent.text = k_LinkText + " ID 02";
                            // Leave this loop or switch case so no later statements in that block run.
                            break;
                    }
                }
                #endregion

            }
            else
            {
                // Restore any character that may have been modified
                // If m_lastIndex differs from negative 1, run the following block.
                if (m_lastIndex != -1)
                {
                    // Call TMP_TextSelector_B.RestoreCachedVertexAttributes with m_lastIndex; restore the original
                    // character mesh data when removing a highlight.
                    RestoreCachedVertexAttributes(m_lastIndex);
                    // Set m_lastIndex to negative 1.
                    m_lastIndex = -1;
                }
            }

        }


        // Record that the pointer has entered this text object.
        // Unity EventSystem supplies eventData with the pointer position, button and selected UI event
        // information.
        public void OnPointerEnter(PointerEventData eventData)
        {
            //Debug.Log("OnPointerEnter()");
            // Set isHoveringObject to true.
            isHoveringObject = true;
        }


        // Record that the pointer has left this text object.
        // Unity EventSystem supplies eventData with the pointer position, button and selected UI event
        // information.
        public void OnPointerExit(PointerEventData eventData)
        {
            //Debug.Log("OnPointerExit()");
            // Set isHoveringObject to false.
            isHoveringObject = false;
        }


        // Check the clicked word or link and show the matching sample popup.
        // Unity EventSystem supplies eventData with the pointer position, button and selected UI event
        // information.
        public void OnPointerClick(PointerEventData eventData)
        {
            //Debug.Log("Click at POS: " + eventData.position + "  World POS: " + eventData.worldPosition);

            // Check if Mouse Intersects any of the characters. If so, assign a random color.
            #region Character Selection Handling
            /*
            int charIndex = TMP_TextUtilities.FindIntersectingCharacter(m_TextMeshPro, Input.mousePosition, m_Camera, true);
            if (charIndex != -1 && charIndex != m_lastIndex)
            {
                //Debug.Log("Character [" + m_TextMeshPro.textInfo.characterInfo[index].character + "] was selected at POS: " + eventData.position);
                m_lastIndex = charIndex;

                Color32 c = new Color32((byte)Random.Range(0, 255), (byte)Random.Range(0, 255), (byte)Random.Range(0, 255), 255);
                int vertexIndex = m_TextMeshPro.textInfo.characterInfo[charIndex].vertexIndex;

                UIVertex[] uiVertices = m_TextMeshPro.textInfo.meshInfo.uiVertices;

                uiVertices[vertexIndex + 0].color = c;
                uiVertices[vertexIndex + 1].color = c;
                uiVertices[vertexIndex + 2].color = c;
                uiVertices[vertexIndex + 3].color = c;

                m_TextMeshPro.canvasRenderer.SetVertices(uiVertices, uiVertices.Length);
            }
            */
            #endregion


            #region Word Selection Handling
            //Check if Mouse intersects any words and if so assign a random color to that word.
            /*
            int wordIndex = TMP_TextUtilities.FindIntersectingWord(m_TextMeshPro, Input.mousePosition, m_Camera);

            // Clear previous word selection.
            if (m_TextPopup_RectTransform != null && m_selectedWord != -1 && (wordIndex == -1 || wordIndex != m_selectedWord))
            {
                TMP_WordInfo wInfo = m_TextMeshPro.textInfo.wordInfo[m_selectedWord];

                // Get a reference to the uiVertices array.
                UIVertex[] uiVertices = m_TextMeshPro.textInfo.meshInfo.uiVertices;

                // Iterate through each of the characters of the word.
                for (int i = 0; i < wInfo.characterCount; i++)
                {
                    int vertexIndex = m_TextMeshPro.textInfo.characterInfo[wInfo.firstCharacterIndex + i].vertexIndex;

                    Color32 c = uiVertices[vertexIndex + 0].color.Tint(1.33333f);

                    uiVertices[vertexIndex + 0].color = c;
                    uiVertices[vertexIndex + 1].color = c;
                    uiVertices[vertexIndex + 2].color = c;
                    uiVertices[vertexIndex + 3].color = c;
                }

                m_TextMeshPro.canvasRenderer.SetVertices(uiVertices, uiVertices.Length);

                m_selectedWord = -1;
            }

            // Handle word selection
            if (wordIndex != -1 && wordIndex != m_selectedWord)
            {
                m_selectedWord = wordIndex;

                TMP_WordInfo wInfo = m_TextMeshPro.textInfo.wordInfo[wordIndex];

                // Get a reference to the uiVertices array.
                UIVertex[] uiVertices = m_TextMeshPro.textInfo.meshInfo.uiVertices;

                // Iterate through each of the characters of the word.
                for (int i = 0; i < wInfo.characterCount; i++)
                {
                    int vertexIndex = m_TextMeshPro.textInfo.characterInfo[wInfo.firstCharacterIndex + i].vertexIndex;

                    Color32 c = uiVertices[vertexIndex + 0].color.Tint(0.75f);

                    uiVertices[vertexIndex + 0].color = c;
                    uiVertices[vertexIndex + 1].color = c;
                    uiVertices[vertexIndex + 2].color = c;
                    uiVertices[vertexIndex + 3].color = c;
                }

                m_TextMeshPro.canvasRenderer.SetVertices(uiVertices, uiVertices.Length);
            }
            */
            #endregion


            #region Link Selection Handling
            /*
            // Check if Mouse intersects any words and if so assign a random color to that word.
            int linkIndex = TMP_TextUtilities.FindIntersectingLink(m_TextMeshPro, Input.mousePosition, m_Camera);
            if (linkIndex != -1)
            {
                TMP_LinkInfo linkInfo = m_TextMeshPro.textInfo.linkInfo[linkIndex];
                int linkHashCode = linkInfo.hashCode;

                //Debug.Log(TMP_TextUtilities.GetSimpleHashCode("id_02"));

                switch (linkHashCode)
                {
                    case 291445: // id_01
                        if (m_LinkObject01 == null)
                            m_LinkObject01 = Instantiate(Link_01_Prefab);
                        else
                        {
                            m_LinkObject01.gameObject.SetActive(true);
                        }

                        break;
                    case 291446: // id_02
                        break;

                }

                // Example of how to modify vertex attributes like colors
                #region Vertex Attribute Modification Example
                UIVertex[] uiVertices = m_TextMeshPro.textInfo.meshInfo.uiVertices;

                Color32 c = new Color32((byte)Random.Range(0, 255), (byte)Random.Range(0, 255), (byte)Random.Range(0, 255), 255);
                for (int i = 0; i < linkInfo.characterCount; i++)
                {
                    TMP_CharacterInfo cInfo = m_TextMeshPro.textInfo.characterInfo[linkInfo.firstCharacterIndex + i];

                    if (!cInfo.isVisible) continue; // Skip invisible characters.

                    int vertexIndex = cInfo.vertexIndex;

                    uiVertices[vertexIndex + 0].color = c;
                    uiVertices[vertexIndex + 1].color = c;
                    uiVertices[vertexIndex + 2].color = c;
                    uiVertices[vertexIndex + 3].color = c;
                }

                m_TextMeshPro.canvasRenderer.SetVertices(uiVertices, uiVertices.Length);
                #endregion
            }
            */
            #endregion
        }


        // This sample leaves pointer-release handling empty.
        // Unity EventSystem supplies eventData with the pointer position, button and selected UI event
        // information.
        public void OnPointerUp(PointerEventData eventData)
        {
            //Debug.Log("OnPointerUp()");
        }


        // Restore the original character mesh data when removing a highlight.
        // index is a int argument supplied by TMP_TextSelector_B.LateUpdate.
        void RestoreCachedVertexAttributes(int index)
        {
            // If index equals negative 1 or index is above m_TextMeshPro.textInfo.characterCount minus 1, return
            // now; skip the rest of this method.
            if (index == -1 || index > m_TextMeshPro.textInfo.characterCount - 1) return;

            // Get the index of the material / sub text object used by this character.
            // Store m_TextMeshPro.textInfo.characterInfo[index].materialReferenceIndex in local materialIndex.
            int materialIndex = m_TextMeshPro.textInfo.characterInfo[index].materialReferenceIndex;

            // Get the index of the first vertex of the selected character.
            // Store m_TextMeshPro.textInfo.characterInfo[index].vertexIndex in local vertexIndex.
            int vertexIndex = m_TextMeshPro.textInfo.characterInfo[index].vertexIndex;

            // Restore Vertices
            // Get a reference to the cached / original vertices.
            // Store m_cachedMeshInfoVertexData[materialIndex] vertices array from its mesh data in local
            // src_vertices.
            Vector3[] src_vertices = m_cachedMeshInfoVertexData[materialIndex].vertices;

            // Get a reference to the vertices that we need to replace.
            // Store m_TextMeshPro.textInfo.meshInfo[materialIndex] vertices array from its mesh data in local
            // dst_vertices.
            Vector3[] dst_vertices = m_TextMeshPro.textInfo.meshInfo[materialIndex].vertices;

            // Restore / Copy vertices from source to destination
            // Set dst_vertices[vertexIndex + 0] to src_vertices[vertexIndex + 0] (entry read from src_vertices).
            dst_vertices[vertexIndex + 0] = src_vertices[vertexIndex + 0];
            // Set dst_vertices[vertexIndex + 1] to src_vertices[vertexIndex + 1] (entry read from src_vertices).
            dst_vertices[vertexIndex + 1] = src_vertices[vertexIndex + 1];
            // Set dst_vertices[vertexIndex + 2] to src_vertices[vertexIndex + 2] (entry read from src_vertices).
            dst_vertices[vertexIndex + 2] = src_vertices[vertexIndex + 2];
            // Set dst_vertices[vertexIndex + 3] to src_vertices[vertexIndex + 3] (entry read from src_vertices).
            dst_vertices[vertexIndex + 3] = src_vertices[vertexIndex + 3];

            // Restore Vertex Colors
            // Get a reference to the vertex colors we need to replace.
            // Store m_TextMeshPro.textInfo.meshInfo[materialIndex] colors32 array from its mesh data in local
            // dst_colors.
            Color32[] dst_colors = m_TextMeshPro.textInfo.meshInfo[materialIndex].colors32;

            // Get a reference to the cached / original vertex colors.
            // Store m_cachedMeshInfoVertexData[materialIndex] colors32 array from its mesh data in local
            // src_colors.
            Color32[] src_colors = m_cachedMeshInfoVertexData[materialIndex].colors32;

            // Copy the vertex colors from source to destination.
            // Set dst_colors[vertexIndex + 0] to src_colors[vertexIndex + 0] (entry read from src_colors).
            dst_colors[vertexIndex + 0] = src_colors[vertexIndex + 0];
            // Set dst_colors[vertexIndex + 1] to src_colors[vertexIndex + 1] (entry read from src_colors).
            dst_colors[vertexIndex + 1] = src_colors[vertexIndex + 1];
            // Set dst_colors[vertexIndex + 2] to src_colors[vertexIndex + 2] (entry read from src_colors).
            dst_colors[vertexIndex + 2] = src_colors[vertexIndex + 2];
            // Set dst_colors[vertexIndex + 3] to src_colors[vertexIndex + 3] (entry read from src_colors).
            dst_colors[vertexIndex + 3] = src_colors[vertexIndex + 3];

            // Restore UV0S
            // UVS0
            // Store m_cachedMeshInfoVertexData[materialIndex] uvs0 array from its mesh data in local src_uv0s.
            Vector4[] src_uv0s = m_cachedMeshInfoVertexData[materialIndex].uvs0;
            // Store m_TextMeshPro.textInfo.meshInfo[materialIndex] uvs0 array from its mesh data in local
            // dst_uv0s.
            Vector4[] dst_uv0s = m_TextMeshPro.textInfo.meshInfo[materialIndex].uvs0;
            // Set dst_uv0s[vertexIndex + 0] to src_uv0s[vertexIndex + 0] (entry read from src_uv0s).
            dst_uv0s[vertexIndex + 0] = src_uv0s[vertexIndex + 0];
            // Set dst_uv0s[vertexIndex + 1] to src_uv0s[vertexIndex + 1] (entry read from src_uv0s).
            dst_uv0s[vertexIndex + 1] = src_uv0s[vertexIndex + 1];
            // Set dst_uv0s[vertexIndex + 2] to src_uv0s[vertexIndex + 2] (entry read from src_uv0s).
            dst_uv0s[vertexIndex + 2] = src_uv0s[vertexIndex + 2];
            // Set dst_uv0s[vertexIndex + 3] to src_uv0s[vertexIndex + 3] (entry read from src_uv0s).
            dst_uv0s[vertexIndex + 3] = src_uv0s[vertexIndex + 3];

            // UVS2
            // Store m_cachedMeshInfoVertexData[materialIndex] uvs2 array from its mesh data in local src_uv2s.
            Vector2[] src_uv2s = m_cachedMeshInfoVertexData[materialIndex].uvs2;
            // Store m_TextMeshPro.textInfo.meshInfo[materialIndex] uvs2 array from its mesh data in local
            // dst_uv2s.
            Vector2[] dst_uv2s = m_TextMeshPro.textInfo.meshInfo[materialIndex].uvs2;
            // Set dst_uv2s[vertexIndex + 0] to src_uv2s[vertexIndex + 0] (entry read from src_uv2s).
            dst_uv2s[vertexIndex + 0] = src_uv2s[vertexIndex + 0];
            // Set dst_uv2s[vertexIndex + 1] to src_uv2s[vertexIndex + 1] (entry read from src_uv2s).
            dst_uv2s[vertexIndex + 1] = src_uv2s[vertexIndex + 1];
            // Set dst_uv2s[vertexIndex + 2] to src_uv2s[vertexIndex + 2] (entry read from src_uv2s).
            dst_uv2s[vertexIndex + 2] = src_uv2s[vertexIndex + 2];
            // Set dst_uv2s[vertexIndex + 3] to src_uv2s[vertexIndex + 3] (entry read from src_uv2s).
            dst_uv2s[vertexIndex + 3] = src_uv2s[vertexIndex + 3];


            // Restore last vertex attribute as we swapped it as well
            // Store (src_vertices.Length / 4 minus 1) multiplied by 4 in local lastIndex.
            int lastIndex = (src_vertices.Length / 4 - 1) * 4;

            // Vertices
            // Set dst_vertices[lastIndex + 0] to src_vertices[lastIndex + 0] (entry read from src_vertices).
            dst_vertices[lastIndex + 0] = src_vertices[lastIndex + 0];
            // Set dst_vertices[lastIndex + 1] to src_vertices[lastIndex + 1] (entry read from src_vertices).
            dst_vertices[lastIndex + 1] = src_vertices[lastIndex + 1];
            // Set dst_vertices[lastIndex + 2] to src_vertices[lastIndex + 2] (entry read from src_vertices).
            dst_vertices[lastIndex + 2] = src_vertices[lastIndex + 2];
            // Set dst_vertices[lastIndex + 3] to src_vertices[lastIndex + 3] (entry read from src_vertices).
            dst_vertices[lastIndex + 3] = src_vertices[lastIndex + 3];

            // Vertex Colors
            // Set src_colors to m_cachedMeshInfoVertexData[materialIndex] colors32 array from its mesh data.
            src_colors = m_cachedMeshInfoVertexData[materialIndex].colors32;
            // Set dst_colors to m_TextMeshPro.textInfo.meshInfo[materialIndex] colors32 array from its mesh data.
            dst_colors = m_TextMeshPro.textInfo.meshInfo[materialIndex].colors32;
            // Set dst_colors[lastIndex + 0] to src_colors[lastIndex + 0] (entry read from src_colors).
            dst_colors[lastIndex + 0] = src_colors[lastIndex + 0];
            // Set dst_colors[lastIndex + 1] to src_colors[lastIndex + 1] (entry read from src_colors).
            dst_colors[lastIndex + 1] = src_colors[lastIndex + 1];
            // Set dst_colors[lastIndex + 2] to src_colors[lastIndex + 2] (entry read from src_colors).
            dst_colors[lastIndex + 2] = src_colors[lastIndex + 2];
            // Set dst_colors[lastIndex + 3] to src_colors[lastIndex + 3] (entry read from src_colors).
            dst_colors[lastIndex + 3] = src_colors[lastIndex + 3];

            // UVS0
            // Set src_uv0s to m_cachedMeshInfoVertexData[materialIndex] uvs0 array from its mesh data.
            src_uv0s = m_cachedMeshInfoVertexData[materialIndex].uvs0;
            // Set dst_uv0s to m_TextMeshPro.textInfo.meshInfo[materialIndex] uvs0 array from its mesh data.
            dst_uv0s = m_TextMeshPro.textInfo.meshInfo[materialIndex].uvs0;
            // Set dst_uv0s[lastIndex + 0] to src_uv0s[lastIndex + 0] (entry read from src_uv0s).
            dst_uv0s[lastIndex + 0] = src_uv0s[lastIndex + 0];
            // Set dst_uv0s[lastIndex + 1] to src_uv0s[lastIndex + 1] (entry read from src_uv0s).
            dst_uv0s[lastIndex + 1] = src_uv0s[lastIndex + 1];
            // Set dst_uv0s[lastIndex + 2] to src_uv0s[lastIndex + 2] (entry read from src_uv0s).
            dst_uv0s[lastIndex + 2] = src_uv0s[lastIndex + 2];
            // Set dst_uv0s[lastIndex + 3] to src_uv0s[lastIndex + 3] (entry read from src_uv0s).
            dst_uv0s[lastIndex + 3] = src_uv0s[lastIndex + 3];

            // UVS2
            // Set src_uv2s to m_cachedMeshInfoVertexData[materialIndex] uvs2 array from its mesh data.
            src_uv2s = m_cachedMeshInfoVertexData[materialIndex].uvs2;
            // Set dst_uv2s to m_TextMeshPro.textInfo.meshInfo[materialIndex] uvs2 array from its mesh data.
            dst_uv2s = m_TextMeshPro.textInfo.meshInfo[materialIndex].uvs2;
            // Set dst_uv2s[lastIndex + 0] to src_uv2s[lastIndex + 0] (entry read from src_uv2s).
            dst_uv2s[lastIndex + 0] = src_uv2s[lastIndex + 0];
            // Set dst_uv2s[lastIndex + 1] to src_uv2s[lastIndex + 1] (entry read from src_uv2s).
            dst_uv2s[lastIndex + 1] = src_uv2s[lastIndex + 1];
            // Set dst_uv2s[lastIndex + 2] to src_uv2s[lastIndex + 2] (entry read from src_uv2s).
            dst_uv2s[lastIndex + 2] = src_uv2s[lastIndex + 2];
            // Set dst_uv2s[lastIndex + 3] to src_uv2s[lastIndex + 3] (entry read from src_uv2s).
            dst_uv2s[lastIndex + 3] = src_uv2s[lastIndex + 3];

            // Need to update the appropriate
            // Upload changed TMP_VertexDataUpdateFlags.All from the TMP mesh arrays to m_TextMeshPro.
            m_TextMeshPro.UpdateVertexData(TMP_VertexDataUpdateFlags.All);
        }
    }
}
