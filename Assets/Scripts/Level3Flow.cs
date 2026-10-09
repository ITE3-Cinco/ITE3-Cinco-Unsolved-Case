using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>Owns Level 3's mutually exclusive catch/escape outcomes.</summary>
public class Level3Flow : MonoBehaviour
{
    public PlayerMovement player;
    public RenatoEnemy renato;
    public LoseSceneController loseController;
    public BoxCollider mainExit;
    public PauseManager pauseManager;
    public Button pauseButton;
    public ScreenFadeController fade;
    public TMP_FontAsset endingFont;
    public float fadeDuration = 1.5f;
    public bool Finished { get; private set; }

    private void LateUpdate()
    {
        // CharacterController overlap fallback also handles spawning/stepping into the volume.
        if (Finished || Time.timeScale <= 0f || PauseManager.IsGamePaused || mainExit == null || player == null) return;
        CharacterController body = player.GetComponent<CharacterController>();
        Vector3 center = body != null ? body.bounds.center : player.transform.position;
        if (mainExit.bounds.Contains(center)) Escape();
    }

    public void Caught()
    {
        if (Finished || loseController == null || player == null) return;
        Finish();
        PlayerAudio audio = player.GetComponent<PlayerAudio>();
        if (audio != null) audio.PlayCaught();
        loseController.ShowLoseScreen();
    }

    public void Escape()
    {
        if (Finished || player == null || Time.timeScale <= 0f || PauseManager.IsGamePaused) return;
        Finish();
        StartCoroutine(ShowEnding());
    }

    private void Finish()
    {
        // Commit the outcome before activating UI/coroutines: catch and escape share this guard.
        Finished = true;
        if (renato != null) renato.Stop();
        player.enabled = false;
        foreach (MouseLook look in player.GetComponentsInChildren<MouseLook>(true)) look.enabled = false;
        foreach (PlayerInteraction interaction in player.GetComponentsInChildren<PlayerInteraction>(true)) interaction.enabled = false;
        Inventory inventory = player.GetComponent<Inventory>();
        if (inventory != null) inventory.enabled = false;
        PlayerRespawn respawn = player.GetComponent<PlayerRespawn>();
        if (respawn != null) respawn.enabled = false;
        if (pauseManager != null) pauseManager.enabled = false;
        if (pauseButton != null) pauseButton.interactable = false;
        Time.timeScale = 0f;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    private IEnumerator ShowEnding()
    {
        if (fade == null) fade = ScreenFadeController.Instance;
        if (fade == null) fade = gameObject.AddComponent<ScreenFadeController>();
        fade.FadeToBlack(fadeDuration);
        yield return new WaitUntil(() => fade.fadeImage != null && fade.fadeImage.color.a >= 1f);
        // Reuse the fade's black panel as the ending background. No second canvas is needed.
        Canvas canvas = fade.fadeImage.canvas;
        if (canvas.GetComponent<GraphicRaycaster>() == null) canvas.gameObject.AddComponent<GraphicRaycaster>();
        foreach (Canvas other in FindObjectsByType<Canvas>(FindObjectsInactive.Exclude))
            if (other != canvas && !other.transform.IsChildOf(canvas.transform)) other.enabled = false;
        TextMeshProUGUI title = CreateText("Escape ending", fade.fadeImage.transform, "You escaped with the truth...");
        Place(title.rectTransform, new Vector2(0.1f, 0.5f), new Vector2(0.9f, 0.7f));
        GameObject buttonObject = new GameObject("Main Menu", typeof(RectTransform), typeof(Image), typeof(Button));
        buttonObject.transform.SetParent(fade.fadeImage.transform, false);
        Place((RectTransform)buttonObject.transform, new Vector2(0.35f, 0.3f), new Vector2(0.65f, 0.42f));
        Image background = buttonObject.GetComponent<Image>();
        background.color = new Color(0.15f, 0.15f, 0.15f, 1f);
        Button button = buttonObject.GetComponent<Button>();
        button.targetGraphic = background;
        button.onClick.AddListener(ReturnToMenu);
        TextMeshProUGUI label = CreateText("Label", buttonObject.transform, "Main Menu");
        Place(label.rectTransform, Vector2.zero, Vector2.one);
        button.Select();
    }

    private TextMeshProUGUI CreateText(string objectName, Transform parent, string text)
    {
        GameObject go = new GameObject(objectName, typeof(RectTransform), typeof(TextMeshProUGUI));
        go.transform.SetParent(parent, false);
        TextMeshProUGUI label = go.GetComponent<TextMeshProUGUI>();
        if (endingFont != null) label.font = endingFont;
        label.text = text;
        label.fontSize = 36f;
        label.enableAutoSizing = true;
        label.fontSizeMin = 16f;
        label.fontSizeMax = 36f;
        label.alignment = TextAlignmentOptions.Center;
        label.raycastTarget = false;
        return label;
    }

    private static void Place(RectTransform rect, Vector2 min, Vector2 max)
    {
        rect.anchorMin = min;
        rect.anchorMax = max;
        rect.offsetMin = rect.offsetMax = Vector2.zero;
    }

    private void ReturnToMenu()
    {
        RespawnPoint.ResetCheckpoint();
        Time.timeScale = 1f;
        SceneManager.LoadScene("MainMenu");
    }
}
