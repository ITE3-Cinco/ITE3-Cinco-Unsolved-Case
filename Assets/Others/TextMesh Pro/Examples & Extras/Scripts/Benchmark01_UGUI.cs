// Purpose: Compares repeated UI text updates using TextMesh Pro or legacy UI Text.
// Use: Attach in a sample canvas scene and assign the canvas and fonts.
// How it works: The selected UI component displays a changing count once per coroutine frame.
// Context: bundled TextMesh Pro example; these notes explain its sample behavior.

// UnityEngine supplies GameObjects, components, transforms, time, math and physics used here.
using UnityEngine;
// System.Collections supplies IEnumerator so coroutines can pause and resume across frames.
using System.Collections;
// Unity UI supplies canvas widgets such as images, sliders and buttons.
using UnityEngine.UI;


namespace TMPro.Examples
{
    
    public class Benchmark01_UGUI : MonoBehaviour
    {

        // BenchmarkType: benchmark type. Assigned in the scene/prefab Inspector; the initializer below is the
        // default.
        public int BenchmarkType = 0;

        // canvas: Canvas used by this component. Assigned in the scene/prefab Inspector.
        public Canvas canvas;
        // TMProFont: tmpro font. Assigned in the scene/prefab Inspector.
        public TMP_FontAsset TMProFont;
        // TextMeshFont: text mesh font. Assigned in the scene/prefab Inspector.
        public Font TextMeshFont;

        // m_textMeshPro: text mesh pro. Start retrieves it from a TextMeshProUGUI component attached to the
        // GameObject that owns this component.
        private TextMeshProUGUI m_textMeshPro;
        //private TextContainer m_textContainer;
        // m_textMesh: text mesh. Start retrieves it from a Text component attached to the GameObject that owns
        // this component.
        private Text m_textMesh;

        // label01: label01. Fixed script constant; callers cannot change it.
        private const string label01 = "The <#0050FF>count is: </color>";
        // label02: label02. Fixed script constant; callers cannot change it.
        private const string label02 = "The <color=#0050FF>count is: </color>";

        //private const string label01 = "TextMesh <#0050FF>Pro!</color>  The count is: {0}";
        //private const string label02 = "Text Mesh<color=#0050FF>        The count is: </color>";

        //private string m_string;
        //private int m_frame;

        // m_material01: material01. Start sets it from m_textMeshPro.font material reference (Renderer.material
        // may create an instance).
        private Material m_material01;
        // m_material02: material02. Start retrieves it from the Material asset from a Resources folder at "Fonts &
        // Materials/LiberationSans SDF - BEVEL".
        private Material m_material02;



        // Create the selected text component, then change its count once per frame for comparison.
        IEnumerator Start()
        {



            // If BenchmarkType equals 0, run the following block.
            if (BenchmarkType == 0) // TextMesh Pro Component
            {
                // Store a TextMeshProUGUI component attached to the GameObject that owns this component in
                // m_textMeshPro.
                m_textMeshPro = gameObject.AddComponent<TextMeshProUGUI>();
                //m_textContainer = GetComponent<TextContainer>();


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
                //m_textMeshPro.enableWordWrapping = true;    
                //m_textMeshPro.lineLength = 60;          
                //m_textMeshPro.characterSpacing = 0.2f;
                //m_textMeshPro.fontColor = new Color32(255, 255, 255, 255);

                // Set m_material01 to m_textMeshPro.font material reference (Renderer.material may create an
                // instance).
                m_material01 = m_textMeshPro.font.material;
                // Store the Material asset from a Resources folder at "Fonts & Materials/LiberationSans SDF -
                // BEVEL" in m_material02.
                m_material02 = Resources.Load<Material>("Fonts & Materials/LiberationSans SDF - BEVEL"); // Make sure the LiberationSans SDF exists before calling this...  


            }
            else if (BenchmarkType == 1) // TextMesh
            {
                // Store a Text component attached to the GameObject that owns this component in m_textMesh.
                m_textMesh = gameObject.AddComponent<Text>();

                // If TextMeshFont exists, run the following block.
                if (TextMeshFont != null)
                {
                    // Copy TextMeshFont from this component into m_textMesh.font.
                    m_textMesh.font = TextMeshFont;
                    //m_textMesh.renderer.sharedMaterial = m_textMesh.font.material;
                }
                else
                {
                    //m_textMesh.font = Resources.Load("Fonts/ARIAL", typeof(Font)) as Font;
                    //m_textMesh.renderer.sharedMaterial = m_textMesh.font.material;
                }

                // Set m_textMesh.fontSize to 48.
                m_textMesh.fontSize = 48;
                // Set m_textMesh.alignment to TextAnchor.MiddleCenter.
                m_textMesh.alignment = TextAnchor.MiddleCenter;

                //m_textMesh.color = new Color32(255, 255, 0, 255);    
            }



            // Repeat this block while i is at most 1000000; start from int i = 0.
            for (int i = 0; i <= 1000000; i++)
            {
                // If BenchmarkType equals 0, run the following block.
                if (BenchmarkType == 0)
                {
                    // Display label01 plus (i modulo (remainder after dividing by) 1000) in m_textMeshPro.text
                    // (the assigned TMP/UI text component).
                    m_textMeshPro.text = label01 + (i % 1000);
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
