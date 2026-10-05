using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class ScreenFadeController : MonoBehaviour
{
    public static ScreenFadeController Instance { get; private set; }

    [Header("Fade Settings")]
    [Tooltip("Full-screen black Image used for fading.")]
    public Image fadeImage;

    [Tooltip("Keep the fade controller alive when changing scenes.")]
    public bool dontDestroyOnLoad = true;

    private Coroutine fadeCoroutine;

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
            fadeImage.color = new Color(0f, 0f, 0f, 0f);
            fadeImage.raycastTarget = false;
        }
    }

    public void FadeToScene(string sceneName, float duration)
    {
        if (string.IsNullOrEmpty(sceneName))
        {
            Debug.LogWarning("[ScreenFade] No scene name was assigned.");
            return;
        }

        if (fadeCoroutine != null)
            StopCoroutine(fadeCoroutine);

        fadeCoroutine = StartCoroutine(FadeAndLoad(sceneName, duration));
    }

    public void FadeToBlack(float duration)
    {
        if (fadeCoroutine != null)
            StopCoroutine(fadeCoroutine);

        fadeCoroutine = StartCoroutine(FadeRoutine(duration, 1f));
    }

    private IEnumerator FadeAndLoad(string sceneName, float duration)
    {
        yield return FadeRoutine(duration, 1f);

        SceneManager.LoadScene(sceneName);
    }

    private IEnumerator FadeRoutine(float duration, float targetAlpha)
    {
        if (fadeImage == null)
        {
            Debug.LogWarning("[ScreenFade] No fade Image is assigned.");
            yield break;
        }

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
