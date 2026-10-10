using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.Video;

/// <summary>Owns Level 3's mutually exclusive catch/escape outcomes.</summary>
public class Level3Flow : MonoBehaviour
{
    public PlayerMovement player;
    public RenatoEnemy renato;
    public LoseSceneController loseController;
    public BoxCollider mainExit;
    public ObjectiveItem objective;
    public bool HasKey { get; private set; }
    public const string ExitObjective = "Escape through the main door.";
    [Header("Caught camera")]
    public Camera caughtCamera;
    [Min(0.2f)] public float caughtFaceDistance = 0.65f;
    [Range(20f, 90f)] public float caughtFieldOfView = 40f;
    [Header("Outcome controls and ending")]
    public PauseManager pauseManager;
    public Button pauseButton;
    public ScreenFadeController fade;
    public TMP_FontAsset endingFont;
    public VideoClip endingClip;
    public AudioMixerGroup endingAudioMixerGroup;
    public float fadeDuration = 1.5f;
    [Min(1f)] public float successMessageDuration = 3f;
    public const string SuccessMessage = "You escape along with the truth...";
    public bool Finished { get; private set; }

    private VideoPlayer endingPlayer;
    private RenderTexture endingTexture;
    private GameObject endingScreen;
    private bool videoEnded;
    private string videoError;

    private void LateUpdate()
    {
        // CharacterController overlap fallback also handles spawning/stepping into the volume.
        if (Finished || Time.timeScale <= 0f || PauseManager.IsGamePaused || mainExit == null || player == null) return;
        CharacterController body = player.GetComponent<CharacterController>();
        Vector3 center = body != null ? body.bounds.center : player.transform.position;
        if (mainExit.bounds.Contains(center)) Escape();
    }

    public void Caught()
    {
        if (Finished || loseController == null || player == null) return;
        Finish();
        PlayerAudio audio = player.GetComponent<PlayerAudio>();
        if (audio != null) audio.PlayCaught();
        FocusCaughtCamera();
        loseController.ShowLoseScreen();
    }

    public bool CollectKey(PlayerMovement collector)
    {
        if (Finished || HasKey || collector == null || collector != player ||
            Time.timeScale <= 0f || PauseManager.IsGamePaused) return false;
        HasKey = true;
        if (objective != null)
        {
            objective.primaryObjective = ExitObjective;
            objective.UpdateUI();
        }
        if (NotificationManager.Instance != null)
            NotificationManager.Instance.ShowNotification("Key acquired! " + ExitObjective, 4f);
        return true;
    }

    private void FocusCaughtCamera()
    {
        if (renato == null) return;
        if (caughtCamera == null) caughtCamera = player.GetComponentInChildren<Camera>();
        if (caughtCamera == null) return;
        Animator animator = renato.GetComponentInChildren<Animator>();
        Transform head = animator != null && animator.isHuman && animator.avatar != null && animator.avatar.isValid
            ? animator.GetBoneTransform(HumanBodyBones.Head) : null;
        Vector3 face = head != null ? head.position : renato.transform.position + Vector3.up * 1.6f;
        // Approach from the front of the model even when Dwight was caught from behind.
        Vector3 forward = animator != null ? animator.transform.forward : renato.transform.forward;
        caughtCamera.transform.position = face + forward * Mathf.Max(0.2f, caughtFaceDistance);
        caughtCamera.transform.rotation = Quaternion.LookRotation(face - caughtCamera.transform.position, Vector3.up);
        caughtCamera.fieldOfView = caughtFieldOfView;
        caughtCamera.nearClipPlane = 0.03f;
        if (player.flashlight != null) player.flashlight.enabled = true;
    }

    public void Escape()
    {
        if (Finished || !HasKey || player == null || Time.timeScale <= 0f || PauseManager.IsGamePaused) return;
        Finish();
        StartCoroutine(ShowEnding());
    }

