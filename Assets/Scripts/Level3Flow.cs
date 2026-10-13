// Purpose: Coordinates the key objective, Renato catch sequence and final escape ending.
// Use: Attach to the Level 3 manager and assign the player, enemy, exit, key and outcome UI.
// How it works: A Finished flag allows one outcome; success freezes controls, shows a message and plays the movie.

// System.Collections supplies IEnumerator so coroutines can pause and resume across frames.
using System.Collections;
// TextMesh Pro supplies the text components, fonts and text-mesh information used here.
using TMPro;
// UnityEngine supplies GameObjects, components, transforms, time, math and physics used here.
using UnityEngine;
// Unity Audio supplies mixer references for routing and controlling sound.
using UnityEngine.Audio;
// Unity SceneManagement supplies scene data, scene-loaded events and scene-loading calls.
using UnityEngine.SceneManagement;
// Unity UI supplies canvas widgets such as images, sliders and buttons.
using UnityEngine.UI;
// Unity Video supplies movie clips, playback events and the VideoPlayer component.
using UnityEngine.Video;

/// <summary>Owns Level 3's mutually exclusive catch/escape outcomes.</summary>
public class Level3Flow : MonoBehaviour
{
    // player: player reference used by this script. Assigned in the scene/prefab Inspector.
    public PlayerMovement player;
    // renato: RenatoEnemy stopped by the Level 3 outcome controller. Assigned in the scene/prefab Inspector.
    public RenatoEnemy renato;
    // loseController: existing LoseSceneController reused for the Level 3 catch outcome. Assigned in the
    // scene/prefab Inspector.
    public LoseSceneController loseController;
    // mainExit: main-door BoxCollider used to detect escape overlap. Assigned in the scene/prefab Inspector.
    public BoxCollider mainExit;
    // objective: ObjectiveItem updated from key collection to the escape goal. Assigned in the scene/prefab
    // Inspector.
    public ObjectiveItem objective;
    // keyItem: ItemData asset added to inventory before HasKey becomes true. Assigned in the scene/prefab
    // Inspector.
    public ItemData keyItem;
    // HasKey can be read by other scripts; only this class can write its backing state.
    public bool HasKey { get; private set; }
    // ExitObjective: objective text displayed after the escape condition is unlocked. Fixed script constant;
    // callers cannot change it.
    public const string ExitObjective = "Escape through the main door.";
    // caughtCamera: camera repositioned to show Renato face during loss. FocusCaughtCamera retrieves it from the
    // first Camera component on or beneath player. Inspector assignment may supply its initial value.
    [Header("Caught camera")]
    public Camera caughtCamera;
    // caughtFaceDistance: camera distance from Renato during the caught close-up. Assigned in the scene/prefab
    // Inspector; the initializer below is the default.
    [Min(0.2f)] public float caughtFaceDistance = 0.65f;
    // caughtFieldOfView: camera field of view in degrees during the caught close-up. Assigned in the scene/prefab
    // Inspector; the initializer below is the default.
    [Range(20f, 90f)] public float caughtFieldOfView = 40f;
    // caughtLightIntensity: brightness of the light created for the caught close-up. Assigned in the scene/prefab
    // Inspector; the initializer below is the default.
    [Header("Caught face spotlight")]
    [Min(0f)] public float caughtLightIntensity = 5f;
    // caughtLightOffset: Offset from the caught camera, giving the face light a slight side angle. Assigned in the
    // scene/prefab Inspector; the initializer below is the default.
    [Tooltip("Offset from the caught camera, giving the face light a slight side angle.")]
    public Vector3 caughtLightOffset = new Vector3(-0.25f, 0.15f, 0f);
    // pauseManager: PauseManager disabled after a final outcome. Assigned in the scene/prefab Inspector.
    [Header("Outcome controls and ending")]
    public PauseManager pauseManager;
    // pauseButton: pause UI Button disabled after the outcome. Assigned in the scene/prefab Inspector.
    public Button pauseButton;
    // fade: ScreenFadeController reused or created for the final ending overlay. ShowEnding retrieves it from a
    // ScreenFadeController component attached to the GameObject that owns this component. Inspector assignment may
    // supply its initial value.
    public ScreenFadeController fade;
    // endingFont: TMP font assigned to generated ending text. Assigned in the scene/prefab Inspector.
    public TMP_FontAsset endingFont;
    // endingTypewriterFont: Font converted to a runtime TMP font for the success message. Assigned in the
    // scene/prefab Inspector.
    public Font endingTypewriterFont;
    // endingClip: VideoClip played after the successful escape message. Assigned in the scene/prefab Inspector.
    public VideoClip endingClip;
    // endingAudioMixerGroup: mixer group receiving movie audio. Assigned in the scene/prefab Inspector.
    public AudioMixerGroup endingAudioMixerGroup;
    // fadeDuration: seconds allocated to a fade. Assigned in the scene/prefab Inspector; the initializer below is
    // the default.
    public float fadeDuration = 1.5f;
    // successMessageDuration: real seconds the escape message stays visible. Assigned in the scene/prefab
    // Inspector; the initializer below is the default.
    [Min(1f)] public float successMessageDuration = 3f;
    // SuccessMessage: message displayed before the ending movie. Fixed script constant; callers cannot change it.
    public const string SuccessMessage = "NAKALIGTAS KA. NAGSISIMULA NA ANG HUSTISYA.";
    // EndingPlaybackSpeed: fixed playback multiplier used for the ending video. Fixed script constant; callers
    // cannot change it.
    private const float EndingPlaybackSpeed = 0.85f;
    // EndingTransitionDuration: fixed real-time duration used for the ending transition. Fixed script constant;
    // callers cannot change it.
    private const float EndingTransitionDuration = 0.8f;
    // Finished can be read by other scripts; only this class can write its backing state.
    public bool Finished { get; private set; }

