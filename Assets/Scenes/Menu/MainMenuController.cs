using UnityEngine; // Unity core features, such as MonoBehaviour and GameObject.
using UnityEngine.SceneManagement; // Used to load scenes and detect scene changes.
using UnityEngine.UI; // Used for UI elements like sliders and buttons.
using UnityEngine.Audio; // Used to control the audio mixer.
using UnityEngine.Rendering; // Used for accessing the Volume component and its profile.


public class MainMenuController : MonoBehaviour
{
    public static MainMenuController Instance; // Stores the single active menu controller instance.

    [Header("Scene Management")]                    // Groups all scene-related settings together in the Inspector.
    [Tooltip("Exact name of the gameplay scene.")]  // Shows what this field is for when hovered.
    [SerializeField] private string gameSceneName = "OpeningCutscene"; // Name of the scene to load when the player starts the game.

    [Header("UI Panels")]                                   // Groups menu panel references together.
    [Tooltip("Panel containing the main menu buttons.")]    // Helps explain what this panel is.
    [SerializeField] private GameObject mainButtonsPanel;   // Reference to the main menu button panel.

    [Tooltip("Panel containing the settings controls.")]    // Explains the settings panel purpose.
    [SerializeField] private GameObject settingsPanel;      // Reference to the settings menu panel.

    [Header("Audio Settings")]                                  // Groups audio settings in the Inspector.
    [Tooltip("Audio Mixer used for music and sound effects.")]  // Tells the user what the mixer is for.
    [SerializeField] private AudioMixer mainAudioMixer;         // Reference to the audio mixer that controls volume.

    [Tooltip("Slider used to control music volume.")]           // Describes the music volume slider.
    [SerializeField] private Slider musicSlider;                // UI slider for music volume.

    [Tooltip("Slider used to control sound effects volume.")]   // Describes the SFX slider.
    [SerializeField] private Slider sfxSlider;                  // UI slider for sound effects volume.

    [Header("Graphics Settings (URP)")]                         // Groups graphics settings together.
    [Tooltip("Global Volume used for brightness.")]             // Explains the volume object used for camera effects.
    [SerializeField] private Volume globalVolume;               // Reference to the global volume profile.

    [Tooltip("Slider used to control brightness.")]             // Describes the brightness slider.
    [SerializeField] private Slider brightnessSlider;           // UI slider for screen brightness.

    [Tooltip("Drag your Main Title text or title image object here.")]
    
    [SerializeField] private GameObject gameTitleObject;        // Reference to the main title object in the menu.

    [Tooltip("Optional: Drag a 'SETTINGS' text header object here if you create one.")] // Explains the purpose of the settings title header.
    [SerializeField] private GameObject settingsTitleHeader;   // Reference to the settings title header object in the menu.


    // Private fields to store the current volume and color adjustments for brightness control.
    private Volume currentVolume;
    private SceneBrightness sceneBrightness;
    private bool isPersistentInstance;

    private void Awake()
    {
        // Keep one controller alive while scenes change.
        if (Instance == null)
        {
            // Set this object as the active menu controller.
            Instance = this;
            isPersistentInstance = true;

            // Keep this object alive between scene loads.
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            // Keep scene-local instances alive so Timeline signal callbacks remain valid.
            isPersistentInstance = false;
        }

        // Load the initial brightness profile from the assigned volume.
        if (isPersistentInstance)
            FetchVolumeProfile(globalVolume);
    }

    private void OnEnable()
    {
        // Reapply brightness whenever a new scene loads.
        // Listen for scene load events.
        if (isPersistentInstance)
            SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        // Stop listening when this object is disabled.
        if (isPersistentInstance)
            SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void Start()
    {
        // Load saved preferences from disk (or use default values on first startup)
        float savedMusic = PlayerPrefs.GetFloat("MusicVolume", 8f);         // Slider range: 0–10
        float savedSFX = PlayerPrefs.GetFloat("SFXVolume", 8f);             // Slider range: 0–10
        float savedBrightness = PlayerPrefs.GetFloat("Brightness", 5f);    // Slider range: 0–10

        // THE SLIDERS TO MATCH THE LOADED VALUES PHYSICALLY
        if (musicSlider != null) 
        {
            musicSlider.SetValueWithoutNotify(savedMusic);
        }

        if (sfxSlider != null) 
        {
            sfxSlider.SetValueWithoutNotify(savedSFX);
        }

        if (brightnessSlider != null) 
        {
            brightnessSlider.SetValueWithoutNotify(savedBrightness);
        }

        // Loaded values to the Mixer and global lighting engine
        SetMusicVolume(savedMusic);
        SetSFXVolume(savedSFX);
        SetBrightness(savedBrightness);

        // Ensure Settings Panel is hidden and Main Menu buttons are visible when starting
        if (settingsPanel != null) 
            settingsPanel.SetActive(false);

        if (mainButtonsPanel != null) 
            mainButtonsPanel.SetActive(true);
    }

    //  This method is called whenever a new scene is loaded, allowing the menu controller to reapply settings and configurations.
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // Apply before rendering, including scene loads while timeScale is zero.
        ApplyLoadedSettingsToNewScene();
    }


