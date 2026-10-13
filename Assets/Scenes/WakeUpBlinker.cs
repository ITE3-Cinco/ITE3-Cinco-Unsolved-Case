// Purpose: Simulates waking up with two slow eyelid blinks before opening the view.
// Use: Attach to the eyelid canvas and assign the top and bottom black UI rectangles.
// How it works: Screen-relative anchors move the lids so the animation adapts to different resolutions.

// System.Collections supplies IEnumerator so coroutines can pause and resume across frames.
using System.Collections;
// UnityEngine supplies GameObjects, components, transforms, time, math and physics used here.
using UnityEngine;
// Unity UI supplies canvas widgets such as images, sliders and buttons.
using UnityEngine.UI;

/// <summary>Resolution-independent eyelids: black, two slow blinks, then fully open.</summary>
[DefaultExecutionOrder(-100)]
public class WakeUpBlinker : MonoBehaviour
{
    // topLid: upper black RectTransform moved to simulate an eyelid. Assigned in the scene/prefab Inspector.
    [Header("Eyelid UI Rects")]
    [SerializeField] private RectTransform topLid;
    // bottomLid: lower black RectTransform moved to simulate an eyelid. Assigned in the scene/prefab Inspector.
    [SerializeField] private RectTransform bottomLid;

    // delayBeforeBlink: gameplay seconds before the wake-up blink sequence begins. Assigned in the scene/prefab
    // Inspector; the initializer below is the default.
    [Header("Animation Settings")]
    [Min(0f)] [SerializeField] private float delayBeforeBlink = 0.8f;
    // blinkSpeedMultiplier: multiplier for blink progress; larger values advance the lids faster. Assigned in the
    // scene/prefab Inspector; the initializer below is the default.
    [Min(0.1f)] [SerializeField] private float blinkSpeedMultiplier = 1f;
    // openness: eye-opening fraction; zero covers the view and one reveals it. Updated by SetOpenness.
    private float openness;
    // eyelidCanvas: Canvas displaying the two black eyelid images. Awake retrieves it from the first Canvas
    // component on this GameObject.
    private Canvas eyelidCanvas;

    /// <summary>Validate both lids and prepare black images that cover the first rendered frame.</summary>
    private void Awake()
    {
        // If topLid is missing or bottomLid is missing, run the following block.
        if (topLid == null || bottomLid == null)
        {
            // Report the failed setup or state as a Unity Console error.
            Debug.LogError("[WakeUpBlinker] Assign both eyelid images.", this);
            // Set enabled to false.
            enabled = false;
            // Return now; skip the rest of this method.
            return;
        }
        // Store the first Canvas component on this GameObject in eyelidCanvas.
        eyelidCanvas = GetComponent<Canvas>();
        // If eyelidCanvas exists, run the following block.
        if (eyelidCanvas != null)
        {
            // Set eyelidCanvas.renderMode to RenderMode.ScreenSpaceOverlay.
            eyelidCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            // Set eyelidCanvas.sortingOrder to 100.
            eyelidCanvas.sortingOrder = 100;
        }
        // Process each lid in a new array/object initialized with the entries specified here.
        foreach (RectTransform lid in new[] { topLid, bottomLid })
        {
            // Activate lid.gameObject.
            lid.gameObject.SetActive(true);
            // Set lid.localRotation to the identity rotation (no rotation).
            lid.localRotation = Quaternion.identity;
            // Set lid.localScale to a vector with all three components equal to 1.
            lid.localScale = Vector3.one;
            // Store the first Image component on lid in local image.
            Image image = lid.GetComponent<Image>();
            // If image is missing, store a Image component attached to GameObject owning lid in image.
            if (image == null) image = lid.gameObject.AddComponent<Image>();
            // Enable image by changing its component enabled flag.
            image.enabled = true;
            // Clear image.sprite to null so it no longer holds a reference.
            image.sprite = null;
            // Set image.color to Color.black.
            image.color = Color.black;
            // Set image.raycastTarget to false.
            image.raycastTarget = false;
        }
        // Call WakeUpBlinker.SetOpenness with 0; move the upper and lower lid anchors away from the center as
        // openness increases.
        SetOpenness(0f); // Cover the very first rendered frame.
    }

    /// <summary>Hide the eyelid canvas during pause so the pause menu remains readable.</summary>
    private void LateUpdate()
    {
        // Let the pause menu remain readable while the scaled blink is suspended.
        // If eyelidCanvas exists, set eyelidCanvas.enabled to not (PauseManager.IsGamePaused).
        if (eyelidCanvas != null) eyelidCanvas.enabled = !PauseManager.IsGamePaused;
    }

