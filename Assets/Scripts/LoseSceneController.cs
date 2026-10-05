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
    public GameObject loseCanvas;

    private void Start()
    {
        // This is a dedicated Lose Scene, so the lose canvas should be visible by default.
        if (loseCanvas != null)
            loseCanvas.SetActive(true);

        if (tryAgainButton != null)
            tryAgainButton.onClick.AddListener(TryAgain);

        if (exitButton != null)
            exitButton.onClick.AddListener(Exit);

        Time.timeScale = 1f);
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
        SceneManager.LoadScene(retrySceneName);
    }

    public void Exit()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(exitSceneName);
    }
}