    // Keep the generated font, video objects and playback result so the ending can clean up or fall back.
    // runtimeEndingFont: generated TMP font released after the ending display. CreateSuccessText retrieves it from
    // the result of TMP_FontAsset.CreateFontAsset using endingTypewriterFont.
    private TMP_FontAsset runtimeEndingFont;
    // endingPlayer: VideoPlayer created for the final movie. PlayEndingVideo retrieves it from the first
    // VideoPlayer component on endingScreen.
    private VideoPlayer endingPlayer;
    // endingTexture: RenderTexture receiving decoded movie frames. Updated by PlayEndingVideo, ReleaseEndingVideo.
    private RenderTexture endingTexture;
    // endingScreen: temporary UI object holding the movie display and player. Updated by PlayEndingVideo,
    // ReleaseEndingVideo.
    private GameObject endingScreen;
    // videoEnded: completion flag set by the ending video event. Updated by OnEndingVideoFinished.
    private bool videoEnded;
    // videoError: error text recorded by the ending video callback. OnEndingVideoError sets it from message.
    private string videoError;

    /// <summary>Check player overlap with the exit as a fallback for missed trigger entry events.</summary>
    private void LateUpdate()
    {
        // CharacterController overlap fallback also handles spawning/stepping into the volume.
        // If Finished || Time.timeScale <= 0f or PauseManager.IsGamePaused or mainExit is missing or player is
        // missing, return now; skip the rest of this method.
        if (Finished || Time.timeScale <= 0f || PauseManager.IsGamePaused || mainExit == null || player == null) return;
        // Store the first CharacterController component on player in local body.
        CharacterController body = player.GetComponent<CharacterController>();
        // Store body.bounds center when body exists; otherwise world position of player.transform in local center.
        Vector3 center = body != null ? body.bounds.center : player.transform.position;
        // If mainExit.bounds contains center, call Level3Flow.Escape; start the ending only during active gameplay
        // after collecting the key.
        if (mainExit.bounds.Contains(center)) Escape();
    }

    /// <summary>Commit a loss once, play caught audio, focus Renato face and show the lose screen.</summary>
    public void Caught()
    {
        // If Finished or loseController is missing or player is missing, return now; skip the rest of this method.
        if (Finished || loseController == null || player == null) return;
        // Call Level3Flow.Finish; lock the outcome, stop enemy and player controls, freeze time and release the
        // cursor for UI.
        Finish();
        // Store the first PlayerAudio component on player in local audio.
        PlayerAudio audio = player.GetComponent<PlayerAudio>();
        // If audio exists, call PlayerAudio.PlayCaught; play the assigned caught clip, or report that it needs an
        // Inspector assignment.
        if (audio != null) audio.PlayCaught();
        // Call Level3Flow.FocusCaughtCamera; frame Renato head and add a short spotlight for the caught sequence.
        FocusCaughtCamera();
        // Call LoseSceneController.ShowLoseScreen; hide gameplay UI, reveal the lose panel, freeze gameplay and
        // allow pointer input.
        loseController.ShowLoseScreen();
    }

    /// <summary>Accept the key only for the configured player after it has successfully entered inventory.</summary>
    // collector is PlayerMovement found by Level3Key.Interact; it must be the same component as this flow player
    // reference.
    public bool CollectKey(PlayerMovement collector)
    {
        // Reject the pickup if the level has ended, the key is already owned, the collector is missing
        // or is not this level's player, or gameplay is frozen/paused. Return false without changing inventory.
        if (Finished || HasKey || collector == null || collector != player ||
            Time.timeScale <= 0f || PauseManager.IsGamePaused) return false;
        // Commit the escape permission only after the key is visible in inventory.
        // Store the first Inventory component on collector in local inventory.
        Inventory inventory = collector.GetComponent<Inventory>();
        // If keyItem is missing or inventory is missing or not (the result of Inventory.AddItem using keyItem),
        // return false to the method caller.
        if (keyItem == null || inventory == null || !inventory.AddItem(keyItem)) return false;
        // Store Level3Flow key permission after Inventory.AddItem succeeded; Escape requires this flag.
        HasKey = true;
        // If objective exists, run the following block.
        if (objective != null)
        {
            // Copy ExitObjective from this component into objective.primaryObjective.
            objective.primaryObjective = ExitObjective;
            // Call ObjectiveItem.UpdateUI; choose a primary goal, escape goal or task list and format the assigned
            // text.
            objective.UpdateUI();
        }
        // If the scene NotificationManager registered in Awake exists, run the following block.
        if (NotificationManager.Instance != null)
            // Call NotificationManager.ShowNotification with "Key acquired! " plus ExitObjective, 4; show a new
            // message and restart its hide timer so it receives its full display time.
            NotificationManager.Instance.ShowNotification("Key acquired! " + ExitObjective, 4f);
        // Return true to the method caller.
        return true;
    }

