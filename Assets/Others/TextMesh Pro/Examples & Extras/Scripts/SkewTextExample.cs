// Purpose: Bends and shears text by changing its character mesh vertices.
// Use: Attach to TMP text and tune VertexCurve, CurveScale and ShearAmount.
// How it works: Copy the curve, transform visible character vertices and send the mesh back for display.
// Context: bundled TextMesh Pro example; these notes explain its sample behavior.

// UnityEngine supplies GameObjects, components, transforms, time, math and physics used here.
using UnityEngine;
// System.Collections supplies IEnumerator so coroutines can pause and resume across frames.
using System.Collections;


namespace TMPro.Examples
{

    public class SkewTextExample : MonoBehaviour
    {

        // m_TextComponent: text component. Awake retrieves it from the first TMP_Text component on the GameObject
        // that owns this component.
        private TMP_Text m_TextComponent;

        // VertexCurve: vertex curve. Assigned in the scene/prefab Inspector; the initializer below is the default.
        public AnimationCurve VertexCurve = new AnimationCurve(new Keyframe(0, 0), new Keyframe(0.25f, 2.0f), new Keyframe(0.5f, 0), new Keyframe(0.75f, 2.0f), new Keyframe(1, 0f));
        //public float AngleMultiplier = 1.0f;
        //public float SpeedMultiplier = 1.0f;
        // CurveScale: curve scale. Updated by WarpText.
        public float CurveScale = 1.0f;
        // ShearAmount: shear amount. Assigned in the scene/prefab Inspector; the initializer below is the default.
        public float ShearAmount = 1.0f;

        // Cache the TMP text component to be curved.
        void Awake()
        {
            // Store the first TMP_Text component on the GameObject that owns this component in m_TextComponent.
            m_TextComponent = gameObject.GetComponent<TMP_Text>();
        }


        // Start the repeating text-warp coroutine.
        void Start()
        {
            // Start coroutine WarpText(); Unity resumes it after each yield.
            StartCoroutine(WarpText());
        }


        // Copy curve keys so the effect can compare against its previous curve settings.
        // curve is a AnimationCurve argument supplied by SkewTextExample.WarpText.
        private AnimationCurve CopyAnimationCurve(AnimationCurve curve)
        {
            // Store a new AnimationCurve instance in local newCurve.
            AnimationCurve newCurve = new AnimationCurve();

            // Set newCurve.keys to curve.keys.
            newCurve.keys = curve.keys;

            // Return newCurve to the method caller.
            return newCurve;
        }


