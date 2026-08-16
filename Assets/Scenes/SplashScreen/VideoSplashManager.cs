using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;


public class VideoSplashManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private VideoPlayer videoPlayer;
    [SerializeField] private RawImage splashRawImage;
    [SerializeField] private CanvasGroup splashCanvasGroup;

    [Header("Settings")]
    [SerializeField] private float fadeDuration = 0.5f;

    private void Awake()
    {
        // Keep image hidden until video starts playing to avoid visual glitches
        if (splashRawImage != null)
        {
            splashRawImage.enabled = false;
        }
    }

    private void Start()
    {
        if (videoPlayer != null)
        {
            // Subscribe to video events
            videoPlayer.prepareCompleted += OnVideoPrepared;
            videoPlayer.loopPointReached += OnVideoFinished;

            // Prepare the video stream
            videoPlayer.Prepare();
        }
    }

    private void OnVideoPrepared(VideoPlayer vp)
    {
        // Reveal raw image once first frame is ready
        if (splashRawImage != null)
        {
            splashRawImage.enabled = true;
        }
        vp.Play();
    }

    private void OnVideoFinished(VideoPlayer vp)
    {
        StartCoroutine(FadeOutSplash());
    }

    private IEnumerator FadeOutSplash()
    {
        float timer = 0f; 
        while (timer < fadeDuration)
        {
            timer += Time.deltaTime;
            if (splashCanvasGroup != null)
            {
                splashCanvasGroup.alpha = 1f - Mathf.Clamp01(timer / fadeDuration);
            }
            yield return null;
        }

        // Hide overlay completely to reveal menu behind it
        gameObject.SetActive(false);
    }

    private void OnDestroy()
    {
        if (videoPlayer != null)
        {
            videoPlayer.prepareCompleted -= OnVideoPrepared;
            videoPlayer.loopPointReached -= OnVideoFinished;
        }
    }
}