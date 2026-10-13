// Purpose: Moves from the splash video into the menu background with fades.
// Use: Attach in the menu scene and assign both video players, canvases and the black overlay.
// How it works: Video end or Space/click starts one sequence that hides the splash and reveals the menu.

// System.Collections supplies IEnumerator so coroutines can pause and resume across frames.
using System.Collections;
// UnityEngine supplies GameObjects, components, transforms, time, math and physics used here.
using UnityEngine;
// Unity UI supplies canvas widgets such as images, sliders and buttons.
using UnityEngine.UI;
// Unity Video supplies movie clips, playback events and the VideoPlayer component.
using UnityEngine.Video;
// Unity Input System supplies the current keyboard/mouse and their key or pointer states.
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
    // splashVideoPlayer: The Video Player playing the splash video. Assigned in the scene/prefab Inspector.
    [Header("Splash Screen References")]
    [Tooltip("The Video Player playing the splash video.")]
    [SerializeField] private VideoPlayer splashVideoPlayer;

    // splashCanvasGroup: The Canvas Group attached to your Splash Screen Canvas. Assigned in the scene/prefab
    // Inspector.
    [Tooltip("The Canvas Group attached to your Splash Screen Canvas.")]
    [SerializeField] private CanvasGroup splashCanvasGroup;

    // menuVideoPlayer: The Video Player playing the menu background video. Assigned in the scene/prefab Inspector.
    [Header("Main Menu References")]
    [Tooltip("The Video Player playing the menu background video.")]
    [SerializeField] private VideoPlayer menuVideoPlayer;

    // menuCanvasGroup: The Canvas Group attached to your Main Menu Canvas. Assigned in the scene/prefab Inspector.
    [Tooltip("The Canvas Group attached to your Main Menu Canvas.")]
    [SerializeField] private CanvasGroup menuCanvasGroup;

    // menuRawImage: The Raw Image component displaying the menu video. Assigned in the scene/prefab Inspector.
    [Tooltip("The Raw Image component displaying the menu video.")]
    [SerializeField] private RawImage menuRawImage;

    // blackScreenCanvasGroup: The Canvas Group attached to TransitionCanvas (the black panel). Assigned in the
    // scene/prefab Inspector.
    [Header("Black Screen Overlay Buffer")]
    [Tooltip("The Canvas Group attached to TransitionCanvas (the black panel).")]
    [SerializeField] private CanvasGroup blackScreenCanvasGroup;

    // fadeOutSplashTime: How fast the splash screen fades out to black (in seconds). Assigned in the scene/prefab
    // Inspector; the initializer below is the default.
    [Header("Transition Timings")]
    [Tooltip("How fast the splash screen fades out to black (in seconds).")]
    [SerializeField] private float fadeOutSplashTime = 0.8f;

    // fadeOutBlackTime: How fast the black overlay dissolves to reveal the menu (in seconds). Assigned in the
    // scene/prefab Inspector; the initializer below is the default.
    [Tooltip("How fast the black overlay dissolves to reveal the menu (in seconds).")]
    [SerializeField] private float fadeOutBlackTime = 0.8f;

    // allowSkip: Allow the player to press Space or Click to skip the splash video. Assigned in the scene/prefab
    // Inspector; the initializer below is the default.
    [Header("Settings")]
    [Tooltip("Allow the player to press Space or Click to skip the splash video.")]
    [SerializeField] private bool allowSkip = true;

    // isTransitioning: guard preventing the splash transition from starting twice. Updated by TriggerTransition.
    private bool isTransitioning = false;

    /// <summary>Set starting visibility so the splash and black overlay cover the prepared menu.</summary>
    private void Awake()
    {
        // Keep the menu ready behind the black overlay.
        // If menuCanvasGroup exists, run the following block.
        if (menuCanvasGroup != null)
            // Set menuCanvasGroup.alpha to 1.
            menuCanvasGroup.alpha = 1f; // Menu is fully visible behind the black fade layer.

        // Hide the menu video until the transition begins.
        // If menuRawImage exists, run the following block.
        if (menuRawImage != null)
            // Disable menuRawImage by changing its component enabled flag.
            menuRawImage.enabled = false;

        // Start with the splash visible.
        // If splashCanvasGroup exists, run the following block.
        if (splashCanvasGroup != null)
            // Set splashCanvasGroup.alpha to 1.
            splashCanvasGroup.alpha = 1f; // Splash screen starts fully opaque.

        // Block the scene until the transition starts.
        // If blackScreenCanvasGroup exists, run the following block.
        if (blackScreenCanvasGroup != null)
            // Set blackScreenCanvasGroup.alpha to 1.
            blackScreenCanvasGroup.alpha = 1f; // Black overlay starts fully opaque so the scene is hidden.
    }

    /// <summary>Listen for splash completion and prepare both videos before the transition.</summary>
    private void Start()
    {
        // Set up the splash video player.
        // If splashVideoPlayer exists, run the following block.
        if (splashVideoPlayer != null)
        {
            // Subscribe OnSplashVideoEnded to splashVideoPlayer.loopPointReached so the callback runs when that
            // event is raised.
            splashVideoPlayer.loopPointReached += OnSplashVideoEnded;
            // Ask splashVideoPlayer VideoPlayer to prepare decoding before playback.
            splashVideoPlayer.Prepare();
        }

        // Pre-load the menu video player into memory.
        // If menuVideoPlayer exists, run the following block.
        if (menuVideoPlayer != null)
        {
            // Ask menuVideoPlayer VideoPlayer to prepare decoding before playback.
            menuVideoPlayer.Prepare();
        }
    }

    /// <summary>Check allowed Space or mouse input for skipping the splash.</summary>
    private void Update()
    {
        // Skip check for the input system.
        // If allowSkip and not (isTransitioning), run the following block.
        if (allowSkip && !isTransitioning)
        {
            // Check for spacebar press or mouse click using the new Input System.
            // Store the current Input System keyboard exists and space key on the current Input System keyboard
            // was newly pressed this frame in local spacePressed.
            bool spacePressed = Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame;
            // Check for mouse click using the new Input System.
            // Store the current Input System mouse exists and Mouse.current.leftButton was newly pressed this
            // frame in local mouseClicked.
            bool mouseClicked = Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame;

            // If either input is detected, trigger the transition.
            // If spacePressed or mouseClicked, run the following block.
            if (spacePressed || mouseClicked)
            {
                // Stop the splash video if it's still playing.
                // Call TransitionManager.TriggerTransition; start the sequence once, ignoring additional end or
                // skip requests.
                TriggerTransition();
            }
        }
    }

    // Callback for when the splash video ends naturally.
    /// <summary>Begin the menu transition when the splash reaches its natural end.</summary>
    // VideoPlayer supplies vp as the player that raised this preparation/completion/error event.
    private void OnSplashVideoEnded(VideoPlayer vp)
    {
        // Trigger the transition when the splash video finishes playing.
        // Call TransitionManager.TriggerTransition; start the sequence once, ignoring additional end or skip
        // requests.
        TriggerTransition();
    }

    // Initiates the transition from the splash screen to the main menu.
    /// <summary>Start the sequence once, ignoring additional end or skip requests.</summary>
    private void TriggerTransition()
    {
        // Prevent multiple transitions from being triggered simultaneously.
        // Stop this method early when isTransitioning.
        if (isTransitioning)
            // Return now; skip the rest of this method.
            return; // Already transitioning, ignore further triggers.

        // Stop the splash video if it's still playing.
        // Set isTransitioning to true.
        isTransitioning = true;
        // Start coroutine PerformCrossfade(); Unity resumes it after each yield.
        StartCoroutine(PerformCrossfade()); // Start the crossfade coroutine to handle the transition.
    }

    // Coroutine to handle the crossfade transition between the splash screen and the main menu.
    /// <summary>Play the menu video, fade out the splash, then dissolve and remove the black overlay.</summary>
    private IEnumerator PerformCrossfade()
    {
        // Enable and play the menu background video under the black overlay.
        // If menuVideoPlayer exists, run the following block.
        if (menuVideoPlayer != null)
        {
            // Ensure the menu video is visible before starting playback.
            // If menuRawImage exists, run the following block.
            if (menuRawImage != null)
                // Enable menuRawImage by changing its component enabled flag.
                menuRawImage.enabled = true; // Show the menu video.

            // Start playing the menu video.
            // Start/resume playback on menuVideoPlayer.
            menuVideoPlayer.Play();
        }

        // Fade out the splash screen to reveal the black overlay.
        // Initialize timer to 0.
        float timer = 0f;
        // Repeat while timer is below fadeOutSplashTime.
        while (timer < fadeOutSplashTime)
        {
            // Add Unity real frame duration in seconds (ignores pause) to timer and store the updated value there.
            timer += Time.unscaledDeltaTime; // Increment the timer based on the time elapsed since the last frame.

            // Gradually reduce the alpha of the splash canvas group to create a fade-out effect.
            // If splashCanvasGroup exists, run the following block.
            if (splashCanvasGroup != null)
            {
                // Calculate the new alpha value based on the elapsed time and fade duration.
                // Set splashCanvasGroup.alpha to 1 minus timer / fadeOutSplashTime limited to the range 0 to 1.
                splashCanvasGroup.alpha = 1f - Mathf.Clamp01(timer / fadeOutSplashTime); // Fade splash from 1 to 0 over time.
            }

            // Pause the coroutine until the next Unity frame so the animation/sequence can render.
            yield return null; // Wait for the next frame before continuing the loop, allowing the fade effect to be smooth over time.
        }

        // Turn off the splash screen completely.
        // If splashCanvasGroup exists, run the following block.
        if (splashCanvasGroup != null)
            // Set splashCanvasGroup.alpha to 0.
            splashCanvasGroup.alpha = 0f; // Splash is now fully transparent/invisible.

        // Turn off the splash video player to free up resources.
        // If splashVideoPlayer exists, run the following block.
        if (splashVideoPlayer != null)
            // Deactivate splashVideoPlayer.gameObject.
            splashVideoPlayer.gameObject.SetActive(false); // Deactivate the splash video player GameObject.

        // Fade out the black overlay to reveal the menu video.
        // Set timer to 0.
        timer = 0f;
        // Repeat while timer is below fadeOutBlackTime.
        while (timer < fadeOutBlackTime)
        {
            // Add Unity real frame duration in seconds (ignores pause) to timer and store the updated value there.
            timer += Time.unscaledDeltaTime; // Increment the timer based on the time elapsed since the last frame.

            // Gradually reduce the alpha of the black screen canvas group to create a fade-out effect.
            // If blackScreenCanvasGroup exists, run the following block.
            if (blackScreenCanvasGroup != null)
            {
                // Calculate the new alpha value based on the elapsed time and fade duration.
                // Set blackScreenCanvasGroup.alpha to 1 minus timer / fadeOutBlackTime limited to the range 0 to
                // 1.
                blackScreenCanvasGroup.alpha = 1f - Mathf.Clamp01(timer / fadeOutBlackTime); // Fade black overlay from 1 to 0 so menu becomes visible.
            }

            // Pause the coroutine until the next Unity frame so the animation/sequence can render.
            yield return null; // Wait for the next frame before continuing the loop, allowing the fade effect to be smooth over time.
        }

        // Turn off the black overlay so buttons can receive clicks.
        // If blackScreenCanvasGroup exists, run the following block.
        if (blackScreenCanvasGroup != null)
        {
            // Set blackScreenCanvasGroup.alpha to 0.
            blackScreenCanvasGroup.alpha = 0f; // Black overlay is fully transparent and no longer blocks interaction.
            // Deactivate blackScreenCanvasGroup.gameObject.
            blackScreenCanvasGroup.gameObject.SetActive(false); // Deactivate the black screen canvas group GameObject to allow interaction with the menu.
        }
    }

    // Clean up event subscriptions when the object is destroyed to prevent memory leaks.
    /// <summary>Remove the video completion callback when this controller is destroyed.</summary>
    private void OnDestroy()
    {
        // Unsubscribe from the splash video player's loop point reached event to avoid potential null reference exceptions.
        // If splashVideoPlayer exists, run the following block.
        if (splashVideoPlayer != null)
        {
            // Remove the event handler to prevent it from being called after the object is destroyed.
            // Unsubscribe OnSplashVideoEnded from splashVideoPlayer.loopPointReached so the callback stops running
            // when that event is raised.
            splashVideoPlayer.loopPointReached -= OnSplashVideoEnded;
        }
    }
}