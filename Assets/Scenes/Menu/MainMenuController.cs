// Purpose: Handles menu buttons and saved audio and brightness settings.
// Use: Attach to menu or cutscene control objects and assign UI, mixer and volume references.
// How it works: The persistent instance reapplies preferences on scene changes; local instances serve scene callbacks.

// UnityEngine supplies GameObjects, components, transforms, time, math and physics used here.
using UnityEngine; // Unity core features, such as MonoBehaviour and GameObject.
// Unity SceneManagement supplies scene data, scene-loaded events and scene-loading calls.
using UnityEngine.SceneManagement; // Used to load scenes and detect scene changes.
// Unity UI supplies canvas widgets such as images, sliders and buttons.
using UnityEngine.UI; // Used for UI elements like sliders and buttons.
// Unity Audio supplies mixer references for routing and controlling sound.
using UnityEngine.Audio; // Used to control the audio mixer.
// Unity rendering supplies Volume/profile types used for camera effects.
using UnityEngine.Rendering; // Used for accessing the Volume component and its profile.


public class MainMenuController : MonoBehaviour
{
    // MainMenuController.Instance is the shared reference set to this component during initialization; other
    // scripts read it to find this manager.
    public static MainMenuController Instance; // Stores the single active menu controller instance.

    // gameSceneName: Exact name of the gameplay scene. Assigned in the scene/prefab Inspector; the initializer
    // below is the default.
    [Header("Scene Management")]                    // Groups all scene-related settings together in the Inspector.
    [Tooltip("Exact name of the gameplay scene.")]  // Shows what this field is for when hovered.
    [SerializeField] private string gameSceneName = "OpeningCutscene"; // Name of the scene to load when the player starts the game.

    // mainButtonsPanel: Panel containing the main menu buttons. Assigned in the scene/prefab Inspector.
    [Header("UI Panels")]                                   // Groups menu panel references together.
    [Tooltip("Panel containing the main menu buttons.")]    // Helps explain what this panel is.
    [SerializeField] private GameObject mainButtonsPanel;   // Reference to the main menu button panel.

    // settingsPanel: Panel containing the settings controls. Assigned in the scene/prefab Inspector.
    [Tooltip("Panel containing the settings controls.")]    // Explains the settings panel purpose.
    [SerializeField] private GameObject settingsPanel;      // Reference to the settings menu panel.

    // mainAudioMixer: Audio Mixer used for music and sound effects. Assigned in the scene/prefab Inspector.
    [Header("Audio Settings")]                                  // Groups audio settings in the Inspector.
    [Tooltip("Audio Mixer used for music and sound effects.")]  // Tells the user what the mixer is for.
    [SerializeField] private AudioMixer mainAudioMixer;         // Reference to the audio mixer that controls volume.

    // musicSlider: Slider used to control music volume. Assigned in the scene/prefab Inspector.
    [Tooltip("Slider used to control music volume.")]           // Describes the music volume slider.
    [SerializeField] private Slider musicSlider;                // UI slider for music volume.

    // sfxSlider: Slider used to control sound effects volume. Assigned in the scene/prefab Inspector.
    [Tooltip("Slider used to control sound effects volume.")]   // Describes the SFX slider.
    [SerializeField] private Slider sfxSlider;                  // UI slider for sound effects volume.

    // globalVolume: Global Volume used for brightness. Assigned in the scene/prefab Inspector.
    [Header("Graphics Settings (URP)")]                         // Groups graphics settings together.
    [Tooltip("Global Volume used for brightness.")]             // Explains the volume object used for camera effects.
    [SerializeField] private Volume globalVolume;               // Reference to the global volume profile.

    // brightnessSlider: Slider used to control brightness. Assigned in the scene/prefab Inspector.
    [Tooltip("Slider used to control brightness.")]             // Describes the brightness slider.
    [SerializeField] private Slider brightnessSlider;           // UI slider for screen brightness.

    // gameTitleObject: Drag your Main Title text or title image object here. Assigned in the scene/prefab
    // Inspector.
    [Tooltip("Drag your Main Title text or title image object here.")]
    
    [SerializeField] private GameObject gameTitleObject;        // Reference to the main title object in the menu.

    // settingsTitleHeader: Optional: Drag a 'SETTINGS' text header object here if you create one. Assigned in the
    // scene/prefab Inspector.
    [Tooltip("Optional: Drag a 'SETTINGS' text header object here if you create one.")] // Explains the purpose of the settings title header.
    [SerializeField] private GameObject settingsTitleHeader;   // Reference to the settings title header object in the menu.


