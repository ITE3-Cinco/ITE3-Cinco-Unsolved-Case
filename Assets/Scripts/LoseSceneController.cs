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

    private void Awake()
    {
        // If this controller is placed directly on the Lose Canvas,
        // use that object automatically.
        if (loseCanvas == null)
            loseCanvas = gameObject;

        if (loseCanvas != null)
            loseCanvas.SetActive(false);
    }

    private void Start()
    {
        Time.timeScale = 1f;
        SetupButtons();
    }

    private void SetupButtons()
    {
        if (tryAgainButton != null)
        {
            tryAgainButton.onClick.RemoveListener(TryAgain);
            tryAgainButton.onClick.AddListener(TryAgain);
        }

        if (exitButton != null)
        {
            exitButton.onClick.RemoveListener(Exit);
            exitButton.onClick.AddListener(Exit);
        }
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
        // If the controller is on the Canvas itself, use that Canvas.
        if (loseCanvas == null)
            loseCanvas = gameObject;

        if (loseCanvas != null)
            loseCanvas.SetActive(true);

        // If the Canvas was inactive at scene start, make sure the
        // buttons are wired now that the lose UI is being shown.
        SetupButtons();

        Debug.Log("[LoseSceneController] Lose Canvas activated.");

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