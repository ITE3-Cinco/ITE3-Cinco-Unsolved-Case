// Purpose: Guides the Note quest and reveals the frightened woman story in Level 2.
// Use: Attach to the NPC interaction root; assign artifacts, ghost, objective and nervous idle.
// How it works: Repeated E interactions move through quest states; all Notes stop the hunt before the reveal.

// UnityEngine supplies GameObjects, components, transforms, time, math and physics used here.
using UnityEngine;
// Unity Events supplies Inspector-connectable events and listener callbacks.
using UnityEngine.Events;

/// <summary>Scene-local story state; Note progress belongs to ArtifactManager.
/// A lose-screen retry reloads both together; an in-scene respawn retains both.</summary>
public class Level2NoteNpc : Interactable
{
    // These named steps remember whether the player is gathering Notes, hearing the reveal or ready to leave.
    public enum StoryState { NotStarted, FindingClues, Returned, Revealing, ReadyToLeave, CluesCollected }

    // whiteLady: GhostEnemy made non-hostile when all Notes are collected. Assigned in the scene/prefab Inspector.
    public GhostEnemy whiteLady;
    // artifacts: ArtifactManager tracking Notes for the Level 2 conversation. Assigned in the scene/prefab
    // Inspector.
    public ArtifactManager artifacts;
    // objective: ObjectiveItem updated with the current story or escape goal. Assigned in the scene/prefab
    // Inspector.
    public ObjectiveItem objective;
    // notifications: NotificationManager reference assigned for the Level 2 story. Assigned in the scene/prefab
    // Inspector.
    public NotificationManager notifications;
    // nervousIdle: StationaryNervousIdle that turns the NPC toward the speaker. Assigned in the scene/prefab
    // Inspector.
    public StationaryNervousIdle nervousIdle;
    // nextSceneName: build-scene name requested after escape or story completion. Assigned in the scene/prefab
    // Inspector; the initializer below is the default.
    public string nextSceneName = "Level3";
    // onCluesReturned: Inspector-connectable event fired when the collected Notes are returned. Assigned in the
    // scene/prefab Inspector; the initializer below is the default.
    public UnityEvent onCluesReturned = new UnityEvent();

    // npcName: speaker name displayed for the frightened woman. Assigned in the scene/prefab Inspector; the
    // initializer below is the default.
    [Header("Speaker names")]
    public string npcName = "Frightened woman";
    // playerName: speaker name displayed for the player reply. Assigned in the scene/prefab Inspector; the
    // initializer below is the default.
    public string playerName = "Player";
    // dialogueTypewriterFont: Font used to create the dialogue TMP font at runtime. Assigned in the scene/prefab
    // Inspector.
    public Font dialogueTypewriterFont;

    // initialDialogue: request displayed before the Note search begins. Assigned in the scene/prefab Inspector;
    // the initializer below is the default.
    [Header("Dialogue (advance by interacting again)")]
    [TextArea(3, 6)] public string initialDialogue = "Please... I'm scared. Something terrible happened here. There are Notes scattered around this place. Please find ALL of them, then come back to me. I can't explain everything yet... Be careful out there.";
    // returnedDialogue: response displayed when all Notes have been collected. Assigned in the scene/prefab
    // Inspector; the initializer below is the default.
    [TextArea(3, 6)] public string returnedDialogue = "You found everything... She's not going to hurt you anymore. You don't need to be afraid of her now. This is what she's been waiting for. Please, listen.";
    // revealDialogue: ordered story lines; revealPage selects the next entry. Assigned in the scene/prefab
    // Inspector; the initializer below is the default.
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

    // State can be read by other scripts; only this class can write its backing state.
    public StoryState State { get; private set; }
    // AllCluesCollected is computed when read: return artifacts exists and artifacts.totalArtifacts is above 0 and
    // unique pickup count stored by artifacts ArtifactManager is at least required artifact count configured on
    // artifacts.
    public bool AllCluesCollected => artifacts != null && artifacts.totalArtifacts > 0 &&
        artifacts.artifactsCollected >= artifacts.totalArtifacts;
    // Conversation state remembers the next reveal line, pending player reply and whether to close the box.
    // revealPage: zero-based next story-reveal line. Runtime data filled by the methods below; starts at the type
    // default.
    private int revealPage;
    // transitioning: guard preventing the level transition from being requested twice. Updated by ContinueStory.
    private bool transitioning;
    // dialogueBox: DialogueBox created when the NPC first speaks. ShowDialogue retrieves it from a DialogueBox
    // component attached to the GameObject that owns this component.
    private DialogueBox dialogueBox;
    // conversationPlayer: player Transform used to hide dialogue when too far away. Updated by Interact.
    private Transform conversationPlayer;
    // pendingPlayerReply: player line waiting for the next interaction. Updated by Interact.
    private string pendingPlayerReply;
    // closeOnNextInteraction: whether the next E interaction should close the reply. Updated by Interact.
    private bool closeOnNextInteraction;

