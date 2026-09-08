using UnityEngine; // Unity core features, such as MonoBehaviour and GameObject.
using UnityEngine.SceneManagement; // Used to load scenes and detect scene changes.
using UnityEngine.UI; // Used for UI elements like sliders and buttons.
using UnityEngine.Audio; // Used to control the audio mixer.
using UnityEngine.Rendering; // Used for global rendering effects and volume settings.
using UnityEngine.Rendering.Universal; // Used for URP post-processing effects like brightness.

public class MainMenuController : MonoBehaviour
{
    public static MainMenuController Instance; // Stores the single active menu controller instance.

    [Header("Scene Management")]                    // Groups all scene-related settings together in the Inspector.
    [Tooltip("Exact name of the gameplay scene.")]  // Shows what this field is for when hovered.
    [SerializeField] private string gameSceneName = "GameScene"; // Name of the scene to load when the player starts the game.

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


    // Stores the brightness adjustment data from the URP volume profile.
    private ColorAdjustments colorAdjustments;  

    private void Awake()
    {
        // Keep one controller alive while scenes change.
        if (Instance == null)
        {
            // Set this object as the active menu controller.
            Instance = this;

            // Keep this object alive between scene loads.
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            // Remove duplicate menu controllers.
            Destroy(gameObject);

            // Stop this object from continuing.
            return;
        }

        // Load the initial brightness profile from the assigned volume.
        FetchVolumeProfile(globalVolume);
    }

    private void OnEnable()
    {
        // Reapply brightness whenever a new scene loads.
        // Listen for scene load events.
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        // Stop listening when this object is disabled.
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void Start()
    {
        // Load saved preferences from disk (or use default values on first startup)
        float savedMusic = PlayerPrefs.GetFloat("MusicVolume", 0.75f);      // Default 75% volume
        float savedSFX = PlayerPrefs.GetFloat("SFXVolume", 0.75f);          // Default 75% volume
        float savedBrightness = PlayerPrefs.GetFloat("Brightness", 0.5f);   // Default 50% middle ground

        // THE SLIDERS TO MATCH THE LOADED VALUES PHYSICALLY
        if (musicSlider != null) 
        {
            musicSlider.value = savedMusic;
        }

        if (sfxSlider != null) 
        {
            sfxSlider.value = savedSFX;
        }

        if (brightnessSlider != null) 
        {
            brightnessSlider.value = savedBrightness;
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

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // Find the new scene's volume and restore brightness.
        // Find the current scene's volume object.
        Volume newVolume = FindAnyObjectByType<Volume>();

        // Get the brightness settings from the current scene.
        FetchVolumeProfile(newVolume);

        // Restore the saved brightness value.
        SetBrightness(PlayerPrefs.GetFloat("Brightness", 0.5f));
    }

    private void FetchVolumeProfile(Volume targetVolume)
    {
        if (targetVolume != null && targetVolume.profile != null)
        {
            // Read the ColorAdjustments effect from the volume profile.
            targetVolume.profile.TryGet(out colorAdjustments);
        }
    }

    public void PlayGame()
    {
        // Load the gameplay scene.
        SceneManager.LoadScene(gameSceneName);
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
            // Mixers use decibels, so convert the slider's 0-1 value.
            float dB = Mathf.Log10(Mathf.Max(value, 0.0001f)) * 20; // Convert a 0–1 slider value to decibels.
            mainAudioMixer.SetFloat("MusicVolume", dB); // Apply the converted music volume to the mixer.
        }

        PlayerPrefs.SetFloat("MusicVolume", value); // Save the music volume value.
    }

    public void SetSFXVolume(float value)
    {
        if (mainAudioMixer != null)
        {
            float dB = Mathf.Log10(Mathf.Max(value, 0.0001f)) * 20; // Convert the slider value to decibels for SFX.
            mainAudioMixer.SetFloat("SFXVolume", dB);               // Apply the sound effects volume to the mixer.
        }

        PlayerPrefs.SetFloat("SFXVolume", value); // Save the SFX volume value.
    }

    public void SetBrightness(float value)
    {
        if (colorAdjustments != null)
        {
            // Original exposure calculation
            colorAdjustments.postExposure.value = Mathf.Lerp(-1.5f, 1.5f, value);
        }

        // Slightly tweak the ambient lighting intensity based on the slider.
        // This forces Unity's Render Graph to dirty its frame cache and redraw 
        // the Game view window instantly, even if the menu is completely static!
        RenderSettings.ambientIntensity = Mathf.Max(value, 0.01f);
        
        PlayerPrefs.SetFloat("Brightness", value);
    }
}