    // Private fields to store the current volume and color adjustments for brightness control.
    // currentVolume: URP Volume currently targeted by menu brightness changes. FetchVolumeProfile sets it from
    // targetVolume.
    private Volume currentVolume;
    // sceneBrightness: SceneBrightness component applying the saved slider setting to the active scene.
    // FetchVolumeProfile retrieves it from the first SceneBrightness component on targetVolume.
    private SceneBrightness sceneBrightness;
    // isPersistentInstance: whether this menu controller is the instance retained across scene changes. Updated by
    // Awake.
    private bool isPersistentInstance;

    /// <summary>Keep the first controller between scenes while leaving local callback controllers available.</summary>
    private void Awake()
    {
        // Keep one controller alive while scenes change.
        // If Instance is missing, run the following block.
        if (Instance == null)
        {
            // Set this object as the active menu controller.
            // Register this component in the class shared Instance field so other scripts can locate it without
            // repeated scene searches.
            Instance = this;
            // Set isPersistentInstance to true.
            isPersistentInstance = true;

            // Keep this object alive between scene loads.
            // Keep the GameObject that owns this component alive when Unity replaces the scene.
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            // Keep scene-local instances alive so Timeline signal callbacks remain valid.
            // Set isPersistentInstance to false.
            isPersistentInstance = false;
        }

        // Load the initial brightness profile from the assigned volume.
        // If isPersistentInstance, run the following block.
        if (isPersistentInstance)
            // Call MainMenuController.FetchVolumeProfile with globalVolume; find or add the scene brightness
            // helper on the supplied volume.
            FetchVolumeProfile(globalVolume);
    }

    /// <summary>Let the persistent instance listen for scene loads.</summary>
    private void OnEnable()
    {
        // Reapply brightness whenever a new scene loads.
        // Listen for scene load events.
        // If isPersistentInstance, run the following block.
        if (isPersistentInstance)
            // Subscribe OnSceneLoaded to SceneManager.sceneLoaded so the callback runs when that event is raised.
            SceneManager.sceneLoaded += OnSceneLoaded;
    }

    /// <summary>Remove the scene-load listener when the persistent instance is disabled.</summary>
    private void OnDisable()
    {
        // Stop listening when this object is disabled.
        // If isPersistentInstance, run the following block.
        if (isPersistentInstance)
            // Unsubscribe OnSceneLoaded from SceneManager.sceneLoaded so the callback stops running when that
            // event is raised.
            SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    /// <summary>Load saved slider values, apply the settings and show the main menu panel.</summary>
    private void Start()
    {
        if (gameObject.scene.name == "MainMenu" || mainButtonsPanel != null && mainButtonsPanel.scene.name == "MainMenu")
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
        // Load saved preferences from disk (or use default values on first startup)
        // Store saved PlayerPrefs value for "MusicVolume" (default 8) in local savedMusic.
        float savedMusic = PlayerPrefs.GetFloat("MusicVolume", 8f);         // Slider range: 0–10
        // Store saved PlayerPrefs value for "SFXVolume" (default 8) in local savedSFX.
        float savedSFX = PlayerPrefs.GetFloat("SFXVolume", 8f);             // Slider range: 0–10
        // Store saved PlayerPrefs value for "Brightness" (default 5) in local savedBrightness.
        float savedBrightness = PlayerPrefs.GetFloat("Brightness", 5f);    // Slider range: 0–10

        // THE SLIDERS TO MATCH THE LOADED VALUES PHYSICALLY
        // If musicSlider exists, run the following block.
        if (musicSlider != null) 
        {
            // Call musicSlider.SetValueWithoutNotify using savedMusic.
            musicSlider.SetValueWithoutNotify(savedMusic);
        }

        // If sfxSlider exists, run the following block.
        if (sfxSlider != null) 
        {
            // Call sfxSlider.SetValueWithoutNotify using savedSFX.
            sfxSlider.SetValueWithoutNotify(savedSFX);
        }

        // If brightnessSlider exists, run the following block.
        if (brightnessSlider != null) 
        {
            // Call brightnessSlider.SetValueWithoutNotify using savedBrightness.
            brightnessSlider.SetValueWithoutNotify(savedBrightness);
        }

        // Loaded values to the Mixer and global lighting engine
        // Call MainMenuController.SetMusicVolume with savedMusic; convert the slider to mixer decibels and store
        // the music preference.
        SetMusicVolume(savedMusic);
        // Call MainMenuController.SetSFXVolume with savedSFX; convert the slider to mixer decibels and store the
        // sound-effects preference.
        SetSFXVolume(savedSFX);
        // Call MainMenuController.SetBrightness with savedBrightness; route to the persistent controller, save the
        // slider value and apply scene exposure.
        SetBrightness(savedBrightness);

        // Ensure Settings Panel is hidden and Main Menu buttons are visible when starting
        // If settingsPanel exists, run the following block.
        if (settingsPanel != null) 
            // Deactivate settingsPanel.
            settingsPanel.SetActive(false);

        // If mainButtonsPanel exists, run the following block.
        if (mainButtonsPanel != null) 
            // Activate mainButtonsPanel.
            mainButtonsPanel.SetActive(true);
    }

    //  This method is called whenever a new scene is loaded, allowing the menu controller to reapply settings and configurations.
    /// <summary>Reapply saved settings after a new scene loads.</summary>
    // SceneManager.sceneLoaded supplies scene as the newly loaded Unity scene.
    // SceneManager.sceneLoaded supplies mode to identify single or additive scene loading.
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // Apply before rendering, including scene loads while timeScale is zero.
        // Call MainMenuController.ApplyLoadedSettingsToNewScene; find the new global volume and reapply brightness
        // and audio preferences.
        ApplyLoadedSettingsToNewScene();
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }


