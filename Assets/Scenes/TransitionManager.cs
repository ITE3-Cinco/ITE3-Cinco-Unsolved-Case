using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;
using UnityEngine.InputSystem;

/***
 * TransitionManager.cs
 * This script manages the transition from the splash screen to the main menu in a Unity project.
 * It handles video playback, fade transitions, and user input for skipping the splash screen.
    * Usage:
    * 1. Attach this script to a GameObject in your scene (e.g., an empty GameObject named "TransitionManager").
    * 2. Assign the required references in the Inspector:
    *    - Splash Video Player: The VideoPlayer component that plays the splash video.
    *    - Splash Canvas Group: The CanvasGroup component attached to the splash screen canvas.
    *    - Menu Video Player: The VideoPlayer component that plays the menu background video.
    *    - Menu Canvas Group: The CanvasGroup component attached to the main menu canvas.
    *    - Menu Raw Image: The RawImage component that displays the menu video.
    *    - Black Screen Canvas Group: The CanvasGroup component attached to the black overlay panel.
    * 3. Configure the transition timings and settings as needed in the Inspector.
**/

public class TransitionManager : MonoBehaviour
{
    [Header("Splash Screen References")]
    [Tooltip("The Video Player playing the splash video.")]
    [SerializeField] private VideoPlayer splashVideoPlayer;

    [Tooltip("The Canvas Group attached to your Splash Screen Canvas.")]
    [SerializeField] private CanvasGroup splashCanvasGroup;

    [Header("Main Menu References")]
    [Tooltip("The Video Player playing the menu background video.")]
    [SerializeField] private VideoPlayer menuVideoPlayer;

    [Tooltip("The Canvas Group attached to your Main Menu Canvas.")]
    [SerializeField] private CanvasGroup menuCanvasGroup;

    [Tooltip("The Raw Image component displaying the menu video.")]
    [SerializeField] private RawImage menuRawImage;

    [Header("Black Screen Overlay Buffer")]
    [Tooltip("The Canvas Group attached to TransitionCanvas (the black panel).")]
    [SerializeField] private CanvasGroup blackScreenCanvasGroup;

    [Header("Transition Timings")]
    [Tooltip("How fast the splash screen fades out to black (in seconds).")]
    [SerializeField] private float fadeOutSplashTime = 0.8f;

    [Tooltip("How fast the black overlay dissolves to reveal the menu (in seconds).")]
    [SerializeField] private float fadeOutBlackTime = 0.8f;

    [Header("Settings")]
    [Tooltip("Allow the player to press Space or Click to skip the splash video.")]
    [SerializeField] private bool allowSkip = true;

    private bool isTransitioning = false;
    private float playbackDeadline;

    private void Awake()
    {
        // Keep the menu ready behind the black overlay.
        if (menuCanvasGroup != null)
        {
            menuCanvasGroup.alpha = 1f; // Menu is fully visible behind the black fade layer.
            menuCanvasGroup.interactable = false;
            menuCanvasGroup.blocksRaycasts = false;
        }

        // Hide the menu video until the transition begins.
        if (menuRawImage != null)
            menuRawImage.enabled = false;

        // Start with the splash visible.
        if (splashCanvasGroup != null)
            splashCanvasGroup.alpha = 1f; // Splash screen starts fully opaque.

        // Block the scene until the transition starts.
        if (blackScreenCanvasGroup != null)
            blackScreenCanvasGroup.alpha = 1f; // Black overlay starts fully opaque so the scene is hidden.
    }

    private void Start()
    {
        // Prepare this first: a missing/failed splash may start the crossfade immediately.
        if (menuVideoPlayer != null)
            menuVideoPlayer.Prepare();

        // Set up the splash video player.
        if (splashVideoPlayer != null)
        {
            splashVideoPlayer.loopPointReached += OnSplashVideoEnded;
            splashVideoPlayer.prepareCompleted += OnSplashPrepared;
            splashVideoPlayer.errorReceived += OnSplashError;
            splashVideoPlayer.timeUpdateMode = VideoTimeUpdateMode.UnscaledGameTime;
            playbackDeadline = Time.realtimeSinceStartup + 15f;
            splashVideoPlayer.Prepare();
        }
        else TriggerTransition();

    }

    private void Update()
    {
        if (SceneTransition.IsLoading) return;
        if (!isTransitioning && splashVideoPlayer != null && splashVideoPlayer.frame < 0 &&
            Time.realtimeSinceStartup >= playbackDeadline)
            TriggerTransition();
        // Skip check for the input system.
        if (allowSkip && !isTransitioning)
        {
            // Check for spacebar press or mouse click using the new Input System.
            bool spacePressed = Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame;
            // Check for mouse click using the new Input System.
            bool mouseClicked = Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame;

            // If either input is detected, trigger the transition.
            if (spacePressed || mouseClicked)
            {
                // Stop the splash video if it's still playing.
                TriggerTransition();
            }
        }
    }

