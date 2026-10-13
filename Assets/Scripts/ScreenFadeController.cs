// Purpose: Offers scene fade requests and a reusable black overlay for the ending.
// Use: Attach to a manager or create at runtime; Instance provides the shared scene controller.
// How it works: Scene loading delegates to SceneTransition; FadeToBlack builds its own overlay and uses real time.

// System.Collections supplies IEnumerator so coroutines can pause and resume across frames.
using System.Collections;
// UnityEngine supplies GameObjects, components, transforms, time, math and physics used here.
using UnityEngine;
// Unity SceneManagement supplies scene data, scene-loaded events and scene-loading calls.
using UnityEngine.SceneManagement;
// Unity UI supplies canvas widgets such as images, sliders and buttons.
using UnityEngine.UI;

public class ScreenFadeController : MonoBehaviour
{
    // Instance can be read by other scripts; only this class can write its backing state.
    public static ScreenFadeController Instance { get; private set; }

    // fadeImage: Legacy image reference. Transitions create a dedicated full-screen overlay above scene UI.
    // EnsureFadeImage retrieves it from the first Image component on panel. Inspector assignment may supply its
    // initial value.
    [Header("Fade Settings")]
    [Tooltip("Legacy image reference. Transitions create a dedicated full-screen overlay above scene UI.")]
    public Image fadeImage;

    // dontDestroyOnLoad: Keep this controller alive between scenes. Usually leave this OFF for a scene-specific
    // fade. Assigned in the scene/prefab Inspector; the initializer below is the default.
    [Tooltip("Keep this controller alive between scenes. Usually leave this OFF for a scene-specific fade.")]
    public bool dontDestroyOnLoad = false;

    // fadeCoroutine: running opacity animation handle, replaced by a new fade request. Updated by FadeToBlack.
    private Coroutine fadeCoroutine;
    // overlayReady: whether the screen-fade UI has been created and assigned. Updated by EnsureFadeImage.
    private bool overlayReady;

    /// <summary>Register one shared fade controller and initialize any assigned image as transparent.</summary>
    private void Awake()
    {
        // If Instance exists and Instance differs from this, run the following block.
        if (Instance != null && Instance != this)
        {
            // Schedule the GameObject that owns this component for destruction at Unity end-of-frame cleanup.
            Destroy(gameObject);
            // Return now; skip the rest of this method.
            return;
        }

        // Register this component in the class shared Instance field so other scripts can locate it without
        // repeated scene searches.
        Instance = this;

        // If dontDestroyOnLoad, run the following block.
        if (dontDestroyOnLoad)
            // Keep the GameObject that owns this component alive when Unity replaces the scene.
            DontDestroyOnLoad(gameObject);

        // If fadeImage exists, run the following block.
        if (fadeImage != null)
        {
            // Store fadeImage.color in local color.
            Color color = fadeImage.color;
            // Set color.a to 0.
            color.a = 0f;
            // Copy color into fadeImage.color.
            fadeImage.color = color;
            // Set fadeImage.raycastTarget to false.
            fadeImage.raycastTarget = false;
        }
    }

    /// <summary>Clear the shared reference when this controller is removed.</summary>
    private void OnDestroy()
    {
        // If Instance equals this, clear this class shared Instance reference so callers cannot reuse the removed
        // component.
        if (Instance == this) Instance = null;
    }

    /// <summary>Create a full-screen overlay above scene UI the first time a local black fade is needed.</summary>
    private void EnsureFadeImage()
    {
        // Always use a dedicated overlay so an assigned image under an inactive
        // canvas, mask or CanvasGroup cannot hide the escape fade.
        // If overlayReady and fadeImage exists, return now; skip the rest of this method.
        if (overlayReady && fadeImage != null) return;
        // Store a new GameObject named "Screen fade" with components the runtime type Canvas in local overlay.
        GameObject overlay = new GameObject("Screen fade", typeof(Canvas));
        // Parent overlay.transform beneath the Transform on this component GameObject; keep its local coordinates.
        overlay.transform.SetParent(transform, false);
        // Store the first Canvas component on overlay in local canvas.
        Canvas canvas = overlay.GetComponent<Canvas>();
        // Set canvas.renderMode to RenderMode.ScreenSpaceOverlay.
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        // Set canvas.sortingOrder to 32767.
        canvas.sortingOrder = 32767;
        // Store a new GameObject named "Black" with components the runtime type RectTransform, the runtime type
        // Image in local panel.
        GameObject panel = new GameObject("Black", typeof(RectTransform), typeof(Image));
        // Parent panel.transform beneath Transform of overlay; keep its local coordinates.
        panel.transform.SetParent(overlay.transform, false);
        // Store the first Image component on panel in fadeImage.
        fadeImage = panel.GetComponent<Image>();
        // Set fadeImage.color to a Color built from 0, 0, 0, 0.
        fadeImage.color = new Color(0f, 0f, 0f, 0f);
        // Store fadeImage.rectTransform in local rect.
        RectTransform rect = fadeImage.rectTransform;
        // Set rect.anchorMin to the zero 2D vector.
        rect.anchorMin = Vector2.zero;
        // Set rect.anchorMax to a 2D vector with both components equal to 1.
        rect.anchorMax = Vector2.one;
        // Set rect.offsetMin to rect.offsetMax = Vector2.zero.
        rect.offsetMin = rect.offsetMax = Vector2.zero;
        // Set overlayReady to true.
        overlayReady = true;
    }

