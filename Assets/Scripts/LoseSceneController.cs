// Purpose: Shows the lose panel and handles retry and exit buttons.
// Use: Attach to the scene lose UI; assign panels, buttons and valid destination scene names.
// How it works: Catching activates the panel and freezes play; retry may restore a checkpoint in the same scene.

// UnityEngine supplies GameObjects, components, transforms, time, math and physics used here.
using UnityEngine;
// Unity SceneManagement supplies scene data, scene-loaded events and scene-loading calls.
using UnityEngine.SceneManagement;
// Unity UI supplies canvas widgets such as images, sliders and buttons.
using UnityEngine.UI;

public class LoseSceneController : MonoBehaviour
{
    // retrySceneName: build-scene name loaded by the Try Again button. Assigned in the scene/prefab Inspector; the
    // initializer below is the default.
    [Header("Scene Names")]
    public string retrySceneName = "PoliceHQ";
    // exitSceneName: build-scene name loaded by the menu/exit button. Assigned in the scene/prefab Inspector; the
    // initializer below is the default.
    public string exitSceneName = "MainMenu";

    // tryAgainButton: try again button. Assigned in the scene/prefab Inspector.
    [Header("Buttons")]
    public Button tryAgainButton;
    // exitButton: exit button. Assigned in the scene/prefab Inspector.
    public Button exitButton;

    // loseCanvas: The existing Lose Canvas/UI panel in the gameplay scene. Awake sets it from the GameObject that
    // owns this component.
    [Header("References")]
    [Tooltip("The existing Lose Canvas/UI panel in the gameplay scene.")]
    public GameObject loseCanvas;

    // gameplayUIToHide: Gameplay UI roots to hide when showing the lose screen. Assigned in the scene/prefab
    // Inspector; the initializer below is the default.
    [Tooltip("Gameplay UI roots to hide when showing the lose screen.")]
    public GameObject[] gameplayUIToHide = new GameObject[0];

    // isShowing: guard preventing the loss screen from opening twice. Updated by ShowLoseScreen.
    private bool isShowing;

    /// <summary>Resolve the lose panel and keep it hidden until a catch requests it.</summary>
    private void Awake()
    {
        // If this controller is placed directly on the Lose Canvas,
        // use that object automatically.
        // If loseCanvas is missing, run the following block.
        if (loseCanvas == null)
            // Copy gameObject into loseCanvas.
            loseCanvas = gameObject;

        // If loseCanvas exists and not (isShowing), run the following block.
        if (loseCanvas != null && !isShowing)
            // Deactivate loseCanvas.
            loseCanvas.SetActive(false);
    }

    /// <summary>Connect the retry and exit buttons when this controller starts.</summary>
    private void Start()
    {
        // Call LoseSceneController.SetupButtons; replace this controller callbacks before adding them so buttons
        // do not call twice.
        SetupButtons();
    }

    /// <summary>Replace this controller callbacks before adding them so buttons do not call twice.</summary>
    private void SetupButtons()
    {
        // If tryAgainButton exists, run the following block.
        if (tryAgainButton != null)
        {
            // Disconnect TryAgain as a callback on tryAgainButton.onClick.
            tryAgainButton.onClick.RemoveListener(TryAgain);
            // Connect TryAgain as a callback on tryAgainButton.onClick.
            tryAgainButton.onClick.AddListener(TryAgain);
        }

        // If exitButton exists, run the following block.
        if (exitButton != null)
        {
            // Disconnect Exit as a callback on exitButton.onClick.
            exitButton.onClick.RemoveListener(Exit);
            // Connect Exit as a callback on exitButton.onClick.
            exitButton.onClick.AddListener(Exit);
        }
    }

    /// <summary>Remove button callbacks when the controller is destroyed.</summary>
    private void OnDestroy()
    {
        // If tryAgainButton exists, run the following block.
        if (tryAgainButton != null)
            // Disconnect TryAgain as a callback on tryAgainButton.onClick.
            tryAgainButton.onClick.RemoveListener(TryAgain);

        // If exitButton exists, run the following block.
        if (exitButton != null)
            // Disconnect Exit as a callback on exitButton.onClick.
            exitButton.onClick.RemoveListener(Exit);
    }

