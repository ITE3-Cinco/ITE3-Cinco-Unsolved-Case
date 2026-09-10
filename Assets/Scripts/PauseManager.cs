using UnityEngine;
// 1. CRUCIAL: Add this namespace at the very top of your script!
using UnityEngine.InputSystem;

public class PauseManager : MonoBehaviour
{
    [Header("UI Panels")]
    [Tooltip("Drag your Pause Menu UI Canvas panel from the hierarchy here!")]
    [SerializeField] private GameObject pauseMenuPanel;

    [Tooltip("Drag the in-game floating corner Pause Button object here!")]
    [SerializeField] private GameObject gameplayPauseButton;

    public static bool IsGamePaused = false;

    private void Start()
    {
        ResumeGame();
    }

    private void Update()
    {
        // 2. FIX: Replace legacy Input.GetKeyDown with the modern Unity 6 input package check
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
        if (pauseMenuPanel != null) pauseMenuPanel.SetActive(true);
        if (gameplayPauseButton != null) gameplayPauseButton.SetActive(false);

        Time.timeScale = 0f;

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public void ResumeGame()
    {
        IsGamePaused = false;
        if (pauseMenuPanel != null) pauseMenuPanel.SetActive(false);
        if (gameplayPauseButton != null) gameplayPauseButton.SetActive(true);

        Time.timeScale = 1f;

        // Unlocks cursor if you aren't using a first-person lock system
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public void RestartLevel()
    {
        Time.timeScale = 1f;
        UnityEngine.SceneManagement.SceneManager.LoadScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene().name);
    }

    public void ExitToMainMenu()
    {
        Time.timeScale = 1f;
        UnityEngine.SceneManagement.SceneManager.LoadScene("MainMenu");
    }
}