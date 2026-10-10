using System;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Events;
using UnityEngine.SceneManagement;

public class Level2NpcTests
{
    private Scene scene;
    private float timeScale;
    private Component npc;
    private Component artifacts;
    private static Type Script(string name) => Type.GetType(name + ", Assembly-CSharp", true);
    private static object Get(object target, string field) => target.GetType().GetField(field).GetValue(target);
    private static void Set(object target, string field, object value) => target.GetType().GetField(field).SetValue(target, value);
    private static object Call(object target, string method, params object[] args) => target.GetType()
        .GetMethod(method, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic).Invoke(target, args);
    private Component[] Components => scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<Component>(true)).ToArray();
    private Component Find(string name) => Components.Single(c => c != null && c.GetType() == Script(name));
    private Component[] Notes => Components.Where(c => c != null && c.GetType() == Script("Interactable") && (bool)Get(c, "isArtifact")).ToArray();
    private string State => npc.GetType().GetProperty("State").GetValue(npc).ToString();

    [SetUp]
    public void SetUp()
    {
        timeScale = Time.timeScale;
        Time.timeScale = 1f;
        scene = EditorSceneManager.OpenScene("Assets/Scenes/Lvl-2.unity", OpenSceneMode.Additive);
        npc = Find("Level2NoteNpc");
        artifacts = Find("ArtifactManager");
    }

    [TearDown]
    public void TearDown()
    {
        EditorSceneManager.CloseScene(scene, true);
        Time.timeScale = timeScale;
    }

    private void PrepareConversation()
    {
        // Suppress notification coroutines in Edit Mode; keep the real tracker,
        // objective and scene wiring. Runtime presentation is a Play Mode check.
        Set(npc, "notifications", null);
        Call(npc, "OnEnable");
        Call(npc, "Start");
    }

    private void Talk() => Call(npc, "Interact", new object[] { null });
    private void Collect(Component note) => Call(artifacts, "CollectArtifact", note);

    [Test]
    public void SceneUsesExistingNotesUiAndRetryWithoutGivingNpcMovementAi()
    {
        Assert.That(Get(npc, "artifacts"), Is.SameAs(artifacts));
        Assert.That(Get(npc, "objective"), Is.SameAs(Find("ObjectiveItem")));
        Assert.That(Get(npc, "notifications"), Is.SameAs(Find("NotificationManager")));
        MonoScript notificationScript = AssetDatabase.LoadAssetAtPath<MonoScript>("Assets/Scripts/NotificationManager.cs");
        Assert.That(notificationScript, Is.Not.Null);
        Assert.That(notificationScript.GetClass(), Is.EqualTo(Script("NotificationManager")));
        Assert.That(Get(Find("PlayerInteraction"), "interactionText"), Is.Not.Null);
        Assert.That(npc.GetComponent<Collider>(), Is.Not.Null);
        Assert.That(npc.GetComponent<NavMeshAgent>(), Is.Null);
        Assert.That(npc.GetComponent<Rigidbody>(), Is.Null);
        Assert.That(npc.GetComponent(Script("GhostEnemy")), Is.Null);
        Assert.That(Notes.Length, Is.EqualTo(3));
        Assert.That(Get(artifacts, "totalArtifacts"), Is.EqualTo(Notes.Length));
        UnityEngine.Object note = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>("Assets/Assets/Inventory/Item_Note.asset");
        foreach (Component collectible in Notes) Assert.That(Get(collectible, "itemData"), Is.EqualTo(note));
        Assert.That(((Behaviour)Find("GhostEnemy")).enabled, Is.True);
        Assert.That(Get(Find("GhostEnemy"), "loseSceneController"), Is.SameAs(Find("LoseSceneController")));
        Assert.That(Get(Find("LoseSceneController"), "retrySceneName"), Is.EqualTo("Lvl-2"));
    }

    [Test]
    public void InitialAndPartialConversationsCannotRevealStoryAndDuplicatesDoNotCompleteIt()
    {
        PrepareConversation();
        int returns = 0;
        ((UnityEvent)Get(npc, "onCluesReturned")).AddListener(() => returns++);
        Talk();
        Assert.That(State, Is.EqualTo("FindingClues"));
        Assert.That(Get(Find("ObjectiveItem"), "primaryObjective"), Is.EqualTo("Find all the clues and return to her."));
        string first = (string)Get(npc, "initialDialogue");
        Assert.That(first, Does.Not.Contain("Renato").And.Not.Contain("White Lady"));
        Component[] notes = Notes;
        Collect(notes[0]);
        Collect(notes[0]);
        Collect(notes[1]);
        Talk();
        Assert.That(Get(artifacts, "artifactsCollected"), Is.EqualTo(2));
        Assert.That(State, Is.EqualTo("FindingClues"));
        Assert.That(returns, Is.Zero);
        Collect(notes[2]);
        Assert.That(State, Is.EqualTo("FindingClues"), "Collection alone must not finish the story gate.");
        Assert.That(Get(Find("ObjectiveItem"), "primaryObjective"), Is.EqualTo("All clues found. Return to the frightened woman."));
        Talk();
        Assert.That(State, Is.EqualTo("Returned"));
        Assert.That(returns, Is.EqualTo(1));
        Assert.That((string)Get(npc, "returnedDialogue"), Does.Not.Contain("Renato").And.Not.Contain("White Lady"));
        string[] reveal = (string[])Get(npc, "revealDialogue");
        for (int i = 0; i < reveal.Length; i++) Talk();
        Assert.That(State, Is.EqualTo("ReadyToLeave"));
        Assert.That(returns, Is.EqualTo(1));
        Assert.That(string.Join(" ", reveal), Does.Contain("Renato").And.Contain("serial killer").And.Contain("White Lady").And.Contain("victims").And.Contain("justice"));
    }

    [Test]
    public void NotesCollectedBeforeMeetingAreRetainedButStillRequireReturnConversation()
    {
        PrepareConversation();
        foreach (Component note in Notes) Collect(note);
        Talk();
        Assert.That(State, Is.EqualTo("FindingClues"));
        Assert.That(Get(Find("ObjectiveItem"), "primaryObjective"), Is.EqualTo("All clues found. Return to the frightened woman."));
        Talk();
        Assert.That(State, Is.EqualTo("Returned"));
    }

    [Test]
    public void FrozenGameplayCannotAdvanceAndFreshSceneResetsQuestWithNotes()
    {
        PrepareConversation();
        Talk();
        Collect(Notes[0]);
        Time.timeScale = 0f;
        Talk();
        Assert.That(State, Is.EqualTo("FindingClues"));
        EditorSceneManager.CloseScene(scene, true);
        scene = EditorSceneManager.OpenScene("Assets/Scenes/Lvl-2.unity", OpenSceneMode.Additive);
        npc = Find("Level2NoteNpc");
        artifacts = Find("ArtifactManager");
        Assert.That(State, Is.EqualTo("NotStarted"));
        Assert.That(Get(artifacts, "artifactsCollected"), Is.EqualTo(0));
        Assert.That(Notes.Length, Is.EqualTo(3));
    }

    [Test]
    public void ExistingFemaleAvatarAndNervousAnimationImportAsHumanoid()
    {
        var model = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Assets/free assets/char_npc/char_npc.fbx");
        Assert.That(model, Is.Not.Null);
        var animator = model.GetComponentInChildren<Animator>();
        Assert.That(animator, Is.Not.Null);
        Assert.That(animator.avatar.isValid && animator.avatar.isHuman, Is.True);
        Component idle = Find("StationaryNervousIdle");
        var clip = (AnimationClip)Get(idle, "nervousClip");
        Assert.That(clip, Is.Not.Null);
        Assert.That(AssetDatabase.GetAssetPath(clip), Is.EqualTo("Assets/Assets/free assets/char_npc/Nervously Look Around.fbx"));
        Assert.That(clip.isHumanMotion && clip.isLooping && clip.length > 0f, Is.True);
        Transform visual = (Transform)Get(idle, "character");
        Assert.That(visual.parent, Is.EqualTo(npc.transform));
        Assert.That(PrefabUtility.GetCorrespondingObjectFromSource(visual.gameObject), Is.EqualTo(model));
        Assert.That(npc.GetComponentsInChildren<Animator>().Length, Is.EqualTo(1));
        Physics.SyncTransforms();
        Collider interaction = npc.GetComponent<Collider>();
        var ray = new Ray(npc.transform.position + Vector3.up * 1.3f + npc.transform.forward * 2f, -npc.transform.forward);
        Assert.That(interaction.Raycast(ray, out RaycastHit hit, 3f), Is.True);
        Assert.That(hit.collider.GetComponent(Script("Level2NoteNpc")), Is.SameAs(npc));
        Assert.That(Get(idle, "environmentRoot"), Is.SameAs(Get(Find("GhostNavigation"), "environmentRoot")));
        Vector3 position = npc.transform.position;
        Call(idle, "FacePlayer", Find("PlayerMovement").transform);
        Assert.That(npc.transform.position, Is.EqualTo(position));
    }
    [Test]
    public void NervousClipAnimatesAndLoopsWithoutMovingNpcOffCurrentFloor()
    {
        Component navigation = Find("GhostNavigation");
        Call(navigation, "Awake");
        Component idle = Find("StationaryNervousIdle");
        Animator placedAnimator = ((Transform)Get(idle, "character")).GetComponentInChildren<Animator>();
        Call(idle, "Start");
        Animator animator = (Animator)idle.GetType().GetProperty("Animator").GetValue(idle);
        Assert.That(animator, Is.SameAs(placedAnimator));
        Assert.That(npc.GetComponentsInChildren<Animator>().Length, Is.EqualTo(1));
        Assert.That(animator.applyRootMotion, Is.False);
        Assert.That(npc.transform.position.y, Is.EqualTo(-1.74f).Within(0.05f));
        Vector3 fixedPosition = npc.transform.position;
        Transform head = animator.GetBoneTransform(HumanBodyBones.Head);
        Quaternion initialHead = head.localRotation;
        var time = idle.GetType().GetField("animationTime", BindingFlags.Instance | BindingFlags.NonPublic);
        float length = ((AnimationClip)Get(idle, "nervousClip")).length;
        Vector3 fixedVisualPosition = animator.transform.localPosition;
        bool poseChanged = false;
        for (int i = 1; i <= 12; i++)
        {
            time.SetValue(idle, length * i / 5f);
            Call(idle, "LateUpdate");
            poseChanged |= Quaternion.Angle(initialHead, head.localRotation) > 0.1f;
            Assert.That(npc.transform.position, Is.EqualTo(fixedPosition));
            Assert.That(Vector3.Distance(animator.transform.localPosition, fixedVisualPosition), Is.LessThan(0.001f));
            Assert.That((float)time.GetValue(idle), Is.LessThan(length));
        }
        Assert.That(poseChanged, Is.True, "The existing clip must animate the Humanoid, not leave a bind pose.");
    }

}
