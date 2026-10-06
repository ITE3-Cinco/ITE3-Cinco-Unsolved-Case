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
    [Tooltip("The existing Lose Canvas/UI panel in the gameplay scene.")]
    public GameObject loseCanvas;

    private void Start()
    {
        // The Lose Canvas is part of the gameplay scene.
        // It must start hidden and only appear when the player is caught.
        if (loseCanvas != null)
            loseCanvas.SetActive(false);

        if (tryAgainButton != null)
            tryAgainButton.onClick.AddListener(TryAgain);

        if (exitButton != null)
            exitButton.onClick.AddListener(Exit);

        Time.timeScale = 1f;
    }

    private void OnDestroy()
    {
        if (tryAgainButton != null)
            tryAgainButton.onClick.RemoveListener(TryAgain);

        if (exitButton != null)
            exitButton.onClick.RemoveListener(Exit);
    }

    public void ShowLoseScreen()
    {
        if (loseCanvas != null)
            loseCanvas.SetActive(true);

        Time.timeScale = 0f;
    }

    public void TryAgain()
    {
        Time.timeScale = 1f;

        if (string.IsNullOrWhiteSpace(retrySceneName))
        {
            Debug.LogError("[LoseSceneController] Retry scene name is empty.");
            return;
        }

        SceneManager.LoadScene(retrySceneName);
    }

    public void Exit()
    {
        Time.timeScale = 1f;

        if (string.IsNullOrWhiteSpace(exitSceneName))
        {
            Debug.LogError("[LoseSceneController] Exit scene name is empty.");
            return;
        }

        SceneManager.LoadScene(exitSceneName);
    }
}