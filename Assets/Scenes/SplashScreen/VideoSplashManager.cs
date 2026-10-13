// Purpose: Prepares and shows the splash video, then fades its overlay away.
// Use: Attach to the splash object and assign VideoPlayer, RawImage and CanvasGroup.
// How it works: Preparation reveals the image; video completion runs a fade that disables the splash object.

// System.Collections supplies IEnumerator so coroutines can pause and resume across frames.
using System.Collections;
// UnityEngine supplies GameObjects, components, transforms, time, math and physics used here.
using UnityEngine;
// Unity UI supplies canvas widgets such as images, sliders and buttons.
using UnityEngine.UI;
// Unity Video supplies movie clips, playback events and the VideoPlayer component.
using UnityEngine.Video;


public class VideoSplashManager : MonoBehaviour
{
    // videoPlayer: VideoPlayer component that prepares and plays the assigned movie. Assigned in the scene/prefab
    // Inspector.
    [Header("References")]
    [SerializeField] private VideoPlayer videoPlayer;
    // splashRawImage: UI RawImage showing the splash video texture. Assigned in the scene/prefab Inspector.
    [SerializeField] private RawImage splashRawImage;
    // splashCanvasGroup: CanvasGroup whose alpha controls splash visibility. Assigned in the scene/prefab
    // Inspector.
    [SerializeField] private CanvasGroup splashCanvasGroup;

    // fadeDuration: seconds allocated to a fade. Assigned in the scene/prefab Inspector; the initializer below is
    // the default.
    [Header("Settings")]
    [SerializeField] private float fadeDuration = 0.5f;

    /// <summary>Hide the splash image until the video has been prepared.</summary>
    private void Awake()
    {
        // Keep image hidden until video starts playing to avoid visual glitches
        // If splashRawImage exists, run the following block.
        if (splashRawImage != null)
        {
            // Disable splashRawImage by changing its component enabled flag.
            splashRawImage.enabled = false;
        }
    }

    /// <summary>Register video callbacks and begin preparation.</summary>
    private void Start()
    {
        // If videoPlayer exists, run the following block.
        if (videoPlayer != null)
        {
            // Subscribe to video events
            // Subscribe OnVideoPrepared to videoPlayer.prepareCompleted so the callback runs when that event is
            // raised.
            videoPlayer.prepareCompleted += OnVideoPrepared;
            // Subscribe OnVideoFinished to videoPlayer.loopPointReached so the callback runs when that event is
            // raised.
            videoPlayer.loopPointReached += OnVideoFinished;

            // Prepare the video stream
            // Ask videoPlayer VideoPlayer to prepare decoding before playback.
            videoPlayer.Prepare();
        }
    }

    /// <summary>Reveal the image and start playback when preparation completes.</summary>
    // VideoPlayer supplies vp as the player that raised this preparation/completion/error event.
    private void OnVideoPrepared(VideoPlayer vp)
    {
        // Reveal raw image once first frame is ready
        // If splashRawImage exists, run the following block.
        if (splashRawImage != null)
        {
            // Enable splashRawImage by changing its component enabled flag.
            splashRawImage.enabled = true;
        }
        // Start/resume playback on vp.
        vp.Play();
    }

    /// <summary>Start hiding the splash after playback reaches its end.</summary>
    // VideoPlayer supplies vp as the player that raised this preparation/completion/error event.
    private void OnVideoFinished(VideoPlayer vp)
    {
        // Start coroutine FadeOutSplash(); Unity resumes it after each yield.
        StartCoroutine(FadeOutSplash());
    }

    /// <summary>Reduce canvas opacity over gameplay frames, then disable the splash object.</summary>
    private IEnumerator FadeOutSplash()
    {
        // Initialize timer to 0.
        float timer = 0f; 
        // Repeat while timer is below fadeDuration.
        while (timer < fadeDuration)
        {
            // Add Unity gameplay frame duration in seconds to timer and store the updated value there.
            timer += Time.deltaTime;
            // If splashCanvasGroup exists, run the following block.
            if (splashCanvasGroup != null)
            {
                // Set splashCanvasGroup.alpha to 1 minus timer / fadeDuration limited to the range 0 to 1.
                splashCanvasGroup.alpha = 1f - Mathf.Clamp01(timer / fadeDuration);
            }
            // Pause the coroutine until the next Unity frame so the animation/sequence can render.
            yield return null;
        }

        // Hide overlay completely to reveal menu behind it
        // Deactivate gameObject.
        gameObject.SetActive(false);
    }

    /// <summary>Unsubscribe from preparation and completion callbacks.</summary>
    private void OnDestroy()
    {
        // If videoPlayer exists, run the following block.
        if (videoPlayer != null)
        {
            // Unsubscribe OnVideoPrepared from videoPlayer.prepareCompleted so the callback stops running when
            // that event is raised.
            videoPlayer.prepareCompleted -= OnVideoPrepared;
            // Unsubscribe OnVideoFinished from videoPlayer.loopPointReached so the callback stops running when
            // that event is raised.
            videoPlayer.loopPointReached -= OnVideoFinished;
        }
    }
}