    /// <summary>Keep dialogue visible only while the nearby conversation can be read during active gameplay.</summary>
    private void LateUpdate()
    {
        // If dialogueBox exists, run the following block.
        if (dialogueBox != null)
            // Call DialogueBox.SetVisible with !PauseManager.IsGamePaused && !SceneTransition.IsLoading and
            // conversationPlayer != null and Vector3.Distance(transform.position, conversationPlayer.position) is
            // at most 4f; show or hide the canvas while keeping the current conversation text.
            dialogueBox.SetVisible(!PauseManager.IsGamePaused && !SceneTransition.IsLoading &&
                conversationPlayer != null && Vector3.Distance(transform.position, conversationPlayer.position) <= 4f);
    }

    /// <summary>Listen for Note progress and immediately check the current quest state.</summary>
    private void OnEnable()
    {
        // If artifacts exists, subscribe OnClueCollected to artifacts.ArtifactCountChanged so the callback runs
        // when that event is raised.
        if (artifacts != null) artifacts.ArtifactCountChanged += OnClueCollected;
        // Call Level2NoteNpc.RefreshClueState; stop the White Lady hunt when all Notes are found and update the
        // objective.
        RefreshClueState();
    }

    /// <summary>Stop listening for Note events and close the dialogue when the NPC is disabled.</summary>
    private void OnDisable()
    {
        // If artifacts exists, unsubscribe OnClueCollected from artifacts.ArtifactCountChanged so the callback
        // stops running when that event is raised.
        if (artifacts != null) artifacts.ArtifactCountChanged -= OnClueCollected;
        // If dialogueBox exists, call DialogueBox.Hide; close the panel and remove it from the list of visible
        // conversations.
        if (dialogueBox != null) dialogueBox.Hide();
    }

    /// <summary>Refresh the quest after scene startup references are ready.</summary>
    // Call Level2NoteNpc.RefreshClueState; stop the White Lady hunt when all Notes are found and update the
    // objective.
    private void Start() => RefreshClueState();

    /// <summary>Recheck the quest immediately after a Note is collected.</summary>
    // count is a int argument supplied by the code, event or Inspector callback invoking OnClueCollected.
    // Call Level2NoteNpc.RefreshClueState; stop the White Lady hunt when all Notes are found and update the
    // objective.
    private void OnClueCollected(int count) => RefreshClueState();

    /// <summary>Stop the White Lady hunt when all Notes are found and update the objective.</summary>
    private void RefreshClueState()
    {
        // If AllCluesCollected, run the following block.
        if (AllCluesCollected)
        {
            // Synchronous with the final successful pickup, before the next AI tick.
            // Keep the enemy object for the story; this cannot be undone by a respawn.
            // If whiteLady exists, call GhostEnemy.BecomeNonHostile; permanent for this scene run; only a fresh
            // scene can restore the hunt.
            if (whiteLady != null) whiteLady.BecomeNonHostile();
            // If State equals StoryState.FindingClues, set State to StoryState.CluesCollected.
            if (State == StoryState.FindingClues) State = StoryState.CluesCollected;
        }
        // Call Level2NoteNpc.UpdateObjective; choose the next goal and interaction prompt from the current story
        // state.
        UpdateObjective();
    }