    private void Finish()
    {
        // Commit the outcome before activating UI/coroutines: catch and escape share this guard.
        Finished = true;
        if (renato != null) renato.Stop();
        player.enabled = false;
        foreach (MouseLook look in player.GetComponentsInChildren<MouseLook>(true)) look.enabled = false;
        foreach (PlayerInteraction interaction in player.GetComponentsInChildren<PlayerInteraction>(true)) interaction.enabled = false;
        Inventory inventory = player.GetComponent<Inventory>();
        if (inventory != null) inventory.enabled = false;
        PlayerRespawn respawn = player.GetComponent<PlayerRespawn>();
        if (respawn != null) respawn.enabled = false;
        if (pauseManager != null) pauseManager.enabled = false;
        if (pauseButton != null) pauseButton.interactable = false;
        Time.timeScale = 0f;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    private IEnumerator ShowEnding()
    {
        if (fade == null) fade = ScreenFadeController.Instance;
        if (fade == null) fade = gameObject.AddComponent<ScreenFadeController>();
        fade.FadeToBlack(fadeDuration);
        yield return new WaitUntil(() => fade.fadeImage != null && fade.fadeImage.color.a >= 1f);
        // Reuse the fade's black panel as the ending background. No second canvas is needed.
        Canvas canvas = fade.fadeImage.canvas;
        if (canvas.GetComponent<GraphicRaycaster>() == null) canvas.gameObject.AddComponent<GraphicRaycaster>();
        foreach (Canvas other in FindObjectsByType<Canvas>(FindObjectsInactive.Exclude))
            if (other != canvas && !other.transform.IsChildOf(canvas.transform)) other.enabled = false;

        TextMeshProUGUI success = CreateText("Escape success", fade.fadeImage.transform, SuccessMessage);
        Place(success.rectTransform, new Vector2(0.1f, 0.35f), new Vector2(0.9f, 0.65f));
        yield return new WaitForSecondsRealtime(Mathf.Max(1f, successMessageDuration));
        Destroy(success.gameObject);

        // The game stays frozen throughout the movie; decoding uses an unscaled clock.
        if (endingClip != null)
        {
            yield return PlayEndingVideo();
            ReleaseEndingVideo();
            if (videoEnded)
            {
                ReturnToMenu();
                yield break;
            }
        }
        Debug.LogWarning("[Level3Flow] Ending video unavailable: " + (videoError ?? "No clip assigned."), this);
        ShowEndingFallback();
    }

    private IEnumerator PlayEndingVideo()
    {
        endingScreen = new GameObject("Final ending", typeof(RectTransform), typeof(RawImage),
            typeof(AspectRatioFitter), typeof(AudioSource), typeof(VideoPlayer));
        endingScreen.transform.SetParent(fade.fadeImage.transform, false);
        Place((RectTransform)endingScreen.transform, Vector2.zero, Vector2.one);
        AspectRatioFitter fit = endingScreen.GetComponent<AspectRatioFitter>();
        fit.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
        fit.aspectRatio = endingClip.height > 0 ? (float)endingClip.width / endingClip.height : 16f / 9f;
        RawImage screen = endingScreen.GetComponent<RawImage>();
        screen.raycastTarget = false;
        screen.enabled = false;

        AudioSource audio = endingScreen.GetComponent<AudioSource>();
        audio.playOnAwake = false;
        audio.spatialBlend = 0f;
        audio.outputAudioMixerGroup = endingAudioMixerGroup;
        endingPlayer = endingScreen.GetComponent<VideoPlayer>();
        endingPlayer.playOnAwake = false;
        endingPlayer.source = VideoSource.VideoClip;
        endingPlayer.clip = endingClip;
        endingPlayer.isLooping = false;
        endingPlayer.waitForFirstFrame = true;
        endingPlayer.timeUpdateMode = VideoTimeUpdateMode.UnscaledGameTime;
        endingPlayer.renderMode = VideoRenderMode.RenderTexture;
        endingTexture = new RenderTexture(Mathf.Max(1, (int)endingClip.width),
            Mathf.Max(1, (int)endingClip.height), 0);
        endingTexture.Create();
        endingPlayer.targetTexture = endingTexture;
        screen.texture = endingTexture;
        endingPlayer.audioOutputMode = VideoAudioOutputMode.AudioSource;
        endingPlayer.controlledAudioTrackCount = (ushort)Mathf.Min(1, endingClip.audioTrackCount);
        if (endingPlayer.controlledAudioTrackCount > 0)
        {
            endingPlayer.EnableAudioTrack(0, true);
            endingPlayer.SetTargetAudioSource(0, audio);
        }
        endingPlayer.loopPointReached += OnEndingVideoFinished;
        endingPlayer.errorReceived += OnEndingVideoError;
        endingPlayer.Prepare();
        float deadline = Time.realtimeSinceStartup + 30f;
        while (!endingPlayer.isPrepared && videoError == null && Time.realtimeSinceStartup < deadline)
            yield return null;
        if (videoError != null) yield break;
        if (!endingPlayer.isPrepared)
        {
            videoError = "Preparation timed out.";
            yield break;
        }
        endingPlayer.Play();
        // Avoid displaying an uninitialized texture before the first decoded frame.
        deadline = Time.realtimeSinceStartup + 30f;
        while (!videoEnded && videoError == null)
        {
            if (endingPlayer.frame >= 0) screen.enabled = true;
            else if (Time.realtimeSinceStartup >= deadline)
            {
                videoError = "Playback did not start.";
                yield break;
            }
            yield return null;
        }
    }

    private void OnEndingVideoFinished(VideoPlayer source) => videoEnded = true;
    private void OnEndingVideoError(VideoPlayer source, string message) => videoError = message;

    private void ReleaseEndingVideo()
    {
        if (endingPlayer != null)
        {
            endingPlayer.loopPointReached -= OnEndingVideoFinished;
            endingPlayer.errorReceived -= OnEndingVideoError;
            endingPlayer.Stop();
            endingPlayer.targetTexture = null;
        }
        if (endingScreen != null) Destroy(endingScreen);
        if (endingTexture != null)
        {
            endingTexture.Release();
            Destroy(endingTexture);
        }
        endingPlayer = null;
        endingScreen = null;
        endingTexture = null;
    }

    private void OnDestroy() => ReleaseEndingVideo();

    private void ShowEndingFallback()
    {
        TextMeshProUGUI title = CreateText("Escape ending", fade.fadeImage.transform, SuccessMessage);
        Place(title.rectTransform, new Vector2(0.1f, 0.5f), new Vector2(0.9f, 0.7f));
        GameObject buttonObject = new GameObject("Main Menu", typeof(RectTransform), typeof(Image), typeof(Button));
        buttonObject.transform.SetParent(fade.fadeImage.transform, false);
        Place((RectTransform)buttonObject.transform, new Vector2(0.35f, 0.3f), new Vector2(0.65f, 0.42f));
        Image background = buttonObject.GetComponent<Image>();
        background.color = new Color(0.15f, 0.15f, 0.15f, 1f);
        Button button = buttonObject.GetComponent<Button>();
        button.targetGraphic = background;
        button.onClick.AddListener(ReturnToMenu);
        TextMeshProUGUI label = CreateText("Label", buttonObject.transform, "Main Menu");
        Place(label.rectTransform, Vector2.zero, Vector2.one);
        button.Select();
    }

    private TextMeshProUGUI CreateText(string objectName, Transform parent, string text)
    {
        GameObject go = new GameObject(objectName, typeof(RectTransform), typeof(TextMeshProUGUI));
        go.transform.SetParent(parent, false);
        TextMeshProUGUI label = go.GetComponent<TextMeshProUGUI>();
        if (endingFont != null) label.font = endingFont;
        label.text = text;
        label.fontSize = 36f;
        label.enableAutoSizing = true;
        label.fontSizeMin = 16f;
        label.fontSizeMax = 36f;
        label.alignment = TextAlignmentOptions.Center;
        label.raycastTarget = false;
        return label;
    }

    private static void Place(RectTransform rect, Vector2 min, Vector2 max)
    {
        rect.anchorMin = min;
        rect.anchorMax = max;
        rect.offsetMin = rect.offsetMax = Vector2.zero;
    }

    private void ReturnToMenu()
    {
        RespawnPoint.ResetCheckpoint();
        Time.timeScale = 1f;
        SceneTransition.Load("MainMenu");
    }
}