    /// <summary>Frame Renato head and add a short spotlight for the caught sequence.</summary>
    private void FocusCaughtCamera()
    {
        // If renato is missing, return now; skip the rest of this method.
        if (renato == null) return;
        // If caughtCamera is missing, store the first Camera component on or beneath player in caughtCamera.
        if (caughtCamera == null) caughtCamera = player.GetComponentInChildren<Camera>();
        // If caughtCamera is missing, return now; skip the rest of this method.
        if (caughtCamera == null) return;
        // Store the first Animator component on or beneath renato in local animator.
        Animator animator = renato.GetComponentInChildren<Animator>();
        // Store the HumanBodyBones.Head bone Transform supplied by animator Humanoid avatar when animator != null
        // && animator.isHuman and animator.avatar != null and animator.avatar.isValid; otherwise no reference
        // (null) in local head.
        Transform head = animator != null && animator.isHuman && animator.avatar != null && animator.avatar.isValid
            ? animator.GetBoneTransform(HumanBodyBones.Head) : null;
        // Store world position of head when head exists; otherwise world position of renato.transform plus the
        // world-up direction (positive Y) multiplied by 1.6f in local face.
        Vector3 face = head != null ? head.position : renato.transform.position + Vector3.up * 1.6f;
        // Approach from the front of the model even when Dwight was caught from behind.
        // Store animator.transform forward axis expressed in world coordinates when animator exists; otherwise
        // renato.transform forward axis expressed in world coordinates in local forward.
        Vector3 forward = animator != null ? animator.transform.forward : renato.transform.forward;
        // Set caughtCamera.transform.position to face + forward * Mathf.Max(0.2f, caughtFaceDistance) (calculated
        // from the values shown).
        caughtCamera.transform.position = face + forward * Mathf.Max(0.2f, caughtFaceDistance);
        // Store a rotation facing direction face minus caughtCamera.transform.position in
        // caughtCamera.transform.rotation.
        caughtCamera.transform.rotation = Quaternion.LookRotation(face - caughtCamera.transform.position, Vector3.up);
        // Copy caughtFieldOfView from this component into caughtCamera.fieldOfView.
        caughtCamera.fieldOfView = caughtFieldOfView;
        // Set caughtCamera.nearClipPlane to 0.03.
        caughtCamera.nearClipPlane = 0.03f;
        // Replace the broad gameplay flashlight with a short, angled beam on the face.
        // If player.flashlight exists, disable player.flashlight by changing its component enabled flag.
        if (player.flashlight != null) player.flashlight.enabled = false;
        // Store a new GameObject named "Caught face spotlight" with components the runtime type Light in local
        // lightObject.
        GameObject lightObject = new GameObject("Caught face spotlight", typeof(Light));
        // Parent lightObject.transform beneath Transform of caughtCamera; keep its local coordinates.
        lightObject.transform.SetParent(caughtCamera.transform, false);
        // Copy caughtLightOffset from this component into lightObject.transform.localPosition.
        lightObject.transform.localPosition = caughtLightOffset;
        // Store a rotation facing direction face minus lightObject.transform.position in
        // lightObject.transform.rotation.
        lightObject.transform.rotation = Quaternion.LookRotation(face - lightObject.transform.position, Vector3.up);
        // Store the first Light component on lightObject in local faceLight.
        Light faceLight = lightObject.GetComponent<Light>();
        // Set faceLight.type to LightType.Spot.
        faceLight.type = LightType.Spot;
        // Copy caughtLightIntensity from this component into faceLight.intensity.
        faceLight.intensity = caughtLightIntensity;
        // Store the largest of 2, caughtFaceDistance + caughtLightOffset.magnitude + 0.5f (calculated from the
        // values shown) in faceLight.range.
        faceLight.range = Mathf.Max(2f, caughtFaceDistance + caughtLightOffset.magnitude + 0.5f);
        // Set faceLight.spotAngle to 60.
        faceLight.spotAngle = 60f;
        // Set faceLight.innerSpotAngle to 35.
        faceLight.innerSpotAngle = 35f;
        // Set faceLight.shadows to LightShadows.Soft.
        faceLight.shadows = LightShadows.Soft;
    }

    /// <summary>Start the ending only during active gameplay after collecting the key.</summary>
    public void Escape()
    {
        // If Finished || !HasKey or player == null or Unity gameplay clock multiplier (0 freezes gameplay time) is
        // at most 0f or PauseManager.IsGamePaused, return now; skip the rest of this method.
        if (Finished || !HasKey || player == null || Time.timeScale <= 0f || PauseManager.IsGamePaused) return;
        // Call Level3Flow.Finish; lock the outcome, stop enemy and player controls, freeze time and release the
        // cursor for UI.
        Finish();
        // Start coroutine ShowEnding(); Unity resumes it after each yield.
        StartCoroutine(ShowEnding());
    }

