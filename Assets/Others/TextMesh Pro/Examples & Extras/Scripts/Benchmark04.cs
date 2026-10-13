// Purpose: Displays sample text across a range of point sizes.
// Use: Use in an orthographic-camera sample scene and choose the size range.
// How it works: Loop through sizes, create text objects and stack them inside the camera view.
// Context: bundled TextMesh Pro example; these notes explain its sample behavior.

// UnityEngine supplies GameObjects, components, transforms, time, math and physics used here.
using UnityEngine;
// System.Collections supplies IEnumerator so coroutines can pause and resume across frames.
using System.Collections;


namespace TMPro.Examples
{

    public class Benchmark04 : MonoBehaviour
    {

        // SpawnType: spawn type. Assigned in the scene/prefab Inspector; the initializer below is the default.
        public int SpawnType = 0;

        // MinPointSize: min point size. Assigned in the scene/prefab Inspector; the initializer below is the
        // default.
        public int MinPointSize = 12;
        // MaxPointSize: max point size. Assigned in the scene/prefab Inspector; the initializer below is the
        // default.
        public int MaxPointSize = 64;
        // Steps: steps. Assigned in the scene/prefab Inspector; the initializer below is the default.
        public int Steps = 4;

        // m_Transform: transform. Start sets it from the Transform on this component GameObject.
        private Transform m_Transform;
        //private TextMeshProFloatingText floatingText_Script;
        //public Material material;


        // When the example starts, loop through sizes, create text objects and stack them inside the camera view.
        void Start()
        {
            // Copy transform into m_Transform.
            m_Transform = transform;

            // Initialize lineHeight to 0.
            float lineHeight = 0;
            // Store Camera.main.orthographicSize = Screen.height / 2 in local orthoSize.
            float orthoSize = Camera.main.orthographicSize = Screen.height / 2;
            // Store Screen.width viewed as float divided by Screen.height in local ratio.
            float ratio = (float)Screen.width / Screen.height;

            // Repeat this block while i is at most MaxPointSize; start from int i = MinPointSize.
            for (int i = MinPointSize; i <= MaxPointSize; i += Steps)
            {
                // If SpawnType equals 0, run the following block.
                if (SpawnType == 0)
                {
                    // TextMesh Pro Implementation
                    // Store a new GameObject named "Text - " plus i plus " Pts" in local go.
                    GameObject go = new GameObject("Text - " + i + " Pts");

                    // If lineHeight is above orthoSize multiplied by 2, return now; skip the rest of this method.
                    if (lineHeight > orthoSize * 2) return;

                    // Set go.transform.position to m_Transform.position + new Vector3(ratio * -orthoSize * 0.975f,
                    // orthoSize * 0.975f - lineHeight, 0) (calculated from the values shown).
                    go.transform.position = m_Transform.position + new Vector3(ratio * -orthoSize * 0.975f, orthoSize * 0.975f - lineHeight, 0);

                    // Store a TextMeshPro component attached to go in local textMeshPro.
                    TextMeshPro textMeshPro = go.AddComponent<TextMeshPro>();

                    //textMeshPro.fontSharedMaterial = material;
                    //textMeshPro.font = Resources.Load("Fonts & Materials/LiberationSans SDF", typeof(TextMeshProFont)) as TextMeshProFont;
                    //textMeshPro.anchor = AnchorPositions.Left;
                    // Set textMeshPro.rectTransform.pivot to a Vector2 built from 0, 0.5.
                    textMeshPro.rectTransform.pivot = new Vector2(0, 0.5f);

                    // Set textMeshPro.textWrappingMode to TextWrappingModes.NoWrap.
                    textMeshPro.textWrappingMode = TextWrappingModes.NoWrap;
                    // Set textMeshPro.extraPadding to true.
                    textMeshPro.extraPadding = true;
                    // Set textMeshPro.isOrthographic to true.
                    textMeshPro.isOrthographic = true;
                    // Copy i into textMeshPro.fontSize.
                    textMeshPro.fontSize = i;

                    // Display i plus " pts - Lorem ipsum dolor sit..." in textMeshPro.text (the assigned TMP/UI
                    // text component).
                    textMeshPro.text = i + " pts - Lorem ipsum dolor sit...";
                    // Set textMeshPro.color to a Color32 built from 255, 255, 255, 255.
                    textMeshPro.color = new Color32(255, 255, 255, 255);

                    // Add i to lineHeight and store the updated value there.
                    lineHeight += i;
                }
                else
                {
                    // TextMesh Implementation
                    // Causes crashes since atlas needed exceeds 4096 X 4096
                    /*
                    GameObject go = new GameObject("Arial " + i);

                    //if (lineHeight > orthoSize * 2 * 0.9f) return;

                    go.transform.position = m_Transform.position + new Vector3(ratio * -orthoSize * 0.975f, orthoSize * 0.975f - lineHeight, 1);

                    TextMesh textMesh = go.AddComponent<TextMesh>();
                    textMesh.font = Resources.Load("Fonts/ARIAL", typeof(Font)) as Font;
                    textMesh.renderer.sharedMaterial = textMesh.font.material;
                    textMesh.anchor = TextAnchor.MiddleLeft;
                    textMesh.fontSize = i * 10;

                    textMesh.color = new Color32(255, 255, 255, 255);
                    textMesh.text = i + " pts - Lorem ipsum dolor sit...";

                    lineHeight += i;
                    */
                }
            }
        }

    }
}