    private void ApplyLoadedSettingsToNewScene()
    {
        // 1. Re-locate the Global Volume in the freshly loaded scene
        Volume newVolume = null;
        Volume[] sceneVolumes = FindObjectsByType<Volume>(FindObjectsInactive.Exclude);
        foreach (Volume volume in sceneVolumes)
        {
            if (volume.isGlobal && volume.gameObject.scene == SceneManager.GetActiveScene())
            {
                newVolume = volume;
                break;
            }
        }
        FetchVolumeProfile(newVolume);

        // 2. Load and slam your saved settings back into place immediately
        float savedBrightness = PlayerPrefs.GetFloat("Brightness", 5f);
        float savedMusic = PlayerPrefs.GetFloat("MusicVolume", 8f);
        float savedSFX = PlayerPrefs.GetFloat("SFXVolume", 8f);

        SetBrightness(savedBrightness);
        SetMusicVolume(savedMusic);
        SetSFXVolume(savedSFX);
        
        Debug.Log($"[Settings Manager] Settings successfully synchronized for scene! Brightness: {savedBrightness}");
    }



    private void FetchVolumeProfile(Volume targetVolume)
    {
        currentVolume = targetVolume;
        sceneBrightness = null;
        if (targetVolume == null) return;
        sceneBrightness = targetVolume.GetComponent<SceneBrightness>();
        if (sceneBrightness == null)
            sceneBrightness = targetVolume.gameObject.AddComponent<SceneBrightness>();
    }

    public void PlayGame()
    {
        // Save current slider positions to disk right before loading the new scene
        PlayerPrefs.Save();
        
        // Explicitly re-enforce the current slider values to lock the Audio Mixer channels
        SetMusicVolume(PlayerPrefs.GetFloat("MusicVolume", 8f));
        SetSFXVolume(PlayerPrefs.GetFloat("SFXVolume", 8f));

        // Load your cutscene
        SceneTransition.Load(gameSceneName);
    }


    // This method is called when the intro cutscene finishes and the player is ready to enter Level 1.
    public void LoadPoliceHQ()
    {
        Debug.Log("[Scene Transition] Intro Cutscene finished. Loading PoliceHQ Level 1...");
        
        // Ensure settings are saved right before moving into the core gameplay level
        PlayerPrefs.Save();

        // Load your Level 1 scene
        SceneTransition.Load("PoliceHQ");
    }


    public void OpenSettings()
    {
        // Show the settings panel and hide the main menu buttons.
        if (mainButtonsPanel != null) mainButtonsPanel.SetActive(false);
        if (settingsPanel != null) settingsPanel.SetActive(true);
        
        // Hide the game title so it clears the top space
        if (gameTitleObject != null) gameTitleObject.SetActive(false);
        if (settingsTitleHeader != null) settingsTitleHeader.SetActive(true);
    }

    public void CloseSettings()
    {
        // Hide the settings panel and show the main menu buttons.
        if (settingsPanel != null) settingsPanel.SetActive(false);
        if (mainButtonsPanel != null) mainButtonsPanel.SetActive(true);
        
        // Bring the game title back when returning to the main screen
        if (gameTitleObject != null) gameTitleObject.SetActive(true);
        if (settingsTitleHeader != null) settingsTitleHeader.SetActive(false);
        
        PlayerPrefs.Save();
    }


    public void QuitGame()
    {
        // Print a message in the console before quitting.
        Debug.Log("Exiting Game...");
        #if UNITY_EDITOR
                // Stop play mode in the Unity editor.
                UnityEditor.EditorApplication.isPlaying = false;
        #else
                // Quit the game in a built application.
                Application.Quit();
        #endif
    }

    public void SetMusicVolume(float value)
    {
        if (mainAudioMixer != null)
        {
            // Convert the 0–10 slider value to 0–1 before converting to decibels.
            float normalizedValue = Mathf.Clamp01(value / 10f);
            float dB = Mathf.Log10(Mathf.Max(normalizedValue, 0.0001f)) * 20;
            mainAudioMixer.SetFloat("MusicVolume", dB); // Apply the converted music volume to the mixer.
        }

        PlayerPrefs.SetFloat("MusicVolume", value); // Save the music volume value.
    }

    public void SetSFXVolume(float value)
    {
        if (mainAudioMixer != null)
        {
            // Convert the 0–10 slider value to 0–1 before converting to decibels.
            float normalizedValue = Mathf.Clamp01(value / 10f);
            float dB = Mathf.Log10(Mathf.Max(normalizedValue, 0.0001f)) * 20;
            mainAudioMixer.SetFloat("SFXVolume", dB);               // Apply the sound effects volume to the mixer.
        }

        PlayerPrefs.SetFloat("SFXVolume", value); // Save the SFX volume value.
    }


    // Call this inside your ApplyLoadedSettingsToNewScene() method
    public void SetBrightness(float sliderValue)
    {
        // Scene-local menu callbacks must adjust the persistent controller's volume.
        if (Instance != null && Instance != this)
        {
            Instance.SetBrightness(sliderValue);
            return;
        }
        if (currentVolume == null)
        {
            foreach (Volume volume in FindObjectsByType<Volume>(FindObjectsInactive.Exclude))
                if (volume.isGlobal && volume.gameObject.scene == SceneManager.GetActiveScene())
                {
                    FetchVolumeProfile(volume);
                    break;
                }
        }
        // Keep the selected brightness value for the next session.
        sliderValue = Mathf.Clamp(sliderValue, 0f, 10f);
        PlayerPrefs.SetFloat("Brightness", sliderValue);

        if (sceneBrightness != null)
            sceneBrightness.Apply(sliderValue);
    }
}