    /// <summary>Lock the outcome, stop enemy and player controls, freeze time and release the cursor for UI.</summary>
    private void Finish()
    {
        // Commit the outcome before activating UI/coroutines: catch and escape share this guard.
        // Store the shared Level3Flow outcome guard before UI or coroutines run so catch and escape cannot both
        // finish.
        Finished = true;
        // If renato exists, stop playback or activity on renato.
        if (renato != null) renato.Stop();
        // Disable player by changing its component enabled flag.
        player.enabled = false;
        // Process each look in all MouseLook components on or beneath player (including inactive children).
        foreach (MouseLook look in player.GetComponentsInChildren<MouseLook>(true)) look.enabled = false;
        // Process each interaction in all PlayerInteraction components on or beneath player (including inactive
        // children).
        foreach (PlayerInteraction interaction in player.GetComponentsInChildren<PlayerInteraction>(true)) interaction.enabled = false;
        // Store the first Inventory component on player in local inventory.
        Inventory inventory = player.GetComponent<Inventory>();
        // If inventory exists, disable inventory by changing its component enabled flag.
        if (inventory != null) inventory.enabled = false;
        // Store the first PlayerRespawn component on player in local respawn.
        PlayerRespawn respawn = player.GetComponent<PlayerRespawn>();
        // If respawn exists, disable respawn by changing its component enabled flag.
        if (respawn != null) respawn.enabled = false;
        // If pauseManager exists, disable pauseManager by changing its component enabled flag.
        if (pauseManager != null) pauseManager.enabled = false;
        // If pauseButton exists, set pauseButton.interactable to false.
        if (pauseButton != null) pauseButton.interactable = false;
        // Set Unity gameplay clock multiplier to zero; scaled movement/timers stop, while code using real time can
        // continue.
        Time.timeScale = 0f;
        // Release Unity pointer capture so menu buttons can be clicked.
        Cursor.lockState = CursorLockMode.None;
        // Show Unity mouse pointer while UI needs clicks.
        Cursor.visible = true;
    }

    /// <summary>Fade to black, show the success text, play the ending and offer a fallback if playback fails.</summary>
    private IEnumerator ShowEnding()
    {
        // If fade is missing, set fade to the fade controller registered in Awake.
        if (fade == null) fade = ScreenFadeController.Instance;
        // If fade is missing, store a ScreenFadeController component attached to the GameObject that owns this
        // component in fade.
        if (fade == null) fade = gameObject.AddComponent<ScreenFadeController>();
        // Call ScreenFadeController.FadeToBlack with fadeDuration; prepare the overlay and replace any previous
        // local fade with a fade to opaque black.
        fade.FadeToBlack(fadeDuration);
        // Yield a wait until the supplied condition becomes true; Unity resumes the coroutine after it completes.
        yield return new WaitUntil(() => fade.fadeImage != null && fade.fadeImage.color.a >= 1f);
        // Reuse the fade's black panel as the ending background. No second canvas is needed.
        // Store fade.fadeImage.canvas in local canvas.
        Canvas canvas = fade.fadeImage.canvas;
        // Store the first CanvasScaler component on canvas in local scaler.
        CanvasScaler scaler = canvas.GetComponent<CanvasScaler>();
        // If scaler is missing, store a CanvasScaler component attached to GameObject owning canvas in scaler.
        if (scaler == null) scaler = canvas.gameObject.AddComponent<CanvasScaler>();
        // Set scaler.uiScaleMode to CanvasScaler.ScaleMode.ScaleWithScreenSize.
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        // Set scaler.referenceResolution to a Vector2 built from 1920, 1080.
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        // Set scaler.screenMatchMode to CanvasScaler.ScreenMatchMode.MatchWidthOrHeight.
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        // Set scaler.matchWidthOrHeight to 0.5.
        scaler.matchWidthOrHeight = 0.5f;
        // If the first GraphicRaycaster component on canvas is missing, a GraphicRaycaster component attached to
        // GameObject owning canvas.
        if (canvas.GetComponent<GraphicRaycaster>() == null) canvas.gameObject.AddComponent<GraphicRaycaster>();
        // Process each other in all scene Canvas components.
        foreach (Canvas other in FindObjectsByType<Canvas>(FindObjectsInactive.Exclude))
            // If other differs from canvas and not (other.transform belongs to canvas.transform Transform
            // hierarchy), disable other by changing its component enabled flag.
            if (other != canvas && !other.transform.IsChildOf(canvas.transform)) other.enabled = false;

        // Store the result of Level3Flow.CreateSuccessText using "Escape success" in local success.
        TextMeshProUGUI success = CreateSuccessText("Escape success");
        // Set success.alpha to 0.
        success.alpha = 0f;
        // Yield the result of Level3Flow.FadeGraphic using success, 0, 1; Unity resumes the coroutine after it
        // completes.
        yield return FadeGraphic(success, 0f, 1f);
        // Yield a wait for the largest of 1f, successMessageDuration real seconds, even while gameplay is frozen;
        // Unity resumes the coroutine after it completes.
        yield return new WaitForSecondsRealtime(Mathf.Max(1f, successMessageDuration));
        // Yield the result of Level3Flow.FadeGraphic using success, 1, 0; Unity resumes the coroutine after it
        // completes.
        yield return FadeGraphic(success, 1f, 0f);
        // Schedule GameObject owning success for destruction at Unity end-of-frame cleanup.
        Destroy(success.gameObject);

        // The game stays frozen throughout the movie; decoding uses an unscaled clock.
        // If endingClip exists, run the following block.
        if (endingClip != null)
        {
            // Yield the result of Level3Flow.PlayEndingVideo; Unity resumes the coroutine after it completes.
            yield return PlayEndingVideo();
            // Call Level3Flow.ReleaseEndingVideo; unsubscribe, stop playback and free the screen and render
            // texture.
            ReleaseEndingVideo();
            // If videoEnded, run the following block.
            if (videoEnded)
            {
                // Call Level3Flow.ReturnToMenu; clear checkpoint progress, restore time and load the main menu.
                ReturnToMenu();
                // End this coroutine immediately instead of waiting for another frame.
                yield break;
            }
        }
        // Report the missing/invalid setup or state as a Unity Console warning.
        Debug.LogWarning("[Level3Flow] Ending video unavailable: " + (videoError ?? "No clip assigned."), this);
        // Call Level3Flow.ShowEndingFallback; show the success message with a Main Menu button when the ending
        // video cannot play.
        ShowEndingFallback();
    }

