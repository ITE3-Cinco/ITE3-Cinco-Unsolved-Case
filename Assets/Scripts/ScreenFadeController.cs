using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class ScreenFadeController : MonoBehaviour
{
    public static ScreenFadeController Instance { get; private set; }

    [Header("Fade Settings")]
    [Tooltip("Legacy image reference. Transitions create a dedicated full-screen overlay above scene UI.")]
    public Image fadeImage;

    [Tooltip("Keep this controller alive between scenes. Usually leave this OFF for a scene-specific fade.")]
    public bool dontDestroyOnLoad = false;

    private Coroutine fadeCoroutine;
    private bool overlayReady;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        if (dontDestroyOnLoad)
            DontDestroyOnLoad(gameObject);

        if (fadeImage != null)
        {
            Color color = fadeImage.color;
            color.a = 0f;
            fadeImage.color = color;
            fadeImage.raycastTarget = false;
        }
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    private void EnsureFadeImage()
    {
        // Always use a dedicated overlay so an assigned image under an inactive
        // canvas, mask or CanvasGroup cannot hide the escape fade.
        if (overlayReady && fadeImage != null) return;
        GameObject overlay = new GameObject("Screen fade", typeof(Canvas));
        overlay.transform.SetParent(transform, false);
        Canvas canvas = overlay.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 32767;
        GameObject panel = new GameObject("Black", typeof(RectTransform), typeof(Image));
        panel.transform.SetParent(overlay.transform, false);
        fadeImage = panel.GetComponent<Image>();
        fadeImage.color = new Color(0f, 0f, 0f, 0f);
        RectTransform rect = fadeImage.rectTransform;
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = rect.offsetMax = Vector2.zero;
        overlayReady = true;
    }

    public void FadeToScene(string sceneName, float duration)
    {
        SceneTransition.Load(sceneName, duration);
    }

    public void FadeToBlack(float duration)
    {
        EnsureFadeImage();
        if (fadeCoroutine != null)
            StopCoroutine(fadeCoroutine);

        fadeCoroutine = StartCoroutine(FadeRoutine(duration, 1f));
    }

    private IEnumerator FadeRoutine(float duration, float targetAlpha)
    {
        if (fadeImage == null)
            yield break;

        fadeImage.raycastTarget = true;

        Color startColor = fadeImage.color;
        float startAlpha = startColor.a;
        float timer = 0f;

        while (timer < duration)
        {
            timer += Time.unscaledDeltaTime;
            float progress = duration <= 0f ? 1f : Mathf.Clamp01(timer / duration);

            Color color = fadeImage.color;
            color.a = Mathf.Lerp(startAlpha, targetAlpha, progress);
            fadeImage.color = color;

            yield return null;
        }

        Color finalColor = fadeImage.color;
        finalColor.a = targetAlpha;
        fadeImage.color = finalColor;
    }
}