    /// <summary>Find the new global volume and reapply brightness and audio preferences.</summary>
    private void ApplyLoadedSettingsToNewScene()
    {
        // 1. Re-locate the Global Volume in the freshly loaded scene
        // Initialize newVolume to no reference (null).
        Volume newVolume = null;
        // Store all scene Volume components in local sceneVolumes.
        Volume[] sceneVolumes = FindObjectsByType<Volume>(FindObjectsInactive.Exclude);
        // Process each volume in sceneVolumes.
        foreach (Volume volume in sceneVolumes)
        {
            // If volume.isGlobal and volume.gameObject.scene equals the result of SceneManager.GetActiveScene, run
            // the following block.
            if (volume.isGlobal && volume.gameObject.scene == SceneManager.GetActiveScene())
            {
                // Copy volume into newVolume.
                newVolume = volume;
                // Leave this loop or switch case so no later statements in that block run.
                break;
            }
        }
        // Call MainMenuController.FetchVolumeProfile with newVolume; find or add the scene brightness helper on
        // the supplied volume.
        FetchVolumeProfile(newVolume);

        // 2. Load and slam your saved settings back into place immediately
        // Store saved PlayerPrefs value for "Brightness" (default 5) in local savedBrightness.
        float savedBrightness = PlayerPrefs.GetFloat("Brightness", 5f);
        // Store saved PlayerPrefs value for "MusicVolume" (default 8) in local savedMusic.
        float savedMusic = PlayerPrefs.GetFloat("MusicVolume", 8f);
        // Store saved PlayerPrefs value for "SFXVolume" (default 8) in local savedSFX.
        float savedSFX = PlayerPrefs.GetFloat("SFXVolume", 8f);

        // Call MainMenuController.SetBrightness with savedBrightness; route to the persistent controller, save the
        // slider value and apply scene exposure.
        SetBrightness(savedBrightness);
        // Call MainMenuController.SetMusicVolume with savedMusic; convert the slider to mixer decibels and store
        // the music preference.
        SetMusicVolume(savedMusic);
        // Call MainMenuController.SetSFXVolume with savedSFX; convert the slider to mixer decibels and store the
        // sound-effects preference.
        SetSFXVolume(savedSFX);
        
        // Write the diagnostic message to the Unity Console; it does not update game UI.
        Debug.Log($"[Settings Manager] Settings successfully synchronized for scene! Brightness: {savedBrightness}");
    }



    /// <summary>Find or add the scene brightness helper on the supplied volume.</summary>
    // targetVolume is a Volume argument supplied by MainMenuController.ApplyLoadedSettingsToNewScene,
    // MainMenuController.Awake, MainMenuController.SetBrightness.
    private void FetchVolumeProfile(Volume targetVolume)
    {
        // Keep the Volume argument supplied by menu setup or scene lookup so later brightness changes affect that
        // scene.
        currentVolume = targetVolume;
        // Clear sceneBrightness to null so it no longer holds a reference.
        sceneBrightness = null;
        // If targetVolume is missing, return now; skip the rest of this method.
        if (targetVolume == null) return;
        // Store the first SceneBrightness component on targetVolume in sceneBrightness.
        sceneBrightness = targetVolume.GetComponent<SceneBrightness>();
        // If sceneBrightness is missing, run the following block.
        if (sceneBrightness == null)
            // Store a SceneBrightness component attached to GameObject owning targetVolume in sceneBrightness.
            sceneBrightness = targetVolume.gameObject.AddComponent<SceneBrightness>();
    }