    /// <summary>Hide gameplay UI, reveal the lose panel, freeze gameplay and allow pointer input.</summary>
    public void ShowLoseScreen()
    {
        // Set before activation: an initially inactive controller can Awake here.
        // Set isShowing to true.
        isShowing = true;
        // If the controller is on the Canvas itself, use that Canvas.
        // If loseCanvas is missing, run the following block.
        if (loseCanvas == null)
            // Copy gameObject into loseCanvas.
            loseCanvas = gameObject;

        // Process each ui in gameplayUIToHide.
        foreach (GameObject ui in gameplayUIToHide)
        {
            // Keep the lose canvas and its ancestors active so its buttons remain usable.
            // If ui exists and not (loseCanvas.transform belongs to ui.transform Transform hierarchy), run the
            // following block.
            if (ui != null && !loseCanvas.transform.IsChildOf(ui.transform))
                // Deactivate ui.
                ui.SetActive(false);
        }

        // If loseCanvas exists, run the following block.
        if (loseCanvas != null)
            // Activate loseCanvas.
            loseCanvas.SetActive(true);

        // If the Canvas was inactive at scene start, make sure the
        // buttons are wired now that the lose UI is being shown.
        // Call LoseSceneController.SetupButtons; replace this controller callbacks before adding them so buttons
        // do not call twice.
        SetupButtons();

        // Write the diagnostic message to the Unity Console; it does not update game UI.
        Debug.Log("[LoseSceneController] Lose Canvas activated.");

        // Set Unity gameplay clock multiplier to zero; scaled movement/timers stop, while code using real time can
        // continue.
        Time.timeScale = 0f;
        // Release Unity pointer capture so menu buttons can be clicked.
        Cursor.lockState = CursorLockMode.None;
        // Show Unity mouse pointer while UI needs clicks.
        Cursor.visible = true;
    }

    /// <summary>Validate the retry destination, prepare matching checkpoint history and reload the scene.</summary>
    public void TryAgain()
    {
        // If the result of string.IsNullOrWhiteSpace using retrySceneName, run the following block.
        if (string.IsNullOrWhiteSpace(retrySceneName))
        {
            // Report the failed setup or state as a Unity Console error.
            Debug.LogError("[LoseSceneController] Retry scene name is empty.");
            // Return now; skip the rest of this method.
            return;
        }

        // Store the result of SceneManager.GetActiveScene in local currentScene.
        Scene currentScene = SceneManager.GetActiveScene();
        // If retrySceneName equals currentScene.name or retrySceneName equals currentScene.path, run the following
        // block.
        if (retrySceneName == currentScene.name || retrySceneName == currentScene.path)
            // Call RespawnPoint.PrepareForRetry with currentScene.path; mark an explicit retry for the saved
            // scene, clearing unrelated checkpoint history.
            RespawnPoint.PrepareForRetry(currentScene.path);
        else
            // Call RespawnPoint.ResetCheckpoint; clear both collection history and the saved spawn for a fresh
            // run.
            RespawnPoint.ResetCheckpoint();

        // Restore Unity gameplay clock multiplier to its normal rate so movement and scaled timers can advance.
        Time.timeScale = 1f;
        // Call SceneTransition.Load with retrySceneName; validate the destination, guard against duplicate loads
        // and create a host that survives the change.
        SceneTransition.Load(retrySceneName);
    }

    /// <summary>Validate the exit destination, clear progress and load the menu scene.</summary>
    public void Exit()
    {
        // If the result of string.IsNullOrWhiteSpace using exitSceneName, run the following block.
        if (string.IsNullOrWhiteSpace(exitSceneName))
        {
            // Report the failed setup or state as a Unity Console error.
            Debug.LogError("[LoseSceneController] Exit scene name is empty.");
            // Return now; skip the rest of this method.
            return;
        }

        // Call RespawnPoint.ResetCheckpoint; clear both collection history and the saved spawn for a fresh run.
        RespawnPoint.ResetCheckpoint();
        // Restore Unity gameplay clock multiplier to its normal rate so movement and scaled timers can advance.
        Time.timeScale = 1f;
        // Call SceneTransition.Load with exitSceneName; validate the destination, guard against duplicate loads
        // and create a host that survives the change.
        SceneTransition.Load(exitSceneName);
    }

}
