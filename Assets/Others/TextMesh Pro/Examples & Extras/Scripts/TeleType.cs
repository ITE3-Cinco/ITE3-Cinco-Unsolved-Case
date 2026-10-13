// Purpose: Demonstrates revealing sample text one character at a time.
// Use: Attach to TMP text in an example scene.
// How it works: Keep the full text and increase maxVisibleCharacters with a short delay between steps.
// Context: bundled TextMesh Pro example; these notes explain its sample behavior.

// UnityEngine supplies GameObjects, components, transforms, time, math and physics used here.
using UnityEngine;
// System.Collections supplies IEnumerator so coroutines can pause and resume across frames.
using System.Collections;


namespace TMPro.Examples
{

    public class TeleType : MonoBehaviour
    {


        //[Range(0, 100)]
        //public int RevealSpeed = 50;

        // label01: label01. Initialized from the message/string written here.
        private string label01 = "Example <sprite=2> of using <sprite=7> <#ffa000>Graphics Inline</color> <sprite=5> with Text in <font=\"Bangers SDF\" material=\"Bangers SDF - Drop Shadow\">TextMesh<#40a0ff>Pro</color></font><sprite=0> and Unity<sprite=1>";
        // label02: label02. Initialized from the message/string written here.
        private string label02 = "Example <sprite=2> of using <sprite=7> <#ffa000>Graphics Inline</color> <sprite=5> with Text in <font=\"Bangers SDF\" material=\"Bangers SDF - Drop Shadow\">TextMesh<#40a0ff>Pro</color></font><sprite=0> and Unity<sprite=2>";


        // m_textMeshPro: text mesh pro. Awake retrieves it from the first TMP_Text component on this GameObject.
        private TMP_Text m_textMeshPro;


        // Prepare sample text and its wrapping and alignment.
        void Awake()
        {
            // Get Reference to TextMeshPro Component
            // Store the first TMP_Text component on this GameObject in m_textMeshPro.
            m_textMeshPro = GetComponent<TMP_Text>();
            // Copy label01 cached by this component into m_textMeshPro.text.
            m_textMeshPro.text = label01;
            // Set m_textMeshPro.textWrappingMode to TextWrappingModes.Normal.
            m_textMeshPro.textWrappingMode = TextWrappingModes.Normal;
            // Set m_textMeshPro.alignment to TextAlignmentOptions.Top.
            m_textMeshPro.alignment = TextAlignmentOptions.Top;



            //if (GetComponentInParent(typeof(Canvas)) as Canvas == null)
            //{
            //    GameObject canvas = new GameObject("Canvas", typeof(Canvas));
            //    gameObject.transform.SetParent(canvas.transform);
            //    canvas.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;

            //    // Set RectTransform Size
            //    gameObject.GetComponent<RectTransform>().sizeDelta = new Vector2(500, 300);
            //    m_textMeshPro.fontSize = 48;
            //}


        }


        // Reveal one character at a time, wait, then repeat with the other sample text.
        IEnumerator Start()
        {

            // Force and update of the mesh to get valid information.
            // Regenerate m_textMeshPro text mesh now so character and vertex information is available.
            m_textMeshPro.ForceMeshUpdate();


            // Store m_textMeshPro.textInfo.characterCount in local totalVisibleCharacters.
            int totalVisibleCharacters = m_textMeshPro.textInfo.characterCount; // Get # of Visible Character in text object
            // Initialize counter to 0.
            int counter = 0;
            // Initialize visibleCount to 0.
            int visibleCount = 0;

            // Repeat while true.
            while (true)
            {
                // Set visibleCount to counter modulo (remainder after dividing by) (totalVisibleCharacters plus
                // 1).
                visibleCount = counter % (totalVisibleCharacters + 1);

                // Copy visibleCount into m_textMeshPro.maxVisibleCharacters.
                m_textMeshPro.maxVisibleCharacters = visibleCount; // How many characters should TextMeshPro display?

                // Once the last character has been revealed, wait 1.0 second and start over.
                // If visibleCount is at least totalVisibleCharacters, run the following block.
                if (visibleCount >= totalVisibleCharacters)
                {
                    // Yield a wait for 1.0 gameplay seconds (pause stops the timer); Unity resumes the coroutine
                    // after it completes.
                    yield return new WaitForSeconds(1.0f);
                    // Copy label02 cached by this component into m_textMeshPro.text.
                    m_textMeshPro.text = label02;
                    // Yield a wait for 1.0 gameplay seconds (pause stops the timer); Unity resumes the coroutine
                    // after it completes.
                    yield return new WaitForSeconds(1.0f);
                    // Copy label01 cached by this component into m_textMeshPro.text.
                    m_textMeshPro.text = label01;
                    // Yield a wait for 1.0 gameplay seconds (pause stops the timer); Unity resumes the coroutine
                    // after it completes.
                    yield return new WaitForSeconds(1.0f);
                }

                // Add 1 to counter and store the updated value there.
                counter += 1;

                // Yield a wait for 0.05 gameplay seconds (pause stops the timer); Unity resumes the coroutine
                // after it completes.
                yield return new WaitForSeconds(0.05f);
            }

            //Debug.Log("Done revealing the text.");
        }

    }
}