        // Copy the curve, transform visible character vertices and send the mesh back for display.
        /// <summary>
        ///  Method to curve text along a Unity animation curve.
        /// </summary>
        /// <param name="textComponent"></param>
        /// <returns></returns>
        IEnumerator WarpText()
        {
            // Set VertexCurve.preWrapMode to WrapMode.Clamp.
            VertexCurve.preWrapMode = WrapMode.Clamp;
            // Set VertexCurve.postWrapMode to WrapMode.Clamp.
            VertexCurve.postWrapMode = WrapMode.Clamp;

            //Mesh mesh = m_TextComponent.textInfo.meshInfo[0].mesh;

            // Declare local vertices to hold Vector3[] data supplied later in this method.
            Vector3[] vertices;
            // Declare local matrix to hold Matrix4x4 data supplied later in this method.
            Matrix4x4 matrix;

            // Set m_TextComponent.havePropertiesChanged to true.
            m_TextComponent.havePropertiesChanged = true; // Need to force the TextMeshPro Object to be updated.
            // Multiply the current CurveScale by 10 and store the result back in CurveScale.
            CurveScale *= 10;
            // Keep CurveScale from this component in local old_CurveScale.
            float old_CurveScale = CurveScale;
            // Keep ShearAmount from this component in local old_ShearValue.
            float old_ShearValue = ShearAmount;
            // Store the result of SkewTextExample.CopyAnimationCurve using VertexCurve in local old_curve.
            AnimationCurve old_curve = CopyAnimationCurve(VertexCurve);

            // Repeat while true.
            while (true)
            {
                // If !m_TextComponent.havePropertiesChanged and old_CurveScale == CurveScale and
                // old_curve.keys[1].value equals VertexCurve.keys[1].value and old_ShearValue equals ShearAmount,
                // run the following block.
                if (!m_TextComponent.havePropertiesChanged && old_CurveScale == CurveScale && old_curve.keys[1].value == VertexCurve.keys[1].value && old_ShearValue == ShearAmount)
                {
                    // Pause the coroutine until the next Unity frame so the animation/sequence can render.
                    yield return null;
                    // Skip this entry and continue with the next loop iteration.
                    continue;
                }

                // Copy CurveScale from this component into old_CurveScale.
                old_CurveScale = CurveScale;
                // Store the result of SkewTextExample.CopyAnimationCurve using VertexCurve in old_curve.
                old_curve = CopyAnimationCurve(VertexCurve);
                // Copy ShearAmount from this component into old_ShearValue.
                old_ShearValue = ShearAmount;

                // Regenerate m_TextComponent text mesh now so character and vertex information is available.
                m_TextComponent.ForceMeshUpdate(); // Generate the mesh and populate the textInfo with data we can use and manipulate.

                // Store m_TextComponent generated TMP character/line/mesh information in local textInfo.
                TMP_TextInfo textInfo = m_TextComponent.textInfo;
                // Store textInfo.characterCount in local characterCount.
                int characterCount = textInfo.characterCount;


                // If characterCount equals 0, skip this entry and continue with the next loop iteration.
                if (characterCount == 0) continue;

                //vertices = textInfo.meshInfo[0].vertices;
                //int lastVertexIndex = textInfo.characterInfo[characterCount - 1].vertexIndex;

                // Store m_TextComponent.bounds.min X component in local boundsMinX.
                float boundsMinX = m_TextComponent.bounds.min.x;  //textInfo.meshInfo[0].mesh.bounds.min.x;
                // Store m_TextComponent.bounds.max X component in local boundsMaxX.
                float boundsMaxX = m_TextComponent.bounds.max.x;  //textInfo.meshInfo[0].mesh.bounds.max.x;



                // Repeat this block while i is below characterCount; start from int i = 0.
                for (int i = 0; i < characterCount; i++)
                {
                    // Skip this loop entry when not (textInfo.characterInfo[i].isVisible).
                    if (!textInfo.characterInfo[i].isVisible)
                        // Skip this entry and continue with the next loop iteration.
                        continue;

                    // Store textInfo.characterInfo[i].vertexIndex in local vertexIndex.
                    int vertexIndex = textInfo.characterInfo[i].vertexIndex;

                    // Get the index of the mesh used by this character.
                    // Store textInfo.characterInfo[i].materialReferenceIndex in local materialIndex.
                    int materialIndex = textInfo.characterInfo[i].materialReferenceIndex;

                    // Set vertices to textInfo.meshInfo[materialIndex] vertices array from its mesh data.
                    vertices = textInfo.meshInfo[materialIndex].vertices;

                    // Compute the baseline mid point for each character
                    // Store a Vector2 built from (vertices[vertexIndex + 0].x + vertices[vertexIndex + 2].x) / 2
                    // (calculated from the values shown), textInfo.characterInfo[i].baseLine in local
                    // offsetToMidBaseline.
                    Vector3 offsetToMidBaseline = new Vector2((vertices[vertexIndex + 0].x + vertices[vertexIndex + 2].x) / 2, textInfo.characterInfo[i].baseLine);
                    //float offsetY = VertexCurve.Evaluate((float)i / characterCount + loopCount / 50f); // Random.Range(-0.25f, 0.25f);

                    // Apply offset to adjust our pivot point.
                    // Add negative offsetToMidBaseline to vertices[vertexIndex + 0] and store the updated value
                    // there.
                    vertices[vertexIndex + 0] += -offsetToMidBaseline;
                    // Add negative offsetToMidBaseline to vertices[vertexIndex + 1] and store the updated value
                    // there.
                    vertices[vertexIndex + 1] += -offsetToMidBaseline;
                    // Add negative offsetToMidBaseline to vertices[vertexIndex + 2] and store the updated value
                    // there.
                    vertices[vertexIndex + 2] += -offsetToMidBaseline;
                    // Add negative offsetToMidBaseline to vertices[vertexIndex + 3] and store the updated value
                    // there.
                    vertices[vertexIndex + 3] += -offsetToMidBaseline;

                    // Apply the Shearing FX
                    // Store ShearAmount multiplied by 0.01 in local shear_value.
                    float shear_value = ShearAmount * 0.01f;
                    // Store a Vector3 built from shear_value * (textInfo.characterInfo[i].topRight.y -
                    // textInfo.characterInfo[i].baseLine) (calculated from the values shown), 0, 0 in local
                    // topShear.
                    Vector3 topShear = new Vector3(shear_value * (textInfo.characterInfo[i].topRight.y - textInfo.characterInfo[i].baseLine), 0, 0);
                    // Store a Vector3 built from shear_value * (textInfo.characterInfo[i].baseLine -
                    // textInfo.characterInfo[i].bottomRight.y) (calculated from the values shown), 0, 0 in local
                    // bottomShear.
                    Vector3 bottomShear = new Vector3(shear_value * (textInfo.characterInfo[i].baseLine - textInfo.characterInfo[i].bottomRight.y), 0, 0);

                    // Add negative bottomShear to vertices[vertexIndex + 0] and store the updated value there.
                    vertices[vertexIndex + 0] += -bottomShear;
                    // Add topShear to vertices[vertexIndex + 1] and store the updated value there.
                    vertices[vertexIndex + 1] += topShear;
                    // Add topShear to vertices[vertexIndex + 2] and store the updated value there.
                    vertices[vertexIndex + 2] += topShear;
                    // Add negative bottomShear to vertices[vertexIndex + 3] and store the updated value there.
                    vertices[vertexIndex + 3] += -bottomShear;


                    // Compute the angle of rotation for each character based on the animation curve
                    // Store (offsetToMidBaseline.x - boundsMinX) / (boundsMaxX - boundsMinX) (calculated from the
                    // values shown) in local x0.
                    float x0 = (offsetToMidBaseline.x - boundsMinX) / (boundsMaxX - boundsMinX); // Character's position relative to the bounds of the mesh.
                    // Store x0 plus 0.0001 in local x1.
                    float x1 = x0 + 0.0001f;
                    // Store the curve/graph value from VertexCurve at x0 multiplied by CurveScale in local y0.
                    float y0 = VertexCurve.Evaluate(x0) * CurveScale;
                    // Store the curve/graph value from VertexCurve at x1 multiplied by CurveScale in local y1.
                    float y1 = VertexCurve.Evaluate(x1) * CurveScale;

                    // Store a Vector3 built from 1, 0, 0 in local horizontal.
                    Vector3 horizontal = new Vector3(1, 0, 0);
                    //Vector3 normal = new Vector3(-(y1 - y0), (x1 * (boundsMaxX - boundsMinX) + boundsMinX) - offsetToMidBaseline.x, 0);
                    // Store new Vector3(x1 * (boundsMaxX - boundsMinX) + boundsMinX, y1) - new
                    // Vector3(offsetToMidBaseline.x, y0) (calculated from the values shown) in local tangent.
                    Vector3 tangent = new Vector3(x1 * (boundsMaxX - boundsMinX) + boundsMinX, y1) - new Vector3(offsetToMidBaseline.x, y0);

                    // Store Mathf.Acos(Vector3.Dot(horizontal, tangent.normalized)) * 57.2957795f (calculated from
                    // the values shown) in local dot.
                    float dot = Mathf.Acos(Vector3.Dot(horizontal, tangent.normalized)) * 57.2957795f;
                    // Store the result of Vector3.Cross using horizontal, tangent in local cross.
                    Vector3 cross = Vector3.Cross(horizontal, tangent);
                    // Store dot when cross Z component is above 0; otherwise 360 minus dot in local angle.
                    float angle = cross.z > 0 ? dot : 360 - dot;

                    // Store a transform matrix from position a Vector3 built from 0, y0, 0, rotation a rotation
                    // built from angles 0, 0, angle in degrees and scale a vector with all three components equal
                    // to 1 in matrix.
                    matrix = Matrix4x4.TRS(new Vector3(0, y0, 0), Quaternion.Euler(0, 0, angle), Vector3.one);

                    // Store the result of matrix.MultiplyPoint3x4 using vertices[vertexIndex + 0] (entry read from
                    // vertices) in vertices[vertexIndex + 0].
                    vertices[vertexIndex + 0] = matrix.MultiplyPoint3x4(vertices[vertexIndex + 0]);
                    // Store the result of matrix.MultiplyPoint3x4 using vertices[vertexIndex + 1] (entry read from
                    // vertices) in vertices[vertexIndex + 1].
                    vertices[vertexIndex + 1] = matrix.MultiplyPoint3x4(vertices[vertexIndex + 1]);
                    // Store the result of matrix.MultiplyPoint3x4 using vertices[vertexIndex + 2] (entry read from
                    // vertices) in vertices[vertexIndex + 2].
                    vertices[vertexIndex + 2] = matrix.MultiplyPoint3x4(vertices[vertexIndex + 2]);
                    // Store the result of matrix.MultiplyPoint3x4 using vertices[vertexIndex + 3] (entry read from
                    // vertices) in vertices[vertexIndex + 3].
                    vertices[vertexIndex + 3] = matrix.MultiplyPoint3x4(vertices[vertexIndex + 3]);

                    // Add offsetToMidBaseline to vertices[vertexIndex + 0] and store the updated value there.
                    vertices[vertexIndex + 0] += offsetToMidBaseline;
                    // Add offsetToMidBaseline to vertices[vertexIndex + 1] and store the updated value there.
                    vertices[vertexIndex + 1] += offsetToMidBaseline;
                    // Add offsetToMidBaseline to vertices[vertexIndex + 2] and store the updated value there.
                    vertices[vertexIndex + 2] += offsetToMidBaseline;
                    // Add offsetToMidBaseline to vertices[vertexIndex + 3] and store the updated value there.
                    vertices[vertexIndex + 3] += offsetToMidBaseline;
                }


                // Upload the mesh with the revised information
                // Upload changed  from the TMP mesh arrays to m_TextComponent.
                m_TextComponent.UpdateVertexData();

                // Pause the coroutine until the next Unity frame so the animation/sequence can render.
                yield return null; // new WaitForSeconds(0.025f);
            }
        }
    }
}
