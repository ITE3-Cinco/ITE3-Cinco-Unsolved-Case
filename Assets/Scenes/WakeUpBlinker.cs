using System.Collections; // Provides IEnumerator support for coroutines.
using UnityEngine; // Provides Unity components, UI transforms, and timing utilities.

public class WakeUpBlinker : MonoBehaviour // Controls the wake-up eyelid animation.
{
    [Header("Eyelid UI Rects")] // Groups the eyelid references in the Inspector.
    [SerializeField] private RectTransform topLid; // References the upper eyelid UI element.
    [SerializeField] private RectTransform bottomLid; // References the lower eyelid UI element.

    [Header("Animation Settings")] // Groups animation settings in the Inspector.
    [SerializeField] private float delayBeforeBlink = 0.5f; // Sets the initial darkness duration.

    private void Start() // Runs when the object first becomes active.
    {
        // Start the wake up process immediately when PoliceHQ loads
        StartCoroutine(WakeUpRoutine()); // Begins the eyelid animation sequence.
    }

    private IEnumerator WakeUpRoutine() // Defines the complete wake-up sequence.
    {
        // 1. Wait a moment in pitch blackness for a tense atmosphere
        yield return new WaitForSeconds(delayBeforeBlink); // Pauses before the first blink.

        // 2. FIRST BLINK: Eyes open slightly, then shut heavy (Adjust the second float to change duration)
        yield return StartCoroutine(AnimateLids(200f, 0.4f)); // Open a bit
        yield return StartCoroutine(AnimateLids(0f, 0.2f));   // Snap shut
        yield return new WaitForSeconds(0.3f);                // Stay shut

        // 3. SECOND BLINK: Open wider, shut halfway
        yield return StartCoroutine(AnimateLids(400f, 0.6f)); // Open wider
        yield return StartCoroutine(AnimateLids(150f, 0.3f)); // Heavy droop
        yield return new WaitForSeconds(0.2f);

        // 4. FINAL WAKE UP: Eyelids completely exit the screen based on display resolution
        float screenHeight = Screen.height; // Gets the current display height in pixels.
        yield return StartCoroutine(AnimateLids(screenHeight, 1.0f)); // Moves both lids off-screen.

        // Disable the canvas so it stops drawing to save performance
        gameObject.SetActive(false); // Disables this eyelid object after the animation.
    }

    private IEnumerator AnimateLids(float targetOffset, float duration) // Animates both eyelids to an offset.
    {
        float time = 0; // Tracks elapsed animation time.
        Vector2 topStart = topLid.anchoredPosition; // Stores the upper lid's starting position.
        Vector2 bottomStart = bottomLid.anchoredPosition; // Stores the lower lid's starting position.

        Vector2 topTarget = new Vector2(topStart.x, targetOffset); // Calculates the upper lid's destination.
        Vector2 bottomTarget = new Vector2(bottomStart.x, -targetOffset); // Calculates the lower lid's destination.

        while (time < duration) // Continues until the requested animation duration has elapsed.
        {
            time += Time.deltaTime; // Adds the time since the previous frame.
            float t = Mathf.SmoothStep(0, 1, time / duration); // Smooth out the motion
            
            topLid.anchoredPosition = Vector2.Lerp(topStart, topTarget, t); // Moves the upper lid smoothly.
            bottomLid.anchoredPosition = Vector2.Lerp(bottomStart, bottomTarget, t); // Moves the lower lid smoothly.
            yield return null; // Waits until the next frame.
        }

        topLid.anchoredPosition = topTarget; // Ensures the upper lid reaches its exact destination.
        bottomLid.anchoredPosition = bottomTarget; // Ensures the lower lid reaches its exact destination.
    }
}