    /// <summary>Save settings, reapply mixer levels and load the configured opening scene.</summary>
    public void PlayGame()
    {
        // Save current slider positions to disk right before loading the new scene
        // Write pending PlayerPrefs changes to persistent storage.
        PlayerPrefs.Save();
        
        // Explicitly re-enforce the current slider values to lock the Audio Mixer channels
        // Call MainMenuController.SetMusicVolume with saved PlayerPrefs value for "MusicVolume" (default 8f);
        // convert the slider to mixer decibels and store the music preference.
        SetMusicVolume(PlayerPrefs.GetFloat("MusicVolume", 8f));
        // Call MainMenuController.SetSFXVolume with saved PlayerPrefs value for "SFXVolume" (default 8f); convert
        // the slider to mixer decibels and store the sound-effects preference.
        SetSFXVolume(PlayerPrefs.GetFloat("SFXVolume", 8f));

        // Load your cutscene
        // Call SceneTransition.Load with gameSceneName; validate the destination, guard against duplicate loads
        // and create a host that survives the change.
        SceneTransition.Load(gameSceneName);
    }


    // This method is called when the intro cutscene finishes and the player is ready to enter Level 1.
    /// <summary>Handle the intro cutscene callback by saving settings and loading Level 1.</summary>
    public void LoadPoliceHQ()
    {
        // Write the diagnostic message to the Unity Console; it does not update game UI.
        Debug.Log("[Scene Transition] Intro Cutscene finished. Loading PoliceHQ Level 1...");
        
        // Ensure settings are saved right before moving into the core gameplay level
        // Write pending PlayerPrefs changes to persistent storage.
        PlayerPrefs.Save();

        // Load your Level 1 scene
        // Call SceneTransition.Load with "PoliceHQ"; validate the destination, guard against duplicate loads and
        // create a host that survives the change.
        SceneTransition.Load("PoliceHQ");
    }


    /// <summary>Show settings controls and swap the visible menu title.</summary>
    public void OpenSettings()
    {
        // Show the settings panel and hide the main menu buttons.
        // If mainButtonsPanel exists, deactivate mainButtonsPanel.
        if (mainButtonsPanel != null) mainButtonsPanel.SetActive(false);
        // If settingsPanel exists, activate settingsPanel.
        if (settingsPanel != null) settingsPanel.SetActive(true);
        
        // Hide the game title so it clears the top space
        // If gameTitleObject exists, deactivate gameTitleObject.
        if (gameTitleObject != null) gameTitleObject.SetActive(false);
        // If settingsTitleHeader exists, activate settingsTitleHeader.
        if (settingsTitleHeader != null) settingsTitleHeader.SetActive(true);
    }

    /// <summary>Return to the main buttons and save the current preferences.</summary>
    public void CloseSettings()
    {
        // Hide the settings panel and show the main menu buttons.
        // If settingsPanel exists, deactivate settingsPanel.
        if (settingsPanel != null) settingsPanel.SetActive(false);
        // If mainButtonsPanel exists, activate mainButtonsPanel.
        if (mainButtonsPanel != null) mainButtonsPanel.SetActive(true);
        
        // Bring the game title back when returning to the main screen
        // If gameTitleObject exists, activate gameTitleObject.
        if (gameTitleObject != null) gameTitleObject.SetActive(true);
        // If settingsTitleHeader exists, deactivate settingsTitleHeader.
        if (settingsTitleHeader != null) settingsTitleHeader.SetActive(false);
        
        // Write pending PlayerPrefs changes to persistent storage.
        PlayerPrefs.Save();
    }


    /// <summary>Stop Play Mode in the Editor or close the built game.</summary>
    public void QuitGame()
    {
        // Print a message in the console before quitting.
        // Write the diagnostic message to the Unity Console; it does not update game UI.
        Debug.Log("Exiting Game...");
        #if UNITY_EDITOR
                // Stop play mode in the Unity editor.
                // Set UnityEditor.EditorApplication.isPlaying to false.
                UnityEditor.EditorApplication.isPlaying = false;
        #else
                // Quit the game in a built application.
                // Call Application.Quit.
                Application.Quit();
        #endif
    }

