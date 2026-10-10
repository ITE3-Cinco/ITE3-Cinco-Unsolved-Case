using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

public class Level2NoteQuestTests
{
    private static Type Gameplay(string name) => Type.GetType(name + ", Assembly-CSharp", true);
    private static object Get(object target, string field) => target.GetType().GetField(field).GetValue(target);
    private static object Property(object target, string name) => target.GetType().GetProperty(name).GetValue(target);
    private static object Call(object target, string method, params object[] args) => target.GetType()
        .GetMethod(method, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic).Invoke(target, args);
    private static Component Find(string type) => (Component)UnityEngine.Object.FindFirstObjectByType(Gameplay(type), FindObjectsInactive.Include);

    [UnityTest]
    public IEnumerator SceneSupportsTheCompleteNoteQuestAndExistingEnemy()
    {
        EditorSceneManager.OpenScene("Assets/Scenes/Lvl-2.unity", OpenSceneMode.Single);
        yield return new EnterPlayMode();
        yield return null;

        Component[] sceneComponents = SceneManager.GetActiveScene().GetRootGameObjects()
            .SelectMany(root => root.GetComponentsInChildren<Component>(true)).ToArray();
        Assert.That(sceneComponents.All(component => component != null), Is.True, "No missing scene scripts.");
        foreach (MeshCollider collider in sceneComponents.OfType<MeshCollider>())
            Assert.That(collider.sharedMesh, Is.Not.Null, collider.name);
        Assert.That(sceneComponents.OfType<Camera>().Count(camera => camera.isActiveAndEnabled), Is.EqualTo(1));
        Assert.That(sceneComponents.OfType<AudioListener>().Count(listener => listener.isActiveAndEnabled), Is.EqualTo(1));

        Component quest = Find("Level2NoteQuest");
        Component npc = Find("Level2NoteNpc");
        Component ghost = Find("GhostEnemy");
        Component player = Find("PlayerMovement");
        Component inventory = Find("Inventory");
        Component manager = Find("ArtifactManager");
        GameObject playerObject = player.gameObject;
        Array notes = (Array)Get(quest, "notes");
        IList items = (IList)Get(inventory, "items");
        Assert.That(notes.Length, Is.EqualTo(3));
        Assert.That(Property(quest, "State").ToString(), Is.EqualTo("NotStarted"));
        Assert.That(Get(Find("PlayerInteraction"), "interactionText"), Is.Not.Null);
        Assert.That(Get(Find("LoseSceneController"), "retrySceneName"), Is.EqualTo("Lvl-2"));
        Assert.That(items.Count, Is.EqualTo(1), "Starts with the existing flashlight.");

        Animator npcAnimator = (Animator)Get(npc, "animator");
        AnimationClip nervous = (AnimationClip)Get(npc, "nervousIdle");
        Assert.That(npcAnimator, Is.Not.Null);
        Assert.That(npcAnimator.avatar.isHuman && npcAnimator.avatar.isValid, Is.True);
        Assert.That(nervous != null && nervous.isHumanMotion, Is.True);
        Assert.That(npcAnimator.applyRootMotion, Is.False);
        Assert.That(Property(ghost.GetComponent(Gameplay("GhostAnimation")), "Animator"), Is.Not.Null);
        Assert.That(ghost.GetComponent<NavMeshAgent>().isOnNavMesh, Is.True);
        Assert.That(Get(ghost, "playerTransform"), Is.EqualTo(player.transform));
        Assert.That(Get(ghost, "loseSceneController"), Is.Not.Null);
        Assert.That(Get(ghost.GetComponent(Gameplay("GhostAudio")), "screamClip"), Is.Not.Null);
        Assert.That(Get(ghost.GetComponent(Gameplay("GhostAudio")), "hummingClip"), Is.Not.Null);

        Physics.SyncTransforms();
        CharacterController controller = player.GetComponent<CharacterController>();
        Vector3 feet = controller.bounds.center - Vector3.up * controller.bounds.extents.y;
        Assert.That(NavMesh.SamplePosition(feet, out NavMeshHit start, 0.75f, NavMesh.AllAreas), Is.True);
        foreach (Transform point in (Transform[])Get(ghost, "patrolWaypoints"))
            AssertReachable(start.position, point.position, point.name);
        foreach (Component note in notes)
            AssertReachable(start.position, note.transform.position, note.name, 1f);
        AssertReachable(start.position, npc.transform.position, "NPC");
        // Keep the enemy from interrupting isolated quest assertions; the live scene above validates its setup.
        ((Behaviour)ghost).enabled = false;
        ghost.GetComponent<NavMeshAgent>().isStopped = true;

        Component first = (Component)notes.GetValue(0);
        Call(first, "Interact", playerObject);
        Assert.That(Get(manager, "artifactsCollected"), Is.EqualTo(0));
        Assert.That(items.Count, Is.EqualTo(1), "Notes cannot be consumed before the first conversation.");
        Call(npc, "Interact", playerObject);
        Assert.That(Property(quest, "State").ToString(), Is.EqualTo("FindingNotes"));
        Call(npc, "Interact", playerObject);
        Assert.That(Property(quest, "State").ToString(), Is.EqualTo("FindingNotes"));

        object flashlight = items[0];
        while (items.Count < (int)Get(inventory, "maxSlots")) items.Add(flashlight);
        LogAssert.Expect(LogType.Warning, "[Interactable] Could not add item to inventory (inventory full). Pickup cancelled.");
        Call(first, "Interact", playerObject);
        Assert.That(Get(manager, "artifactsCollected"), Is.EqualTo(0), "Full inventory preserves the Note.");
        while (items.Count > 1) items.RemoveAt(items.Count - 1);
        Call(first, "Interact", playerObject);
        Call(first, "Interact", playerObject); // Same-frame duplicate, before Destroy runs.
        Assert.That(Get(manager, "artifactsCollected"), Is.EqualTo(1));
        Assert.That(items.Count, Is.EqualTo(2));
        yield return null;
        Assert.That(first == null, Is.True);

        for (int i = 1; i < notes.Length; i++) Call(notes.GetValue(i), "Interact", playerObject);
        Assert.That(items.Count, Is.EqualTo(2), "Notes share one hotbar icon.");
        Assert.That(Get(manager, "artifactsCollected"), Is.EqualTo(3));
        Assert.That(Property(quest, "RemainingNotes"), Is.EqualTo(0));
        Assert.That(Property(quest, "State").ToString(), Is.EqualTo("AllNotesCollected"));
        Assert.That(Get(Find("ObjectiveItem"), "primaryObjective").ToString(), Does.Contain("Return"));
        Assert.That(Get(Find("ObjectiveItem"), "artifactsCompleted"), Is.EqualTo(false), "No Level 1 escape objective.");
        Call(npc, "Interact", playerObject);
        Assert.That(Property(quest, "State").ToString(), Is.EqualTo("Completed"));
        Assert.That(items.Count, Is.EqualTo(1), "Returned Notes leave the inventory.");
        Assert.That(items[0], Is.EqualTo(flashlight));
        Assert.That(npc != null && npc.gameObject.activeInHierarchy, Is.True);
        Assert.That(Application.CanStreamedLevelBeLoaded((string)Get(quest, "nextSceneName")), Is.True);
        LogAssert.NoUnexpectedReceived();
        yield return new ExitPlayMode();
    }

    private static void AssertReachable(Vector3 from, Vector3 to, string label, float radius = 0.75f)
    {
        Assert.That(NavMesh.SamplePosition(to, out NavMeshHit hit, radius, NavMesh.AllAreas), Is.True, label);
        NavMeshPath path = new NavMeshPath();
        Assert.That(NavMesh.CalculatePath(from, hit.position, NavMesh.AllAreas, path), Is.True, label);
        Assert.That(path.status, Is.EqualTo(NavMeshPathStatus.PathComplete), label);
    }

    [UnityTearDown]
    public IEnumerator TearDown()
    {
        if (Application.isPlaying) yield return new ExitPlayMode();
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
    }
}
