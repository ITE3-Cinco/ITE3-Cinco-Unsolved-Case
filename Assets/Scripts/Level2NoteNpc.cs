using UnityEngine;
using UnityEngine.Events;

/// <summary>Scene-local story state; Note progress belongs to ArtifactManager.
/// A lose-screen retry reloads both together; an in-scene respawn retains both.</summary>
public class Level2NoteNpc : Interactable
{
    public enum StoryState { NotStarted, FindingClues, Returned, Revealing, ReadyToLeave }

    public ArtifactManager artifacts;
    public ObjectiveItem objective;
    public NotificationManager notifications;
    public StationaryNervousIdle nervousIdle;
    public string nextSceneName = "Level3";
    public UnityEvent onCluesReturned = new UnityEvent();

    [Header("Dialogue (advance by interacting again)")]
    [TextArea(3, 6)] public string initialDialogue = "Please... I'm scared. Something terrible happened here. There are Notes scattered around this place. Please find ALL of them, then come back to me. I can't explain everything yet... Be careful out there.";
    [TextArea(3, 6)] public string returnedDialogue = "You found everything... Thank you. These clues can finally bring the truth to light. Please, listen. There's something I need to tell you.";
    [TextArea(3, 6)] public string[] revealDialogue =
    {
        "Renato... the coworker you met before coming here. He is the serial killer behind this cold case. These Notes are the evidence of what he did.",
        "I was one of his victims. The woman you've seen in white... that's me. I am the White Lady.",
        "I was trying to reach you, to warn you about Renato. I need the truth exposed. I need justice for all of us. Take this evidence and survive."
    };

    public StoryState State { get; private set; }
    public bool AllCluesCollected => artifacts != null && artifacts.totalArtifacts > 0 &&
        artifacts.artifactsCollected >= artifacts.totalArtifacts;
    private int revealPage;
    private bool transitioning;

    private void OnEnable()
    {
        if (artifacts != null) artifacts.ArtifactCountChanged += OnClueCollected;
    }

    private void OnDisable()
    {
        if (artifacts != null) artifacts.ArtifactCountChanged -= OnClueCollected;
    }

    private void Start() => UpdateObjective();

    private void OnClueCollected(int count) => UpdateObjective();

    public override void Interact(GameObject playerObject)
    {
        if (artifacts == null || transitioning || SceneTransition.IsLoading ||
            PauseManager.IsGamePaused || Time.timeScale <= 0f) return;
        PlayerMovement player = playerObject != null ? playerObject.GetComponentInChildren<PlayerMovement>() : null;
        if (player != null && !player.enabled) return;
        if (nervousIdle != null && playerObject != null) nervousIdle.FacePlayer(player != null ? player.transform : playerObject.transform);

        switch (State)
        {
            case StoryState.NotStarted:
                State = StoryState.FindingClues;
                Say(initialDialogue);
                break;
            case StoryState.FindingClues:
                if (!AllCluesCollected)
                {
                    Say($"Please keep looking... {artifacts.totalArtifacts - artifacts.artifactsCollected} Notes are still missing. Bring them all back to me.");
                    break;
                }
                State = StoryState.Returned;
                Say(returnedDialogue);
                UpdateObjective();
                onCluesReturned.Invoke();
                return;
            case StoryState.Returned:
            case StoryState.Revealing:
                if (revealDialogue != null && revealPage < revealDialogue.Length)
                {
                    Say(revealDialogue[revealPage++]);
                    State = revealPage < revealDialogue.Length ? StoryState.Revealing : StoryState.ReadyToLeave;
                }
                else State = StoryState.ReadyToLeave;
                break;
            case StoryState.ReadyToLeave:
                ContinueStory();
                break;
        }
        UpdateObjective();
    }

    private void Say(string line)
    {
        if (notifications != null) notifications.ShowNotification("Frightened woman: " + line, 18f);
    }

    private void UpdateObjective()
    {
        string message;
        if (State == StoryState.NotStarted) message = "Talk to the frightened woman.";
        else if (State == StoryState.FindingClues)
            message = AllCluesCollected ? "All clues found. Return to the frightened woman." : "Find all the clues and return to her.";
        else if (State == StoryState.ReadyToLeave) message = "Take the evidence onward. Talk to her to continue.";
        else message = "Listen to what the frightened woman has to tell you.";

        promptMessage = State == StoryState.Returned || State == StoryState.Revealing
            ? "Press [E] to hear her story" : State == StoryState.ReadyToLeave
            ? "Press [E] to continue to Level 3" : "Press [E] to talk to the frightened woman";
        if (objective == null) return;
        objective.primaryObjective = message;
        objective.UpdateUI();
    }

    private void ContinueStory()
    {
        if (!Application.CanStreamedLevelBeLoaded(nextSceneName))
        {
            Debug.LogError($"[Level2NoteNpc] Add '{nextSceneName}' to the build scenes.", this);
            return;
        }
        transitioning = true;
        // The pursuit remains active through collection and the return conversation.
        // Freeze only when committing to the existing scene transition.
        Time.timeScale = 0f;
        foreach (PauseManager pause in FindObjectsByType<PauseManager>(FindObjectsInactive.Exclude))
            pause.enabled = false;
        ScreenFadeController fade = ScreenFadeController.Instance;
        if (fade == null) fade = gameObject.AddComponent<ScreenFadeController>();
        fade.FadeToScene(nextSceneName, 1.5f);
    }
}
