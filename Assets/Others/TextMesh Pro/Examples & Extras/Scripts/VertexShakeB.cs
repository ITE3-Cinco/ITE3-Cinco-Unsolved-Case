// Purpose: Applies a shaking effect across sample text lines.
// Use: Attach to TMP text and tune the angle, speed and curve values.
// How it works: Scale each character around its own center, then rotate its line around the line center.
// Context: bundled TextMesh Pro example; these notes explain its sample behavior.

// UnityEngine supplies GameObjects, components, transforms, time, math and physics used here.
using UnityEngine;
// System.Collections supplies IEnumerator so coroutines can pause and resume across frames.
using System.Collections;


namespace TMPro.Examples
{

    public class VertexShakeB : MonoBehaviour
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
            // If obj = m_TextComponent, run the following block.
            if (obj = m_TextComponent)
                // Set hasTextChanged to true.
                hasTextChanged = true;
        }

        // Scale each character around its own center, then rotate its line around the line center.
        /// <summary>
        /// Scale characters and rotate their lines to create a shake.
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
            // Store a new array/object initialized with the entries specified here in local copyOfVertices.
            Vector3[][] copyOfVertices = new Vector3[0][];

            // Set hasTextChanged to true.
            hasTextChanged = true;

            // Repeat while true.
            while (true)
            {
                // Allocate new vertices 
                // If hasTextChanged, run the following block.
                if (hasTextChanged)
                {
                    // If length of copyOfVertices is below length of textInfo.meshInfo, run the following block.
                    if (copyOfVertices.Length < textInfo.meshInfo.Length)
                        // Set copyOfVertices to a new array/object initialized with the entries specified here.
                        copyOfVertices = new Vector3[textInfo.meshInfo.Length][];

                    // Repeat this block while i is below length of textInfo.meshInfo; start from int i = 0.
                    for (int i = 0; i < textInfo.meshInfo.Length; i++)
                    {
                        // Store length of textInfo.meshInfo[i].vertices in local length.
                        int length = textInfo.meshInfo[i].vertices.Length;
                        // Set copyOfVertices[i] to a new array/object initialized with the entries specified here.
                        copyOfVertices[i] = new Vector3[length];
                    }

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

                // Store textInfo.lineCount in local lineCount.
                int lineCount = textInfo.lineCount;

                // Iterate through each line of the text.
                // Repeat this block while i is below lineCount; start from int i = 0.
                for (int i = 0; i < lineCount; i++)
                {

                    // Store textInfo.lineInfo[i].firstCharacterIndex in local first.
                    int first = textInfo.lineInfo[i].firstCharacterIndex;
                    // Store textInfo.lineInfo[i].lastCharacterIndex in local last.
                    int last = textInfo.lineInfo[i].lastCharacterIndex;

                    // Determine the center of each line
                    // Store (textInfo.characterInfo[first].bottomLeft + textInfo.characterInfo[last].topRight) / 2
                    // (calculated from the values shown) in local centerOfLine.
                    Vector3 centerOfLine = (textInfo.characterInfo[first].bottomLeft + textInfo.characterInfo[last].topRight) / 2;
                    // Store a rotation built from angles 0, 0, Unity random value between -0.25f and 0.25f in
                    // degrees in local rotation.
                    Quaternion rotation = Quaternion.Euler(0, 0, Random.Range(-0.25f, 0.25f));

                    // Iterate through each character of the line.
                    // Repeat this block while j is at most last; start from int j = first.
                    for (int j = first; j <= last; j++)
                    {
                        // Skip characters that are not visible and thus have no geometry to manipulate.
                        // Skip this loop entry when not (textInfo.characterInfo[j].isVisible).
                        if (!textInfo.characterInfo[j].isVisible)
                            // Skip this entry and continue with the next loop iteration.
                            continue;

                        // Get the index of the material used by the current character.
                        // Store textInfo.characterInfo[j].materialReferenceIndex in local materialIndex.
                        int materialIndex = textInfo.characterInfo[j].materialReferenceIndex;

                        // Get the index of the first vertex used by this text element.
                        // Store textInfo.characterInfo[j].vertexIndex in local vertexIndex.
                        int vertexIndex = textInfo.characterInfo[j].vertexIndex;

                        // Get the vertices of the mesh used by this text element (character or sprite).
                        // Store textInfo.meshInfo[materialIndex] vertices array from its mesh data in local
                        // sourceVertices.
                        Vector3[] sourceVertices = textInfo.meshInfo[materialIndex].vertices;

                        // Determine the center point of each character at the baseline.
                        // Store (sourceVertices[vertexIndex + 0] + sourceVertices[vertexIndex + 2]) / 2
                        // (calculated from the values shown) in local charCenter.
                        Vector3 charCenter = (sourceVertices[vertexIndex + 0] + sourceVertices[vertexIndex + 2]) / 2;

                        // Need to translate all 4 vertices of each quad to aligned with center of character.
                        // This is needed so the matrix TRS is applied at the origin for each character.
                        // Set copyOfVertices[materialIndex][vertexIndex + 0] to sourceVertices[vertexIndex + 0]
                        // (entry read from sourceVertices) minus charCenter.
                        copyOfVertices[materialIndex][vertexIndex + 0] = sourceVertices[vertexIndex + 0] - charCenter;
                        // Set copyOfVertices[materialIndex][vertexIndex + 1] to sourceVertices[vertexIndex + 1]
                        // (entry read from sourceVertices) minus charCenter.
                        copyOfVertices[materialIndex][vertexIndex + 1] = sourceVertices[vertexIndex + 1] - charCenter;
                        // Set copyOfVertices[materialIndex][vertexIndex + 2] to sourceVertices[vertexIndex + 2]
                        // (entry read from sourceVertices) minus charCenter.
                        copyOfVertices[materialIndex][vertexIndex + 2] = sourceVertices[vertexIndex + 2] - charCenter;
                        // Set copyOfVertices[materialIndex][vertexIndex + 3] to sourceVertices[vertexIndex + 3]
                        // (entry read from sourceVertices) minus charCenter.
                        copyOfVertices[materialIndex][vertexIndex + 3] = sourceVertices[vertexIndex + 3] - charCenter;

                        // Determine the random scale change for each character.
                        // Store Unity random value between 0.95 and 1.05 in local randomScale.
                        float randomScale = Random.Range(0.95f, 1.05f);

                        // Setup the matrix for the scale change.
                        // Store a transform matrix from position a vector with all three components equal to 1,
                        // rotation the identity rotation (no rotation) and scale Vector3.one * randomScale in
                        // matrix.
                        matrix = Matrix4x4.TRS(Vector3.one, Quaternion.identity, Vector3.one * randomScale);

                        // Apply the scale change relative to the center of each character.
                        // Store the result of matrix.MultiplyPoint3x4 using
                        // copyOfVertices[materialIndex][vertexIndex + 0] (entry read from
                        // copyOfVertices[materialIndex]) in copyOfVertices[materialIndex][vertexIndex + 0].
                        copyOfVertices[materialIndex][vertexIndex + 0] = matrix.MultiplyPoint3x4(copyOfVertices[materialIndex][vertexIndex + 0]);
                        // Store the result of matrix.MultiplyPoint3x4 using
                        // copyOfVertices[materialIndex][vertexIndex + 1] (entry read from
                        // copyOfVertices[materialIndex]) in copyOfVertices[materialIndex][vertexIndex + 1].
                        copyOfVertices[materialIndex][vertexIndex + 1] = matrix.MultiplyPoint3x4(copyOfVertices[materialIndex][vertexIndex + 1]);
                        // Store the result of matrix.MultiplyPoint3x4 using
                        // copyOfVertices[materialIndex][vertexIndex + 2] (entry read from
                        // copyOfVertices[materialIndex]) in copyOfVertices[materialIndex][vertexIndex + 2].
                        copyOfVertices[materialIndex][vertexIndex + 2] = matrix.MultiplyPoint3x4(copyOfVertices[materialIndex][vertexIndex + 2]);
                        // Store the result of matrix.MultiplyPoint3x4 using
                        // copyOfVertices[materialIndex][vertexIndex + 3] (entry read from
                        // copyOfVertices[materialIndex]) in copyOfVertices[materialIndex][vertexIndex + 3].
                        copyOfVertices[materialIndex][vertexIndex + 3] = matrix.MultiplyPoint3x4(copyOfVertices[materialIndex][vertexIndex + 3]);

                        // Revert the translation change.
                        // Add charCenter to copyOfVertices[materialIndex][vertexIndex + 0] and store the updated
                        // value there.
                        copyOfVertices[materialIndex][vertexIndex + 0] += charCenter;
                        // Add charCenter to copyOfVertices[materialIndex][vertexIndex + 1] and store the updated
                        // value there.
                        copyOfVertices[materialIndex][vertexIndex + 1] += charCenter;
                        // Add charCenter to copyOfVertices[materialIndex][vertexIndex + 2] and store the updated
                        // value there.
                        copyOfVertices[materialIndex][vertexIndex + 2] += charCenter;
                        // Add charCenter to copyOfVertices[materialIndex][vertexIndex + 3] and store the updated
                        // value there.
                        copyOfVertices[materialIndex][vertexIndex + 3] += charCenter;

                        // Need to translate all 4 vertices of each quad to aligned with the center of the line.
                        // This is needed so the matrix TRS is applied from the center of the line.
                        // Subtract centerOfLine from copyOfVertices[materialIndex][vertexIndex + 0] and store the
                        // updated value there.
                        copyOfVertices[materialIndex][vertexIndex + 0] -= centerOfLine;
                        // Subtract centerOfLine from copyOfVertices[materialIndex][vertexIndex + 1] and store the
                        // updated value there.
                        copyOfVertices[materialIndex][vertexIndex + 1] -= centerOfLine;
                        // Subtract centerOfLine from copyOfVertices[materialIndex][vertexIndex + 2] and store the
                        // updated value there.
                        copyOfVertices[materialIndex][vertexIndex + 2] -= centerOfLine;
                        // Subtract centerOfLine from copyOfVertices[materialIndex][vertexIndex + 3] and store the
                        // updated value there.
                        copyOfVertices[materialIndex][vertexIndex + 3] -= centerOfLine;

                        // Setup the matrix rotation.
                        // Store a transform matrix from position a vector with all three components equal to 1,
                        // rotation rotation and scale a vector with all three components equal to 1 in matrix.
                        matrix = Matrix4x4.TRS(Vector3.one, rotation, Vector3.one);

                        // Apply the matrix TRS to the individual characters relative to the center of the current line.
                        // Store the result of matrix.MultiplyPoint3x4 using
                        // copyOfVertices[materialIndex][vertexIndex + 0] (entry read from
                        // copyOfVertices[materialIndex]) in copyOfVertices[materialIndex][vertexIndex + 0].
                        copyOfVertices[materialIndex][vertexIndex + 0] = matrix.MultiplyPoint3x4(copyOfVertices[materialIndex][vertexIndex + 0]);
                        // Store the result of matrix.MultiplyPoint3x4 using
                        // copyOfVertices[materialIndex][vertexIndex + 1] (entry read from
                        // copyOfVertices[materialIndex]) in copyOfVertices[materialIndex][vertexIndex + 1].
                        copyOfVertices[materialIndex][vertexIndex + 1] = matrix.MultiplyPoint3x4(copyOfVertices[materialIndex][vertexIndex + 1]);
                        // Store the result of matrix.MultiplyPoint3x4 using
                        // copyOfVertices[materialIndex][vertexIndex + 2] (entry read from
                        // copyOfVertices[materialIndex]) in copyOfVertices[materialIndex][vertexIndex + 2].
                        copyOfVertices[materialIndex][vertexIndex + 2] = matrix.MultiplyPoint3x4(copyOfVertices[materialIndex][vertexIndex + 2]);
                        // Store the result of matrix.MultiplyPoint3x4 using
                        // copyOfVertices[materialIndex][vertexIndex + 3] (entry read from
                        // copyOfVertices[materialIndex]) in copyOfVertices[materialIndex][vertexIndex + 3].
                        copyOfVertices[materialIndex][vertexIndex + 3] = matrix.MultiplyPoint3x4(copyOfVertices[materialIndex][vertexIndex + 3]);

                        // Revert the translation change.
                        // Add centerOfLine to copyOfVertices[materialIndex][vertexIndex + 0] and store the updated
                        // value there.
                        copyOfVertices[materialIndex][vertexIndex + 0] += centerOfLine;
                        // Add centerOfLine to copyOfVertices[materialIndex][vertexIndex + 1] and store the updated
                        // value there.
                        copyOfVertices[materialIndex][vertexIndex + 1] += centerOfLine;
                        // Add centerOfLine to copyOfVertices[materialIndex][vertexIndex + 2] and store the updated
                        // value there.
                        copyOfVertices[materialIndex][vertexIndex + 2] += centerOfLine;
                        // Add centerOfLine to copyOfVertices[materialIndex][vertexIndex + 3] and store the updated
                        // value there.
                        copyOfVertices[materialIndex][vertexIndex + 3] += centerOfLine;
                    }
                }

                // Push changes into meshes
                // Repeat this block while i is below length of textInfo.meshInfo; start from int i = 0.
                for (int i = 0; i < textInfo.meshInfo.Length; i++)
                {
                    // Set textInfo.meshInfo[i].mesh.vertices to copyOfVertices[i] (entry read from
                    // copyOfVertices).
                    textInfo.meshInfo[i].mesh.vertices = copyOfVertices[i];
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