    /// <summary>Build a movie screen, prepare video and audio, then reveal decoded frames using real time.</summary>
    private IEnumerator PlayEndingVideo()
    {
        // Set endingScreen to a new GameObject named "Final ending" with components the runtime type
        // RectTransform, the runtime type RawImage, the runtime type AspectRatioFitter, the runtime type
        // AudioSource, the runtime type VideoPlayer.
        endingScreen = new GameObject("Final ending", typeof(RectTransform), typeof(RawImage),
            typeof(AspectRatioFitter), typeof(AudioSource), typeof(VideoPlayer));
        // Parent endingScreen.transform beneath Transform of fade.fadeImage; keep its local coordinates.
        endingScreen.transform.SetParent(fade.fadeImage.transform, false);
        // Call Level3Flow.Place with Transform of endingScreen viewed as RectTransform, the zero 2D vector, a 2D
        // vector with both components equal to 1; anchor a UI rectangle to a fraction of its parent and clear its
        // pixel offsets.
        Place((RectTransform)endingScreen.transform, Vector2.zero, Vector2.one);
        // Store the first AspectRatioFitter component on endingScreen in local fit.
        AspectRatioFitter fit = endingScreen.GetComponent<AspectRatioFitter>();
        // Set fit.aspectMode to AspectRatioFitter.AspectMode.FitInParent.
        fit.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
        // Set fit.aspectRatio to endingClip.width viewed as float divided by endingClip.height when
        // endingClip.height is above 0; otherwise 16 divided by 9.
        fit.aspectRatio = endingClip.height > 0 ? (float)endingClip.width / endingClip.height : 16f / 9f;
        // Store the first RawImage component on endingScreen in local screen.
        RawImage screen = endingScreen.GetComponent<RawImage>();
        // Set screen.raycastTarget to false.
        screen.raycastTarget = false;
        // Disable screen by changing its component enabled flag.
        screen.enabled = false;
        // Set screen.color to a Color built from 1, 1, 1, 0.
        screen.color = new Color(1f, 1f, 1f, 0f);

        // Route movie audio through the configured mixer and use unscaled playback while the game is frozen.
        // Store the first AudioSource component on endingScreen in local audio.
        AudioSource audio = endingScreen.GetComponent<AudioSource>();
        // Set audio.playOnAwake to false.
        audio.playOnAwake = false;
        // Set audio.spatialBlend to 0.
        audio.spatialBlend = 0f;
        // Copy endingAudioMixerGroup from this component into audio.outputAudioMixerGroup.
        audio.outputAudioMixerGroup = endingAudioMixerGroup;
        // Store the first VideoPlayer component on endingScreen in endingPlayer.
        endingPlayer = endingScreen.GetComponent<VideoPlayer>();
        // Set endingPlayer.playOnAwake to false.
        endingPlayer.playOnAwake = false;
        // Set endingPlayer.source to VideoSource.VideoClip.
        endingPlayer.source = VideoSource.VideoClip;
        // Copy endingClip from this component into endingPlayer.clip.
        endingPlayer.clip = endingClip;
        // Set endingPlayer.isLooping to false.
        endingPlayer.isLooping = false;
        // Set endingPlayer.waitForFirstFrame to true.
        endingPlayer.waitForFirstFrame = true;
        // Set endingPlayer.timeUpdateMode to VideoTimeUpdateMode.UnscaledGameTime.
        endingPlayer.timeUpdateMode = VideoTimeUpdateMode.UnscaledGameTime;
        // Set endingPlayer.renderMode to VideoRenderMode.RenderTexture.
        endingPlayer.renderMode = VideoRenderMode.RenderTexture;
        // Decode movie frames into a texture that the RawImage can display.
        // Set endingTexture to a new RenderTexture initialized with the largest of 1, (int)endingClip.width, the
        // largest of 1, (int)endingClip.height, 0.
        endingTexture = new RenderTexture(Mathf.Max(1, (int)endingClip.width),
            Mathf.Max(1, (int)endingClip.height), 0);
        // Call endingTexture.Create.
        endingTexture.Create();
        // Copy endingTexture cached by this component into endingPlayer.targetTexture.
        endingPlayer.targetTexture = endingTexture;
        // Copy endingTexture cached by this component into screen.texture.
        screen.texture = endingTexture;
        // Set endingPlayer.audioOutputMode to VideoAudioOutputMode.AudioSource.
        endingPlayer.audioOutputMode = VideoAudioOutputMode.AudioSource;
        // Set endingPlayer.controlledAudioTrackCount to the smallest of 1, endingClip.audioTrackCount viewed as
        // ushort.
        endingPlayer.controlledAudioTrackCount = (ushort)Mathf.Min(1, endingClip.audioTrackCount);
        // If endingPlayer.controlledAudioTrackCount is above 0, run the following block.
        if (endingPlayer.controlledAudioTrackCount > 0)
        {
            // Call endingPlayer.EnableAudioTrack using 0, true.
            endingPlayer.EnableAudioTrack(0, true);
            // Call endingPlayer.SetTargetAudioSource using 0, audio.
            endingPlayer.SetTargetAudioSource(0, audio);
        }
        // Subscribe OnEndingVideoFinished to endingPlayer.loopPointReached so the callback runs when that event is
        // raised.
        endingPlayer.loopPointReached += OnEndingVideoFinished;
        // Subscribe OnEndingVideoError to endingPlayer.errorReceived so the callback runs when that event is
        // raised.
        endingPlayer.errorReceived += OnEndingVideoError;
        // Ask endingPlayer VideoPlayer to prepare decoding before playback.
        endingPlayer.Prepare();
        // Allow up to 30 real seconds for preparation; an error or timeout returns to the fallback path.
        // Store Unity elapsed real time since startup plus 30 in local deadline.
        float deadline = Time.realtimeSinceStartup + 30f;
        // Repeat while not (endingPlayer.isPrepared) and videoError is missing and Unity elapsed real time since
        // startup is below deadline.
        while (!endingPlayer.isPrepared && videoError == null && Time.realtimeSinceStartup < deadline)
            // Pause the coroutine until the next Unity frame so the animation/sequence can render.
            yield return null;
        // If videoError exists, run the following block.
        if (videoError != null) yield break;
        // If not (endingPlayer.isPrepared), run the following block.
        if (!endingPlayer.isPrepared)
        {
            // Set videoError to "Preparation timed out.".
            videoError = "Preparation timed out.";
            // End this coroutine immediately instead of waiting for another frame.
            yield break;
        }
        // Copy EndingPlaybackSpeed cached by this component into endingPlayer.playbackSpeed.
        endingPlayer.playbackSpeed = EndingPlaybackSpeed;
        // Start/resume playback on endingPlayer.
        endingPlayer.Play();
        // Avoid displaying an uninitialized texture before the first decoded frame.
        // Set deadline to Unity elapsed real time since startup plus 30.
        deadline = Time.realtimeSinceStartup + 30f;
        // Initialize revealTime to 0.
        float revealTime = 0f;
        // Repeat while not (videoEnded) and videoError is missing.
        while (!videoEnded && videoError == null)
        {
            // If endingPlayer.frame is at least 0, run the following block.
            if (endingPlayer.frame >= 0)
            {
                // Enable screen by changing its component enabled flag.
                screen.enabled = true;
                // Add Unity real frame duration in seconds (ignores pause) to revealTime and store the updated
                // value there.
                revealTime += Time.unscaledDeltaTime;
                // Set screen.color to a Color built from 1, 1, 1, a smooth blend from 0f to 1f using fraction
                // revealTime / EndingTransitionDuration.
                screen.color = new Color(1f, 1f, 1f,
                    Mathf.SmoothStep(0f, 1f, revealTime / EndingTransitionDuration));
            }
            else if (Time.realtimeSinceStartup >= deadline)
            {
                // Set videoError to "Playback did not start.".
                videoError = "Playback did not start.";
                // End this coroutine immediately instead of waiting for another frame.
                yield break;
            }
            // Pause the coroutine until the next Unity frame so the animation/sequence can render.
            yield return null;
        }
    }

