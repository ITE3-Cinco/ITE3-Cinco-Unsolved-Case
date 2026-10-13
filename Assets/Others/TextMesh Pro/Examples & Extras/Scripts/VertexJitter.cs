// Purpose: Adds small random movements and rotations to individual text characters.
// Use: Attach to sample TMP text and tune the effect multipliers.
// How it works: Keep original mesh data, transform each visible character and upload the changed vertices.
// Context: bundled TextMesh Pro example; these notes explain its sample behavior.

// UnityEngine supplies GameObjects, components, transforms, time, math and physics used here.
using UnityEngine;
// System.Collections supplies IEnumerator so coroutines can pause and resume across frames.
using System.Collections;


namespace TMPro.Examples
{

    public class VertexJitter : MonoBehaviour
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

        /// <summary>
        /// Structure to hold pre-computed animation data.
        /// </summary>
        private struct VertexAnim
        {
            // angleRange: maximum random tilt in degrees. Runtime data filled by the methods below; starts at the
            // type default.
            public float angleRange;
            // angle: current character tilt in degrees. Runtime data filled by the methods below; starts at the
            // type default.
            public float angle;
            // speed: per-character animation speed. Runtime data filled by the methods below; starts at the type
            // default.
            public float speed;
        }

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

        // Keep original mesh data, transform each visible character and upload the changed vertices.
        /// <summary>
        /// Move and rotate the visible characters of a TMP text object.
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

            // Initialize loopCount to 0.
            int loopCount = 0;
            // Set hasTextChanged to true.
            hasTextChanged = true;

            // Create an Array which contains pre-computed Angle Ranges and Speeds for a bunch of characters.
            // Store a new array/object initialized with the entries specified here in local vertexAnim.
            VertexAnim[] vertexAnim = new VertexAnim[1024];
            // Repeat this block while i is below 1024; start from int i = 0.
            for (int i = 0; i < 1024; i++)
            {
                // Store Unity random value between 10 and 25 in vertexAnim[i].angleRange.
                vertexAnim[i].angleRange = Random.Range(10f, 25f);
                // Store Unity random value between 1 and 3 in vertexAnim[i].speed.
                vertexAnim[i].speed = Random.Range(1f, 3f);
            }

            // Cache the vertex data of the text object as the Jitter FX is applied to the original position of the characters.
            // Store a copy of textInfo generated mesh arrays, used as the original effect baseline in local
            // cachedMeshInfo.
            TMP_MeshInfo[] cachedMeshInfo = textInfo.CopyMeshInfoVertexData();

            // Repeat while true.
            while (true)
            {
                // Get new copy of vertex data if the text has changed.
                // If hasTextChanged, run the following block.
                if (hasTextChanged)
                {
                    // Update the copy of the vertex data for the text object.
                    // Store a copy of textInfo generated mesh arrays, used as the original effect baseline in
                    // cachedMeshInfo.
                    cachedMeshInfo = textInfo.CopyMeshInfoVertexData();

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

                    // Retrieve the pre-computed animation data for the given character.
                    // Store vertexAnim[i] (entry read from vertexAnim) in local vertAnim.
                    VertexAnim vertAnim = vertexAnim[i];

                    // Get the index of the material used by the current character.
                    // Store textInfo.characterInfo[i].materialReferenceIndex in local materialIndex.
                    int materialIndex = textInfo.characterInfo[i].materialReferenceIndex;

                    // Get the index of the first vertex used by this text element.
                    // Store textInfo.characterInfo[i].vertexIndex in local vertexIndex.
                    int vertexIndex = textInfo.characterInfo[i].vertexIndex;

                    // Get the cached vertices of the mesh used by this text element (character or sprite).
                    // Store cachedMeshInfo[materialIndex] vertices array from its mesh data in local
                    // sourceVertices.
                    Vector3[] sourceVertices = cachedMeshInfo[materialIndex].vertices;

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

                    // Store a smooth blend from negative vertAnim.angleRange to vertAnim.angleRange using fraction
                    // the result of Mathf.PingPong using loopCount / 25f * vertAnim.speed, 1f in vertAnim.angle.
                    vertAnim.angle = Mathf.SmoothStep(-vertAnim.angleRange, vertAnim.angleRange, Mathf.PingPong(loopCount / 25f * vertAnim.speed, 1f));
                    // Store a Vector3 built from Unity random value between -.25f and .25f, Unity random value
                    // between -.25f and .25f, 0 in local jitterOffset.
                    Vector3 jitterOffset = new Vector3(Random.Range(-.25f, .25f), Random.Range(-.25f, .25f), 0);

                    // Store a transform matrix from position jitterOffset * CurveScale, rotation a rotation built
                    // from angles 0, 0, Random.Range(-5f, 5f) * AngleMultiplier in degrees and scale a vector with
                    // all three components equal to 1 in matrix.
                    matrix = Matrix4x4.TRS(jitterOffset * CurveScale, Quaternion.Euler(0, 0, Random.Range(-5f, 5f) * AngleMultiplier), Vector3.one);

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

                    // Copy vertAnim into vertexAnim[i].
                    vertexAnim[i] = vertAnim;
                }

                // Push changes into meshes
                // Repeat this block while i is below length of textInfo.meshInfo; start from int i = 0.
                for (int i = 0; i < textInfo.meshInfo.Length; i++)
                {
                    // Set textInfo.meshInfo[i].mesh.vertices to textInfo.meshInfo[i] vertices array from its mesh
                    // data.
                    textInfo.meshInfo[i].mesh.vertices = textInfo.meshInfo[i].vertices;
                    // Upload textInfo.meshInfo[i].mesh to m_TextComponent material mesh slot i.
                    m_TextComponent.UpdateGeometry(textInfo.meshInfo[i].mesh, i);
                }

                // Add 1 to loopCount and store the updated value there.
                loopCount += 1;

                // Yield a wait for 0.1 gameplay seconds (pause stops the timer); Unity resumes the coroutine after
                // it completes.
                yield return new WaitForSeconds(0.1f);
            }
        }

    }
}