    /// <summary>Convert the slider to mixer decibels and store the music preference.</summary>
    // value is a float argument supplied by MainMenuController.ApplyLoadedSettingsToNewScene,
    // MainMenuController.PlayGame, MainMenuController.Start.
    public void SetMusicVolume(float value)
    {
        // If mainAudioMixer exists, run the following block.
        if (mainAudioMixer != null)
        {
            // Convert the 0–10 slider value to 0–1 before converting to decibels.
            // Store value divided by 10f limited to the range 0 to 1 in local normalizedValue.
            float normalizedValue = Mathf.Clamp01(value / 10f);
            // Store Mathf.Log10(Mathf.Max(normalizedValue, 0.0001f)) * 20 (calculated from the values shown) in
            // local dB.
            float dB = Mathf.Log10(Mathf.Max(normalizedValue, 0.0001f)) * 20;
            // Set mainAudioMixer numeric property "MusicVolume" from dB.
            mainAudioMixer.SetFloat("MusicVolume", dB); // Apply the converted music volume to the mixer.
        }

        // Save value under PlayerPrefs key "MusicVolume".
        PlayerPrefs.SetFloat("MusicVolume", value); // Save the music volume value.
    }

    /// <summary>Convert the slider to mixer decibels and store the sound-effects preference.</summary>
    // value is a float argument supplied by MainMenuController.ApplyLoadedSettingsToNewScene,
    // MainMenuController.PlayGame, MainMenuController.Start.
    public void SetSFXVolume(float value)
    {
        // If mainAudioMixer exists, run the following block.
        if (mainAudioMixer != null)
        {
            // Convert the 0–10 slider value to 0–1 before converting to decibels.
            // Store value divided by 10f limited to the range 0 to 1 in local normalizedValue.
            float normalizedValue = Mathf.Clamp01(value / 10f);
            // Store Mathf.Log10(Mathf.Max(normalizedValue, 0.0001f)) * 20 (calculated from the values shown) in
            // local dB.
            float dB = Mathf.Log10(Mathf.Max(normalizedValue, 0.0001f)) * 20;
            // Set mainAudioMixer numeric property "SFXVolume" from dB.
            mainAudioMixer.SetFloat("SFXVolume", dB);               // Apply the sound effects volume to the mixer.
        }

        // Save value under PlayerPrefs key "SFXVolume".
        PlayerPrefs.SetFloat("SFXVolume", value); // Save the SFX volume value.
    }


    // Call this inside your ApplyLoadedSettingsToNewScene() method
    /// <summary>Route to the persistent controller, save the slider value and apply scene exposure.</summary>
    // sliderValue is a float argument supplied by MainMenuController.ApplyLoadedSettingsToNewScene,
    // MainMenuController.SetBrightness, MainMenuController.Start.
    public void SetBrightness(float sliderValue)
    {
        // Scene-local menu callbacks must adjust the persistent controller's volume.
        // If Instance exists and Instance differs from this, run the following block.
        if (Instance != null && Instance != this)
        {
            // Call MainMenuController.SetBrightness with sliderValue; route to the persistent controller, save the
            // slider value and apply scene exposure.
            Instance.SetBrightness(sliderValue);
            // Return now; skip the rest of this method.
            return;
        }
        // If currentVolume is missing, run the following block.
        if (currentVolume == null)
        {
            // Process each volume in all scene Volume components.
            foreach (Volume volume in FindObjectsByType<Volume>(FindObjectsInactive.Exclude))
                // If volume.isGlobal and volume.gameObject.scene equals the result of SceneManager.GetActiveScene,
                // run the following block.
                if (volume.isGlobal && volume.gameObject.scene == SceneManager.GetActiveScene())
                {
                    // Call MainMenuController.FetchVolumeProfile with volume; find or add the scene brightness
                    // helper on the supplied volume.
                    FetchVolumeProfile(volume);
                    // Leave this loop or switch case so no later statements in that block run.
                    break;
                }
        }
        // Keep the selected brightness value for the next session.
        // Store sliderValue limited between 0 and 10 in sliderValue.
        sliderValue = Mathf.Clamp(sliderValue, 0f, 10f);
        // Save sliderValue under PlayerPrefs key "Brightness".
        PlayerPrefs.SetFloat("Brightness", sliderValue);

        // If sceneBrightness exists, run the following block.
        if (sceneBrightness != null)
            // Call SceneBrightness.Apply with sliderValue; cache the authored exposure once, then adjust the
            // runtime profile from the slider value.
            sceneBrightness.Apply(sliderValue);
    }
}
