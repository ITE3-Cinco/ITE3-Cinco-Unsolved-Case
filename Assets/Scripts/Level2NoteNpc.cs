using UnityEngine;
using UnityEngine.Events;

/// <summary>Scene-local story state; Note progress belongs to ArtifactManager.
/// A lose-screen retry reloads both together; an in-scene respawn retains both.</summary>
public class Level2NoteNpc : Interactable
{
    public enum StoryState { NotStarted, FindingClues, Returned, Revealing, ReadyToLeave, CluesCollected }

    public GhostEnemy whiteLady;
    public ArtifactManager artifacts;
    public ObjectiveItem objective;
    public NotificationManager notifications;
    public StationaryNervousIdle nervousIdle;
    public string nextSceneName = "Level3";
    public UnityEvent onCluesReturned = new UnityEvent();

    [Header("Speaker names")]
    public string npcName = "Frightened woman";
    public string playerName = "Player";
    public Font dialogueTypewriterFont;

    [Header("Dialogue (advance by interacting again)")]
    [TextArea(3, 6)] public string initialDialogue = "Please... I'm scared. Something terrible happened here. There are Notes scattered around this place. Please find ALL of them, then come back to me. I can't explain everything yet... Be careful out there.";
    [TextArea(3, 6)] public string returnedDialogue = "You found everything... She's not going to hurt you anymore. You don't need to be afraid of her now. This is what she's been waiting for. Please, listen.";
    [TextArea(3, 6)] public string[] revealDialogue =
    {
        "Renato... the coworker you met before coming here. He is the serial killer behind this cold case. These Notes are the evidence of what he did.",
        "Renato is dangerous. Now that you know the truth, you are a target. Keep the evidence close, and don't let him find you.",
        "The woman in white... I know why she followed you. She needed you to listen, but fear kept getting in the way.",
        "The White Lady wasn't trying to kill you.",
        "She was trying to reach you, to warn you about Renato. She wanted you to expose him and seek justice for the people he silenced.",
        "How do I know what she wanted? Because I am the White Lady.",
        "I was one of Renato's victims. It was me you encountered before you came here, and me you saw in these halls. I couldn't make you understand then. Now you have the truth. Take this evidence and survive."
    };

    public StoryState State { get; private set; }
    public bool AllCluesCollected => artifacts != null && artifacts.totalArtifacts > 0 &&
        artifacts.artifactsCollected >= artifacts.totalArtifacts;
    private int revealPage;
    private bool transitioning;
    private DialogueBox dialogueBox;
    private Transform conversationPlayer;
    private string pendingPlayerReply;
    private bool closeOnNextInteraction;

    private void LateUpdate()
    {
        if (dialogueBox != null)
            dialogueBox.SetVisible(!PauseManager.IsGamePaused && !SceneTransition.IsLoading &&
                conversationPlayer != null && Vector3.Distance(transform.position, conversationPlayer.position) <= 4f);
    }

    private void OnEnable()
    {
        if (artifacts != null) artifacts.ArtifactCountChanged += OnClueCollected;
        RefreshClueState();
    }

    private void OnDisable()
    {
        if (artifacts != null) artifacts.ArtifactCountChanged -= OnClueCollected;
        if (dialogueBox != null) dialogueBox.Hide();
    }

    private void Start() => RefreshClueState();

    private void OnClueCollected(int count) => RefreshClueState();

    private void RefreshClueState()
    {
        if (AllCluesCollected)
        {
            // Synchronous with the final successful pickup, before the next AI tick.
            // Keep the enemy object for the story; this cannot be undone by a respawn.
            if (whiteLady != null) whiteLady.BecomeNonHostile();
            if (State == StoryState.FindingClues) State = StoryState.CluesCollected;
        }
        UpdateObjective();
    }

    public override void Interact(GameObject playerObject)
    {
        if (artifacts == null || transitioning || SceneTransition.IsLoading ||
            PauseManager.IsGamePaused || Time.timeScale <= 0f) return;
        PlayerMovement player = playerObject != null ? playerObject.GetComponentInChildren<PlayerMovement>() : null;
        if (player != null && !player.enabled) return;
        if (nervousIdle != null && playerObject != null) nervousIdle.FacePlayer(player != null ? player.transform : playerObject.transform);

        conversationPlayer = player != null ? player.transform : playerObject != null ? playerObject.transform : null;
        if (dialogueBox != null && dialogueBox.AdvancePage()) return;
        if (pendingPlayerReply != null)
        {
            ShowDialogue(playerName, pendingPlayerReply, "[E] Close");
            pendingPlayerReply = null;
            closeOnNextInteraction = true;
            return;
        }
        if (closeOnNextInteraction)
        {
            dialogueBox.Hide();
            closeOnNextInteraction = false;
            return;
        }

        switch (State)
        {
            case StoryState.NotStarted:
                State = AllCluesCollected ? StoryState.CluesCollected : StoryState.FindingClues;
                Say(initialDialogue);
                pendingPlayerReply = "I’ll look for the Notes and bring them back to you.";
                break;
            case StoryState.FindingClues:
            case StoryState.CluesCollected:
                if (!AllCluesCollected)
                {
                    pendingPlayerReply = "I’ll keep looking.";
                    Say($"Please keep looking... {artifacts.totalArtifacts - artifacts.artifactsCollected} Notes are still missing. Bring them all back to me.");
                    break;
                }
                if (whiteLady != null) whiteLady.BecomeNonHostile();
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
        ShowDialogue(npcName, line, "[E] Continue talking");
    }

    private void ShowDialogue(string speaker, string line, string hint)
    {
        if (dialogueBox == null) dialogueBox = gameObject.AddComponent<DialogueBox>();
        dialogueBox.Show(speaker, line, hint, dialogueTypewriterFont);
    }

    private void UpdateObjective()
    {
        string message;
        if (State == StoryState.NotStarted) message = "Talk to the frightened woman.";
        else if (State == StoryState.FindingClues || State == StoryState.CluesCollected)
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
        CollectionProgress.PrepareForNextLevel(nextSceneName);
        transitioning = true;
        if (dialogueBox != null) dialogueBox.Hide();
        // The White Lady is already harmless. Freeze controls during the scene fade.
        Time.timeScale = 0f;
        foreach (PauseManager pause in FindObjectsByType<PauseManager>(FindObjectsInactive.Exclude))
            pause.enabled = false;
        ScreenFadeController fade = ScreenFadeController.Instance;
        if (fade == null) fade = gameObject.AddComponent<ScreenFadeController>();
        fade.FadeToScene(nextSceneName, 1.5f);
    }
}
