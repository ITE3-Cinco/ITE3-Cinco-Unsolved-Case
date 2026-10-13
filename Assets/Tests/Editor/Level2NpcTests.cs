// Purpose: checks Level 2 quest, reveal order and stationary NPC animation.
// Use: run in Unity Test Runner > Edit Mode; this script is not attached to a game object.
// How it works: arrange a scene or objects, invoke behavior, assert results, then clean up.

// System supplies .NET types, delegates and attributes such as Serializable.
using System;
// LINQ filters and combines collections, such as finding a scene component of a chosen type.
using System.Linq;
// Reflection reads fields and invokes gameplay methods by name in Editor tests.
using System.Reflection;
// NUnit supplies test attributes and assertions used by Unity Edit Mode tests.
using NUnit.Framework;
// UnityEditor supplies Edit Mode tools and asset/prefab operations; this code runs in the Editor.
using UnityEditor;
// Unity Editor scene APIs open, close and mark scenes for saving.
using UnityEditor.SceneManagement;
// UnityEngine supplies GameObjects, components, transforms, time, math and physics used here.
using UnityEngine;
// Unity AI supplies NavMesh agents, paths and walkable-surface building/query tools.
using UnityEngine.AI;
// Unity Events supplies Inspector-connectable events and listener callbacks.
using UnityEngine.Events;
// Unity SceneManagement supplies scene data, scene-loaded events and scene-loading calls.
using UnityEngine.SceneManagement;

