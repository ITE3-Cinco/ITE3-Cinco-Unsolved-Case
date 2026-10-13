// Purpose: Compares repeated world-text updates using TextMesh Pro or TextMesh.
// Use: Attach in a sample scene and assign the selected text font.
// How it works: A startup coroutine creates text and updates a count once per frame.
// Context: bundled TextMesh Pro example; these notes explain its sample behavior.

// UnityEngine supplies GameObjects, components, transforms, time, math and physics used here.
using UnityEngine;
// System.Collections supplies IEnumerator so coroutines can pause and resume across frames.
using System.Collections;


namespace TMPro.Examples
{

    public class Benchmark01 : MonoBehaviour
    {

        // BenchmarkType: benchmark type. Assigned in the scene/prefab Inspector; the initializer below is the
        // default.
        public int BenchmarkType = 0;

        // TMProFont: tmpro font. Assigned in the scene/prefab Inspector.
        public TMP_FontAsset TMProFont;
        // TextMeshFont: text mesh font. Assigned in the scene/prefab Inspector.
        public Font TextMeshFont;

        // m_textMeshPro: text mesh pro. Start retrieves it from a TextMeshPro component attached to the GameObject
        // that owns this component.
        private TextMeshPro m_textMeshPro;
        // m_textContainer: text container. Runtime data filled by the methods below; starts at the type default.
        private TextContainer m_textContainer;
        // m_textMesh: text mesh. Start retrieves it from a TextMesh component attached to the GameObject that owns
        // this component.
        private TextMesh m_textMesh;

        // label01: label01. Fixed script constant; callers cannot change it.
        private const string label01 = "The <#0050FF>count is: </color>{0}";
        // label02: label02. Fixed script constant; callers cannot change it.
        private const string label02 = "The <color=#0050FF>count is: </color>";

        //private string m_string;
        //private int m_frame;

        // m_material01: material01. Start sets it from m_textMeshPro.font material reference (Renderer.material
        // may create an instance).
        private Material m_material01;
        // m_material02: material02. Start retrieves it from the Material asset from a Resources folder at "Fonts &
        // Materials/LiberationSans SDF - Drop Shadow".
        private Material m_material02;



