using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class PauseManager : MonoBehaviour
{
    [Header("UI Panels")]
    [Tooltip("Drag your Pause Menu UI Canvas panel from the hierarchy here!")]
    [SerializeField] private GameObject pauseMenuPanel;

    [Tooltip("Drag the in-game floating corner Pause Button object here!")]
    [SerializeField] private GameObject gameplayPauseButton;

    public static bool IsGamePaused { get; private set; }

    private void Start()
    {
        IsGamePaused = false;
        SetPauseUI(false);
        if (!SceneTransition.IsLoading) Time.timeScale = 1f;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void Update()
    {
        if (SceneTransition.IsLoading) return;
        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            if (IsGamePaused)
                ResumeGame();
            else
                PauseGame();
        }
    }

    public void PauseGame()
    {
        if (!isActiveAndEnabled || SceneTransition.IsLoading || Time.timeScale <= 0f) return;
        IsGamePaused = true;
        SetPauseUI(true);

        Time.timeScale = 0f;

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public void ResumeGame()
    {
        if (!isActiveAndEnabled || SceneTransition.IsLoading) return;
        IsGamePaused = false;
        SetPauseUI(false);

        Time.timeScale = 1f;

        // First-person gameplay needs the cursor locked again.
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void SetPauseUI(bool paused)
    {
        if (pauseMenuPanel != null) pauseMenuPanel.SetActive(paused);
        if (gameplayPauseButton != null) gameplayPauseButton.SetActive(!paused);
    }

    public void EndGameplay()
    {
        IsGamePaused = false;
        SetPauseUI(false);
        if (gameplayPauseButton != null) gameplayPauseButton.SetActive(false);
        enabled = false;
    }

    public void RestartLevel()
    {
        if (!isActiveAndEnabled || SceneTransition.IsLoading) return;
        RespawnPoint.ResetCheckpoint();
        Time.timeScale = 1f;
        IsGamePaused = false;
        SceneTransition.Load(SceneManager.GetActiveScene().name);
    }

    public void ExitToMainMenu()
    {
        if (!isActiveAndEnabled || SceneTransition.IsLoading) return;
        RespawnPoint.ResetCheckpoint();
        Time.timeScale = 1f;
        IsGamePaused = false;
        SceneTransition.Load("MainMenu");
    }

    private void OnDestroy()
    {
        // Prevent a stale static pause state from leaking into another scene.
        IsGamePaused = false;
        Time.timeScale = 1f;
    }
}
