// Purpose: Randomly enlarges characters and adjusts their draw order.
// Use: Attach to TMP text in an example scene.
// How it works: Scale visible character meshes, sort them by size and upload the updated geometry.
// Context: bundled TextMesh Pro example; these notes explain its sample behavior.

// UnityEngine supplies GameObjects, components, transforms, time, math and physics used here.
using UnityEngine;
// LINQ filters and combines collections, such as finding a scene component of a chosen type.
using System.Linq;
// System.Collections supplies IEnumerator so coroutines can pause and resume across frames.
using System.Collections;
// System.Collections.Generic supplies typed lists, sets and dictionaries for stored data.
using System.Collections.Generic;


namespace TMPro.Examples
{

    public class VertexZoom : MonoBehaviour
    {
        // AngleMultiplier: angle multiplier. Assigned in the scene/prefab Inspector; the initializer below is the
        // default.
        public float AngleMultiplier = 1.0f;
        // SpeedMultiplier: speed multiplier. Assigned in the scene/prefab Inspector; the initializer below is the
        // default.
        public float SpeedMultiplier = 1.0f;
        // CurveScale: curve scale. Assigned in the scene/prefab Inspector; the initializer below is the default.
        public float CurveScale = 1.0f;

        // m_TextComponent: text component. Awake retrieves it from the first TMP_Text component on this
        // GameObject.
        private TMP_Text m_TextComponent;
        // hasTextChanged: has text changed. Updated by ON_TEXT_CHANGED, AnimateVertexColors.
        private bool hasTextChanged;


