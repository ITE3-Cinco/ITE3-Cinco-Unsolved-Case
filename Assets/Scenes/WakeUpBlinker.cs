using System.Collections;
using UnityEngine;

[RequireComponent(typeof(CanvasGroup))]
public class WakeUpBlinker : MonoBehaviour
{
    [Header("Eyelid UI Rects")]
    [SerializeField] private RectTransform topLid;
    [SerializeField] private RectTransform bottomLid;

    [Header("Blurred Backdrop UI")]
    [Tooltip("Drag your BlurredBackgroundLayer UI Image object here!")]
    [SerializeField] private CanvasGroup blurCanvasGroup;

    [Header("Animation Settings")]
    [SerializeField] private float delayBeforeBlink = 0.8f;

    private CanvasGroup masterCanvasGroup;

    private void Awake()
    {
        masterCanvasGroup = GetComponent<CanvasGroup>();
        
        // SECURE PITCH BLACK ON FRAME 1
        masterCanvasGroup.alpha = 1f;
        if (blurCanvasGroup != null) blurCanvasGroup.alpha = 1f;

        topLid.anchoredPosition = new Vector2(topLid.anchoredPosition.x, 0f);
        bottomLid.anchoredPosition = new Vector2(bottomLid.anchoredPosition.x, 0f);
    }

    private void Start()
    {
        // Run the smooth cinematic blink routine natively
        StartCoroutine(WakeUpRoutine());
    }

    private IEnumerator WakeUpRoutine()
    {
        // 1. Chill in total pitch darkness
        yield return new WaitForSeconds(delayBeforeBlink);

        // 2. FIRST BLINK: Eyelids open slightly, revealing the heavy blurred snapshot
        yield return StartCoroutine(AnimateBlinkStep(220f, 1.0f, 0.5f));  
        yield return StartCoroutine(AnimateBlinkStep(0f, 1.0f, 0.2f));    
        yield return new WaitForSeconds(0.4f);                           

        // 3. SECOND BLINK: Open wider, background begins to slightly clear up
        yield return StartCoroutine(AnimateBlinkStep(500f, 0.5f, 0.6f));  
        yield return StartCoroutine(AnimateBlinkStep(150f, 0.7f, 0.25f)); 
        yield return new WaitForSeconds(0.2f);

        // 4. FINAL OPEN: Eyelids open fully past borders while blurred texture fades out completely
        float elapsedTime = 0f;
        float duration = 1.4f;

        // 5. Animate the eyelids opening fully and fade out the blurred background
        Vector2 startTopPos = topLid.anchoredPosition; // Current position of the top eyelid
        Vector2 startBottomPos = bottomLid.anchoredPosition; // Current position of the bottom eyelid
        Vector2 targetTopPos = new Vector2(startTopPos.x, 1200f); // Move the top eyelid off-screen upwards
        Vector2 targetBottomPos = new Vector2(startBottomPos.x, -1200f); // Move the bottom eyelid off-screen downwards

        // Store the current alpha of the blurCanvasGroup to ensure smooth transition
        float currentBlurAlpha = blurCanvasGroup != null ? blurCanvasGroup.alpha : 1f;

        // 6. Animate the eyelids and blur fade out over time
        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, elapsedTime / duration);

            // Open the eyelids smoothly
            topLid.anchoredPosition = Vector2.Lerp(startTopPos, targetTopPos, t);
            bottomLid.anchoredPosition = Vector2.Lerp(startBottomPos, targetBottomPos, t);

            // Fade out the pre-blurred background graphic smoothly to expose the real 3D level
            if (blurCanvasGroup != null)
            {
                blurCanvasGroup.alpha = Mathf.Lerp(currentBlurAlpha, 0f, t);
            }

            // Fade the entire canvas out to keep gameplay view completely clear
            masterCanvasGroup.alpha = Mathf.Lerp(1f, 0f, t);

            yield return null; // Wait for the next frame
        }

        // Disable object to completely save processor performance draw calls
        gameObject.SetActive(false);
    }

    // Animate a single blink step with specified eyelid positions and blur opacity over a given duration
    private IEnumerator AnimateBlinkStep(float targetPositionOffset, float targetBlurOpacity, float duration)
    {
        // Store the initial positions of the eyelids and the initial alpha of the blurCanvasGroup
        float elapsedTime = 0f;
        Vector2 startTopPos = topLid.anchoredPosition;
        Vector2 startBottomPos = bottomLid.anchoredPosition;

        // Calculate the target positions for the eyelids based on the specified offset
        Vector2 targetTopPos = new Vector2(startTopPos.x, targetPositionOffset);
        Vector2 targetBottomPos = new Vector2(startBottomPos.x, -targetPositionOffset);
        float startBlurAlpha = blurCanvasGroup != null ? blurCanvasGroup.alpha : 1f;

        // Animate the eyelids and blur opacity over the specified duration
        while (elapsedTime < duration)
        {
            // Increment elapsed time and calculate the interpolation factor
            elapsedTime += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, elapsedTime / duration);

            // Smoothly interpolate the eyelid positions
            topLid.anchoredPosition = Vector2.Lerp(startTopPos, targetTopPos, t);
            bottomLid.anchoredPosition = Vector2.Lerp(startBottomPos, targetBottomPos, t);
            
            // Smoothly interpolate the blur opacity if the blurCanvasGroup is assigned
            if (blurCanvasGroup != null)
            {
                blurCanvasGroup.alpha = Mathf.Lerp(startBlurAlpha, targetBlurOpacity, t);
            }

            yield return null; // Wait for the next frame
        }

        // Ensure final positions and opacity are set precisely at the end of the animation
        topLid.anchoredPosition = targetTopPos;
        bottomLid.anchoredPosition = targetBottomPos;

        // Ensure the blur opacity is set to the target value at the end of the animation
        if (blurCanvasGroup != null) blurCanvasGroup.alpha = targetBlurOpacity;
    }
}
