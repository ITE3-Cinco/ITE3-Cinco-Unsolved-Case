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
        ResumeGame();
    }

    private void Update()
    {
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
        IsGamePaused = true;

        if (pauseMenuPanel != null)
            pauseMenuPanel.SetActive(true);

        if (gameplayPauseButton != null)
            gameplayPauseButton.SetActive(false);

        Time.timeScale = 0f;

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public void ResumeGame()
    {
        IsGamePaused = false;

        if (pauseMenuPanel != null)
            pauseMenuPanel.SetActive(false);

        if (gameplayPauseButton != null)
            gameplayPauseButton.SetActive(true);

        Time.timeScale = 1f;

        // First-person gameplay needs the cursor locked again.
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    public void RestartLevel()
    {
        Time.timeScale = 1f;
        IsGamePaused = false;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void ExitToMainMenu()
    {
        Time.timeScale = 1f;
        IsGamePaused = false;
        SceneManager.LoadScene("MainMenu");
    }

    private void OnDestroy()
    {
        // Prevent a stale static pause state from leaking into another scene.
        IsGamePaused = false;
        Time.timeScale = 1f;
    }
}