    private void OnSplashPrepared(VideoPlayer player)
    {
        if (!isTransitioning) player.Play();
    }

    private void OnSplashError(VideoPlayer player, string message)
    {
        Debug.LogWarning("[TransitionManager] Splash unavailable: " + message);
        TriggerTransition();
    }

    // Callback for when the splash video ends naturally.
    private void OnSplashVideoEnded(VideoPlayer vp)
    {
        // Trigger the transition when the splash video finishes playing.
        TriggerTransition();
    }

    // Initiates the transition from the splash screen to the main menu.
    private void TriggerTransition()
    {
        // Prevent multiple transitions from being triggered simultaneously.
        if (isTransitioning)
            return; // Already transitioning, ignore further triggers.

        // Stop the splash video if it's still playing.
        isTransitioning = true;
        StartCoroutine(PerformCrossfade()); // Start the crossfade coroutine to handle the transition.
    }

    // Coroutine to handle the crossfade transition between the splash screen and the main menu.
    private IEnumerator PerformCrossfade()
    {
        // Enable and play the menu background video under the black overlay.
        if (menuVideoPlayer != null)
        {
            // Ensure the menu video is visible before starting playback.
            if (menuRawImage != null)
                menuRawImage.enabled = true; // Show the menu video.

            // Start playing the menu video.
            menuVideoPlayer.Play();
        }

        // Fade out the splash screen to reveal the black overlay.
        float timer = 0f;
        while (timer < fadeOutSplashTime)
        {
            timer += Time.unscaledDeltaTime; // Increment the timer based on the time elapsed since the last frame.

            // Gradually reduce the alpha of the splash canvas group to create a fade-out effect.
            if (splashCanvasGroup != null)
            {
                // Calculate the new alpha value based on the elapsed time and fade duration.
                splashCanvasGroup.alpha = 1f - Mathf.Clamp01(timer / fadeOutSplashTime); // Fade splash from 1 to 0 over time.
            }

            yield return null; // Wait for the next frame before continuing the loop, allowing the fade effect to be smooth over time.
        }

        // Turn off the splash screen completely.
        if (splashCanvasGroup != null)
        {
            splashCanvasGroup.alpha = 0f; // Splash is now fully transparent/invisible.
            splashCanvasGroup.blocksRaycasts = false;
            splashCanvasGroup.interactable = false;
        }

        // Turn off the splash video player to free up resources.
        if (splashVideoPlayer != null)
        {
            splashVideoPlayer.Stop();
            splashVideoPlayer.gameObject.SetActive(false); // Deactivate the splash video player GameObject.
        }

        // Fade out the black overlay to reveal the menu video.
        timer = 0f;
        while (timer < fadeOutBlackTime)
        {
            timer += Time.unscaledDeltaTime; // Increment the timer based on the time elapsed since the last frame.

            // Gradually reduce the alpha of the black screen canvas group to create a fade-out effect.
            if (blackScreenCanvasGroup != null)
            {
                // Calculate the new alpha value based on the elapsed time and fade duration.
                blackScreenCanvasGroup.alpha = 1f - Mathf.Clamp01(timer / fadeOutBlackTime); // Fade black overlay from 1 to 0 so menu becomes visible.
            }

            yield return null; // Wait for the next frame before continuing the loop, allowing the fade effect to be smooth over time.
        }

        // Turn off the black overlay so buttons can receive clicks.
        if (blackScreenCanvasGroup != null)
        {
            blackScreenCanvasGroup.alpha = 0f; // Black overlay is fully transparent and no longer blocks interaction.
            blackScreenCanvasGroup.gameObject.SetActive(false); // Deactivate the black screen canvas group GameObject to allow interaction with the menu.
        }
        if (menuCanvasGroup != null)
        {
            menuCanvasGroup.interactable = true;
            menuCanvasGroup.blocksRaycasts = true;
        }
    }

    // Clean up event subscriptions when the object is destroyed to prevent memory leaks.
    private void OnDestroy()
    {
        // Unsubscribe from the splash video player's loop point reached event to avoid potential null reference exceptions.
        if (splashVideoPlayer != null)
        {
            // Remove the event handler to prevent it from being called after the object is destroyed.
            splashVideoPlayer.loopPointReached -= OnSplashVideoEnded;
            splashVideoPlayer.prepareCompleted -= OnSplashPrepared;
            splashVideoPlayer.errorReceived -= OnSplashError;
        }
    }
}
