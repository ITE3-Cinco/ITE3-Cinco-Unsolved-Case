// Purpose: Provides a shared fade and asynchronous scene-loading path.
// Use: Call SceneTransition.Load from menu, retry or escape code; it creates its own persistent host.
// How it works: A loading flag prevents duplicates; real-time fading works while gameplay time is frozen.

// System.Collections supplies IEnumerator so coroutines can pause and resume across frames.
using System.Collections;
// UnityEngine supplies GameObjects, components, transforms, time, math and physics used here.
using UnityEngine;
// Unity SceneManagement supplies scene data, scene-loaded events and scene-loading calls.
using UnityEngine.SceneManagement;
// Unity UI supplies canvas widgets such as images, sliders and buttons.
using UnityEngine.UI;

/// <summary>One unscaled fade and asynchronous load path for every scene entry point.</summary>
[DefaultExecutionOrder(-1000)]
public class SceneTransition : MonoBehaviour
{
    // IsLoading can be read by other scripts; only this class can write its backing state.
    public static bool IsLoading { get; private set; }
    // overlay: CanvasGroup controlling the generated loading overlay opacity. LoadRoutine retrieves it from the
    // first CanvasGroup component on canvasObject.
    private CanvasGroup overlay;

    /// <summary>Clear the shared loading flag at the start of a play session.</summary>
    // Set IsLoading to false.
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetState() => IsLoading = false;

    /// <summary>Validate the destination, guard against duplicate loads and create a host that survives the change.</summary>
    // sceneName is a build-scene name supplied by a menu, retry, story or escape caller.
    // duration is the caller requested real-time fade length; the method default applies when it is omitted.
    public static void Load(string sceneName, float duration = 0.5f)
    {
        // If IsLoading, return now; skip the rest of this method.
        if (IsLoading) return;
        // If the result of string.IsNullOrWhiteSpace using sceneName or not (Unity can load build-scene
        // sceneName), run the following block.
        if (string.IsNullOrWhiteSpace(sceneName) || !Application.CanStreamedLevelBeLoaded(sceneName))
        {
            // Report the missing/invalid setup or state as a Unity Console warning.
            Debug.LogWarning("[SceneTransition] Destination is missing from Build Settings: " + sceneName);
            // Return now; skip the rest of this method.
            return;
        }
        // Set IsLoading to true.
        IsLoading = true;
        // Store a new GameObject named "Scene transition" in local host.
        GameObject host = new GameObject("Scene transition");
        // Keep host alive when Unity replaces the scene.
        DontDestroyOnLoad(host);
        // Store a SceneTransition component attached to host in local transition.
        SceneTransition transition = host.AddComponent<SceneTransition>();
        // Start coroutine transition.LoadRoutine(sceneName, Mathf.Max(0f, duration)); Unity resumes it after each
        // yield.
        transition.StartCoroutine(transition.LoadRoutine(sceneName, Mathf.Max(0f, duration)));
    }