    /// <summary>Run the timed partial openings and closings, then open fully and disable the overlay.</summary>
    private IEnumerator Start()
    {
        // Yield a wait for delayBeforeBlink gameplay seconds (pause stops the timer); Unity resumes the coroutine
        // after it completes.
        yield return new WaitForSeconds(delayBeforeBlink);
        // Partially open and close twice, then fully open to reveal the level.
        // Yield the result of WakeUpBlinker.AnimateLids using 0.22, 1.1; Unity resumes the coroutine after it
        // completes.
        yield return AnimateLids(0.22f, 1.1f);
        // Yield the result of WakeUpBlinker.AnimateLids using 0, 0.55; Unity resumes the coroutine after it
        // completes.
        yield return AnimateLids(0f, 0.55f);
        // Yield a wait for 0.45 gameplay seconds (pause stops the timer); Unity resumes the coroutine after it
        // completes.
        yield return new WaitForSeconds(0.45f);
        // Yield the result of WakeUpBlinker.AnimateLids using 0.55, 1.4; Unity resumes the coroutine after it
        // completes.
        yield return AnimateLids(0.55f, 1.4f);
        // Yield the result of WakeUpBlinker.AnimateLids using 0, 0.65; Unity resumes the coroutine after it
        // completes.
        yield return AnimateLids(0f, 0.65f);
        // Yield a wait for 0.35 gameplay seconds (pause stops the timer); Unity resumes the coroutine after it
        // completes.
        yield return new WaitForSeconds(0.35f);
        // Yield the result of WakeUpBlinker.AnimateLids using 1, 2.4; Unity resumes the coroutine after it
        // completes.
        yield return AnimateLids(1f, 2.4f);
        // Deactivate gameObject.
        gameObject.SetActive(false);
    }

    /// <summary>Blend from current openness to the requested opening over several gameplay frames.</summary>
    // target is a float argument supplied by WakeUpBlinker.Start.
    // duration is a float argument supplied by WakeUpBlinker.Start.
    private IEnumerator AnimateLids(float target, float duration)
    {
        // Keep openness cached by this component in local start.
        float start = openness;
        // Initialize elapsed to 0.
        float elapsed = 0f;
        // Repeat while elapsed is below duration.
        while (elapsed < duration)
        {
            // Add Time.deltaTime * Mathf.Max(0.1f, blinkSpeedMultiplier) (calculated from the values shown) to
            // elapsed and store the updated value there.
            elapsed += Time.deltaTime * Mathf.Max(0.1f, blinkSpeedMultiplier);
            // Call WakeUpBlinker.SetOpenness with a blend from start to target using fraction Mathf.SmoothStep(0f,
            // 1f, elapsed / duration); move the upper and lower lid anchors away from the center as openness
            // increases.
            SetOpenness(Mathf.Lerp(start, target, Mathf.SmoothStep(0f, 1f, elapsed / duration)));
            // Pause the coroutine until the next Unity frame so the animation/sequence can render.
            yield return null;
        }
        // Call WakeUpBlinker.SetOpenness with target; move the upper and lower lid anchors away from the center as
        // openness increases.
        SetOpenness(target);
    }

    /// <summary>Move the upper and lower lid anchors away from the center as openness increases.</summary>
    // value is a float argument supplied by WakeUpBlinker.AnimateLids, WakeUpBlinker.Awake.
    private void SetOpenness(float value)
    {
        // Store value limited to the range 0 to 1 in openness.
        openness = Mathf.Clamp01(value);
        // Anchors are fractions of the canvas, so CanvasScaler and aspect-ratio
        // changes cannot leave the old 100-pixel strips at the screen edges.
        // Store openness multiplied by 0.51 in local shift.
        float shift = openness * 0.51f;
        // Set topLid.anchorMin to a Vector2 built from 0, 0.5 plus shift.
        topLid.anchorMin = new Vector2(0f, 0.5f + shift);
        // Set topLid.anchorMax to a Vector2 built from 1, 1 plus shift.
        topLid.anchorMax = new Vector2(1f, 1f + shift);
        // Set bottomLid.anchorMin to a Vector2 built from 0, negative shift.
        bottomLid.anchorMin = new Vector2(0f, -shift);
        // Set bottomLid.anchorMax to a Vector2 built from 1, 0.5 minus shift.
        bottomLid.anchorMax = new Vector2(1f, 0.5f - shift);
        // Set topLid.offsetMin to a Vector2 built from 0, negative 1.
        topLid.offsetMin = new Vector2(0f, -1f);
        // Set topLid.offsetMax to the zero 2D vector.
        topLid.offsetMax = Vector2.zero;
        // Set bottomLid.offsetMin to the zero 2D vector.
        bottomLid.offsetMin = Vector2.zero;
        // Set bottomLid.offsetMax to a Vector2 built from 0, 1.
        bottomLid.offsetMax = new Vector2(0f, 1f);
    }
}
