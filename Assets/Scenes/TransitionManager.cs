using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;
using UnityEngine.InputSystem;

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

    private void Awake()
    {
        // Keep the Main Menu Canvas fully visible (Alpha = 1) so it sits ready behind the black screen
        if (menuCanvasGroup != null) menuCanvasGroup.alpha = 1f;
        if (menuRawImage != null) menuRawImage.enabled = false;

        // Start the Splash Screen visible (Alpha = 1)
        if (splashCanvasGroup != null) splashCanvasGroup.alpha = 1f;

        // Keep the Black Overlay fully opaque (Alpha = 1) to block premature video frames
        if (blackScreenCanvasGroup != null) blackScreenCanvasGroup.alpha = 1f;
    }

    private void Start()
    {
        // Set up the Splash Video Player
        if (splashVideoPlayer != null)
        {
            splashVideoPlayer.loopPointReached += OnSplashVideoEnded;
            splashVideoPlayer.Prepare();
        }

        // Pre-load the Menu Video Player into memory
        if (menuVideoPlayer != null)
        {
            menuVideoPlayer.Prepare();
        }
    }

    private void Update()
    {
        // Skip check for new Input System
        if (allowSkip && !isTransitioning)
        {
            bool spacePressed = Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame;
            bool mouseClicked = Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame;

            if (spacePressed || mouseClicked)
            {
                TriggerTransition();
            }
        }
    }

    private void OnSplashVideoEnded(VideoPlayer vp)
    {
        TriggerTransition();
    }

    private void TriggerTransition()
    {
        if (isTransitioning) return;
        isTransitioning = true;

        StartCoroutine(PerformCrossfade());
    }

    private IEnumerator PerformCrossfade()
    {
        // STEP 1: Enable and play menu background video under the black overlay
        if (menuVideoPlayer != null)
        {
            if (menuRawImage != null) menuRawImage.enabled = true;
            menuVideoPlayer.Play();
        }

        // STEP 2: Fade OUT Splash Screen (Alpha 1 -> 0) to reveal Black Overlay
        float timer = 0f;
        while (timer < fadeOutSplashTime)
        {
            timer += Time.deltaTime;
            if (splashCanvasGroup != null)
            {
                splashCanvasGroup.alpha = 1f - Mathf.Clamp01(timer / fadeOutSplashTime);
            }
            yield return null;
        }

        // Turn off splash screen object completely
        if (splashCanvasGroup != null) splashCanvasGroup.alpha = 0f;
        if (splashVideoPlayer != null) splashVideoPlayer.gameObject.SetActive(false);

        // STEP 3: Fade OUT Black Overlay (Alpha 1 -> 0) to reveal the playing Menu Video
        timer = 0f;
        while (timer < fadeOutBlackTime)
        {
            timer += Time.deltaTime;
            if (blackScreenCanvasGroup != null)
            {
                blackScreenCanvasGroup.alpha = 1f - Mathf.Clamp01(timer / fadeOutBlackTime);
            }
            yield return null;
        }

        // Turn off Black Overlay so buttons can receive clicks cleanly
        if (blackScreenCanvasGroup != null)
        {
            blackScreenCanvasGroup.alpha = 0f;
            blackScreenCanvasGroup.gameObject.SetActive(false);
        }
    }

    private void OnDestroy()
    {
        if (splashVideoPlayer != null)
        {
            splashVideoPlayer.loopPointReached -= OnSplashVideoEnded;
        }
    }
}