// Purpose: Changes the colors of text characters in sequence.
// Use: Attach to TMP text in a sample scene.
// How it works: A coroutine edits the four vertex colors of each visible character and refreshes the mesh.
// Context: bundled TextMesh Pro example; these notes explain its sample behavior.

// UnityEngine supplies GameObjects, components, transforms, time, math and physics used here.
using UnityEngine;
// System.Collections supplies IEnumerator so coroutines can pause and resume across frames.
using System.Collections;


namespace TMPro.Examples
{

    public class VertexColorCycler : MonoBehaviour
    {

        // m_TextComponent: text component. Awake retrieves it from the first TMP_Text component on this
        // GameObject.
        private TMP_Text m_TextComponent;

        // Cache the TMP text component whose mesh this effect will change.
        void Awake()
        {
            // Store the first TMP_Text component on this GameObject in m_TextComponent.
            m_TextComponent = GetComponent<TMP_Text>();
        }


        // Start the repeating text-mesh animation coroutine.
        void Start()
        {
            // Start coroutine AnimateVertexColors(); Unity resumes it after each yield.
            StartCoroutine(AnimateVertexColors());
        }


        // A coroutine edits the four vertex colors of each visible character and refreshes the mesh.
        /// <summary>
        /// Method to animate vertex colors of a TMP Text object.
        /// </summary>
        /// <returns></returns>
        IEnumerator AnimateVertexColors()
        {
            // Force the text object to update right away so we can have geometry to modify right from the start.
            // Regenerate m_TextComponent text mesh now so character and vertex information is available.
            m_TextComponent.ForceMeshUpdate();

            // Store m_TextComponent generated TMP character/line/mesh information in local textInfo.
            TMP_TextInfo textInfo = m_TextComponent.textInfo;
            // Initialize currentCharacter to 0.
            int currentCharacter = 0;

            // Declare local newVertexColors to hold Color32[] data supplied later in this method.
            Color32[] newVertexColors;
            // Store m_TextComponent.color in local c0.
            Color32 c0 = m_TextComponent.color;

            // Repeat while true.
            while (true)
            {
                // Store textInfo.characterCount in local characterCount.
                int characterCount = textInfo.characterCount;

                // If No Characters then just yield and wait for some text to be added
                // If characterCount equals 0, run the following block.
                if (characterCount == 0)
                {
                    // Yield a wait for 0.25 gameplay seconds (pause stops the timer); Unity resumes the coroutine
                    // after it completes.
                    yield return new WaitForSeconds(0.25f);
                    // Skip this entry and continue with the next loop iteration.
                    continue;
                }

                // Get the index of the material used by the current character.
                // Store textInfo.characterInfo[currentCharacter].materialReferenceIndex in local materialIndex.
                int materialIndex = textInfo.characterInfo[currentCharacter].materialReferenceIndex;

                // Get the vertex colors of the mesh used by this text element (character or sprite).
                // Set newVertexColors to textInfo.meshInfo[materialIndex] colors32 array from its mesh data.
                newVertexColors = textInfo.meshInfo[materialIndex].colors32;

                // Get the index of the first vertex used by this text element.
                // Store textInfo.characterInfo[currentCharacter].vertexIndex in local vertexIndex.
                int vertexIndex = textInfo.characterInfo[currentCharacter].vertexIndex;

                // Only change the vertex color if the text element is visible.
                // If textInfo.characterInfo[currentCharacter].isVisible, run the following block.
                if (textInfo.characterInfo[currentCharacter].isVisible)
                {
                    // Set c0 to a Color32 built from Unity random value between 0 and 255 (integer upper bound
                    // excluded) viewed as byte, Unity random value between 0 and 255 (integer upper bound
                    // excluded) viewed as byte, Unity random value between 0 and 255 (integer upper bound
                    // excluded) viewed as byte, 255.
                    c0 = new Color32((byte)Random.Range(0, 255), (byte)Random.Range(0, 255), (byte)Random.Range(0, 255), 255);

                    // Copy c0 into newVertexColors[vertexIndex + 0].
                    newVertexColors[vertexIndex + 0] = c0;
                    // Copy c0 into newVertexColors[vertexIndex + 1].
                    newVertexColors[vertexIndex + 1] = c0;
                    // Copy c0 into newVertexColors[vertexIndex + 2].
                    newVertexColors[vertexIndex + 2] = c0;
                    // Copy c0 into newVertexColors[vertexIndex + 3].
                    newVertexColors[vertexIndex + 3] = c0;

                    // New function which pushes (all) updated vertex data to the appropriate meshes when using either the Mesh Renderer or CanvasRenderer.
                    // Upload changed TMP_VertexDataUpdateFlags.Colors32 from the TMP mesh arrays to
                    // m_TextComponent.
                    m_TextComponent.UpdateVertexData(TMP_VertexDataUpdateFlags.Colors32);

                    // This last process could be done to only update the vertex data that has changed as opposed to all of the vertex data but it would require extra steps and knowing what type of renderer is used.
                    // These extra steps would be a performance optimization but it is unlikely that such optimization will be necessary.
                }

                // Set currentCharacter to (currentCharacter plus 1) modulo (remainder after dividing by)
                // characterCount.
                currentCharacter = (currentCharacter + 1) % characterCount;

                // Yield a wait for 0.05 gameplay seconds (pause stops the timer); Unity resumes the coroutine
                // after it completes.
                yield return new WaitForSeconds(0.05f);
            }
        }

    }
}
