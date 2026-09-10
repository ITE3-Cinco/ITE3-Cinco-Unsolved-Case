using System.Collections;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal; // Use HDRP if your project uses High Def pipeline

public class WakeUpBlinker : MonoBehaviour
{
    [Header("Eyelid UI Rects")]
    [SerializeField] private RectTransform topLid;
    [SerializeField] private RectTransform bottomLid;

    [Header("Animation Settings")]
    [SerializeField] private float delayBeforeBlink = 0.5f;

    // Post processing blur parameters
    private Volume sceneVolume;
    private DepthOfField depthOfField;

    private void Start()
    {
        // Find the Global Volume in the PoliceHQ scene to control eye blur
        sceneVolume = FindAnyObjectByType<Volume>();
        if (sceneVolume != null && sceneVolume.profile != null)
        {
            sceneVolume.profile.TryGet(out depthOfField);
        }

        // FORCE PITCH BLACK ON START: Ensure eyelids are completely meeting in the center immediately
        topLid.anchoredPosition = new Vector2(topLid.anchoredPosition.x, 0f);
        bottomLid.anchoredPosition = new Vector2(bottomLid.anchoredPosition.x, 0f);

        // Start the wake up process immediately when scene loads
        StartCoroutine(WakeUpRoutine());
    }

    private IEnumerator WakeUpRoutine()
    {
        // Force vision to be extremely blurry while eyes are closed/waking up
        SetBlurAmount(0.1f); 

        // 1. Wait a moment in pitch blackness for a tense atmosphere
        yield return new WaitForSeconds(delayBeforeBlink);

        // 2. FIRST BLINK: Eyes open slightly, then shut heavy
        yield return StartCoroutine(AnimateLidsAndBlur(200f, 0.1f, 0.5f)); // Open slightly, blur stays high
        yield return StartCoroutine(AnimateLidsAndBlur(0f, 0.1f, 0.2f));   // Snap shut completely
        yield return new WaitForSeconds(0.4f);

        // 3. SECOND BLINK: Open wider, vision begins to slightly clear up
        yield return StartCoroutine(AnimateLidsAndBlur(400f, 1.5f, 0.6f)); // Eyes open mid-way, blur starts lifting
        yield return StartCoroutine(AnimateLidsAndBlur(150f, 0.8f, 0.3f)); // Heavy groop down, eyes focus drops again
        yield return new WaitForSeconds(0.3f);

        // 4. FINAL WAKE UP: Eyelids exit screen completely, vision clears to crystal sharp perfect focus
        float screenHeight = Screen.height;
        yield return StartCoroutine(AnimateLidsAndBlur(screenHeight, 10f, 1.2f)); // 10f focus distance means completely sharp vision

        // Disable post-processing blur overrides completely so gameplay is completely unaffected
        if (depthOfField != null)
        {
            depthOfField.focusDistance.overrideState = false;
        }

        // Disable the canvas to save runtime performance
        gameObject.SetActive(false);
    }

    // Animate eyelids and blur focus distance over time
    private IEnumerator AnimateLidsAndBlur(float targetOffset, float targetFocusDistance, float duration)
    {
        // Store initial positions and focus distance
        float time = 0;

        // Store initial positions of the eyelids
        Vector2 topStart = topLid.anchoredPosition;
        Vector2 bottomStart = bottomLid.anchoredPosition;

        // Calculate target positions for the eyelids based on the target offset
        Vector2 topTarget = new Vector2(topStart.x, targetOffset);
        Vector2 bottomTarget = new Vector2(bottomStart.x, -targetOffset);

        // Store the initial focus distance of the depth of field effect
        float startFocus = depthOfField != null ? depthOfField.focusDistance.value : 10f;

        // Animate over the specified duration
        while (time < duration)
        {
            // Increment time based on the frame's delta time
            time += Time.deltaTime;
            float t = Mathf.SmoothStep(0, 1, time / duration);
            
            // Move eyelids smoothly
            topLid.anchoredPosition = Vector2.Lerp(topStart, topTarget, t);
            bottomLid.anchoredPosition = Vector2.Lerp(bottomStart, bottomTarget, t);
            
            // Linear interpolate the blur focus distance smoothly
            if (depthOfField != null)
            {
                // Ensure the Depth of Field effect is enabled and set to the desired focus distance
                depthOfField.focusDistance.overrideState = true;
                depthOfField.focusDistance.value = Mathf.Lerp(startFocus, targetFocusDistance, t);
            }

            // Wait for the next frame before continuing the loop
            yield return null;
        }

        // Ensure final positions and focus distance are set precisely
        topLid.anchoredPosition = topTarget;
        bottomLid.anchoredPosition = bottomTarget;
        SetBlurAmount(targetFocusDistance); // Ensure final focus distance is set precisely
    }

    // Helper method to set the blur focus distance
    private void SetBlurAmount(float focusDistanceValue)
    {
        // Ensure the Depth of Field effect is enabled and set to the desired focus distance
        if (depthOfField != null)
        {
            depthOfField.focusDistance.overrideState = true;
            depthOfField.focusDistance.value = focusDistanceValue;
        }
    }
}