        // Cache the TMP text component whose mesh this effect will change.
        void Awake()
        {
            // Store the first TMP_Text component on this GameObject in m_TextComponent.
            m_TextComponent = GetComponent<TMP_Text>();
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


        // Start the repeating text-mesh animation coroutine.
        void Start()
        {
            // Start coroutine AnimateVertexColors(); Unity resumes it after each yield.
            StartCoroutine(AnimateVertexColors());
        }


        // Mark text geometry for refreshing when a text-change event is received.
        // obj is a Object argument supplied by the code, event or Inspector callback invoking ON_TEXT_CHANGED.
        void ON_TEXT_CHANGED(Object obj)
        {
            // If obj equals m_TextComponent, run the following block.
            if (obj == m_TextComponent)
                // Set hasTextChanged to true.
                hasTextChanged = true;
        }

        // Scale visible character meshes, sort them by size and upload the updated geometry.
        /// <summary>
        /// Enlarge characters and sort their mesh draw order by scale.
        /// </summary>
        /// <returns></returns>
        IEnumerator AnimateVertexColors()
        {

            // We force an update of the text object since it would only be updated at the end of the frame. Ie. before this code is executed on the first frame.
            // Alternatively, we could yield and wait until the end of the frame when the text object will be generated.
            // Regenerate m_TextComponent text mesh now so character and vertex information is available.
            m_TextComponent.ForceMeshUpdate();

            // Store m_TextComponent generated TMP character/line/mesh information in local textInfo.
            TMP_TextInfo textInfo = m_TextComponent.textInfo;

            // Declare local matrix to hold Matrix4x4 data supplied later in this method.
            Matrix4x4 matrix;
            // Store a copy of textInfo generated mesh arrays, used as the original effect baseline in local
            // cachedMeshInfoVertexData.
            TMP_MeshInfo[] cachedMeshInfoVertexData = textInfo.CopyMeshInfoVertexData();

            // Allocations for sorting of the modified scales
            // Store a new list with no entries in local modifiedCharScale.
            List<float> modifiedCharScale = new List<float>();
            // Store a new list with no entries in local scaleSortingOrder.
            List<int> scaleSortingOrder = new List<int>();

            // Set hasTextChanged to true.
            hasTextChanged = true;

            // Repeat while true.
            while (true)
            {
                // Allocate new vertices
                // If hasTextChanged, run the following block.
                if (hasTextChanged)
                {
                    // Get updated vertex data
                    // Store a copy of textInfo generated mesh arrays, used as the original effect baseline in
                    // cachedMeshInfoVertexData.
                    cachedMeshInfoVertexData = textInfo.CopyMeshInfoVertexData();

                    // Set hasTextChanged to false.
                    hasTextChanged = false;
                }

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

                // Clear list of character scales
                // Remove all entries from modifiedCharScale.
                modifiedCharScale.Clear();
                // Remove all entries from scaleSortingOrder.
                scaleSortingOrder.Clear();

                // Repeat this block while i is below characterCount; start from int i = 0.
                for (int i = 0; i < characterCount; i++)
                {
                    // Store textInfo.characterInfo[i] (entry read from textInfo.characterInfo) in local charInfo.
                    TMP_CharacterInfo charInfo = textInfo.characterInfo[i];

                    // Skip characters that are not visible and thus have no geometry to manipulate.
                    // Skip this loop entry when not (charInfo.isVisible).
                    if (!charInfo.isVisible)
                        // Skip this entry and continue with the next loop iteration.
                        continue;

                    // Get the index of the material used by the current character.
                    // Store textInfo.characterInfo[i].materialReferenceIndex in local materialIndex.
                    int materialIndex = textInfo.characterInfo[i].materialReferenceIndex;

                    // Get the index of the first vertex used by this text element.
                    // Store textInfo.characterInfo[i].vertexIndex in local vertexIndex.
                    int vertexIndex = textInfo.characterInfo[i].vertexIndex;

                    // Get the cached vertices of the mesh used by this text element (character or sprite).
                    // Store cachedMeshInfoVertexData[materialIndex] vertices array from its mesh data in local
                    // sourceVertices.
                    Vector3[] sourceVertices = cachedMeshInfoVertexData[materialIndex].vertices;

                    // Determine the center point of each character at the baseline.
                    //Vector2 charMidBasline = new Vector2((sourceVertices[vertexIndex + 0].x + sourceVertices[vertexIndex + 2].x) / 2, charInfo.baseLine);
                    // Determine the center point of each character.
                    // Store (sourceVertices[vertexIndex + 0] + sourceVertices[vertexIndex + 2]) / 2 (calculated
                    // from the values shown) in local charMidBasline.
                    Vector2 charMidBasline = (sourceVertices[vertexIndex + 0] + sourceVertices[vertexIndex + 2]) / 2;

                    // Need to translate all 4 vertices of each quad to aligned with middle of character / baseline.
                    // This is needed so the matrix TRS is applied at the origin for each character.
                    // Keep charMidBasline in local offset.
                    Vector3 offset = charMidBasline;

                    // Store textInfo.meshInfo[materialIndex] vertices array from its mesh data in local
                    // destinationVertices.
                    Vector3[] destinationVertices = textInfo.meshInfo[materialIndex].vertices;

                    // Set destinationVertices[vertexIndex + 0] to sourceVertices[vertexIndex + 0] (entry read from
                    // sourceVertices) minus offset.
                    destinationVertices[vertexIndex + 0] = sourceVertices[vertexIndex + 0] - offset;
                    // Set destinationVertices[vertexIndex + 1] to sourceVertices[vertexIndex + 1] (entry read from
                    // sourceVertices) minus offset.
                    destinationVertices[vertexIndex + 1] = sourceVertices[vertexIndex + 1] - offset;
                    // Set destinationVertices[vertexIndex + 2] to sourceVertices[vertexIndex + 2] (entry read from
                    // sourceVertices) minus offset.
                    destinationVertices[vertexIndex + 2] = sourceVertices[vertexIndex + 2] - offset;
                    // Set destinationVertices[vertexIndex + 3] to sourceVertices[vertexIndex + 3] (entry read from
                    // sourceVertices) minus offset.
                    destinationVertices[vertexIndex + 3] = sourceVertices[vertexIndex + 3] - offset;

                    //Vector3 jitterOffset = new Vector3(Random.Range(-.25f, .25f), Random.Range(-.25f, .25f), 0);

                    // Determine the random scale change for each character.
                    // Store Unity random value between 1 and 1.5 in local randomScale.
                    float randomScale = Random.Range(1f, 1.5f);

                    // Add modified scale and index
                    // Add randomScale to modifiedCharScale.
                    modifiedCharScale.Add(randomScale);
                    // Add number of entries in modifiedCharScale minus 1 to scaleSortingOrder.
                    scaleSortingOrder.Add(modifiedCharScale.Count - 1);

                    // Setup the matrix for the scale change.
                    //matrix = Matrix4x4.TRS(jitterOffset, Quaternion.Euler(0, 0, Random.Range(-5f, 5f)), Vector3.one * randomScale);
                    // Store a transform matrix from position a Vector3 built from 0, 0, 0, rotation the identity
                    // rotation (no rotation) and scale Vector3.one * randomScale in matrix.
                    matrix = Matrix4x4.TRS(new Vector3(0, 0, 0), Quaternion.identity, Vector3.one * randomScale);

                    // Store the result of matrix.MultiplyPoint3x4 using destinationVertices[vertexIndex + 0]
                    // (entry read from destinationVertices) in destinationVertices[vertexIndex + 0].
                    destinationVertices[vertexIndex + 0] = matrix.MultiplyPoint3x4(destinationVertices[vertexIndex + 0]);
                    // Store the result of matrix.MultiplyPoint3x4 using destinationVertices[vertexIndex + 1]
                    // (entry read from destinationVertices) in destinationVertices[vertexIndex + 1].
                    destinationVertices[vertexIndex + 1] = matrix.MultiplyPoint3x4(destinationVertices[vertexIndex + 1]);
                    // Store the result of matrix.MultiplyPoint3x4 using destinationVertices[vertexIndex + 2]
                    // (entry read from destinationVertices) in destinationVertices[vertexIndex + 2].
                    destinationVertices[vertexIndex + 2] = matrix.MultiplyPoint3x4(destinationVertices[vertexIndex + 2]);
                    // Store the result of matrix.MultiplyPoint3x4 using destinationVertices[vertexIndex + 3]
                    // (entry read from destinationVertices) in destinationVertices[vertexIndex + 3].
                    destinationVertices[vertexIndex + 3] = matrix.MultiplyPoint3x4(destinationVertices[vertexIndex + 3]);

                    // Add offset to destinationVertices[vertexIndex + 0] and store the updated value there.
                    destinationVertices[vertexIndex + 0] += offset;
                    // Add offset to destinationVertices[vertexIndex + 1] and store the updated value there.
                    destinationVertices[vertexIndex + 1] += offset;
                    // Add offset to destinationVertices[vertexIndex + 2] and store the updated value there.
                    destinationVertices[vertexIndex + 2] += offset;
                    // Add offset to destinationVertices[vertexIndex + 3] and store the updated value there.
                    destinationVertices[vertexIndex + 3] += offset;

                    // Restore Source UVS which have been modified by the sorting
                    // Store cachedMeshInfoVertexData[materialIndex] uvs0 array from its mesh data in local
                    // sourceUVs0.
                    Vector4[] sourceUVs0 = cachedMeshInfoVertexData[materialIndex].uvs0;
                    // Store textInfo.meshInfo[materialIndex] uvs0 array from its mesh data in local
                    // destinationUVs0.
                    Vector4[] destinationUVs0 = textInfo.meshInfo[materialIndex].uvs0;

                    // Set destinationUVs0[vertexIndex + 0] to sourceUVs0[vertexIndex + 0] (entry read from
                    // sourceUVs0).
                    destinationUVs0[vertexIndex + 0] = sourceUVs0[vertexIndex + 0];
                    // Set destinationUVs0[vertexIndex + 1] to sourceUVs0[vertexIndex + 1] (entry read from
                    // sourceUVs0).
                    destinationUVs0[vertexIndex + 1] = sourceUVs0[vertexIndex + 1];
                    // Set destinationUVs0[vertexIndex + 2] to sourceUVs0[vertexIndex + 2] (entry read from
                    // sourceUVs0).
                    destinationUVs0[vertexIndex + 2] = sourceUVs0[vertexIndex + 2];
                    // Set destinationUVs0[vertexIndex + 3] to sourceUVs0[vertexIndex + 3] (entry read from
                    // sourceUVs0).
                    destinationUVs0[vertexIndex + 3] = sourceUVs0[vertexIndex + 3];

                    // Restore Source Vertex Colors
                    // Store cachedMeshInfoVertexData[materialIndex] colors32 array from its mesh data in local
                    // sourceColors32.
                    Color32[] sourceColors32 = cachedMeshInfoVertexData[materialIndex].colors32;
                    // Store textInfo.meshInfo[materialIndex] colors32 array from its mesh data in local
                    // destinationColors32.
                    Color32[] destinationColors32 = textInfo.meshInfo[materialIndex].colors32;

                    // Set destinationColors32[vertexIndex + 0] to sourceColors32[vertexIndex + 0] (entry read from
                    // sourceColors32).
                    destinationColors32[vertexIndex + 0] = sourceColors32[vertexIndex + 0];
                    // Set destinationColors32[vertexIndex + 1] to sourceColors32[vertexIndex + 1] (entry read from
                    // sourceColors32).
                    destinationColors32[vertexIndex + 1] = sourceColors32[vertexIndex + 1];
                    // Set destinationColors32[vertexIndex + 2] to sourceColors32[vertexIndex + 2] (entry read from
                    // sourceColors32).
                    destinationColors32[vertexIndex + 2] = sourceColors32[vertexIndex + 2];
                    // Set destinationColors32[vertexIndex + 3] to sourceColors32[vertexIndex + 3] (entry read from
                    // sourceColors32).
                    destinationColors32[vertexIndex + 3] = sourceColors32[vertexIndex + 3];
                }

                // Push changes into meshes
                // Repeat this block while i is below length of textInfo.meshInfo; start from int i = 0.
                for (int i = 0; i < textInfo.meshInfo.Length; i++)
                {
                    //// Sort Quads based modified scale
                    // Call scaleSortingOrder.Sort using the callback/condition defined here.
                    scaleSortingOrder.Sort((a, b) => modifiedCharScale[a].CompareTo(modifiedCharScale[b]));

                    // Call textInfo.meshInfo[i].SortGeometry using scaleSortingOrder.
                    textInfo.meshInfo[i].SortGeometry(scaleSortingOrder);

                    // Updated modified vertex attributes
                    // Set textInfo.meshInfo[i].mesh.vertices to textInfo.meshInfo[i] vertices array from its mesh
                    // data.
                    textInfo.meshInfo[i].mesh.vertices = textInfo.meshInfo[i].vertices;
                    // Call textInfo.meshInfo[i].mesh.SetUVs using 0, textInfo.meshInfo[i] uvs0 array from its mesh
                    // data.
                    textInfo.meshInfo[i].mesh.SetUVs(0, textInfo.meshInfo[i].uvs0);
                    // Set textInfo.meshInfo[i].mesh.colors32 to textInfo.meshInfo[i] colors32 array from its mesh
                    // data.
                    textInfo.meshInfo[i].mesh.colors32 = textInfo.meshInfo[i].colors32;

                    // Upload textInfo.meshInfo[i].mesh to m_TextComponent material mesh slot i.
                    m_TextComponent.UpdateGeometry(textInfo.meshInfo[i].mesh, i);
                }

                // Yield a wait for 0.1 gameplay seconds (pause stops the timer); Unity resumes the coroutine after
                // it completes.
                yield return new WaitForSeconds(0.1f);
            }
        }

    }
}