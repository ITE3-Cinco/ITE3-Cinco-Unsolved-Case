// Purpose: Demonstrates creating and updating a TextMesh Pro counter.
// Use: Attach to an object in a text example scene.
// How it works: Create and format a text component, then increase the displayed count every frame.
// Context: bundled TextMesh Pro example; these notes explain its sample behavior.

// UnityEngine supplies GameObjects, components, transforms, time, math and physics used here.
using UnityEngine;
// System.Collections supplies IEnumerator so coroutines can pause and resume across frames.
using System.Collections;


namespace TMPro.Examples
{

    public class SimpleScript : MonoBehaviour
    {

        // m_textMeshPro: text mesh pro. Start retrieves it from a TextMeshPro component attached to the GameObject
        // that owns this component.
        private TextMeshPro m_textMeshPro;
        //private TMP_FontAsset m_FontAsset;

        // label: label. Fixed script constant; callers cannot change it.
        private const string label = "The <#0050FF>count is: </color>{0:2}";
        // m_frame: frame. Updated by Update.
        private float m_frame;


        // When the example starts, create and format a text component, then increase the displayed count every frame.
        void Start()
        {
            // Add new TextMesh Pro Component
            // Store a TextMeshPro component attached to the GameObject that owns this component in m_textMeshPro.
            m_textMeshPro = gameObject.AddComponent<TextMeshPro>();

            // Set m_textMeshPro.autoSizeTextContainer to true.
            m_textMeshPro.autoSizeTextContainer = true;

            // Load the Font Asset to be used.
            //m_FontAsset = Resources.Load("Fonts & Materials/LiberationSans SDF", typeof(TMP_FontAsset)) as TMP_FontAsset;
            //m_textMeshPro.font = m_FontAsset;

            // Assign Material to TextMesh Pro Component
            //m_textMeshPro.fontSharedMaterial = Resources.Load("Fonts & Materials/LiberationSans SDF - Bevel", typeof(Material)) as Material;
            //m_textMeshPro.fontSharedMaterial.EnableKeyword("BEVEL_ON");

            // Set various font settings.
            // Set m_textMeshPro.fontSize to 48.
            m_textMeshPro.fontSize = 48;

            // Set m_textMeshPro.alignment to TextAlignmentOptions.Center.
            m_textMeshPro.alignment = TextAlignmentOptions.Center;

            //m_textMeshPro.anchorDampening = true; // Has been deprecated but under consideration for re-implementation.
            //m_textMeshPro.enableAutoSizing = true;

            //m_textMeshPro.characterSpacing = 0.2f;
            //m_textMeshPro.wordSpacing = 0.1f;

            //m_textMeshPro.enableCulling = true;
            // Set m_textMeshPro.textWrappingMode to TextWrappingModes.NoWrap.
            m_textMeshPro.textWrappingMode = TextWrappingModes.NoWrap;

            //textMeshPro.fontColor = new Color32(255, 255, 255, 255);
        }


        // Increase the stored counter and format it into the displayed label each frame.
        void Update()
        {
            // Call m_textMeshPro.SetText using label, m_frame modulo (remainder after dividing by) 1000.
            m_textMeshPro.SetText(label, m_frame % 1000);
            // Add 1 multiplied by Unity gameplay frame duration in seconds to m_frame and store the updated value
            // there.
            m_frame += 1 * Time.deltaTime;
        }

    }
}