    /// <summary>Handle one E interaction: finish dialogue pages, reply, or advance the current quest state.</summary>
    // playerObject normally comes from PlayerInteraction; it identifies who is speaking and which way the NPC
    // should face.
    public override void Interact(GameObject playerObject)
    {
        // Stop this method early when artifacts == null || transitioning or SceneTransition.IsLoading or
        // PauseManager.IsGamePaused or Unity gameplay clock multiplier (0 freezes gameplay time) is at most 0.
        if (artifacts == null || transitioning || SceneTransition.IsLoading ||
            PauseManager.IsGamePaused || Time.timeScale <= 0f) return;
        // Store the first PlayerMovement component on or beneath playerObject when playerObject exists; otherwise
        // no reference (null) in local player.
        PlayerMovement player = playerObject != null ? playerObject.GetComponentInChildren<PlayerMovement>() : null;
        // If player exists and not (enabled state of player), return now; skip the rest of this method.
        if (player != null && !player.enabled) return;
        // If nervousIdle exists and playerObject exists, call StationaryNervousIdle.FacePlayer with Transform of
        // player when player exists; otherwise Transform of playerObject; turn the NPC toward the player on the
        // horizontal plane when a conversation begins.
        if (nervousIdle != null && playerObject != null) nervousIdle.FacePlayer(player != null ? player.transform : playerObject.transform);

        // Set conversationPlayer to Transform of player when player exists; otherwise Transform of playerObject
        // when playerObject exists; otherwise no reference (null).
        conversationPlayer = player != null ? player.transform : playerObject != null ? playerObject.transform : null;
        // If dialogueBox exists and the result of DialogueBox.AdvancePage, return now; skip the rest of this
        // method.
        if (dialogueBox != null && dialogueBox.AdvancePage()) return;
        // If pendingPlayerReply exists, run the following block.
        if (pendingPlayerReply != null)
        {
            // Call Level2NoteNpc.ShowDialogue with playerName, pendingPlayerReply, "[E] Close"; create the
            // dialogue component on demand and fill the speaker, line and hint.
            ShowDialogue(playerName, pendingPlayerReply, "[E] Close");
            // Clear pendingPlayerReply to null so it no longer holds a reference.
            pendingPlayerReply = null;
            // Set closeOnNextInteraction to true.
            closeOnNextInteraction = true;
            // Return now; skip the rest of this method.
            return;
        }
        // If closeOnNextInteraction, run the following block.
        if (closeOnNextInteraction)
        {
            // Call DialogueBox.Hide; close the panel and remove it from the list of visible conversations.
            dialogueBox.Hide();
            // Set closeOnNextInteraction to false.
            closeOnNextInteraction = false;
            // Return now; skip the rest of this method.
            return;
        }

        // Advance only the current story step: start the quest, require all Notes, reveal lines, then leave.
        // Choose the matching branch using State.
        switch (State)
        {
            case StoryState.NotStarted:
                // Set State to StoryState.CluesCollected when AllCluesCollected; otherwise
                // StoryState.FindingClues.
                State = AllCluesCollected ? StoryState.CluesCollected : StoryState.FindingClues;
                // Call Level2NoteNpc.Say with initialDialogue; display a line spoken by the NPC with the continue
                // hint.
                Say(initialDialogue);
                // Set pendingPlayerReply to "I’ll look for the Notes and bring them back to you.".
                pendingPlayerReply = "I’ll look for the Notes and bring them back to you.";
                // Leave this loop or switch case so no later statements in that block run.
                break;
            case StoryState.FindingClues:
            case StoryState.CluesCollected:
                // If not (AllCluesCollected), run the following block.
                if (!AllCluesCollected)
                {
                    // Set pendingPlayerReply to "I’ll keep looking.".
                    pendingPlayerReply = "I’ll keep looking.";
                    // Call Level2NoteNpc.Say with formatted text inserting {; display a line spoken by the NPC
                    // with the continue hint.
                    Say($"Please keep looking... {artifacts.totalArtifacts - artifacts.artifactsCollected} Notes are still missing. Bring them all back to me.");
                    // Leave this loop or switch case so no later statements in that block run.
                    break;
                }
                // If whiteLady exists, call GhostEnemy.BecomeNonHostile; permanent for this scene run; only a
                // fresh scene can restore the hunt.
                if (whiteLady != null) whiteLady.BecomeNonHostile();
                // Set State to StoryState.Returned.
                State = StoryState.Returned;
                // Call Level2NoteNpc.Say with returnedDialogue; display a line spoken by the NPC with the continue
                // hint.
                Say(returnedDialogue);
                // Call Level2NoteNpc.UpdateObjective; choose the next goal and interaction prompt from the current
                // story state.
                UpdateObjective();
                // Notify onCluesReturned listeners.
                onCluesReturned.Invoke();
                // Return now; skip the rest of this method.
                return;
            case StoryState.Returned:
            case StoryState.Revealing:
                // If revealDialogue exists and revealPage is below length of revealDialogue, run the following
                // block.
                if (revealDialogue != null && revealPage < revealDialogue.Length)
                {
                    // Call Level2NoteNpc.Say with revealDialogue[revealPage++] (entry read from revealDialogue);
                    // display a line spoken by the NPC with the continue hint.
                    Say(revealDialogue[revealPage++]);
                    // Set State to StoryState.Revealing when revealPage is below length of revealDialogue;
                    // otherwise StoryState.ReadyToLeave.
                    State = revealPage < revealDialogue.Length ? StoryState.Revealing : StoryState.ReadyToLeave;
                }
                else State = StoryState.ReadyToLeave;
                // Leave this loop or switch case so no later statements in that block run.
                break;
            case StoryState.ReadyToLeave:
                // Call Level2NoteNpc.ContinueStory; validate Level 3, close dialogue, freeze gameplay and start
                // the scene fade.
                ContinueStory();
                // Leave this loop or switch case so no later statements in that block run.
                break;
        }
        // Call Level2NoteNpc.UpdateObjective; choose the next goal and interaction prompt from the current story
        // state.
        UpdateObjective();
    }

