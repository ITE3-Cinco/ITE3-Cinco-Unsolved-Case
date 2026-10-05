using UnityEngine;
using UnityEngine.SceneManagement;

public class LoseSceneController : MonoBehaviour
{
    [Header("Scene Names")]
    [Tooltip("The level the player should return to when trying again.")]
    public string retrySceneName = "PoliceHQ";

    [Tooltip("Optional scene for the Exit button. Leave empty to return to the main menu.")]
    public string exitSceneName = "";

    public void TryAgain()
    {
        if (string.IsNullOrEmpty(retrySceneName))
        {
            Debug.LogWarning("[LoseScene] No retry scene name is assigned.");
            return;
        }

        SceneManager.LoadScene(retrySceneName);
    }

    public void Exit()
    {
        string sceneToLoad = string.IsNullOrEmpty(exitSceneName)
            ? "MainMenu"
            : exitSceneName;

        SceneManager.LoadScene(sceneToLoad);
    }
}
