using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>One unscaled fade and asynchronous load path for every scene entry point.</summary>
[DefaultExecutionOrder(-1000)]
public class SceneTransition : MonoBehaviour
{
    public static bool IsLoading { get; private set; }
    private CanvasGroup overlay;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetState() => IsLoading = false;

    public static void Load(string sceneName, float duration = 0.5f)
    {
        if (IsLoading) return;
        if (string.IsNullOrWhiteSpace(sceneName) || !Application.CanStreamedLevelBeLoaded(sceneName))
        {
            Debug.LogWarning("[SceneTransition] Destination is missing from Build Settings: " + sceneName);
            return;
        }
        IsLoading = true;
        GameObject host = new GameObject("Scene transition");
        DontDestroyOnLoad(host);
        SceneTransition transition = host.AddComponent<SceneTransition>();
        transition.StartCoroutine(transition.LoadRoutine(sceneName, Mathf.Max(0f, duration)));
    }

    private IEnumerator LoadRoutine(string sceneName, float duration)
    {
        GameObject canvasObject = new GameObject("Loading overlay", typeof(Canvas), typeof(CanvasGroup), typeof(GraphicRaycaster));
        canvasObject.transform.SetParent(transform, false);
        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 32767;
        overlay = canvasObject.GetComponent<CanvasGroup>();
        overlay.alpha = 0f;
        GameObject panel = new GameObject("Black", typeof(RectTransform), typeof(Image));
        panel.transform.SetParent(canvasObject.transform, false);
        panel.GetComponent<Image>().color = Color.black;
        RectTransform rect = (RectTransform)panel.transform;
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = rect.offsetMax = Vector2.zero;

        Time.timeScale = 0f;
        yield return Fade(0f, 1f, duration);
        yield return null; // Render opaque black before scene initialization work.
        AsyncOperation loading = null;
        try
        {
            loading = SceneManager.LoadSceneAsync(sceneName);
        }
        catch (System.Exception error)
        {
            Debug.LogException(error);
        }
        if (loading == null)
        {
            Destroy(gameObject);
            yield break;
        }
        while (!loading.isDone) yield return null;
        // New scene Awake/Start (including runtime navigation setup) remains covered.
        Time.timeScale = 0f;
        yield return null;
        yield return Fade(1f, 0f, duration);
        IsLoading = false;
        Time.timeScale = 1f;
        Destroy(gameObject);
    }

    private IEnumerator Fade(float from, float to, float duration)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            overlay.alpha = Mathf.Lerp(from, to, Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / duration)));
            yield return null;
        }
        overlay.alpha = to;
    }

    private void Update()
    {
        if (IsLoading) Time.timeScale = 0f;
    }

    private void OnDestroy()
    {
        IsLoading = false;
        Time.timeScale = 1f;
    }
}