    /// <summary>Display a line spoken by the NPC with the continue hint.</summary>
    // line is a string argument supplied by Level2NoteNpc.Interact.
    private void Say(string line)
    {
        // Call Level2NoteNpc.ShowDialogue with npcName, line, "[E] Continue talking"; create the dialogue
        // component on demand and fill the speaker, line and hint.
        ShowDialogue(npcName, line, "[E] Continue talking");
    }

    /// <summary>Create the dialogue component on demand and fill the speaker, line and hint.</summary>
    // speaker is a string argument supplied by Level2NoteNpc.Interact, Level2NoteNpc.Say.
    // line is a string argument supplied by Level2NoteNpc.Interact, Level2NoteNpc.Say.
    // hint is a string argument supplied by Level2NoteNpc.Interact, Level2NoteNpc.Say.
    private void ShowDialogue(string speaker, string line, string hint)
    {
        // If dialogueBox is missing, store a DialogueBox component attached to the GameObject that owns this
        // component in dialogueBox.
        if (dialogueBox == null) dialogueBox = gameObject.AddComponent<DialogueBox>();
        // Call DialogueBox.Show with speaker, line, hint, dialogueTypewriterFont; create the panel if needed, fill
        // its text and show the first page of this line.
        dialogueBox.Show(speaker, line, hint, dialogueTypewriterFont);
    }

    /// <summary>Choose the next goal and interaction prompt from the current story state.</summary>
    private void UpdateObjective()
    {
        // Declare local message to hold string data supplied later in this method.
        string message;
        // If State equals StoryState.NotStarted, set message to "Talk to the frightened woman.".
        if (State == StoryState.NotStarted) message = "Talk to the frightened woman.";
        else if (State == StoryState.FindingClues || State == StoryState.CluesCollected)
            // Set message to "All clues found. Return to the frightened woman." when AllCluesCollected; otherwise
            // "Find all the clues and return to her.".
            message = AllCluesCollected ? "All clues found. Return to the frightened woman." : "Find all the clues and return to her.";
        else if (State == StoryState.ReadyToLeave) message = "Take the evidence onward. Talk to her to continue.";
        else message = "Listen to what the frightened woman has to tell you.";

        // Set promptMessage to "Press [E] to hear her story" when State equals StoryState.Returned or State equals
        // StoryState.Revealing; otherwise "Press [E] to continue to Level 3" when State equals
        // StoryState.ReadyToLeave; otherwise "Press [E] to talk to the frightened woman".
        promptMessage = State == StoryState.Returned || State == StoryState.Revealing
            ? "Press [E] to hear her story" : State == StoryState.ReadyToLeave
            ? "Press [E] to continue to Level 3" : "Press [E] to talk to the frightened woman";
        // If objective is missing, return now; skip the rest of this method.
        if (objective == null) return;
        // Copy message into objective.primaryObjective.
        objective.primaryObjective = message;
        // Call ObjectiveItem.UpdateUI; choose a primary goal, escape goal or task list and format the assigned
        // text.
        objective.UpdateUI();
    }

    /// <summary>Validate Level 3, close dialogue, freeze gameplay and start the scene fade.</summary>
    private void ContinueStory()
    {
        // If not (Unity can load build-scene nextSceneName), run the following block.
        if (!Application.CanStreamedLevelBeLoaded(nextSceneName))
        {
            // Report the failed setup or state as a Unity Console error.
            Debug.LogError($"[Level2NoteNpc] Add '{nextSceneName}' to the build scenes.", this);
            // Return now; skip the rest of this method.
            return;
        }
        // Set transitioning to true.
        transitioning = true;
        // If dialogueBox exists, call DialogueBox.Hide; close the panel and remove it from the list of visible
        // conversations.
        if (dialogueBox != null) dialogueBox.Hide();
        // The White Lady is already harmless. Freeze controls during the scene fade.
        // Set Unity gameplay clock multiplier to zero; scaled movement/timers stop, while code using real time can
        // continue.
        Time.timeScale = 0f;
        // Process each pause in all scene PauseManager components.
        foreach (PauseManager pause in FindObjectsByType<PauseManager>(FindObjectsInactive.Exclude))
            // Disable pause by changing its component enabled flag.
            pause.enabled = false;
        // Store the fade controller registered in Awake in local fade.
        ScreenFadeController fade = ScreenFadeController.Instance;
        // If fade is missing, store a ScreenFadeController component attached to the GameObject that owns this
        // component in fade.
        if (fade == null) fade = gameObject.AddComponent<ScreenFadeController>();
        // Call ScreenFadeController.FadeToScene with nextSceneName, 1.5; pass the destination and duration to the
        // common scene transition loader.
        fade.FadeToScene(nextSceneName, 1.5f);
    }
}