public class Level2NpcTests
{
    // scene: Unity scene opened by test setup and closed during teardown. Updated by SetUp,
    // FrozenGameplayCannotAdvanceAndFreshSceneResetsQuestWithNotes.
    private Scene scene;
    // timeScale: time scale. SetUp sets it from Unity gameplay clock multiplier (0 freezes gameplay time).
    private float timeScale;
    // npc: npc. SetUp retrieves it from the result of Level2NpcTests.Find using "Level2NoteNpc".
    private Component npc;
    // artifacts: ArtifactManager component located in the test scene. SetUp retrieves it from the result of
    // Level2NpcTests.Find using "ArtifactManager".
    private Component artifacts;
    // Resolve a gameplay class by name so the test can access it without changing assembly layout.
    // name comes from the test/helper caller; it supplies the type or field name requested.
    // Return the .NET type named name plus ", Assembly-CSharp" (including its assembly name) to the caller.
    private static Type Script(string name) => Type.GetType(name + ", Assembly-CSharp", true);
    // Read a named field through reflection so assertions can inspect gameplay state.
    // target comes from the test/helper caller; it supplies the object to inspect or change.
    // field comes from the test/helper caller; it supplies the field name to access.
    // Return value read through reflection from target to the caller.
    private static object Get(object target, string field) => target.GetType().GetField(field).GetValue(target);
    // Assign a named field through reflection to arrange the test scenario.
    // target comes from the test/helper caller; it supplies the object to inspect or change.
    // field comes from the test/helper caller; it supplies the field name to access.
    // value comes from the test/helper caller; it supplies the value to assign.
    // Write value through reflection on target.
    private static void Set(object target, string field, object value) => target.GetType().GetField(field).SetValue(target, value);
    // Invoke the named gameplay method through reflection, including callbacks not run automatically in Edit Mode.
    // target comes from the test/helper caller; it supplies the object to inspect or change.
    // method comes from the test/helper caller; it supplies the method name to invoke.
    // Return Invoke the reflected method target.GetType() .GetMethod(method, BindingFlags.Instance |
    // BindingFlags.Public | BindingFlags.NonPublic) on target with arguments args. to the caller.
    private static object Call(object target, string method, params object[] args) => target.GetType()
        .GetMethod(method, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic).Invoke(target, args);
    // Components is computed when read: return a new array copied from scene.GetRootGameObjects().SelectMany(g =>
    // g.GetComponentsInChildren<Component>(true)).
    private Component[] Components => scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<Component>(true)).ToArray();
    // Find the single component of the requested gameplay type in this test scene.
    // name comes from the test/helper caller; it supplies the type or field name requested.
    // Return the result of Components.Single using the callback/condition defined here to the caller.
    private Component Find(string name) => Components.Single(c => c != null && c.GetType() == Script(name));
    // Notes is computed when read: return a new array copied from Components.Where(c => c != null && c.GetType()
    // == Script("Interactable") && (bool)Get(c, "isArtifact")).
    private Component[] Notes => Components.Where(c => c != null && c.GetType() == Script("Interactable") && (bool)Get(c, "isArtifact")).ToArray();
    // State is computed when read: return the result of npc.GetType().GetProperty("State").GetValue(npc).ToString.
    private string State => npc.GetType().GetProperty("State").GetValue(npc).ToString();

    // Before each test, prepare the required objects or scene and save shared state for cleanup.
    [SetUp]
    public void SetUp()
    {
        // Set timeScale to Unity gameplay clock multiplier (0 freezes gameplay time).
        timeScale = Time.timeScale;
        // Restore Unity gameplay clock multiplier to its normal rate so movement and scaled timers can advance.
        Time.timeScale = 1f;
        // Store the Editor scene opened from "Assets/Scenes/Lvl-2.unity" with mode OpenSceneMode.Additive in
        // scene.
        scene = EditorSceneManager.OpenScene("Assets/Scenes/Lvl-2.unity", OpenSceneMode.Additive);
        // Store the result of Level2NpcTests.Find using "Level2NoteNpc" in npc.
        npc = Find("Level2NoteNpc");
        // Store the result of Level2NpcTests.Find using "ArtifactManager" in artifacts.
        artifacts = Find("ArtifactManager");
    }

    // After each test, remove temporary objects or scenes and restore saved shared state.
    [TearDown]
    public void TearDown()
    {
        // Close Editor scene scene and remove it from the hierarchy when requested.
        EditorSceneManager.CloseScene(scene, true);
        // Copy timeScale cached by this component into Time.timeScale.
        Time.timeScale = timeScale;
    }

    // Initialize the scene NPC conversation callbacks for the following story assertions.
    private void PrepareConversation()
    {
        // Suppress notification coroutines in Edit Mode; keep the real tracker,
        // objective and scene wiring. Runtime presentation is a Play Mode check.
        // Call Level2NpcTests.Set with npc, "notifications", no reference (null); assign a named field through
        // reflection to arrange the test scenario.
        Set(npc, "notifications", null);
        // Call Level2NpcTests.Call with npc, "OnEnable"; invoke the named gameplay method through reflection,
        // including callbacks not run automatically in Edit Mode.
        Call(npc, "OnEnable");
        // Call Level2NpcTests.Call with npc, "Start"; invoke the named gameplay method through reflection,
        // including callbacks not run automatically in Edit Mode.
        Call(npc, "Start");
    }

    // Simulate one NPC interaction without a physical player input event.
    // Call Level2NpcTests.Call with npc, "Interact", a new array/object initialized with the entries specified
    // here; invoke the named gameplay method through reflection, including callbacks not run automatically in Edit
    // Mode.
    private void Talk() => Call(npc, "Interact", new object[] { null });
    // Send a Note pickup directly to the artifact tracker for the story test.
    // note comes from the test/helper caller; it supplies the note used below.
    // Call Level2NpcTests.Call with artifacts, "CollectArtifact", note; invoke the named gameplay method through
    // reflection, including callbacks not run automatically in Edit Mode.
    private void Collect(Component note) => Call(artifacts, "CollectArtifact", note);

    // Check that scene uses existing notes ui and retry without giving npc movement ai.
    [Test]
    public void SceneUsesExistingNotesUiAndRetryWithoutGivingNpcMovementAi()
    {
        // Verify the result of Level2NpcTests.Get using npc, "artifacts" against the result of Is.SameAs using
        // artifacts; fail the test if the assertion is not met.
        Assert.That(Get(npc, "artifacts"), Is.SameAs(artifacts));
        // Verify the result of Level2NpcTests.Get using npc, "objective" against the result of Is.SameAs using
        // Find("ObjectiveItem"); fail the test if the assertion is not met.
        Assert.That(Get(npc, "objective"), Is.SameAs(Find("ObjectiveItem")));
        // Verify the result of Level2NpcTests.Get using npc, "notifications" against the result of Is.SameAs using
        // Find("NotificationManager"); fail the test if the assertion is not met.
        Assert.That(Get(npc, "notifications"), Is.SameAs(Find("NotificationManager")));
        // Store the MonoScript asset imported by Unity at "Assets/Scripts/NotificationManager.cs" in local
        // notificationScript.
        MonoScript notificationScript = AssetDatabase.LoadAssetAtPath<MonoScript>("Assets/Scripts/NotificationManager.cs");
        // Verify notificationScript against Is.Not.Null; fail the test if the assertion is not met.
        Assert.That(notificationScript, Is.Not.Null);
        // Verify the result of notificationScript.GetClass against the result of Is.EqualTo using
        // Script("NotificationManager"); fail the test if the assertion is not met.
        Assert.That(notificationScript.GetClass(), Is.EqualTo(Script("NotificationManager")));
        // Verify the result of Level2NpcTests.Get using Find("PlayerInteraction"), "interactionText" against
        // Is.Not.Null; fail the test if the assertion is not met.
        Assert.That(Get(Find("PlayerInteraction"), "interactionText"), Is.Not.Null);
        // Verify the first Collider component on npc against Is.Not.Null; fail the test if the assertion is not
        // met.
        Assert.That(npc.GetComponent<Collider>(), Is.Not.Null);
        // Verify the first NavMeshAgent component on npc against Is.Null; fail the test if the assertion is not
        // met.
        Assert.That(npc.GetComponent<NavMeshAgent>(), Is.Null);
        // Verify the first Rigidbody component on npc against Is.Null; fail the test if the assertion is not met.
        Assert.That(npc.GetComponent<Rigidbody>(), Is.Null);
        // Verify the first Script("GhostEnemy") component on npc against Is.Null; fail the test if the assertion
        // is not met.
        Assert.That(npc.GetComponent(Script("GhostEnemy")), Is.Null);
        // Verify length of Notes against the result of Is.EqualTo using 3; fail the test if the assertion is not
        // met.
        Assert.That(Notes.Length, Is.EqualTo(3));
        // Verify the result of Level2NpcTests.Get using artifacts, "totalArtifacts" against the result of
        // Is.EqualTo using Notes.Length; fail the test if the assertion is not met.
        Assert.That(Get(artifacts, "totalArtifacts"), Is.EqualTo(Notes.Length));
        // Store the UnityEngine.Object asset imported by Unity at "Assets/Assets/Inventory/Item_Note.asset" in
        // local note.
        UnityEngine.Object note = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>("Assets/Assets/Inventory/Item_Note.asset");
        // Process each collectible in Notes.
        foreach (Component collectible in Notes) Assert.That(Get(collectible, "itemData"), Is.EqualTo(note));
        // Verify enabled state of ((Behaviour)Find("GhostEnemy")) against Is.True; fail the test if the assertion
        // is not met.
        Assert.That(((Behaviour)Find("GhostEnemy")).enabled, Is.True);
        // Verify the result of Level2NpcTests.Get using Find("GhostEnemy"), "loseSceneController" against the
        // result of Is.SameAs using Find("LoseSceneController"); fail the test if the assertion is not met.
        Assert.That(Get(Find("GhostEnemy"), "loseSceneController"), Is.SameAs(Find("LoseSceneController")));
        // Verify the result of Level2NpcTests.Get using Find("LoseSceneController"), "retrySceneName" against the
        // result of Is.EqualTo using "Lvl-2"; fail the test if the assertion is not met.
        Assert.That(Get(Find("LoseSceneController"), "retrySceneName"), Is.EqualTo("Lvl-2"));
    }

    // Check that initial and partial conversations cannot reveal story and duplicates do not complete it.
    [Test]
    public void InitialAndPartialConversationsCannotRevealStoryAndDuplicatesDoNotCompleteIt()
    {
        // Call Level2NpcTests.PrepareConversation; initialize the scene NPC conversation callbacks for the
        // following story assertions.
        PrepareConversation();
        // Initialize returns to 0.
        int returns = 0;
        // Connect the callback/condition defined here as a callback on ((UnityEvent)Get(npc, "onCluesReturned")).
        ((UnityEvent)Get(npc, "onCluesReturned")).AddListener(() => returns++);
        // Call Level2NpcTests.Talk; simulate one NPC interaction without a physical player input event.
        Talk();
        // Verify State against the result of Is.EqualTo using "FindingClues"; fail the test if the assertion is
        // not met.
        Assert.That(State, Is.EqualTo("FindingClues"));
        // Verify the result of Level2NpcTests.Get using Find("ObjectiveItem"), "primaryObjective" against the
        // result of Is.EqualTo using "Find all the clues and return to her."; fail the test if the assertion is
        // not met.
        Assert.That(Get(Find("ObjectiveItem"), "primaryObjective"), Is.EqualTo("Find all the clues and return to her."));
        // Store the result of Level2NpcTests.Get using npc, "initialDialogue" viewed as string in local first.
        string first = (string)Get(npc, "initialDialogue");
        // Verify first against the result of Does.Not.Contain("Renato").And.Not.Contain using "White Lady"; fail
        // the test if the assertion is not met.
        Assert.That(first, Does.Not.Contain("Renato").And.Not.Contain("White Lady"));
        // Keep Notes in local notes.
        Component[] notes = Notes;
        // Call Level2NpcTests.Collect with notes[0] (entry read from notes); send a Note pickup directly to the
        // artifact tracker for the story test.
        Collect(notes[0]);
        // Call Level2NpcTests.Collect with notes[0] (entry read from notes); send a Note pickup directly to the
        // artifact tracker for the story test.
        Collect(notes[0]);
        // Call Level2NpcTests.Collect with notes[1] (entry read from notes); send a Note pickup directly to the
        // artifact tracker for the story test.
        Collect(notes[1]);
        // Call Level2NpcTests.Talk; simulate one NPC interaction without a physical player input event.
        Talk();
        // Verify the result of Level2NpcTests.Get using artifacts, "artifactsCollected" against the result of
        // Is.EqualTo using 2; fail the test if the assertion is not met.
        Assert.That(Get(artifacts, "artifactsCollected"), Is.EqualTo(2));
        // Verify State against the result of Is.EqualTo using "FindingClues"; fail the test if the assertion is
        // not met.
        Assert.That(State, Is.EqualTo("FindingClues"));
        // Verify returns against Is.Zero; fail the test if the assertion is not met.
        Assert.That(returns, Is.Zero);
        // Call Level2NpcTests.Collect with notes[2] (entry read from notes); send a Note pickup directly to the
        // artifact tracker for the story test.
        Collect(notes[2]);
        // Verify State against the result of Is.EqualTo using "CluesCollected", the message/string written here;
        // fail the test if the assertion is not met.
        Assert.That(State, Is.EqualTo("CluesCollected"), "Collection must make the return safe without finishing the story gate.");
        // Verify the result of Level2NpcTests.Get using Find("ObjectiveItem"), "primaryObjective" against the
        // result of Is.EqualTo using "All clues found. Return to the frightened woman."; fail the test if the
        // assertion is not met.
        Assert.That(Get(Find("ObjectiveItem"), "primaryObjective"), Is.EqualTo("All clues found. Return to the frightened woman."));
        // Call Level2NpcTests.Talk; simulate one NPC interaction without a physical player input event.
        Talk();
        // Verify State against the result of Is.EqualTo using "Returned"; fail the test if the assertion is not
        // met.
        Assert.That(State, Is.EqualTo("Returned"));
        // Verify returns against the result of Is.EqualTo using 1; fail the test if the assertion is not met.
        Assert.That(returns, Is.EqualTo(1));
        // Verify the result of Level2NpcTests.Get using npc, "returnedDialogue" viewed as string against the
        // result of Does.Not.Contain("Renato").And.Not.Contain using "White Lady"; fail the test if the assertion
        // is not met.
        Assert.That((string)Get(npc, "returnedDialogue"), Does.Not.Contain("Renato").And.Not.Contain("White Lady"));
        // Store the result of Level2NpcTests.Get using npc, "revealDialogue" viewed as string[] in local reveal.
        string[] reveal = (string[])Get(npc, "revealDialogue");
        // Repeat this block while i is below length of reveal; start from int i = 0.
        for (int i = 0; i < reveal.Length; i++) Talk();
        // Verify State against the result of Is.EqualTo using "ReadyToLeave"; fail the test if the assertion is
        // not met.
        Assert.That(State, Is.EqualTo("ReadyToLeave"));
        // Verify returns against the result of Is.EqualTo using 1; fail the test if the assertion is not met.
        Assert.That(returns, Is.EqualTo(1));
        // Verify the result of string.Join using " ", reveal against the result of
        // Does.Contain("Renato").And.Contain("serial killer").And.Contain("White
        // Lady").And.Contain("victims").And.Contain using "justice"; fail the test if the assertion is not met.
        Assert.That(string.Join(" ", reveal), Does.Contain("Renato").And.Contain("serial killer").And.Contain("White Lady").And.Contain("victims").And.Contain("justice"));
    }

    // Check that notes collected before meeting are retained but still require return conversation.
    [Test]
    public void NotesCollectedBeforeMeetingAreRetainedButStillRequireReturnConversation()
    {
        // Call Level2NpcTests.PrepareConversation; initialize the scene NPC conversation callbacks for the
        // following story assertions.
        PrepareConversation();
        // Process each note in Notes.
        foreach (Component note in Notes) Collect(note);
        // Call Level2NpcTests.Talk; simulate one NPC interaction without a physical player input event.
        Talk();
        // Verify State against the result of Is.EqualTo using "CluesCollected"; fail the test if the assertion is
        // not met.
        Assert.That(State, Is.EqualTo("CluesCollected"));
        // Verify the result of Level2NpcTests.Get using Find("ObjectiveItem"), "primaryObjective" against the
        // result of Is.EqualTo using "All clues found. Return to the frightened woman."; fail the test if the
        // assertion is not met.
        Assert.That(Get(Find("ObjectiveItem"), "primaryObjective"), Is.EqualTo("All clues found. Return to the frightened woman."));
        // Call Level2NpcTests.Talk; simulate one NPC interaction without a physical player input event.
        Talk();
        // Verify State against the result of Is.EqualTo using "Returned"; fail the test if the assertion is not
        // met.
        Assert.That(State, Is.EqualTo("Returned"));
    }

    // Check that frozen gameplay cannot advance and fresh scene resets quest with notes.
    [Test]
    public void FrozenGameplayCannotAdvanceAndFreshSceneResetsQuestWithNotes()
    {
        // Call Level2NpcTests.PrepareConversation; initialize the scene NPC conversation callbacks for the
        // following story assertions.
        PrepareConversation();
        // Call Level2NpcTests.Talk; simulate one NPC interaction without a physical player input event.
        Talk();
        // Call Level2NpcTests.Collect with Notes[0] (entry read from Notes); send a Note pickup directly to the
        // artifact tracker for the story test.
        Collect(Notes[0]);
        // Set Unity gameplay clock multiplier to zero; scaled movement/timers stop, while code using real time can
        // continue.
        Time.timeScale = 0f;
        // Call Level2NpcTests.Talk; simulate one NPC interaction without a physical player input event.
        Talk();
        // Verify State against the result of Is.EqualTo using "FindingClues"; fail the test if the assertion is
        // not met.
        Assert.That(State, Is.EqualTo("FindingClues"));
        // Close Editor scene scene and remove it from the hierarchy when requested.
        EditorSceneManager.CloseScene(scene, true);
        // Store the Editor scene opened from "Assets/Scenes/Lvl-2.unity" with mode OpenSceneMode.Additive in
        // scene.
        scene = EditorSceneManager.OpenScene("Assets/Scenes/Lvl-2.unity", OpenSceneMode.Additive);
        // Store the result of Level2NpcTests.Find using "Level2NoteNpc" in npc.
        npc = Find("Level2NoteNpc");
        // Store the result of Level2NpcTests.Find using "ArtifactManager" in artifacts.
        artifacts = Find("ArtifactManager");
        // Verify State against the result of Is.EqualTo using "NotStarted"; fail the test if the assertion is not
        // met.
        Assert.That(State, Is.EqualTo("NotStarted"));
        // Verify the result of Level2NpcTests.Get using artifacts, "artifactsCollected" against the result of
        // Is.EqualTo using 0; fail the test if the assertion is not met.
        Assert.That(Get(artifacts, "artifactsCollected"), Is.EqualTo(0));
        // Verify length of Notes against the result of Is.EqualTo using 3; fail the test if the assertion is not
        // met.
        Assert.That(Notes.Length, Is.EqualTo(3));
        // Restore Unity gameplay clock multiplier to its normal rate so movement and scaled timers can advance.
        Time.timeScale = 1f;
        // Call Level2NpcTests.PrepareConversation; initialize the scene NPC conversation callbacks for the
        // following story assertions.
        PrepareConversation();
        // Call Level2NpcTests.Talk; simulate one NPC interaction without a physical player input event.
        Talk();
        // Verify the result of Level2NpcTests.Hostile using Find("GhostEnemy") against Is.True, "Retry must
        // restore the hunt."; fail the test if the assertion is not met.
        Assert.That(Hostile(Find("GhostEnemy")), Is.True, "Retry must restore the hunt.");
    }

    // Read the ghost hostility property so the test can check quest safety.
    // ghost comes from the test/helper caller; it supplies the ghost used below.
    // Return value read through reflection from ghost viewed as bool to the caller.
    private bool Hostile(Component ghost) => (bool)ghost.GetType().GetProperty("IsHostile").GetValue(ghost);

    // Check that hunt starts with quest and final clue immediately stops every catch path through reveal.
    [Test]
    public void HuntStartsWithQuestAndFinalClueImmediatelyStopsEveryCatchPathThroughReveal()
    {
        // Call Level2NpcTests.PrepareConversation; initialize the scene NPC conversation callbacks for the
        // following story assertions.
        PrepareConversation();
        // Store the result of Level2NpcTests.Find using "GhostEnemy" in local ghost.
        Component ghost = Find("GhostEnemy");
        // Verify the result of Level2NpcTests.Get using npc, "whiteLady" against the result of Is.SameAs using
        // ghost; fail the test if the assertion is not met.
        Assert.That(Get(npc, "whiteLady"), Is.SameAs(ghost));
        // Verify the result of Level2NpcTests.Get using ghost, "clueStory" against the result of Is.SameAs using
        // npc; fail the test if the assertion is not met.
        Assert.That(Get(ghost, "clueStory"), Is.SameAs(npc));
        // Verify the result of Level2NpcTests.Hostile using ghost against Is.False; fail the test if the assertion
        // is not met.
        Assert.That(Hostile(ghost), Is.False);
        // Call Level2NpcTests.Call with ghost, "Awake"; invoke the named gameplay method through reflection,
        // including callbacks not run automatically in Edit Mode.
        Call(ghost, "Awake");
        // Call Level2NpcTests.Talk; simulate one NPC interaction without a physical player input event.
        Talk();
        // Verify the result of Level2NpcTests.Hostile using ghost against Is.True; fail the test if the assertion
        // is not met.
        Assert.That(Hostile(ghost), Is.True);
        // Exercise chasing before even the first Note; navigation is covered separately.
        // Call Level2NpcTests.Set with ghost, "requireNavigation", false; assign a named field through reflection
        // to arrange the test scenario.
        Set(ghost, "requireNavigation", false);
        // Call Level2NpcTests.Set with ghost, "requireLineOfSight", false; assign a named field through reflection
        // to arrange the test scenario.
        Set(ghost, "requireLineOfSight", false);
        // Store the result of Level2NpcTests.Get using ghost, "playerTransform" viewed as Transform in local
        // player.
        Transform player = (Transform)Get(ghost, "playerTransform");
        // Set ghost.transform.position to world position of player plus Vector3 right axis expressed in world
        // coordinates multiplied by 3.
        ghost.transform.position = player.position + Vector3.right * 3f;
        // Call Level2NpcTests.Call with ghost, "Update"; invoke the named gameplay method through reflection,
        // including callbacks not run automatically in Edit Mode.
        Call(ghost, "Update");
        // Verify the result of Level2NpcTests.Get using ghost, "isChasing" against Is.True; fail the test if the
        // assertion is not met.
        Assert.That(Get(ghost, "isChasing"), Is.True);
        // Keep Notes in local notes.
        Component[] notes = Notes;
        // Call Level2NpcTests.Collect with notes[0] (entry read from notes); send a Note pickup directly to the
        // artifact tracker for the story test.
        Collect(notes[0]);
        // Call Level2NpcTests.Collect with notes[1] (entry read from notes); send a Note pickup directly to the
        // artifact tracker for the story test.
        Collect(notes[1]);
        // Verify the result of Level2NpcTests.Hostile using ghost against Is.True; fail the test if the assertion
        // is not met.
        Assert.That(Hostile(ghost), Is.True);
        // Call Level2NpcTests.Collect with notes[2] (entry read from notes); send a Note pickup directly to the
        // artifact tracker for the story test.
        Collect(notes[2]);
        // Assert immediately, without giving Update a chance to repair the state.
        // Verify the result of Level2NpcTests.Hostile using ghost against Is.False; fail the test if the assertion
        // is not met.
        Assert.That(Hostile(ghost), Is.False);
        // Verify the result of Level2NpcTests.Get using ghost, "isChasing" against Is.False; fail the test if the
        // assertion is not met.
        Assert.That(Get(ghost, "isChasing"), Is.False);
        // Verify enabled state of ghost.GetComponent<NavMeshAgent>() against Is.False; fail the test if the
        // assertion is not met.
        Assert.That(ghost.GetComponent<NavMeshAgent>().enabled, Is.False);
        // Verify ghost.gameObject own activation flag against Is.True, "The existing enemy is retained."; fail the
        // test if the assertion is not met.
        Assert.That(ghost.gameObject.activeSelf, Is.True, "The existing enemy is retained.");
        // Verify the result of ghost.GetComponentsInChildren<Renderer>(true).All using r => !r.enabled against
        // Is.True; fail the test if the assertion is not met.
        Assert.That(ghost.GetComponentsInChildren<Renderer>(true).All(r => !r.enabled), Is.True);
        // Verify the result of ghost.GetComponentsInChildren<Collider>(true).All using c => !c.enabled against
        // Is.True; fail the test if the assertion is not met.
        Assert.That(ghost.GetComponentsInChildren<Collider>(true).All(c => !c.enabled), Is.True);
        // Verify the result of Level2NpcTests.Get using Find("ObjectiveItem"), "artifactsCompleted" against
        // Is.True; fail the test if the assertion is not met.
        Assert.That(Get(Find("ObjectiveItem"), "artifactsCompleted"), Is.True);

        // Set ghost.transform.position to world position of player.
        ghost.transform.position = player.position;
        // Store world position of ghost.transform in local stoppedPosition.
        Vector3 stoppedPosition = ghost.transform.position;
        // Call Level2NpcTests.Call with ghost, "UpdateCheckpointPatrol", world position of player plus
        // Vector3.right multiplied by 20f; invoke the named gameplay method through reflection, including
        // callbacks not run automatically in Edit Mode.
        Call(ghost, "UpdateCheckpointPatrol", player.position + Vector3.right * 20f);
        // Call Level2NpcTests.Call with ghost, "Repel", 5; invoke the named gameplay method through reflection,
        // including callbacks not run automatically in Edit Mode.
        Call(ghost, "Repel", 5f);
        // Call Level2NpcTests.Call with ghost, "OnDisable"; invoke the named gameplay method through reflection,
        // including callbacks not run automatically in Edit Mode.
        Call(ghost, "OnDisable");
        // Call Level2NpcTests.Call with ghost, "OnEnable"; invoke the named gameplay method through reflection,
        // including callbacks not run automatically in Edit Mode.
        Call(ghost, "OnEnable");
        // Call Level2NpcTests.Call with npc, "OnDisable"; invoke the named gameplay method through reflection,
        // including callbacks not run automatically in Edit Mode.
        Call(npc, "OnDisable");
        // Call Level2NpcTests.Call with npc, "OnEnable"; invoke the named gameplay method through reflection,
        // including callbacks not run automatically in Edit Mode.
        Call(npc, "OnEnable");
        // Store the result of Level2NpcTests.Get using npc, "revealDialogue" viewed as string[] in local pages.
        string[] pages = (string[])Get(npc, "revealDialogue");
        // Repeat this block while page is at most length of pages; start from int page = 0.
        for (int page = 0; page <= pages.Length; page++)
        {
            // Call Level2NpcTests.Call with ghost, "Update"; invoke the named gameplay method through reflection,
            // including callbacks not run automatically in Edit Mode.
            Call(ghost, "Update");
            // Call Level2NpcTests.Call with ghost, "TryCatchPlayerByDistance"; invoke the named gameplay method
            // through reflection, including callbacks not run automatically in Edit Mode.
            Call(ghost, "TryCatchPlayerByDistance");
            // Call Level2NpcTests.Call with ghost, "CatchPlayer"; invoke the named gameplay method through
            // reflection, including callbacks not run automatically in Edit Mode.
            Call(ghost, "CatchPlayer");
            // Verify the result of Level2NpcTests.Hostile using ghost against Is.False; fail the test if the
            // assertion is not met.
            Assert.That(Hostile(ghost), Is.False);
            // Verify world position of ghost.transform against the result of Is.EqualTo using stoppedPosition;
            // fail the test if the assertion is not met.
            Assert.That(ghost.transform.position, Is.EqualTo(stoppedPosition));
            // Verify ((GameObject)Get(Find("LoseSceneController"), "loseCanvas")) own activation flag against
            // Is.False; fail the test if the assertion is not met.
            Assert.That(((GameObject)Get(Find("LoseSceneController"), "loseCanvas")).activeSelf, Is.False);
            // Verify Unity gameplay clock multiplier (0 freezes gameplay time) against the result of Is.EqualTo
            // using 1f; fail the test if the assertion is not met.
            Assert.That(Time.timeScale, Is.EqualTo(1f));
            // Verify enabled state of ((Behaviour)Find("PlayerMovement")) against Is.True; fail the test if the
            // assertion is not met.
            Assert.That(((Behaviour)Find("PlayerMovement")).enabled, Is.True);
            // Call Level2NpcTests.Talk; simulate one NPC interaction without a physical player input event.
            Talk();
        }
        // Verify State against the result of Is.EqualTo using "ReadyToLeave"; fail the test if the assertion is
        // not met.
        Assert.That(State, Is.EqualTo("ReadyToLeave"));
        // Verify the result of Level2NpcTests.Get using npc, "nextSceneName" viewed as string against the result
        // of Is.EqualTo using "Level3"; fail the test if the assertion is not met.
        Assert.That((string)Get(npc, "nextSceneName"), Is.EqualTo("Level3"));
        // Verify the result of EditorBuildSettings.scenes.Any using s => s.enabled && s.path ==
        // "Assets/Scenes/Level3.unity" against Is.True; fail the test if the assertion is not met.
        Assert.That(EditorBuildSettings.scenes.Any(s => s.enabled && s.path == "Assets/Scenes/Level3.unity"), Is.True);
    }

    // Check that reveal orders reassurance culprit danger warning identity and victim.
    [Test]
    public void RevealOrdersReassuranceCulpritDangerWarningIdentityAndVictim()
    {
        // Verify the result of Level2NpcTests.Get using npc, "returnedDialogue" viewed as string against the
        // result of Does.Contain using "She's not going to hurt you anymore"; fail the test if the assertion is
        // not met.
        Assert.That((string)Get(npc, "returnedDialogue"), Does.Contain("She's not going to hurt you anymore"));
        // Store the result of Level2NpcTests.Get using npc, "revealDialogue" viewed as string[] in local pages.
        string[] pages = (string[])Get(npc, "revealDialogue");
        // Verify length of pages against the result of Is.EqualTo using 7; fail the test if the assertion is not
        // met.
        Assert.That(pages.Length, Is.EqualTo(7));
        // Verify pages[0] (entry read from pages) against the result of Does.Contain("Renato").And.Contain using
        // "serial killer"; fail the test if the assertion is not met.
        Assert.That(pages[0], Does.Contain("Renato").And.Contain("serial killer"));
        // Verify pages[1] (entry read from pages) against the result of Does.Contain("dangerous").And.Contain
        // using "target"; fail the test if the assertion is not met.
        Assert.That(pages[1], Does.Contain("dangerous").And.Contain("target"));
        // Verify pages[2] (entry read from pages) against the result of Does.Contain using "woman in white"; fail
        // the test if the assertion is not met.
        Assert.That(pages[2], Does.Contain("woman in white"));
        // Verify pages[3] (entry read from pages) against the result of Is.EqualTo using "The White Lady wasn't
        // trying to kill you."; fail the test if the assertion is not met.
        Assert.That(pages[3], Is.EqualTo("The White Lady wasn't trying to kill you."));
        // Verify pages[4] (entry read from pages) against the result of Does.Contain("warn
        // you").And.Contain("expose him").And.Contain using "justice"; fail the test if the assertion is not met.
        Assert.That(pages[4], Does.Contain("warn you").And.Contain("expose him").And.Contain("justice"));
        // Verify pages[5] (entry read from pages) against the result of Does.Contain using "Because I am the White
        // Lady."; fail the test if the assertion is not met.
        Assert.That(pages[5], Does.Contain("Because I am the White Lady."));
        // Verify pages[6] (entry read from pages) against the result of Does.Contain("Renato's
        // victims").And.Contain("before you came here").And.Contain using "these halls"; fail the test if the
        // assertion is not met.
        Assert.That(pages[6], Does.Contain("Renato's victims").And.Contain("before you came here").And.Contain("these halls"));
        // Verify the result of string.Join using " ", pages.Take(5) against the result of Does.Not.Contain using
        // "I am the White Lady"; fail the test if the assertion is not met.
        Assert.That(string.Join(" ", pages.Take(5)), Does.Not.Contain("I am the White Lady"));
    }

    // Check that existing female avatar and nervous animation import as humanoid.
    [Test]
    public void ExistingFemaleAvatarAndNervousAnimationImportAsHumanoid()
    {
        // Store the GameObject asset imported by Unity at "Assets/Assets/free assets/char_npc/char_npc.fbx" in
        // local model.
        var model = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Assets/free assets/char_npc/char_npc.fbx");
        // Verify model against Is.Not.Null; fail the test if the assertion is not met.
        Assert.That(model, Is.Not.Null);
        // Store the first Animator component on or beneath model in local animator.
        var animator = model.GetComponentInChildren<Animator>();
        // Verify animator against Is.Not.Null; fail the test if the assertion is not met.
        Assert.That(animator, Is.Not.Null);
        // Verify animator.avatar.isValid and animator.avatar.isHuman against Is.True; fail the test if the
        // assertion is not met.
        Assert.That(animator.avatar.isValid && animator.avatar.isHuman, Is.True);
        // Store the result of Level2NpcTests.Find using "StationaryNervousIdle" in local idle.
        Component idle = Find("StationaryNervousIdle");
        // Store the result of Level2NpcTests.Get using idle, "nervousClip" viewed as AnimationClip in local clip.
        var clip = (AnimationClip)Get(idle, "nervousClip");
        // Verify clip against Is.Not.Null; fail the test if the assertion is not met.
        Assert.That(clip, Is.Not.Null);
        // Verify the result of AssetDatabase.GetAssetPath using clip against the result of Is.EqualTo using
        // "Assets/Assets/free assets/char_npc/Nervously Look Around.fbx"; fail the test if the assertion is not
        // met.
        Assert.That(AssetDatabase.GetAssetPath(clip), Is.EqualTo("Assets/Assets/free assets/char_npc/Nervously Look Around.fbx"));
        // Verify clip.isHumanMotion and clip.isLooping and clip.length is above 0f against Is.True; fail the test
        // if the assertion is not met.
        Assert.That(clip.isHumanMotion && clip.isLooping && clip.length > 0f, Is.True);
        // Store the result of Level2NpcTests.Get using idle, "character" viewed as Transform in local visual.
        Transform visual = (Transform)Get(idle, "character");
        // Verify visual parent Transform against the result of Is.EqualTo using npc.transform; fail the test if
        // the assertion is not met.
        Assert.That(visual.parent, Is.EqualTo(npc.transform));
        // Verify the result of PrefabUtility.GetCorrespondingObjectFromSource using visual.gameObject against the
        // result of Is.EqualTo using model; fail the test if the assertion is not met.
        Assert.That(PrefabUtility.GetCorrespondingObjectFromSource(visual.gameObject), Is.EqualTo(model));
        // Verify length of npc.GetComponentsInChildren<Animator>() against the result of Is.EqualTo using 1; fail
        // the test if the assertion is not met.
        Assert.That(npc.GetComponentsInChildren<Animator>().Length, Is.EqualTo(1));
        // Update Unity Physics with Transform changes before collision queries.
        Physics.SyncTransforms();
        // Store the first Collider component on npc in local interaction.
        Collider interaction = npc.GetComponent<Collider>();
        // Store a new Ray initialized with npc.transform.position + Vector3.up * 1.3f + npc.transform.forward * 2f
        // (calculated from the values shown), negative npc.transform forward axis expressed in world coordinates
        // in local ray.
        var ray = new Ray(npc.transform.position + Vector3.up * 1.3f + npc.transform.forward * 2f, -npc.transform.forward);
        // Verify the ray hits a collider within the specified range/layers; store collider, point and distance in
        // RaycastHit hit against Is.True; fail the test if the assertion is not met.
        Assert.That(interaction.Raycast(ray, out RaycastHit hit, 3f), Is.True);
        // Verify the first Script("Level2NoteNpc") component on hit.collider against the result of Is.SameAs using
        // npc; fail the test if the assertion is not met.
        Assert.That(hit.collider.GetComponent(Script("Level2NoteNpc")), Is.SameAs(npc));
        // Verify the result of Level2NpcTests.Get using idle, "environmentRoot" against the result of Is.SameAs
        // using Get(Find("GhostNavigation"), "environmentRoot"); fail the test if the assertion is not met.
        Assert.That(Get(idle, "environmentRoot"), Is.SameAs(Get(Find("GhostNavigation"), "environmentRoot")));
        // Store world position of npc.transform in local position.
        Vector3 position = npc.transform.position;
        // Call Level2NpcTests.Call with idle, "FacePlayer", Transform of Find("PlayerMovement"); invoke the named
        // gameplay method through reflection, including callbacks not run automatically in Edit Mode.
        Call(idle, "FacePlayer", Find("PlayerMovement").transform);
        // Verify world position of npc.transform against the result of Is.EqualTo using position; fail the test if
        // the assertion is not met.
        Assert.That(npc.transform.position, Is.EqualTo(position));
    }
    // Check that nervous clip animates and loops without moving npc off current floor.
    [Test]
    public void NervousClipAnimatesAndLoopsWithoutMovingNpcOffCurrentFloor()
    {
        // Store the result of Level2NpcTests.Find using "GhostNavigation" in local navigation.
        Component navigation = Find("GhostNavigation");
        // Call Level2NpcTests.Call with navigation, "Awake"; invoke the named gameplay method through reflection,
        // including callbacks not run automatically in Edit Mode.
        Call(navigation, "Awake");
        // Store the result of Level2NpcTests.Find using "StationaryNervousIdle" in local idle.
        Component idle = Find("StationaryNervousIdle");
        // Store the first Animator component on or beneath ((Transform)Get(idle, "character")) in local
        // placedAnimator.
        Animator placedAnimator = ((Transform)Get(idle, "character")).GetComponentInChildren<Animator>();
        // Call Level2NpcTests.Call with idle, "Start"; invoke the named gameplay method through reflection,
        // including callbacks not run automatically in Edit Mode.
        Call(idle, "Start");
        // Store value read through reflection from idle viewed as Animator in local animator.
        Animator animator = (Animator)idle.GetType().GetProperty("Animator").GetValue(idle);
        // Verify animator against the result of Is.SameAs using placedAnimator; fail the test if the assertion is
        // not met.
        Assert.That(animator, Is.SameAs(placedAnimator));
        // Verify length of npc.GetComponentsInChildren<Animator>() against the result of Is.EqualTo using 1; fail
        // the test if the assertion is not met.
        Assert.That(npc.GetComponentsInChildren<Animator>().Length, Is.EqualTo(1));
        // Verify animator.applyRootMotion against Is.False; fail the test if the assertion is not met.
        Assert.That(animator.applyRootMotion, Is.False);
        // Verify npc.transform.position Y component against the result of Is.EqualTo(-1.74f).Within using 0.05f;
        // fail the test if the assertion is not met.
        Assert.That(npc.transform.position.y, Is.EqualTo(-1.74f).Within(0.05f));
        // Store world position of npc.transform in local fixedPosition.
        Vector3 fixedPosition = npc.transform.position;
        // Store the HumanBodyBones.Head bone Transform supplied by animator Humanoid avatar in local head.
        Transform head = animator.GetBoneTransform(HumanBodyBones.Head);
        // Store parent-relative rotation of head in local initialHead.
        Quaternion initialHead = head.localRotation;
        // Store reflection metadata for field "animationTime" on idle.GetType() in local time.
        var time = idle.GetType().GetField("animationTime", BindingFlags.Instance | BindingFlags.NonPublic);
        // Store ((AnimationClip)Get(idle, "nervousClip")) clip length in seconds in local length.
        float length = ((AnimationClip)Get(idle, "nervousClip")).length;
        // Store parent-relative position of animator.transform in local fixedVisualPosition.
        Vector3 fixedVisualPosition = animator.transform.localPosition;
        // Initialize poseChanged to false.
        bool poseChanged = false;
        // Repeat this block while i is at most 12; start from int i = 1.
        for (int i = 1; i <= 12; i++)
        {
            // Write length multiplied by i divided by 5 through reflection on idle.
            time.SetValue(idle, length * i / 5f);
            // Call Level2NpcTests.Call with idle, "LateUpdate"; invoke the named gameplay method through
            // reflection, including callbacks not run automatically in Edit Mode.
            Call(idle, "LateUpdate");
            // Set poseChanged to the angle in degrees between initialHead and head.localRotation is above 0.1.
            poseChanged |= Quaternion.Angle(initialHead, head.localRotation) > 0.1f;
            // Verify world position of npc.transform against the result of Is.EqualTo using fixedPosition; fail
            // the test if the assertion is not met.
            Assert.That(npc.transform.position, Is.EqualTo(fixedPosition));
            // Verify the distance between animator.transform.localPosition and fixedVisualPosition against the
            // result of Is.LessThan using 0.001f; fail the test if the assertion is not met.
            Assert.That(Vector3.Distance(animator.transform.localPosition, fixedVisualPosition), Is.LessThan(0.001f));
            // Verify value read through reflection from idle viewed as float against the result of Is.LessThan
            // using length; fail the test if the assertion is not met.
            Assert.That((float)time.GetValue(idle), Is.LessThan(length));
        }
        // Verify poseChanged against Is.True, the message/string written here; fail the test if the assertion is
        // not met.
        Assert.That(poseChanged, Is.True, "The existing clip must animate the Humanoid, not leave a bind pose.");
    }

}
