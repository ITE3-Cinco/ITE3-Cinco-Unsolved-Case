// Purpose: Shows a shared counter example for world or canvas TextMesh Pro text.
// Use: Choose ObjectType and whether the sample text is static.
// How it works: Find or add the chosen TMP component, then optionally update its counter each frame.
// Context: bundled TextMesh Pro example; these notes explain its sample behavior.

// UnityEngine supplies GameObjects, components, transforms, time, math and physics used here.
using UnityEngine;
// Unity UI supplies canvas widgets such as images, sliders and buttons.
using UnityEngine.UI;
// System.Collections supplies IEnumerator so coroutines can pause and resume across frames.
using System.Collections;
// TextMesh Pro supplies the text components, fonts and text-mesh information used here.
using TMPro;


namespace TMPro.Examples
{

    public class TMP_ExampleScript_01 : MonoBehaviour
    {
        public enum objectType { TextMeshPro = 0, TextMeshProUGUI = 1 };

        // ObjectType: object type. Assigned in the scene/prefab Inspector.
        public objectType ObjectType;
        // isStatic: is static. Assigned in the scene/prefab Inspector.
        public bool isStatic;

        // m_text: text. Updated by Awake.
        private TMP_Text m_text;

        //private TMP_InputField m_inputfield;


        // k_label: k label. Fixed script constant; callers cannot change it.
        private const string k_label = "The count is <#0080ff>{0}</color>";
        // count: count. Updated by Update.
        private int count;

        // Before the example starts, cache its components and prepare the starting display.
        void Awake()
        {
            // Get a reference to the TMP text component if one already exists otherwise add one.
            // This example show the convenience of having both TMP components derive from TMP_Text. 
            // If ObjectType equals 0, run the following block.
            if (ObjectType == 0)
                // Recalculate m_text from the first TextMeshPro component on this GameObject or, if missing, a
                // TextMeshPro component attached to the GameObject that owns this component.
                m_text = GetComponent<TextMeshPro>() ?? gameObject.AddComponent<TextMeshPro>();
            else
                // Recalculate m_text from the first TextMeshProUGUI component on this GameObject or, if missing, a
                // TextMeshProUGUI component attached to the GameObject that owns this component.
                m_text = GetComponent<TextMeshProUGUI>() ?? gameObject.AddComponent<TextMeshProUGUI>();

            // Load a new font asset and assign it to the text object.
            // Store the TMP_FontAsset asset from a Resources folder at "Fonts & Materials/Anton SDF" in
            // m_text.font.
            m_text.font = Resources.Load<TMP_FontAsset>("Fonts & Materials/Anton SDF");

            // Load a new material preset which was created with the context menu duplicate.
            // Store the Material asset from a Resources folder at "Fonts & Materials/Anton SDF - Drop Shadow" in
            // m_text.fontSharedMaterial.
            m_text.fontSharedMaterial = Resources.Load<Material>("Fonts & Materials/Anton SDF - Drop Shadow");

            // Set the size of the font.
            // Set m_text.fontSize to 120.
            m_text.fontSize = 120;

            // Set the text
            // Display "A <#0080ff>simple</color> line of text." in m_text.text (the assigned TMP/UI text
            // component).
            m_text.text = "A <#0080ff>simple</color> line of text.";

            // Get the preferred width and height based on the supplied width and height as opposed to the actual size of the current text container.
            // Store the width/height m_text needs within the supplied size constraints in local size.
            Vector2 size = m_text.GetPreferredValues(Mathf.Infinity, Mathf.Infinity);

            // Set the size of the RectTransform based on the new calculated values.
            // Set m_text.rectTransform.sizeDelta to a Vector2 built from size X component, size Y component.
            m_text.rectTransform.sizeDelta = new Vector2(size.x, size.y);
        }


        // Unless isStatic is enabled, show an increasing counter that repeats from 0 to 999.
        void Update()
        {
            // If not (isStatic), run the following block.
            if (!isStatic)
            {
                // Call m_text.SetText using k_label, count modulo (remainder after dividing by) 1000.
                m_text.SetText(k_label, count % 1000);
                // Add 1 to count and store the updated value there.
                count += 1;
            }
        }

    }
}
