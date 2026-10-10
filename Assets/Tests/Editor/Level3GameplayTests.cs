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
using UnityEngine.UI;
using UnityEngine.Video;

public class Level3GameplayTests
{
    private Scene level;
    private float timeScale;
    private CursorLockMode cursorLock;
    private bool cursorVisible;
    private static Type Gameplay(string name) => Type.GetType(name + ", Assembly-CSharp", true);
    private static object Get(object target, string field) => target.GetType().GetField(field).GetValue(target);
    private static void Set(object target, string field, object value) => target.GetType().GetField(field).SetValue(target, value);
    private static void Call(object target, string method) => target.GetType()
        .GetMethod(method, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic).Invoke(target, null);
    private Component[] Components => level.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<Component>(true)).ToArray();
    private Component Find(string type) => Components.Single(c => c != null && c.GetType() == Gameplay(type));

    [SetUp]
    public void SetUp()
    {
        timeScale = Time.timeScale;
        cursorLock = Cursor.lockState;
        cursorVisible = Cursor.visible;
        Time.timeScale = 1f;
        level = EditorSceneManager.OpenScene("Assets/Scenes/Level3.unity", OpenSceneMode.Additive);
    }

    [TearDown]
    public void TearDown()
    {
        if (level.IsValid()) EditorSceneManager.CloseScene(level, true);
        Time.timeScale = timeScale;
        Cursor.lockState = cursorLock;
        Cursor.visible = cursorVisible;
    }

    [Test]
    public void CopiedReferencesAndExistingRenatoAssetsResolve()
    {
        Assert.That(Components.Count(c => c is Camera && c.gameObject.activeInHierarchy), Is.EqualTo(1));
        Assert.That(Components.Count(c => c is AudioListener && c.gameObject.activeInHierarchy), Is.EqualTo(1));
        Assert.That(Components.Count(c => c != null && c.GetType().Name == "EventSystem"), Is.EqualTo(1));
        Component player = Find("PlayerMovement");
        Assert.That(Get(player, "flashlight"), Is.Not.Null);
        Assert.That(Get(player, "staminaSlider"), Is.Not.Null);
        Assert.That(Get(player, "hasFlashlight"), Is.EqualTo(true));
        Component inventory = Find("Inventory");
        Assert.That(Get(inventory, "hotbarUI"), Is.Not.Null);
        Assert.That(((IList)Get(inventory, "items"))[0], Is.EqualTo(Get(player, "flashlightItemData")));
        Component objective = Find("ObjectiveItem");
        Assert.That(Get(objective, "objectiveText"), Is.Not.Null);
        Assert.That(Get(objective, "primaryObjective"), Is.EqualTo("Escape the asylum."));
        Component lose = Find("LoseSceneController");
        Assert.That(Get(lose, "retrySceneName"), Is.EqualTo("Level3"));
        Assert.That(Get(lose, "exitSceneName"), Is.EqualTo("MainMenu"));
        Assert.That(Get(lose, "tryAgainButton"), Is.TypeOf<Button>());
        Assert.That(Get(lose, "exitButton"), Is.TypeOf<Button>());
        Assert.That(Get(Find("Level3Flow"), "pauseButton"), Is.TypeOf<Button>());
        Component animation = Find("RenatoAnimation");
        Animator animator = (Animator)Get(animation, "animator");
        Assert.That(animator, Is.Not.Null);
        Assert.That(animator.avatar.isValid && animator.avatar.isHuman, Is.True);
        foreach (string field in new[] { "walkingClip", "runningClip", "standClip", "lookClip" })
        {
            AnimationClip clip = (AnimationClip)Get(animation, field);
            Assert.That(clip, Is.Not.Null, field);
            Assert.That(clip.isHumanMotion, Is.True, field);
        }
    }

    [Test]
    public void EscapeUsesTheImportedFinalEndingAndAnEnabledMainMenu()
    {
        Component flow = Find("Level3Flow");
        VideoClip clip = (VideoClip)Get(flow, "endingClip");
        Assert.That(clip, Is.Not.Null);
        Assert.That(AssetDatabase.GetAssetPath(clip), Is.EqualTo("Assets/Assets/ENDING CLIP/final ending.mp4"));
        Assert.That(clip.frameCount, Is.GreaterThan(0));
        Assert.That(clip.width, Is.GreaterThan(0));
        Assert.That(Get(flow, "endingAudioMixerGroup"), Is.Not.Null);
        Assert.That(EditorBuildSettings.scenes.Any(s => s.enabled && s.path == "Assets/Scenes/MainMenu.unity"), Is.True);
    }

    [Test]
    public void RuntimeBakeConnectsSpawnPatrolSearchRoomsAndExit()
    {
        Component navigation = Find("GhostNavigation");
        Physics.SyncTransforms();
        Call(navigation, "Awake");
        CharacterController player = Find("PlayerMovement").GetComponent<CharacterController>();
        Vector3 feet = player.bounds.center - Vector3.up * player.bounds.extents.y;
        Assert.That(NavMesh.SamplePosition(feet, out NavMeshHit start, 0.75f, NavMesh.AllAreas), Is.True, "Dwight spawn");
        Component enemy = Find("RenatoEnemy");
        AssertReachable(start.position, enemy.transform.position, "Renato spawn");
        foreach (Transform point in (Transform[])Get(enemy, "patrolPoints")) AssertReachable(start.position, point.position, point.name);
        foreach (object room in (IEnumerable)Get(enemy, "rooms"))
        {
            Transform point = (Transform)Get(room, "searchPosition");
            AssertReachable(start.position, point.position, point.name);
            point = (Transform)Get(room, "entrance");
            AssertReachable(start.position, point.position, point.name);
        }
        BoxCollider exit = (BoxCollider)Get(Find("Level3Flow"), "mainExit");
        AssertReachable(start.position, new Vector3(exit.bounds.center.x, 0.04f, exit.bounds.center.z), "Main door");
    }

    private static void AssertReachable(Vector3 from, Vector3 to, string label)
    {
        Assert.That(NavMesh.SamplePosition(to, out NavMeshHit hit, 0.75f, NavMesh.AllAreas), Is.True, label);
        NavMeshPath path = new NavMeshPath();
        Assert.That(NavMesh.CalculatePath(from, hit.position, NavMesh.AllAreas, path), Is.True, label);
        Assert.That(path.status, Is.EqualTo(NavMeshPathStatus.PathComplete), label);
    }

    [TestCase(1)]
    [TestCase(40)]
    public void SolidWallsBlockSightButTriggersDoNot(int wallCount)
    {
        Component enemy = Find("RenatoEnemy");
        Transform player = (Transform)Get(enemy, "player");
        enemy.transform.position = new Vector3(1000, 1000, 1000);
        player.position = enemy.transform.position + new Vector3(0, 1.5f, 4);
        Collider[] walls = new Collider[wallCount];
        for (int i = 0; i < wallCount; i++)
        {
            GameObject wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            SceneManager.MoveGameObjectToScene(wall, level);
            wall.transform.position = enemy.transform.position + new Vector3(0, 1.5f, 1f + i * 0.05f);
            wall.transform.localScale = new Vector3(3, 3, 0.02f);
            walls[i] = wall.GetComponent<Collider>();
        }
        Physics.SyncTransforms();
        MethodInfo sight = enemy.GetType().GetMethod("HasClearSight");
        Assert.That(sight.Invoke(enemy, null), Is.EqualTo(false));
        foreach (Collider wall in walls) wall.isTrigger = true;
        Physics.SyncTransforms();
        Assert.That(sight.Invoke(enemy, null), Is.EqualTo(true));
    }

    [Test]
    public void RenatoAudioCrossfadesWithPursuitAndFadesOutAfterCatch()
    {
        Component audio = Find("RenatoAudio");
        Component enemy = Find("RenatoEnemy");
        Assert.That(Get(audio, "whistleClip"), Is.Not.Null);
        Assert.That(Get(audio, "chaseClip"), Is.Not.Null);
        Call(audio, "Awake");
        AudioSource whistle = (AudioSource)Get(audio, "whistleSource");
        AudioSource chase = (AudioSource)Get(audio, "chaseSource");
        AudioSource ambience = (AudioSource)Get(audio, "ambienceSource");
        Assert.That(whistle, Is.Not.SameAs(chase));
        Assert.That(whistle.spatialBlend, Is.EqualTo(1f));
        Assert.That(chase.spatialBlend, Is.Zero);
        Assert.That(whistle.outputAudioMixerGroup.name, Is.EqualTo("SFXVolume"));
        Assert.That(chase.outputAudioMixerGroup.name, Is.EqualTo("MusicVolume"));
        MethodInfo mix = audio.GetType().GetMethod("UpdateMix", BindingFlags.Instance | BindingFlags.NonPublic);
        Action<float> tick = dt => mix.Invoke(audio, new object[] { dt });
        tick(2f);
        Assert.That(whistle.volume, Is.EqualTo(0.7f).Within(0.001f));
        Assert.That(chase.volume, Is.Zero);
        FieldInfo state = enemy.GetType().GetField("<State>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic);
        state.SetValue(enemy, Enum.Parse(state.FieldType, "Chase"));
        tick(1f);
        Assert.That(whistle.volume, Is.InRange(0.01f, 0.69f));
        Assert.That(chase.volume, Is.InRange(0.01f, 0.64f));
        tick(1f);
        Assert.That(chase.volume, Is.EqualTo(0.65f).Within(0.001f));
        Assert.That(whistle.volume, Is.Zero);
        Assert.That(ambience.volume, Is.EqualTo(0.04f).Within(0.001f));
        state.SetValue(enemy, Enum.Parse(state.FieldType, "Investigate"));
        tick(1f);
        Assert.That(chase.volume, Is.InRange(0.01f, 0.64f), "Losing pursuit fades rather than cuts the music.");
        tick(1f);
        Assert.That(chase.volume, Is.Zero);
        Assert.That(whistle.volume, Is.EqualTo(0.7f).Within(0.001f));
        Call(Find("PlayerAudio"), "Awake");
        Call(Find("Level3Flow"), "Caught");
        Assert.That(Time.timeScale, Is.Zero);
        tick(2f);
        Assert.That(whistle.volume, Is.Zero);
        Assert.That(chase.volume, Is.Zero);
        Assert.That(ambience.volume, Is.Zero);
    }

    [TestCase(0.02f)]
    [TestCase(0.5f)]
    public void PhysicalMovementCannotPassThroughWallsEvenWithoutANavMesh(float deltaTime)
    {
        Component enemy = Find("RenatoEnemy");
        Call(enemy, "Awake");
        CharacterController body = enemy.GetComponent<CharacterController>();
        Assert.That(body, Is.Not.Null);
        Assert.That(body.isTrigger, Is.False);
        Assert.That(enemy.GetComponent<NavMeshAgent>().updatePosition, Is.False);
        body.enabled = false;
        enemy.transform.position = new Vector3(1000, 1000.05f, 1000);
        body.enabled = true;
        GameObject floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
        SceneManager.MoveGameObjectToScene(floor, level);
        floor.transform.position = new Vector3(1000, 999.9f, 1000);
        floor.transform.localScale = new Vector3(100, 0.2f, 100);
        GameObject wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
        SceneManager.MoveGameObjectToScene(wall, level);
        wall.transform.position = new Vector3(1000, 1001.5f, 1002);
        wall.transform.localScale = new Vector3(4, 3, 0.2f);
        Physics.SyncTransforms();
        MethodInfo move = enemy.GetType().GetMethod("MoveBody", BindingFlags.Instance | BindingFlags.NonPublic);
        int steps = Mathf.CeilToInt(2f / deltaTime);
        for (int i = 0; i < steps; i++) move.Invoke(enemy, new object[] { Vector3.forward * 6f, deltaTime });
        Assert.That(enemy.transform.position.z, Is.LessThan(1001.8f), "Solid wall must stop the capsule, including long frames.");
        wall.GetComponent<Collider>().isTrigger = true;
        Physics.SyncTransforms();
        for (int i = 0; i < steps; i++) move.Invoke(enemy, new object[] { Vector3.forward * 6f, deltaTime });
        Assert.That(enemy.transform.position.z, Is.GreaterThan(1002.5f), "Trigger volumes must remain passable.");
    }

    [Test]
    public void CatchLocksControlsAndPreventsEscapeOrRepeatedLoss()
    {
        Component flow = Find("Level3Flow");
        Component lose = Find("LoseSceneController");
        Call(Find("PlayerAudio"), "Awake");
        Call(flow, "Caught");
        GameObject canvas = (GameObject)Get(lose, "loseCanvas");
        Assert.That(canvas.activeSelf, Is.True);
        Assert.That(Time.timeScale, Is.Zero);
        Assert.That(((Behaviour)Get(flow, "player")).enabled, Is.False);
        Assert.That(((Behaviour)Get(flow, "renato")).enabled, Is.False);
        Assert.That(((Button)Get(flow, "pauseButton")).interactable, Is.False);
        canvas.SetActive(false);
        Call(flow, "Caught");
        Call(flow, "Escape");
        Assert.That(canvas.activeSelf, Is.False, "Repeated loss must not reactivate UI.");
        Assert.That(flow.GetType().GetProperty("Finished").GetValue(flow), Is.EqualTo(true));
    }
}