    /// <summary>Record that the movie reached its end so the sequence can return to the menu.</summary>
    // VideoPlayer supplies source as the player that raised this preparation/completion/error event.
    // Set videoEnded to true.
    private void OnEndingVideoFinished(VideoPlayer source) => videoEnded = true;
    /// <summary>Store the playback error so the sequence can display its fallback.</summary>
    // VideoPlayer supplies source as the player that raised this preparation/completion/error event.
    // message is a string argument supplied by the code, event or Inspector callback invoking OnEndingVideoError.
    // Copy message (argument from the caller) into videoError.
    private void OnEndingVideoError(VideoPlayer source, string message) => videoError = message;

    /// <summary>Unsubscribe, stop playback and free the screen and render texture.</summary>
    private void ReleaseEndingVideo()
    {
        // If endingPlayer exists, run the following block.
        if (endingPlayer != null)
        {
            // Unsubscribe OnEndingVideoFinished from endingPlayer.loopPointReached so the callback stops running
            // when that event is raised.
            endingPlayer.loopPointReached -= OnEndingVideoFinished;
            // Unsubscribe OnEndingVideoError from endingPlayer.errorReceived so the callback stops running when
            // that event is raised.
            endingPlayer.errorReceived -= OnEndingVideoError;
            // Stop playback or activity on endingPlayer.
            endingPlayer.Stop();
            // Clear endingPlayer.targetTexture to null so it no longer holds a reference.
            endingPlayer.targetTexture = null;
        }
        // If endingScreen exists, schedule endingScreen for destruction at Unity end-of-frame cleanup.
        if (endingScreen != null) Destroy(endingScreen);
        // If endingTexture exists, run the following block.
        if (endingTexture != null)
        {
            // Release the graphics resource held by endingTexture.
            endingTexture.Release();
            // Schedule endingTexture for destruction at Unity end-of-frame cleanup.
            Destroy(endingTexture);
        }
        // Clear endingPlayer to null so it no longer holds a reference.
        endingPlayer = null;
        // Clear endingScreen to null so it no longer holds a reference.
        endingScreen = null;
        // Clear endingTexture to null so it no longer holds a reference.
        endingTexture = null;
    }

