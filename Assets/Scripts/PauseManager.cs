// Purpose: Opens the pause menu and switches between UI control and gameplay.
// Use: Attach to a gameplay manager; assign the pause panel and corner button.
// How it works: Escape toggles pause; timeScale freezes gameplay while the cursor is released for menu clicks.

// UnityEngine supplies GameObjects, components, transforms, time, math and physics used here.
using UnityEngine;
// Unity Input System supplies the current keyboard/mouse and their key or pointer states.
using UnityEngine.InputSystem;
// Unity SceneManagement supplies scene data, scene-loaded events and scene-loading calls.
using UnityEngine.SceneManagement;

public class PauseManager : MonoBehaviour
{
    // pauseMenuPanel: Drag your Pause Menu UI Canvas panel from the hierarchy here!. Assigned in the scene/prefab
    // Inspector.
    [Header("UI Panels")]
    [Tooltip("Drag your Pause Menu UI Canvas panel from the hierarchy here!")]
    [SerializeField] private GameObject pauseMenuPanel;

    // gameplayPauseButton: Drag the in-game floating corner Pause Button object here!. Assigned in the
    // scene/prefab Inspector.
    [Tooltip("Drag the in-game floating corner Pause Button object here!")]
    [SerializeField] private GameObject gameplayPauseButton;

    // IsGamePaused can be read by other scripts; only this class can write its backing state.
    public static bool IsGamePaused { get; private set; }

    /// <summary>Begin the scene in the resumed state with gameplay cursor control.</summary>
    private void Start()
    {
        // Call PauseManager.ResumeGame; hide pause controls, restore gameplay time and capture the mouse pointer.
        ResumeGame();
    }

    /// <summary>Toggle pause when Escape is pressed, unless a scene is loading.</summary>
    private void Update()
    {
        // If SceneTransition.IsLoading, return now; skip the rest of this method.
        if (SceneTransition.IsLoading) return;
        // If the current Input System keyboard exists and escape key on the current Input System keyboard was
        // newly pressed this frame, run the following block.
        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            // If IsGamePaused, run the following block.
            if (IsGamePaused)
                // Call PauseManager.ResumeGame; hide pause controls, restore gameplay time and capture the mouse
                // pointer.
                ResumeGame();
            /// <summary>Reveal pause controls, freeze gameplay time and release the mouse pointer.</summary>
            else
                // Call PauseManager.PauseGame; reveal pause controls, freeze gameplay time and release the mouse
                // pointer.
                PauseGame();
        }
    }

    /// <summary>Reveal pause controls, freeze gameplay time and release the mouse pointer.</summary>
    public void PauseGame()
    {
        // If SceneTransition.IsLoading, return now; skip the rest of this method.
        if (SceneTransition.IsLoading) return;
        // Set PauseManager shared pause flag; gameplay scripts read it before processing input or behavior.
        IsGamePaused = true;

        // If pauseMenuPanel exists, run the following block.
        if (pauseMenuPanel != null)
            // Activate pauseMenuPanel.
            pauseMenuPanel.SetActive(true);

        // If gameplayPauseButton exists, run the following block.
        if (gameplayPauseButton != null)
            // Deactivate gameplayPauseButton.
            gameplayPauseButton.SetActive(false);

        // Set Unity gameplay clock multiplier to zero; scaled movement/timers stop, while code using real time can
        // continue.
        Time.timeScale = 0f;

        // Release Unity pointer capture so menu buttons can be clicked.
        Cursor.lockState = CursorLockMode.None;
        // Show Unity mouse pointer while UI needs clicks.
        Cursor.visible = true;
    }

    /// <summary>Hide pause controls, restore gameplay time and capture the mouse pointer.</summary>
    public void ResumeGame()
    {
        // Clear PauseManager shared pause flag so gameplay scripts can resume their updates.
        IsGamePaused = false;

        // If pauseMenuPanel exists, run the following block.
        if (pauseMenuPanel != null)
            // Deactivate pauseMenuPanel.
            pauseMenuPanel.SetActive(false);

        // If gameplayPauseButton exists, run the following block.
        if (gameplayPauseButton != null)
            // Activate gameplayPauseButton.
            gameplayPauseButton.SetActive(true);

        // Restore Unity gameplay clock multiplier to its normal rate so movement and scaled timers can advance.
        Time.timeScale = 1f;

        // First-person gameplay needs the cursor locked again.
        // Capture the Unity pointer at the view center for first-person mouse look.
        Cursor.lockState = CursorLockMode.Locked;
        // Hide Unity mouse pointer during first-person control.
        Cursor.visible = false;
    }

    /// <summary>Clear pause state and reload the currently active scene.</summary>
    public void RestartLevel()
    {
        // Restore Unity gameplay clock multiplier to its normal rate so movement and scaled timers can advance.
        Time.timeScale = 1f;
        // Clear PauseManager shared pause flag so gameplay scripts can resume their updates.
        IsGamePaused = false;
        // Call SceneTransition.Load with SceneManager.GetActiveScene().name; validate the destination, guard
        // against duplicate loads and create a host that survives the change.
        SceneTransition.Load(SceneManager.GetActiveScene().name);
    }

    /// <summary>Clear pause state and load the main menu.</summary>
    public void ExitToMainMenu()
    {
        // Restore Unity gameplay clock multiplier to its normal rate so movement and scaled timers can advance.
        Time.timeScale = 1f;
        // Clear PauseManager shared pause flag so gameplay scripts can resume their updates.
        IsGamePaused = false;
        // Call SceneTransition.Load with "MainMenu"; validate the destination, guard against duplicate loads and
        // create a host that survives the change.
        SceneTransition.Load("MainMenu");
    }

    /// <summary>Reset shared pause state so a new scene does not inherit it.</summary>
    private void OnDestroy()
    {
        // Prevent a stale static pause state from leaking into another scene.
        // Clear PauseManager shared pause flag so gameplay scripts can resume their updates.
        IsGamePaused = false;
        // Restore Unity gameplay clock multiplier to its normal rate so movement and scaled timers can advance.
        Time.timeScale = 1f;
    }
}