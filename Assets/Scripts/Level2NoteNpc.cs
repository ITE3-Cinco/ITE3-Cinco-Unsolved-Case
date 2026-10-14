// Level 2 quest controller: talk to the woman, find all three Notes, then hear her story.
// Attach this to the NPC's interaction root. PlayerInteraction calls Interact when E targets her.
// ArtifactManager owns Note progress; this script owns conversation progress for this scene run.

using UnityEngine;
using UnityEngine.Events;

public class Level2NoteNpc : Interactable
{
    // The current story step decides what the next interaction does.
    // Flow: NotStarted -> FindingClues -> CluesCollected -> Returned -> Revealing -> ReadyToLeave.
    // Keep the existing enum order: Unity may store enum values as numbers.
    public enum StoryState { NotStarted, FindingClues, Returned, Revealing, ReadyToLeave, CluesCollected }

    // Inspector references: the existing ghost, Note counter, objective UI and NPC animation.
    public GhostEnemy whiteLady;
    public ArtifactManager artifacts;
    public ObjectiveItem objective;
    // Retained scene reference. NPC dialogue uses DialogueBox; pickup messages use notifications.
    public NotificationManager notifications;
    public StationaryNervousIdle nervousIdle;
    // Destination after the final conversation. It must be included in Unity build scenes.
    public string nextSceneName = "Level3";
    // Notify any Inspector listeners once, when the player returns with all Notes.
    public UnityEvent onCluesReturned = new UnityEvent();

    // Names and font used by the conversation panel.
    [Header("Speaker names")]
    public string npcName = "Frightened woman";
    public string playerName = "Player";
    public Font dialogueTypewriterFont;

    // Dialogue is editable in the Inspector; the scene can override these default lines.
    // The request plays first. The reassurance and seven lore entries require all Notes.
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

    // Other scripts can read this state, but only the NPC can advance it.
    // GhostEnemy uses FindingClues to allow hunting while Notes are still missing.
    public StoryState State { get; private set; }
    // Read completion from the same counter used by Note pickups. There is no separate NPC counter.
    public bool AllCluesCollected => artifacts != null && artifacts.totalArtifacts > 0 &&
        artifacts.artifactsCollected >= artifacts.totalArtifacts;
    // Index of the next lore entry to show; starts at zero.
    private int revealPage;
    // Prevent repeated E presses from starting more than one scene transition.
    private bool transitioning;
    // Create the dialogue panel only when someone first talks to the NPC.
    private DialogueBox dialogueBox;
    // Remember the speaker so the panel can hide when they walk away.
    private Transform conversationPlayer;
    // After a request or reminder, show the player reply before closing the conversation.
    private string pendingPlayerReply;
    // Once the reply is shown, the following E press closes the box.
    private bool closeOnNextInteraction;

    // Hide dialogue during pause/loading or beyond four units; keep its text and progress.
    private void LateUpdate()
    {
        if (dialogueBox != null)
            dialogueBox.SetVisible(!PauseManager.IsGamePaused && !SceneTransition.IsLoading &&
                conversationPlayer != null && Vector3.Distance(transform.position, conversationPlayer.position) <= 4f);
    }

    // Listen for Note pickups and check progress when this component becomes active.
    private void OnEnable()
    {
        if (artifacts != null) artifacts.ArtifactCountChanged += OnClueCollected;
        RefreshClueState();
    }

    // Remove the listener to avoid duplicate callbacks, and close the conversation panel.
    private void OnDisable()
    {
        if (artifacts != null) artifacts.ArtifactCountChanged -= OnClueCollected;
        if (dialogueBox != null) dialogueBox.Hide();
    }

    // Check again after scene startup so the objective reflects the ready scene references.
    private void Start() => RefreshClueState();

    // ArtifactManager raises this event after a successful unique pickup.
    // Read its current totals instead of keeping another copy of the event count.
    private void OnClueCollected(int count) => RefreshClueState();

    // The final Note makes the return trip safe immediately, without waiting for another E press.
    private void RefreshClueState()
    {
        // Stop pursuit and hide the ghost during the pickup event, before the next AI update.
        if (AllCluesCollected)
        {
            // Make the existing ghost harmless for the rest of this scene run.
            if (whiteLady != null) whiteLady.BecomeNonHostile();
            // Only advance an active search. Notes found early must not skip the first conversation.
            if (State == StoryState.FindingClues) State = StoryState.CluesCollected;
        }
        UpdateObjective();
    }