    /// <summary>Release movie and generated font resources when the scene flow is removed.</summary>
    private void OnDestroy()
    {
        // Call Level3Flow.ReleaseEndingVideo; unsubscribe, stop playback and free the screen and render texture.
        ReleaseEndingVideo();
        // Generated font textures and materials belong to this scene and must be released with the font.
        // If runtimeEndingFont exists, run the following block.
        if (runtimeEndingFont != null)
        {
            // Process each atlas in runtimeEndingFont.atlasTextures.
            foreach (Texture2D atlas in runtimeEndingFont.atlasTextures)
                // If atlas exists, schedule atlas for destruction at Unity end-of-frame cleanup.
                if (atlas != null) Destroy(atlas);
            // If runtimeEndingFont material reference (Renderer.material may create an instance) exists, schedule
            // runtimeEndingFont material reference (Renderer.material may create an instance) for destruction at
            // Unity end-of-frame cleanup.
            if (runtimeEndingFont.material != null) Destroy(runtimeEndingFont.material);
            // Schedule runtimeEndingFont for destruction at Unity end-of-frame cleanup.
            Destroy(runtimeEndingFont);
        }
    }

    /// <summary>Show the success message with a Main Menu button when the ending video cannot play.</summary>
    private void ShowEndingFallback()
    {
        // Call Level3Flow.CreateSuccessText with "Escape ending"; create and position the large ending message
        // using the configured or generated font.
        CreateSuccessText("Escape ending");
        // Store a new GameObject named "Main Menu" with components the runtime type RectTransform, the runtime
        // type Image, the runtime type Button in local buttonObject.
        GameObject buttonObject = new GameObject("Main Menu", typeof(RectTransform), typeof(Image), typeof(Button));
        // Parent buttonObject.transform beneath Transform of fade.fadeImage; keep its local coordinates.
        buttonObject.transform.SetParent(fade.fadeImage.transform, false);
        // Call Level3Flow.Place with Transform of buttonObject viewed as RectTransform, a Vector2 built from 0.35,
        // 0.2, a Vector2 built from 0.65, 0.3; anchor a UI rectangle to a fraction of its parent and clear its
        // pixel offsets.
        Place((RectTransform)buttonObject.transform, new Vector2(0.35f, 0.2f), new Vector2(0.65f, 0.3f));
        // Store the first Image component on buttonObject in local background.
        Image background = buttonObject.GetComponent<Image>();
        // Set background.color to a Color built from 0.15, 0.15, 0.15, 1.
        background.color = new Color(0.15f, 0.15f, 0.15f, 1f);
        // Store the first Button component on buttonObject in local button.
        Button button = buttonObject.GetComponent<Button>();
        // Copy background into button.targetGraphic.
        button.targetGraphic = background;
        // Connect ReturnToMenu as a callback on button.onClick.
        button.onClick.AddListener(ReturnToMenu);
        // Store the result of Level3Flow.CreateText using "Label", Transform of buttonObject, "Main Menu" in local
        // label.
        TextMeshProUGUI label = CreateText("Label", buttonObject.transform, "Main Menu");
        // Call Level3Flow.Place with label.rectTransform, the zero 2D vector, a 2D vector with both components
        // equal to 1; anchor a UI rectangle to a fraction of its parent and clear its pixel offsets.
        Place(label.rectTransform, Vector2.zero, Vector2.one);
        // Call button.Select.
        button.Select();
    }

    /// <summary>Create and position the large ending message using the configured or generated font.</summary>
    // objectName is a string argument supplied by Level3Flow.ShowEnding, Level3Flow.ShowEndingFallback.
    private TextMeshProUGUI CreateSuccessText(string objectName)
    {
        // Store the result of Level3Flow.CreateText using objectName, Transform of fade.fadeImage, text from
        // SuccessMessage with ". " replaced by ".\n" in local title.
        TextMeshProUGUI title = CreateText(objectName, fade.fadeImage.transform,
            SuccessMessage.Replace(". ", ".\n"));
        // If runtimeEndingFont is missing and endingTypewriterFont exists, run the following block.
        if (runtimeEndingFont == null && endingTypewriterFont != null)
            // Store the result of TMP_FontAsset.CreateFontAsset using endingTypewriterFont in runtimeEndingFont.
            runtimeEndingFont = TMP_FontAsset.CreateFontAsset(endingTypewriterFont);
        // If runtimeEndingFont exists, copy runtimeEndingFont cached by this component into title.font.
        if (runtimeEndingFont != null) title.font = runtimeEndingFont;
        // Set title.color to Color.white.
        title.color = Color.white;
        // Set title.fontSize to 72.
        title.fontSize = 72f;
        // Set title.fontSizeMin to 40.
        title.fontSizeMin = 40f;
        // Set title.fontSizeMax to 72.
        title.fontSizeMax = 72f;
        // Call Level3Flow.Place with title.rectTransform, a Vector2 built from 0.08, 0.35, a Vector2 built from
        // 0.92, 0.65; anchor a UI rectangle to a fraction of its parent and clear its pixel offsets.
        Place(title.rectTransform, new Vector2(0.08f, 0.35f), new Vector2(0.92f, 0.65f));
        // Return title to the method caller.
        return title;
    }

