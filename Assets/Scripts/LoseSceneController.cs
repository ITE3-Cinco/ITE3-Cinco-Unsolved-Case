using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class LoseSceneController : MonoBehaviour
{
    [Header("Scene Names")]
    public string retrySceneName = "PoliceHQ";
    public string exitSceneName = "MainMenu";

    [Header("Buttons")]
    public Button tryAgainButton;
    public Button exitButton;

    [Header("References")]
    public GameObject loseCanvas;   // Drag the LoseCanvas here

    void Start()
    {
        // Hide the lose screen at the beginning
        if (loseCanvas != null)
            loseCanvas.SetActive(false);

        // Wire buttons
        if (tryAgainButton != null)
            tryAgainButton.onClick.AddListener(TryAgain);

        if (exitButton != null)
            exitButton.onClick.AddListener(Exit);
    }

    // Call this when the ghost catches the player
    public void ShowLoseScreen()
    {
        if (loseCanvas != null)
            loseCanvas.SetActive(true);

        // Optional: pause the game
        Time.timeScale = 0f;
    }

    public void TryAgain()
    {
        Time.timeScale = 1f;   // unpause
        SceneManager.LoadScene(retrySceneName);
    }

    public void Exit()
    {
        Time.timeScale = 1f;   // unpause
        SceneManager.LoadScene(exitSceneName);
    }
}