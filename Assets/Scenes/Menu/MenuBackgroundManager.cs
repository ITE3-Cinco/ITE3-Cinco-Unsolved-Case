using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;

public class MenuBackgroundManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private VideoPlayer menuVideoPlayer;
    [SerializeField] private RawImage menuRawImage;
    [SerializeField] private CanvasGroup menuCanvasGroup;

    [Header("Settings")]
    [SerializeField] private float fadeInDuration = 1.0f; // 

    private void Awake()
    {
        // Start hidden so it doesn't clash with the splash screen
        if (menuRawImage != null) menuRawImage.enabled = false;
        if (menuCanvasGroup != null) menuCanvasGroup.alpha = 0f;
    }

    private void Start()
    {
        if (menuVideoPlayer != null)
        {
            // Prepare the video in memory without displaying it yet
            menuVideoPlayer.prepareCompleted += OnMenuVideoPrepared;
            menuVideoPlayer.Prepare();
        }
    }

    private void OnMenuVideoPrepared(VideoPlayer vp)
    {
        // Video is ready in background memory—pause at frame 0
        vp.Pause();
    }

    // Call this method when the splash screen starts fading out
    public void StartMenuVideoWithFade()
    {
        if (menuRawImage != null) menuRawImage.enabled = true;
        if (menuVideoPlayer != null) menuVideoPlayer.Play();

        StartCoroutine(FadeInMenuBG());
    }

    private IEnumerator FadeInMenuBG()
    {
        float timer = 0f;
        while (timer < fadeInDuration)
        {
            timer += Time.deltaTime;
            if (menuCanvasGroup != null)
            {
                menuCanvasGroup.alpha = Mathf.Clamp01(timer / fadeInDuration);
            }
            yield return null;
        }

        if (menuCanvasGroup != null) menuCanvasGroup.alpha = 1f;
    }

    private void OnDestroy()
    {
        if (menuVideoPlayer != null)
        {
            menuVideoPlayer.prepareCompleted -= OnMenuVideoPrepared;
        }
    }
}