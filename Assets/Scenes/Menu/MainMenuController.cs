using UnityEngine;
using UnityEngine.SceneManagement; // Allows switching between Unity scenes
using UnityEngine.UI;              // Gives access to UI elements like Sliders and Panels
using UnityEngine.Audio;           // Allows controlling Unity AudioMixer parameters

public class MainMenuController : MonoBehaviour
{
    // ==========================================
    // SCENE & PANEL REFERENCES
    // ==========================================
    [Header("Scene Management")]
    [Tooltip("Type the exact name of your main gameplay scene here.")]
    [SerializeField] private string gameSceneName = "GameScene";

    [Header("UI Panels")]
    [Tooltip("Drag your ButtonPanel (containing Investigate, Options, Credits, Exit) here.")]
    [SerializeField] private GameObject mainButtonsPanel;

    [Tooltip("Drag your SettingsPanel object here.")]
    [SerializeField] private GameObject settingsPanel;

    // ==========================================
    // AUDIO REFERENCES
    // ==========================================
    [Header("Audio Settings")]
    [Tooltip("Drag your Main Audio Mixer asset here.")]
    [SerializeField] private AudioMixer mainAudioMixer;

    [Tooltip("Drag your Music Volume Slider object here.")]
    [SerializeField] private Slider musicSlider;

    [Tooltip("Drag your SFX Volume Slider object here.")]
    [SerializeField] private Slider sfxSlider;

    // ==========================================
    // GAMEPLAY & GRAPHICS REFERENCES
    // ==========================================
    [Header("Gameplay & Graphics Settings")]
    [Tooltip("Drag your Mouse Sensitivity Slider object here.")]
    [SerializeField] private Slider sensitivitySlider;

    [Tooltip("Drag your Brightness/Gamma Slider object here.")]
    [SerializeField] private Slider brightnessSlider;

    [Tooltip("Optional: Drag your scene's Directional Light here to adjust brightness visually.")]
    [SerializeField] private Light sceneDirectionalLight;

    // Start runs automatically when the scene begins
    private void Start()
    {
        // 1. Load saved preferences from disk (or use default values on first startup)
        if (musicSlider != null) 
            musicSlider.value = PlayerPrefs.GetFloat("MusicVolume", 0.75f); // Default 75% volume

        if (sfxSlider != null) 
            sfxSlider.value = PlayerPrefs.GetFloat("SFXVolume", 0.75f);   // Default 75% volume

        if (sensitivitySlider != null) 
            sensitivitySlider.value = PlayerPrefs.GetFloat("MouseSensitivity", 2.0f); // Default sensitivity 2.0

        if (brightnessSlider != null) 
            brightnessSlider.value = PlayerPrefs.GetFloat("Brightness", 1.0f); // Default brightness 1.0

        // 2. Ensure Settings Panel is hidden and Main Menu buttons are visible when starting
        if (settingsPanel != null) 
            settingsPanel.SetActive(false);

        if (mainButtonsPanel != null) 
            mainButtonsPanel.SetActive(true);
    }

    // ==========================================
    // BUTTON CLICK ACTIONS
    // ==========================================

    // Attach to the "Investigate" / Play Button OnClick() event
    public void PlayGame()
    {
        // Loads your main game level scene
        SceneManager.LoadScene(gameSceneName);
    }

    // Attach to the "Options" / Settings Button OnClick() event
    public void OpenSettings()
    {
        // Hides main buttons, reveals the settings overlay panel
        mainButtonsPanel.SetActive(false);
        settingsPanel.SetActive(true);
    }

    // Attach to the "Back" Button OnClick() event inside the Settings Panel
    public void CloseSettings()
    {
        // Hides settings overlay panel, reveals main buttons
        settingsPanel.SetActive(false);
        mainButtonsPanel.SetActive(true);

        // Save all slider changes to storage so settings persist next session
        PlayerPrefs.Save();
    }

    // Attach to the "Exit" Button OnClick() event
    public void QuitGame()
    {
        Debug.Log("Exiting Game...");
        // Closes the application executable
        Application.Quit();
    }

    // ==========================================
    // SLIDER ADJUSTMENT FUNCTIONS
    // ==========================================

    // Attach to MusicSlider's On Value Changed (Single) event
    public void SetMusicVolume(float value)
    {
        // Convert linear slider values (0.0001 to 1.0) into logarithmic Decibels (-80dB to 0dB) for AudioMixer
        if (mainAudioMixer != null)
            mainAudioMixer.SetFloat("MusicVolume", Mathf.Log10(Mathf.Clamp(value, 0.0001f, 1f)) * 20);

        // Save value locally
        PlayerPrefs.SetFloat("MusicVolume", value);
    }

    // Attach to SFXSlider's On Value Changed (Single) event
    public void SetSFXVolume(float value)
    {
        if (mainAudioMixer != null)
            mainAudioMixer.SetFloat("SFXVolume", Mathf.Log10(Mathf.Clamp(value, 0.0001f, 1f)) * 20);

        // Save value locally
        PlayerPrefs.SetFloat("SFXVolume", value);
    }

    // Attach to SensitivitySlider's On Value Changed (Single) event
    public void SetSensitivity(float value)
    {
        // Saves sensitivity float so your First-Person Player Controller script can read it during gameplay
        PlayerPrefs.SetFloat("MouseSensitivity", value);
    }

    // Attach to BrightnessSlider's On Value Changed (Single) event
    public void SetBrightness(float value)
    {
        // Adjusts global environment ambient lighting intensity
        RenderSettings.ambientIntensity = value;

        // Scale directional sun/moon light intensity if assigned
        if (sceneDirectionalLight != null) 
            sceneDirectionalLight.intensity = value;

        // Save value locally
        PlayerPrefs.SetFloat("Brightness", value);
    }
}