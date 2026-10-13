// Purpose: Demonstrates revealing existing text like a console or typewriter.
// Use: Attach to sample TMP text; Start selects the character reveal routine.
// How it works: Watch for text regeneration and increase visible characters or words over timed steps.
// Context: bundled TextMesh Pro example; these notes explain its sample behavior.

// UnityEngine supplies GameObjects, components, transforms, time, math and physics used here.
using UnityEngine;
// System.Collections supplies IEnumerator so coroutines can pause and resume across frames.
using System.Collections;


namespace TMPro.Examples
{
    public class TextConsoleSimulator : MonoBehaviour
    {
        // m_TextComponent: text component. Awake retrieves it from the first TMP_Text component on the GameObject
        // that owns this component.
        private TMP_Text m_TextComponent;
        // hasTextChanged: has text changed. Updated by ON_TEXT_CHANGED, RevealCharacters.
        private bool hasTextChanged;

        // Before the example starts, cache its components and prepare the starting display.
        void Awake()
        {
            // Store the first TMP_Text component on the GameObject that owns this component in m_TextComponent.
            m_TextComponent = gameObject.GetComponent<TMP_Text>();
        }


        // When the example starts, watch for text regeneration and increase visible characters or words over timed steps.
        void Start()
        {
            // Start coroutine RevealCharacters(m_TextComponent); Unity resumes it after each yield.
            StartCoroutine(RevealCharacters(m_TextComponent));
            //StartCoroutine(RevealWords(m_TextComponent));
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


        // Mark text geometry for refreshing when a text-change event is received.
        // Event received when the text object has changed.
        // obj is a Object argument supplied by the code, event or Inspector callback invoking ON_TEXT_CHANGED.
        void ON_TEXT_CHANGED(Object obj)
        {
            // Set hasTextChanged to true.
            hasTextChanged = true;
        }


        // Increase the visible character count in timed steps, refreshing when text changes.
        /// <summary>
        /// Method revealing the text one character at a time.
        /// </summary>
        /// <returns></returns>
        // textComponent is a TMP_Text argument supplied by TextConsoleSimulator.Start.
        IEnumerator RevealCharacters(TMP_Text textComponent)
        {
            // Regenerate textComponent text mesh now so character and vertex information is available.
            textComponent.ForceMeshUpdate();

            // Store textComponent generated TMP character/line/mesh information in local textInfo.
            TMP_TextInfo textInfo = textComponent.textInfo;

            // Store textInfo.characterCount in local totalVisibleCharacters.
            int totalVisibleCharacters = textInfo.characterCount; // Get # of Visible Character in text object
            // Initialize visibleCount to 0.
            int visibleCount = 0;

            // Repeat while true.
            while (true)
            {
                // If hasTextChanged, run the following block.
                if (hasTextChanged)
                {
                    // Set totalVisibleCharacters to textInfo.characterCount.
                    totalVisibleCharacters = textInfo.characterCount; // Update visible character count.
                    // Set hasTextChanged to false.
                    hasTextChanged = false; 
                }

                // If visibleCount is above totalVisibleCharacters, run the following block.
                if (visibleCount > totalVisibleCharacters)
                {
                    // Yield a wait for 1.0 gameplay seconds (pause stops the timer); Unity resumes the coroutine
                    // after it completes.
                    yield return new WaitForSeconds(1.0f);
                    // Set visibleCount to 0.
                    visibleCount = 0;
                }

                // Copy visibleCount into textComponent.maxVisibleCharacters.
                textComponent.maxVisibleCharacters = visibleCount; // How many characters should TextMeshPro display?

                // Add 1 to visibleCount and store the updated value there.
                visibleCount += 1;

                // Pause the coroutine until the next Unity frame so the animation/sequence can render.
                yield return null;
            }
        }


        // Increase the visible range one word at a time using the generated word information.
        /// <summary>
        /// Method revealing the text one word at a time.
        /// </summary>
        /// <returns></returns>
        // textComponent is a TMP_Text argument supplied by the code, event or Inspector callback invoking
        // RevealWords.
        IEnumerator RevealWords(TMP_Text textComponent)
        {
            // Regenerate textComponent text mesh now so character and vertex information is available.
            textComponent.ForceMeshUpdate();

            // Store textComponent.textInfo.wordCount in local totalWordCount.
            int totalWordCount = textComponent.textInfo.wordCount;
            // Store textComponent.textInfo.characterCount in local totalVisibleCharacters.
            int totalVisibleCharacters = textComponent.textInfo.characterCount; // Get # of Visible Character in text object
            // Initialize counter to 0.
            int counter = 0;
            // Initialize currentWord to 0.
            int currentWord = 0;
            // Initialize visibleCount to 0.
            int visibleCount = 0;

            // Repeat while true.
            while (true)
            {
                // Set currentWord to counter modulo (remainder after dividing by) (totalWordCount plus 1).
                currentWord = counter % (totalWordCount + 1);

                // Get last character index for the current word.
                // If currentWord equals 0, run the following block.
                if (currentWord == 0) // Display no words.
                    // Set visibleCount to 0.
                    visibleCount = 0;
                else if (currentWord < totalWordCount) // Display all other words with the exception of the last one.
                    // Set visibleCount to textComponent.textInfo.wordInfo[currentWord - 1].lastCharacterIndex + 1
                    // (calculated from the values shown).
                    visibleCount = textComponent.textInfo.wordInfo[currentWord - 1].lastCharacterIndex + 1;
                else if (currentWord == totalWordCount) // Display last word and all remaining characters.
                    // Copy totalVisibleCharacters into visibleCount.
                    visibleCount = totalVisibleCharacters;

                // Copy visibleCount into textComponent.maxVisibleCharacters.
                textComponent.maxVisibleCharacters = visibleCount; // How many characters should TextMeshPro display?

                // Once the last character has been revealed, wait 1.0 second and start over.
                // If visibleCount is at least totalVisibleCharacters, run the following block.
                if (visibleCount >= totalVisibleCharacters)
                {
                    // Yield a wait for 1.0 gameplay seconds (pause stops the timer); Unity resumes the coroutine
                    // after it completes.
                    yield return new WaitForSeconds(1.0f);
                }

                // Add 1 to counter and store the updated value there.
                counter += 1;

                // Yield a wait for 0.1 gameplay seconds (pause stops the timer); Unity resumes the coroutine after
                // it completes.
                yield return new WaitForSeconds(0.1f);
            }
        }

    }
}