using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

[DefaultExecutionOrder(-50)]
public class Level2NoteQuest : MonoBehaviour
{
    public enum QuestState { NotStarted, FindingNotes, AllNotesCollected, Completed }

    [Header("Existing collection and UI systems")]
    public ArtifactManager artifacts;
    public ObjectiveItem objective;
    public NotificationManager notifications;
    public ItemData noteItem;
    public Interactable[] notes;

    [Header("Dialogue")]
    [TextArea] public string requestDialogue = "Please... you have to help me. There are notes scattered around this place. Find them all and bring them back to me. I don't feel safe here...";
    [TextArea] public string completionDialogue = "You found them all. Thank you... Now we can leave. Talk to me when you're ready to continue.";
    [Min(1f)] public float dialogueDuration = 10f;

    [Header("Progression")]
    public string nextSceneName = "Level3";
    public UnityEvent onCompleted = new UnityEvent();
    [SerializeField] private QuestState state;

    private readonly HashSet<Interactable> registeredNotes = new HashSet<Interactable>();
    public QuestState State => state;
    public int TotalNotes => registeredNotes.Count;
    public int CollectedNotes => artifacts != null ? artifacts.artifactsCollected : 0;
    public int RemainingNotes => Mathf.Max(0, TotalNotes - CollectedNotes);
    public string ProgressMessage => state == QuestState.AllNotesCollected
        ? "All Notes found. Return to the frightened woman."
        : $"Notes found: {CollectedNotes}/{TotalNotes} ({RemainingNotes} remaining).";

    private void Awake()
    {
        state = QuestState.NotStarted;
        if (notes != null)
            foreach (Interactable note in notes)
                if (note != null && note.isArtifact && note.itemData == noteItem && note.noteQuest == this)
                    registeredNotes.Add(note);

        if (artifacts == null || noteItem == null || registeredNotes.Count == 0)
        {
            Debug.LogError("[Level2NoteQuest] Assign the artifact manager, Note item and quest Notes.", this);
            return;
        }
        artifacts.totalArtifacts = TotalNotes;
        // Collection finishes the search; only talking to the NPC finishes the quest.
        artifacts.artifactObjective = null;
    }

    private void OnEnable()
    {
        if (artifacts != null) artifacts.ArtifactCountChanged += OnNoteCollected;
    }

    private void OnDisable()
    {
        if (artifacts != null) artifacts.ArtifactCountChanged -= OnNoteCollected;
    }

    private void Start() => UpdateObjective();

    public bool CanCollect(Interactable note)
    {
        if (state == QuestState.NotStarted)
            Notify("Speak to the frightened woman about these Notes first.", 4f);
        return state == QuestState.FindingNotes && artifacts != null && registeredNotes.Contains(note);
    }

    public void Talk(GameObject playerObject)
    {
        if (SceneTransition.IsLoading || Time.timeScale <= 0f || artifacts == null || TotalNotes == 0) return;
        switch (state)
        {
            case QuestState.NotStarted:
                state = QuestState.FindingNotes;
                Notify(requestDialogue, dialogueDuration);
                break;
            case QuestState.FindingNotes:
                Notify($"Please keep looking... {RemainingNotes} Notes are still missing.", dialogueDuration);
                break;
            case QuestState.AllNotesCollected:
                state = QuestState.Completed;
                Inventory inventory = playerObject != null ? playerObject.GetComponentInChildren<Inventory>() : null;
                if (inventory != null)
                {
                    inventory.items.RemoveAll(item => item == noteItem);
                    inventory.SelectSlot(Mathf.Clamp(inventory.selectedIndex, 0, Mathf.Max(0, inventory.items.Count - 1)));
                }
                Notify(completionDialogue, dialogueDuration);
                UpdateObjective();
                onCompleted.Invoke();
                return;
            case QuestState.Completed:
                if (!string.IsNullOrWhiteSpace(nextSceneName))
                {
                    if (Application.CanStreamedLevelBeLoaded(nextSceneName))
                        SceneTransition.Load(nextSceneName);
                    else
                        Debug.LogError($"[Level2NoteQuest] Add '{nextSceneName}' to Build Settings.", this);
                }
                return;
        }
        UpdateObjective();
    }

    private void OnNoteCollected(int count)
    {
        if (state != QuestState.FindingNotes) return;
        if (count >= TotalNotes) state = QuestState.AllNotesCollected;
        UpdateObjective();
    }

    private void UpdateObjective()
    {
        if (objective == null) return;
        switch (state)
        {
            case QuestState.NotStarted:
                objective.primaryObjective = "Speak to the frightened woman.";
                break;
            case QuestState.FindingNotes:
                objective.primaryObjective = $"Find the missing Notes: {CollectedNotes}/{TotalNotes} ({RemainingNotes} remaining).";
                break;
            case QuestState.AllNotesCollected:
                objective.primaryObjective = "Return all Notes to the frightened woman.";
                break;
            case QuestState.Completed:
                objective.primaryObjective = "Notes returned. Talk to the woman to continue.";
                break;
        }
        objective.UpdateUI();
    }

    private void Notify(string message, float duration)
    {
        if (notifications != null) notifications.ShowNotification(message, duration);
    }
}
