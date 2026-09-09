using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;

// Manages the menu background video and fade effect.
public class MenuBackgroundManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private VideoPlayer menuVideoPlayer; // Holds the video player component.
    [SerializeField] private RawImage menuRawImage; // Displays the video frame on the UI.
    [SerializeField] private CanvasGroup menuCanvasGroup; // Controls the fade transparency.

    [Header("Settings")]
    [SerializeField] private float fadeInDuration = 1.0f; // Time for the background to fade in.

    private void Awake()
    {
        // Hide the menu background at start so it does not clash with the splash screen.
        if (menuRawImage != null) menuRawImage.enabled = false; // Turn off the video image.
        if (menuCanvasGroup != null) menuCanvasGroup.alpha = 0f; // Start fully transparent.
    }

    private void Start()
    {
        if (menuVideoPlayer != null)
        {
            // Load the video into memory before showing it.
            menuVideoPlayer.prepareCompleted += OnMenuVideoPrepared; // Listen when video is ready.
            menuVideoPlayer.Prepare(); // Prepare the video in the background.
        }
    }

    private void OnMenuVideoPrepared(VideoPlayer vp)
    {
        // The video is ready, so pause it at the first frame.
        vp.Pause(); // Stop it from playing before the menu starts.
    }

    // Call this when the splash screen starts fading out.
    public void StartMenuVideoWithFade()
    {
        if (menuRawImage != null) menuRawImage.enabled = true; // Show the background image.
        if (menuVideoPlayer != null) menuVideoPlayer.Play(); // Start the video playback.

        StartCoroutine(FadeInMenuBG()); // Start the fade-in effect.
    }

    private IEnumerator FadeInMenuBG()
    {
        float timer = 0f; // Counts how long the fade has been running.
        while (timer < fadeInDuration)
        {
            timer += Time.deltaTime; // Increase the timer by frame time.
            if (menuCanvasGroup != null)
            {
                menuCanvasGroup.alpha = Mathf.Clamp01(timer / fadeInDuration); // Fade from 0 to 1 smoothly.
            }
            yield return null; // Wait until the next frame.
        }

        if (menuCanvasGroup != null) menuCanvasGroup.alpha = 1f; // Make sure the image is fully visible.
    }

    private void OnDestroy()
    {
        if (menuVideoPlayer != null)
        {
            menuVideoPlayer.prepareCompleted -= OnMenuVideoPrepared; // Remove the event listener to avoid memory issues.
        }
    }
}