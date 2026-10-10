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

    [Tooltip("Gameplay UI roots to hide when showing the lose screen.")]
    public GameObject[] gameplayUIToHide = new GameObject[0];

    private bool isShowing;

    private void Awake()
    {
        // If this controller is placed directly on the Lose Canvas,
        // use that object automatically.
        if (loseCanvas == null)
            loseCanvas = gameObject;

        if (loseCanvas != null && !isShowing)
            loseCanvas.SetActive(false);
    }

    private void Start()
    {
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
        // Set before activation: an initially inactive controller can Awake here.
        isShowing = true;
        // If the controller is on the Canvas itself, use that Canvas.
        if (loseCanvas == null)
            loseCanvas = gameObject;

        foreach (GameObject ui in gameplayUIToHide)
        {
            // Keep the lose canvas and its ancestors active so its buttons remain usable.
            if (ui != null && !loseCanvas.transform.IsChildOf(ui.transform))
                ui.SetActive(false);
        }

        if (loseCanvas != null)
            loseCanvas.SetActive(true);

        // If the Canvas was inactive at scene start, make sure the
        // buttons are wired now that the lose UI is being shown.
        SetupButtons();

        Debug.Log("[LoseSceneController] Lose Canvas activated.");

        Time.timeScale = 0f;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public void TryAgain()
    {
        if (string.IsNullOrWhiteSpace(retrySceneName))
        {
            Debug.LogError("[LoseSceneController] Retry scene name is empty.");
            return;
        }

        Scene currentScene = SceneManager.GetActiveScene();
        if (retrySceneName == currentScene.name || retrySceneName == currentScene.path)
            RespawnPoint.PrepareForRetry(currentScene.path);
        else
            RespawnPoint.ResetCheckpoint();

        Time.timeScale = 1f;
        SceneTransition.Load(retrySceneName);
    }

    public void Exit()
    {
        if (string.IsNullOrWhiteSpace(exitSceneName))
        {
            Debug.LogError("[LoseSceneController] Exit scene name is empty.");
            return;
        }

        RespawnPoint.ResetCheckpoint();
        Time.timeScale = 1f;
        SceneTransition.Load(exitSceneName);
    }

}