        // Create the selected text component, then change its count once per frame for comparison.
        IEnumerator Start()
        {



            // If BenchmarkType equals 0, run the following block.
            if (BenchmarkType == 0) // TextMesh Pro Component
            {
                // Store a TextMeshPro component attached to the GameObject that owns this component in
                // m_textMeshPro.
                m_textMeshPro = gameObject.AddComponent<TextMeshPro>();
                // Set m_textMeshPro.autoSizeTextContainer to true.
                m_textMeshPro.autoSizeTextContainer = true;

                //m_textMeshPro.anchorDampening = true;

                // If TMProFont exists, run the following block.
                if (TMProFont != null)
                    // Copy TMProFont from this component into m_textMeshPro.font.
                    m_textMeshPro.font = TMProFont;

                //m_textMeshPro.font = Resources.Load("Fonts & Materials/Anton SDF", typeof(TextMeshProFont)) as TextMeshProFont; // Make sure the Anton SDF exists before calling this...
                //m_textMeshPro.fontSharedMaterial = Resources.Load("Fonts & Materials/Anton SDF", typeof(Material)) as Material; // Same as above make sure this material exists.

                // Set m_textMeshPro.fontSize to 48.
                m_textMeshPro.fontSize = 48;
                // Set m_textMeshPro.alignment to TextAlignmentOptions.Center.
                m_textMeshPro.alignment = TextAlignmentOptions.Center;
                //m_textMeshPro.anchor = AnchorPositions.Center;
                // Set m_textMeshPro.extraPadding to true.
                m_textMeshPro.extraPadding = true;
                //m_textMeshPro.outlineWidth = 0.25f;
                //m_textMeshPro.fontSharedMaterial.SetFloat("_OutlineWidth", 0.2f);
                //m_textMeshPro.fontSharedMaterial.EnableKeyword("UNDERLAY_ON");
                //m_textMeshPro.lineJustification = LineJustificationTypes.Center;
                // Set m_textMeshPro.textWrappingMode to TextWrappingModes.NoWrap.
                m_textMeshPro.textWrappingMode = TextWrappingModes.NoWrap;
                //m_textMeshPro.lineLength = 60;
                //m_textMeshPro.characterSpacing = 0.2f;
                //m_textMeshPro.fontColor = new Color32(255, 255, 255, 255);

                // Set m_material01 to m_textMeshPro.font material reference (Renderer.material may create an
                // instance).
                m_material01 = m_textMeshPro.font.material;
                // Store the Material asset from a Resources folder at "Fonts & Materials/LiberationSans SDF - Drop
                // Shadow" in m_material02.
                m_material02 = Resources.Load<Material>("Fonts & Materials/LiberationSans SDF - Drop Shadow"); // Make sure the LiberationSans SDF exists before calling this...


            }
            else if (BenchmarkType == 1) // TextMesh
            {
                // Store a TextMesh component attached to the GameObject that owns this component in m_textMesh.
                m_textMesh = gameObject.AddComponent<TextMesh>();

                // If TextMeshFont exists, run the following block.
                if (TextMeshFont != null)
                {
                    // Copy TextMeshFont from this component into m_textMesh.font.
                    m_textMesh.font = TextMeshFont;
                    // Set m_textMesh.GetComponent<Renderer>().sharedMaterial to m_textMesh.font material reference
                    // (Renderer.material may create an instance).
                    m_textMesh.GetComponent<Renderer>().sharedMaterial = m_textMesh.font.material;
                }
                else
                {
                    // Set m_textMesh.font to Resources.Load("Fonts/ARIAL", typeof(Font)) as Font.
                    m_textMesh.font = Resources.Load("Fonts/ARIAL", typeof(Font)) as Font;
                    // Set m_textMesh.GetComponent<Renderer>().sharedMaterial to m_textMesh.font material reference
                    // (Renderer.material may create an instance).
                    m_textMesh.GetComponent<Renderer>().sharedMaterial = m_textMesh.font.material;
                }

                // Set m_textMesh.fontSize to 48.
                m_textMesh.fontSize = 48;
                // Set m_textMesh.anchor to TextAnchor.MiddleCenter.
                m_textMesh.anchor = TextAnchor.MiddleCenter;

                //m_textMesh.color = new Color32(255, 255, 0, 255);
            }



            // Repeat this block while i is at most 1000000; start from int i = 0.
            for (int i = 0; i <= 1000000; i++)
            {
                // If BenchmarkType equals 0, run the following block.
                if (BenchmarkType == 0)
                {
                    // Call m_textMeshPro.SetText using label01, i modulo (remainder after dividing by) 1000.
                    m_textMeshPro.SetText(label01, i % 1000);
                    // If i modulo (remainder after dividing by) 1000 equals 999, run the following block.
                    if (i % 1000 == 999)
                        // Set m_textMeshPro.fontSharedMaterial to m_textMeshPro.fontSharedMaterial = m_material02
                        // when m_textMeshPro.fontSharedMaterial equals m_material01; otherwise
                        // m_textMeshPro.fontSharedMaterial = m_material01.
                        m_textMeshPro.fontSharedMaterial = m_textMeshPro.fontSharedMaterial == m_material01 ? m_textMeshPro.fontSharedMaterial = m_material02 : m_textMeshPro.fontSharedMaterial = m_material01;



                }
                else if (BenchmarkType == 1)
                    // Display label02 plus the result of (i % 1000).ToString in m_textMesh.text (the assigned
                    // TMP/UI text component).
                    m_textMesh.text = label02 + (i % 1000).ToString();

                // Pause the coroutine until the next Unity frame so the animation/sequence can render.
                yield return null;
            }


            // Pause the coroutine until the next Unity frame so the animation/sequence can render.
            yield return null;
        }


        /*
        void Update()
        {
            if (BenchmarkType == 0)
            {
                m_textMeshPro.text = (m_frame % 1000).ToString();
            }
            else if (BenchmarkType == 1)
            {
                m_textMesh.text = (m_frame % 1000).ToString();
            }

            m_frame += 1;
        }
        */
    }
}