    // Handle one E press from PlayerInteraction. This override talks instead of picking up the NPC.
    public override void Interact(GameObject playerObject)
    {
        // Ignore interaction if the quest is unconfigured or gameplay is frozen.
        if (artifacts == null || transitioning || SceneTransition.IsLoading ||
            PauseManager.IsGamePaused || Time.timeScale <= 0f) return;
        // Find the actual player body, and turn the woman toward it without moving her.
        PlayerMovement player = playerObject != null ? playerObject.GetComponentInChildren<PlayerMovement>() : null;
        if (player != null && !player.enabled) return;
        if (nervousIdle != null && playerObject != null) nervousIdle.FacePlayer(player != null ? player.transform : playerObject.transform);

        // Remember who is talking for the distance check in LateUpdate.
        conversationPlayer = player != null ? player.transform : playerObject != null ? playerObject.transform : null;
        // Finish any overflow pages of the current line before changing the story step.
        if (dialogueBox != null && dialogueBox.AdvancePage()) return;
        // After the NPC request/reminder is fully read, show the player response.
        if (pendingPlayerReply != null)
        {
            ShowDialogue(playerName, pendingPlayerReply, "[E] Close");
            pendingPlayerReply = null;
            closeOnNextInteraction = true;
            return;
        }
        // Close the response on the next interaction; do not advance the quest on that same press.
        if (closeOnNextInteraction)
        {
            dialogueBox.Hide();
            closeOnNextInteraction = false;
            return;
        }

        // Choose the conversation branch from the current story step.
        switch (State)
        {
            // Always show the request first, even if the player already found every Note.
            // Otherwise, entering FindingClues allows GhostEnemy to hunt while the search is incomplete.
            case StoryState.NotStarted:
                State = AllCluesCollected ? StoryState.CluesCollected : StoryState.FindingClues;
                Say(initialDialogue);
                pendingPlayerReply = "I’ll look for the Notes and bring them back to you.";
                break;
            // On a return visit, either report missing Notes or accept the completed search.
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
                // This branch changes State to Returned, so later interactions cannot fire this event again.
                onCluesReturned.Invoke();
                return;
            // Show one lore entry per interaction after any overflow pages have been read.
            case StoryState.Returned:
            case StoryState.Revealing:
                if (revealDialogue != null && revealPage < revealDialogue.Length)
                {
                    // Show the current entry, then move the index to the next one.
                    Say(revealDialogue[revealPage++]);
                    State = revealPage < revealDialogue.Length ? StoryState.Revealing : StoryState.ReadyToLeave;
                }
                else State = StoryState.ReadyToLeave;
                break;
            // Leave only on an extra interaction after the final lore entry has been read.
            case StoryState.ReadyToLeave:
                ContinueStory();
                break;
        }
        UpdateObjective();
    }

    // Show an NPC line with her speaker name and the continue hint.
    private void Say(string line)
    {
        ShowDialogue(npcName, line, "[E] Continue talking");
    }

    // Create the panel on first use, then reuse it for NPC lines and player replies.
    private void ShowDialogue(string speaker, string line, string hint)
    {
        if (dialogueBox == null) dialogueBox = gameObject.AddComponent<DialogueBox>();
        dialogueBox.Show(speaker, line, hint, dialogueTypewriterFont);
    }

    // Keep the objective and the targeted E prompt consistent with the story step.
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

    // Check that the destination exists, then freeze gameplay and fade into the next scene.
    private void ContinueStory()
    {
        // If the scene is missing from the build, stay here and report the setup error.
        if (!Application.CanStreamedLevelBeLoaded(nextSceneName))
        {
            Debug.LogError($"[Level2NoteNpc] Add '{nextSceneName}' to the build scenes.", this);
            return;
        }
        transitioning = true;
        if (dialogueBox != null) dialogueBox.Hide();
        // Pause movement and scaled timers while the shared transition runs its fade.
        Time.timeScale = 0f;
        // Prevent the pause menu from changing the gameplay clock during the transition.
        foreach (PauseManager pause in FindObjectsByType<PauseManager>(FindObjectsInactive.Exclude))
            pause.enabled = false;
        // Reuse the shared fade controller, or create one if this scene has none.
        ScreenFadeController fade = ScreenFadeController.Instance;
        if (fade == null) fade = gameObject.AddComponent<ScreenFadeController>();
        fade.FadeToScene(nextSceneName, 1.5f);
    }
}