    /// <summary>Change UI opacity over several frames even though gameplay time is frozen.</summary>
    // graphic is a Graphic argument supplied by Level3Flow.ShowEnding.
    // from is a float argument supplied by Level3Flow.ShowEnding.
    // to is a float argument supplied by Level3Flow.ShowEnding.
    private static IEnumerator FadeGraphic(Graphic graphic, float from, float to)
    {
        // Initialize elapsed to 0.
        float elapsed = 0f;
        // Repeat while elapsed is below EndingTransitionDuration.
        while (elapsed < EndingTransitionDuration)
        {
            // Add Unity real frame duration in seconds (ignores pause) to elapsed and store the updated value
            // there.
            elapsed += Time.unscaledDeltaTime;
            // Store graphic.color in local color.
            Color color = graphic.color;
            // Store a smooth blend from from to to using fraction elapsed divided by EndingTransitionDuration in
            // color.a.
            color.a = Mathf.SmoothStep(from, to, elapsed / EndingTransitionDuration);
            // Copy color into graphic.color.
            graphic.color = color;
            // Pause the coroutine until the next Unity frame so the animation/sequence can render.
            yield return null;
        }
    }

    /// <summary>Create centered text that scales to its available UI area.</summary>
    // objectName is a string argument supplied by Level3Flow.CreateSuccessText, Level3Flow.ShowEndingFallback.
    // parent is a Transform argument supplied by Level3Flow.CreateSuccessText, Level3Flow.ShowEndingFallback.
    // text is a string argument supplied by Level3Flow.CreateSuccessText, Level3Flow.ShowEndingFallback.
    private TextMeshProUGUI CreateText(string objectName, Transform parent, string text)
    {
        // Store a new GameObject named objectName with components the runtime type RectTransform, the runtime type
        // TextMeshProUGUI in local go.
        GameObject go = new GameObject(objectName, typeof(RectTransform), typeof(TextMeshProUGUI));
        // Parent go.transform beneath parent; keep its local coordinates.
        go.transform.SetParent(parent, false);
        // Store the first TextMeshProUGUI component on go in local label.
        TextMeshProUGUI label = go.GetComponent<TextMeshProUGUI>();
        // If endingFont exists, copy endingFont from this component into label.font.
        if (endingFont != null) label.font = endingFont;
        // Copy text (argument from the caller) into label.text.
        label.text = text;
        // Set label.fontSize to 36.
        label.fontSize = 36f;
        // Set label.enableAutoSizing to true.
        label.enableAutoSizing = true;
        // Set label.fontSizeMin to 16.
        label.fontSizeMin = 16f;
        // Set label.fontSizeMax to 36.
        label.fontSizeMax = 36f;
        // Set label.alignment to TextAlignmentOptions.Center.
        label.alignment = TextAlignmentOptions.Center;
        // Set label.raycastTarget to false.
        label.raycastTarget = false;
        // Return label to the method caller.
        return label;
    }

    /// <summary>Anchor a UI rectangle to a fraction of its parent and clear its pixel offsets.</summary>
    // rect is a RectTransform argument supplied by Level3Flow.CreateSuccessText, Level3Flow.PlayEndingVideo,
    // Level3Flow.ShowEndingFallback.
    // min is a Vector2 argument supplied by Level3Flow.CreateSuccessText, Level3Flow.PlayEndingVideo,
    // Level3Flow.ShowEndingFallback.
    // max is a Vector2 argument supplied by Level3Flow.CreateSuccessText, Level3Flow.PlayEndingVideo,
    // Level3Flow.ShowEndingFallback.
    private static void Place(RectTransform rect, Vector2 min, Vector2 max)
    {
        // Copy min (argument from the caller) into rect.anchorMin.
        rect.anchorMin = min;
        // Copy max (argument from the caller) into rect.anchorMax.
        rect.anchorMax = max;
        // Set rect.offsetMin to rect.offsetMax = Vector2.zero.
        rect.offsetMin = rect.offsetMax = Vector2.zero;
    }

    /// <summary>Clear checkpoint progress, restore time and load the main menu.</summary>
    private void ReturnToMenu()
    {
        // Call RespawnPoint.ResetCheckpoint; clear both collection history and the saved spawn for a fresh run.
        RespawnPoint.ResetCheckpoint();
        // Restore Unity gameplay clock multiplier to its normal rate so movement and scaled timers can advance.
        Time.timeScale = 1f;
        // Call SceneTransition.Load with "MainMenu"; validate the destination, guard against duplicate loads and
        // create a host that survives the change.
        SceneTransition.Load("MainMenu");
    }
}