    /// <summary>Pass the destination and duration to the common scene transition loader.</summary>
    // sceneName is a string argument supplied by EscapeManager.TryTransition, Level2NoteNpc.ContinueStory.
    // duration is a float argument supplied by EscapeManager.TryTransition, Level2NoteNpc.ContinueStory.
    public void FadeToScene(string sceneName, float duration)
    {
        // Call SceneTransition.Load with sceneName, duration; validate the destination, guard against duplicate
        // loads and create a host that survives the change.
        SceneTransition.Load(sceneName, duration);
    }

    /// <summary>Prepare the overlay and replace any previous local fade with a fade to opaque black.</summary>
    // duration is a float argument supplied by Level3Flow.ShowEnding.
    public void FadeToBlack(float duration)
    {
        // Call ScreenFadeController.EnsureFadeImage; create a full-screen overlay above scene UI the first time a
        // local black fade is needed.
        EnsureFadeImage();
        // If fadeCoroutine exists, run the following block.
        if (fadeCoroutine != null)
            // Stop the coroutine stored in fadeCoroutine.
            StopCoroutine(fadeCoroutine);

        // Store the coroutine handle for FadeRoutine(duration, 1f); Unity resumes it after each yield. in
        // fadeCoroutine.
        fadeCoroutine = StartCoroutine(FadeRoutine(duration, 1f));
    }

    /// <summary>Block pointer input and blend opacity over real time until the target is reached.</summary>
    // duration is a float argument supplied by ScreenFadeController.FadeToBlack.
    // targetAlpha is a float argument supplied by ScreenFadeController.FadeToBlack.
    private IEnumerator FadeRoutine(float duration, float targetAlpha)
    {
        // If fadeImage is missing, run the following block.
        if (fadeImage == null)
            // End this coroutine immediately instead of waiting for another frame.
            yield break;

        // Set fadeImage.raycastTarget to true.
        fadeImage.raycastTarget = true;

        // Store fadeImage.color in local startColor.
        Color startColor = fadeImage.color;
        // Store startColor opacity component in local startAlpha.
        float startAlpha = startColor.a;
        // Initialize timer to 0.
        float timer = 0f;

        // Use unscaledDeltaTime so this ending overlay continues fading after timeScale is set to zero.
        // Repeat while timer is below duration.
        while (timer < duration)
        {
            // Add Unity real frame duration in seconds (ignores pause) to timer and store the updated value there.
            timer += Time.unscaledDeltaTime;
            // Store 1 when duration is at most 0; otherwise timer / duration limited to the range 0 to 1 in local
            // progress.
            float progress = duration <= 0f ? 1f : Mathf.Clamp01(timer / duration);

            // Store fadeImage.color in local color.
            Color color = fadeImage.color;
            // Store a blend from startAlpha to targetAlpha using fraction progress in color.a.
            color.a = Mathf.Lerp(startAlpha, targetAlpha, progress);
            // Copy color into fadeImage.color.
            fadeImage.color = color;

            // Pause the coroutine until the next Unity frame so the animation/sequence can render.
            yield return null;
        }

        // Store fadeImage.color in local finalColor.
        Color finalColor = fadeImage.color;
        // Copy targetAlpha (argument from the caller) into finalColor.a.
        finalColor.a = targetAlpha;
        // Copy finalColor into fadeImage.color.
        fadeImage.color = finalColor;
    }
}