    /// <summary>Build the overlay, fade to black, load the scene, then reveal it and resume time.</summary>
    // sceneName is a string argument supplied by SceneTransition.Load.
    // duration is a float argument supplied by SceneTransition.Load.
    private IEnumerator LoadRoutine(string sceneName, float duration)
    {
        // Draw a black overlay above normal scene UI and stretch it across the whole screen.
        // Store a new GameObject named "Loading overlay" with components the runtime type Canvas, the runtime type
        // CanvasGroup, the runtime type GraphicRaycaster in local canvasObject.
        GameObject canvasObject = new GameObject("Loading overlay", typeof(Canvas), typeof(CanvasGroup), typeof(GraphicRaycaster));
        // Parent canvasObject.transform beneath the Transform on this component GameObject; keep its local
        // coordinates.
        canvasObject.transform.SetParent(transform, false);
        // Store the first Canvas component on canvasObject in local canvas.
        Canvas canvas = canvasObject.GetComponent<Canvas>();
        // Set canvas.renderMode to RenderMode.ScreenSpaceOverlay.
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        // Set canvas.sortingOrder to 32767.
        canvas.sortingOrder = 32767;
        // Store the first CanvasGroup component on canvasObject in overlay.
        overlay = canvasObject.GetComponent<CanvasGroup>();
        // Set overlay.alpha to 0.
        overlay.alpha = 0f;
        // Store a new GameObject named "Black" with components the runtime type RectTransform, the runtime type
        // Image in local panel.
        GameObject panel = new GameObject("Black", typeof(RectTransform), typeof(Image));
        // Parent panel.transform beneath Transform of canvasObject; keep its local coordinates.
        panel.transform.SetParent(canvasObject.transform, false);
        // Set panel.GetComponent<Image>().color to Color.black.
        panel.GetComponent<Image>().color = Color.black;
        // Store Transform of panel viewed as RectTransform in local rect.
        RectTransform rect = (RectTransform)panel.transform;
        // Set rect.anchorMin to the zero 2D vector.
        rect.anchorMin = Vector2.zero;
        // Set rect.anchorMax to a 2D vector with both components equal to 1.
        rect.anchorMax = Vector2.one;
        // Set rect.offsetMin to rect.offsetMax = Vector2.zero.
        rect.offsetMin = rect.offsetMax = Vector2.zero;

        // Set Unity gameplay clock multiplier to zero; scaled movement/timers stop, while code using real time can
        // continue.
        Time.timeScale = 0f;
        // Yield the result of SceneTransition.Fade using 0, 1, duration; Unity resumes the coroutine after it
        // completes.
        yield return Fade(0f, 1f, duration);
        // Pause the coroutine until the next Unity frame so the animation/sequence can render.
        yield return null; // Render opaque black before scene initialization work.
        // Start asynchronous loading and handle a rejected load without leaving the game frozen.
        // Initialize loading to no reference (null).
        AsyncOperation loading = null;
        try
        {
            // Store Unity asynchronous loading operation for scene sceneName in loading.
            loading = SceneManager.LoadSceneAsync(sceneName);
        }
        catch (System.Exception error)
        {
            // Report exception error in the Unity Console.
            Debug.LogException(error);
        }
        // If loading is missing, run the following block.
        if (loading == null)
        {
            // Schedule the GameObject that owns this component for destruction at Unity end-of-frame cleanup.
            Destroy(gameObject);
            // End this coroutine immediately instead of waiting for another frame.
            yield break;
        }
        // Repeat while not (loading.isDone).
        while (!loading.isDone) yield return null;
        // New scene Awake/Start (including runtime navigation setup) remains covered.
        // Set Unity gameplay clock multiplier to zero; scaled movement/timers stop, while code using real time can
        // continue.
        Time.timeScale = 0f;
        // Pause the coroutine until the next Unity frame so the animation/sequence can render.
        yield return null;
        // Yield the result of SceneTransition.Fade using 1, 0, duration; Unity resumes the coroutine after it
        // completes.
        yield return Fade(1f, 0f, duration);
        // Set IsLoading to false.
        IsLoading = false;
        // Restore Unity gameplay clock multiplier to its normal rate so movement and scaled timers can advance.
        Time.timeScale = 1f;
        // Schedule the GameObject that owns this component for destruction at Unity end-of-frame cleanup.
        Destroy(gameObject);
    }

    /// <summary>Change overlay opacity over real time, yielding each frame for a smooth transition.</summary>
    // from is a float argument supplied by SceneTransition.LoadRoutine.
    // to is a float argument supplied by SceneTransition.LoadRoutine.
    // duration is a float argument supplied by SceneTransition.LoadRoutine.
    private IEnumerator Fade(float from, float to, float duration)
    {
        // Initialize elapsed to 0.
        float elapsed = 0f;
        // Repeat while elapsed is below duration.
        while (elapsed < duration)
        {
            // Add Unity real frame duration in seconds (ignores pause) to elapsed and store the updated value
            // there.
            elapsed += Time.unscaledDeltaTime;
            // Store a blend from from to to using fraction a smooth blend from 0f to 1f using fraction
            // Mathf.Clamp01(elapsed / duration) in overlay.alpha.
            overlay.alpha = Mathf.Lerp(from, to, Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / duration)));
            // Pause the coroutine until the next Unity frame so the animation/sequence can render.
            yield return null;
        }
        // Copy to (argument from the caller) into overlay.alpha.
        overlay.alpha = to;
    }

    /// <summary>Keep gameplay frozen while the new scene initializes behind the overlay.</summary>
    private void Update()
    {
        // If IsLoading, set Unity gameplay clock multiplier to zero; scaled movement/timers stop, while code using
        // real time can continue.
        if (IsLoading) Time.timeScale = 0f;
    }

    /// <summary>Clear loading state and restore gameplay time when the transition host is removed.</summary>
    private void OnDestroy()
    {
        // Set IsLoading to false.
        IsLoading = false;
        // Restore Unity gameplay clock multiplier to its normal rate so movement and scaled timers can advance.
        Time.timeScale = 1f;
    }
}
