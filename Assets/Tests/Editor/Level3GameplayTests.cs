// Purpose: checks Level 3 navigation, key pickup, audio and final outcomes.
// Use: run in Unity Test Runner > Edit Mode; this script is not attached to a game object.
// How it works: arrange a scene or objects, invoke behavior, assert results, then clean up.

// System supplies .NET types, delegates and attributes such as Serializable.
using System;
// System.Collections supplies IEnumerator so coroutines can pause and resume across frames.
using System.Collections;
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
// Unity SceneManagement supplies scene data, scene-loaded events and scene-loading calls.
using UnityEngine.SceneManagement;
// Unity UI supplies canvas widgets such as images, sliders and buttons.
using UnityEngine.UI;
// Unity Video supplies movie clips, playback events and the VideoPlayer component.
using UnityEngine.Video;

public class Level3GameplayTests
{
    // level: Unity scene opened by test setup and closed during teardown. Updated by SetUp.
    private Scene level;
    // timeScale: time scale. SetUp sets it from Unity gameplay clock multiplier (0 freezes gameplay time).
    private float timeScale;
    // cursorLock: cursor lock. SetUp sets it from Unity mouse-pointer capture mode.
    private CursorLockMode cursorLock;
    // cursorVisible: cursor visible. SetUp sets it from Unity mouse-pointer visibility.
    private bool cursorVisible;
    // Resolve a gameplay class by name from Unity Assembly-CSharp for reflection-based checks.
    // name comes from the test/helper caller; it supplies the type or field name requested.
    // Return the .NET type named name plus ", Assembly-CSharp" (including its assembly name) to the caller.
    private static Type Gameplay(string name) => Type.GetType(name + ", Assembly-CSharp", true);
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
    // Invoke the reflected method target.GetType() .GetMethod(method, BindingFlags.Instance | BindingFlags.Public
    // | BindingFlags.NonPublic) on target with arguments no reference (null).
    private static void Call(object target, string method) => target.GetType()
        .GetMethod(method, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic).Invoke(target, null);
    // Components is computed when read: return a new array copied from level.GetRootGameObjects().SelectMany(r =>
    // r.GetComponentsInChildren<Component>(true)).
    private Component[] Components => level.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<Component>(true)).ToArray();
    // Find the single component of the requested gameplay type in this test scene.
    // type comes from the test/helper caller; it supplies the gameplay type name to find.
    // Return the result of Components.Single using the callback/condition defined here to the caller.
    private Component Find(string type) => Components.Single(c => c != null && c.GetType() == Gameplay(type));

    // Before each test, prepare the required objects or scene and save shared state for cleanup.
    [SetUp]
    public void SetUp()
    {
        // Set timeScale to Unity gameplay clock multiplier (0 freezes gameplay time).
        timeScale = Time.timeScale;
        // Set cursorLock to Unity mouse-pointer capture mode.
        cursorLock = Cursor.lockState;
        // Set cursorVisible to Unity mouse-pointer visibility.
        cursorVisible = Cursor.visible;
        // Restore Unity gameplay clock multiplier to its normal rate so movement and scaled timers can advance.
        Time.timeScale = 1f;
        // Store the Editor scene opened from "Assets/Scenes/Level3.unity" with mode OpenSceneMode.Additive in
        // level.
        level = EditorSceneManager.OpenScene("Assets/Scenes/Level3.unity", OpenSceneMode.Additive);
    }

    // After each test, remove temporary objects or scenes and restore saved shared state.
    [TearDown]
    public void TearDown()
    {
        // If level is a valid initialized Unity handle/object, close Editor scene level and remove it from the
        // hierarchy when requested.
        if (level.IsValid()) EditorSceneManager.CloseScene(level, true);
        // Copy timeScale cached by this component into Time.timeScale.
        Time.timeScale = timeScale;
        // Copy cursorLock cached by this component into Cursor.lockState.
        Cursor.lockState = cursorLock;
        // Copy cursorVisible cached by this component into Cursor.visible.
        Cursor.visible = cursorVisible;
    }

    // Check that copied references and existing renato assets resolve.
    [Test]
    public void CopiedReferencesAndExistingRenatoAssetsResolve()
    {
        // Verify the result of Components.Count using c => c is Camera && c.gameObject.activeInHierarchy against
        // the result of Is.EqualTo using 1; fail the test if the assertion is not met.
        Assert.That(Components.Count(c => c is Camera && c.gameObject.activeInHierarchy), Is.EqualTo(1));
        // Verify the result of Components.Count using c => c is AudioListener && c.gameObject.activeInHierarchy
        // against the result of Is.EqualTo using 1; fail the test if the assertion is not met.
        Assert.That(Components.Count(c => c is AudioListener && c.gameObject.activeInHierarchy), Is.EqualTo(1));
        // Verify the result of Components.Count using c => c != null && c.GetType().Name == "EventSystem" against
        // the result of Is.EqualTo using 1; fail the test if the assertion is not met.
        Assert.That(Components.Count(c => c != null && c.GetType().Name == "EventSystem"), Is.EqualTo(1));
        // Store the result of Level3GameplayTests.Find using "PlayerMovement" in local player.
        Component player = Find("PlayerMovement");
        // Verify the result of Level3GameplayTests.Get using player, "flashlight" against Is.Not.Null; fail the
        // test if the assertion is not met.
        Assert.That(Get(player, "flashlight"), Is.Not.Null);
        // Verify the result of Level3GameplayTests.Get using player, "staminaSlider" against Is.Not.Null; fail the
        // test if the assertion is not met.
        Assert.That(Get(player, "staminaSlider"), Is.Not.Null);
        // Verify the result of Level3GameplayTests.Get using player, "hasFlashlight" against the result of
        // Is.EqualTo using true; fail the test if the assertion is not met.
        Assert.That(Get(player, "hasFlashlight"), Is.EqualTo(true));
        // Store the result of Level3GameplayTests.Find using "Inventory" in local inventory.
        Component inventory = Find("Inventory");
        // Verify the result of Level3GameplayTests.Get using inventory, "hotbarUI" against Is.Not.Null; fail the
        // test if the assertion is not met.
        Assert.That(Get(inventory, "hotbarUI"), Is.Not.Null);
        // Verify ((IList)Get(inventory, "items"))[0] (entry read from ((IList)Get(inventory, "items"))) against
        // the result of Is.EqualTo using Get(player, "flashlightItemData"); fail the test if the assertion is not
        // met.
        Assert.That(((IList)Get(inventory, "items"))[0], Is.EqualTo(Get(player, "flashlightItemData")));
        // Store the result of Level3GameplayTests.Find using "ObjectiveItem" in local objective.
        Component objective = Find("ObjectiveItem");
        // Verify the result of Level3GameplayTests.Get using objective, "objectiveText" against Is.Not.Null; fail
        // the test if the assertion is not met.
        Assert.That(Get(objective, "objectiveText"), Is.Not.Null);
        // Verify the result of Level3GameplayTests.Get using objective, "primaryObjective" against the result of
        // Is.EqualTo using "Find the key, then escape through the main door."; fail the test if the assertion is
        // not met.
        Assert.That(Get(objective, "primaryObjective"), Is.EqualTo("Find the key, then escape through the main door."));
        // Store the result of Level3GameplayTests.Find using "LoseSceneController" in local lose.
        Component lose = Find("LoseSceneController");
        // Verify the result of Level3GameplayTests.Get using lose, "retrySceneName" against the result of
        // Is.EqualTo using "Level3"; fail the test if the assertion is not met.
        Assert.That(Get(lose, "retrySceneName"), Is.EqualTo("Level3"));
        // Verify the result of Level3GameplayTests.Get using lose, "exitSceneName" against the result of
        // Is.EqualTo using "MainMenu"; fail the test if the assertion is not met.
        Assert.That(Get(lose, "exitSceneName"), Is.EqualTo("MainMenu"));
        // Verify the result of Level3GameplayTests.Get using lose, "tryAgainButton" against the result of
        // Is.TypeOf; fail the test if the assertion is not met.
        Assert.That(Get(lose, "tryAgainButton"), Is.TypeOf<Button>());
        // Verify the result of Level3GameplayTests.Get using lose, "exitButton" against the result of Is.TypeOf;
        // fail the test if the assertion is not met.
        Assert.That(Get(lose, "exitButton"), Is.TypeOf<Button>());
        // Verify the result of Level3GameplayTests.Get using Find("Level3Flow"), "pauseButton" against the result
        // of Is.TypeOf; fail the test if the assertion is not met.
        Assert.That(Get(Find("Level3Flow"), "pauseButton"), Is.TypeOf<Button>());
        // Store the result of Level3GameplayTests.Find using "RenatoAnimation" in local animation.
        Component animation = Find("RenatoAnimation");
        // Store the result of Level3GameplayTests.Get using animation, "animator" viewed as Animator in local
        // animator.
        Animator animator = (Animator)Get(animation, "animator");
        // Verify animator against Is.Not.Null; fail the test if the assertion is not met.
        Assert.That(animator, Is.Not.Null);
        // Verify animator.avatar.isValid and animator.avatar.isHuman against Is.True; fail the test if the
        // assertion is not met.
        Assert.That(animator.avatar.isValid && animator.avatar.isHuman, Is.True);
        // Process each field in a new array/object initialized with the entries specified here.
        foreach (string field in new[] { "walkingClip", "runningClip", "standClip", "lookClip" })
        {
            // Store the result of Level3GameplayTests.Get using animation, field viewed as AnimationClip in local
            // clip.
            AnimationClip clip = (AnimationClip)Get(animation, field);
            // Verify clip against Is.Not.Null, field; fail the test if the assertion is not met.
            Assert.That(clip, Is.Not.Null, field);
            // Verify whether clip is a Humanoid animation clip against Is.True, field; fail the test if the
            // assertion is not met.
            Assert.That(clip.isHumanMotion, Is.True, field);
            // Verify clip clip length in seconds against the result of Is.GreaterThan using 0f, field; fail the
            // test if the assertion is not met.
            Assert.That(clip.length, Is.GreaterThan(0f), field);
        }
    }

    // Check that escape uses the imported final ending and an enabled main menu.
    [Test]
    public void EscapeUsesTheImportedFinalEndingAndAnEnabledMainMenu()
    {
        // Store the result of Level3GameplayTests.Find using "Level3Flow" in local flow.
        Component flow = Find("Level3Flow");
        // Store the result of Level3GameplayTests.Get using flow, "endingClip" viewed as VideoClip in local clip.
        VideoClip clip = (VideoClip)Get(flow, "endingClip");
        // Verify clip against Is.Not.Null; fail the test if the assertion is not met.
        Assert.That(clip, Is.Not.Null);
        // Verify the result of AssetDatabase.GetAssetPath using clip against the result of Is.EqualTo using
        // "Assets/Assets/ENDING CLIP/final ending.mp4"; fail the test if the assertion is not met.
        Assert.That(AssetDatabase.GetAssetPath(clip), Is.EqualTo("Assets/Assets/ENDING CLIP/final ending.mp4"));
        // Verify clip.frameCount against the result of Is.GreaterThan using 0; fail the test if the assertion is
        // not met.
        Assert.That(clip.frameCount, Is.GreaterThan(0));
        // Verify clip.width against the result of Is.GreaterThan using 0; fail the test if the assertion is not
        // met.
        Assert.That(clip.width, Is.GreaterThan(0));
        // Verify the result of Level3GameplayTests.Get using flow, "endingAudioMixerGroup" against Is.Not.Null;
        // fail the test if the assertion is not met.
        Assert.That(Get(flow, "endingAudioMixerGroup"), Is.Not.Null);
        // Verify the result of EditorBuildSettings.scenes.Any using s => s.enabled && s.path ==
        // "Assets/Scenes/MainMenu.unity" against Is.True; fail the test if the assertion is not met.
        Assert.That(EditorBuildSettings.scenes.Any(s => s.enabled && s.path == "Assets/Scenes/MainMenu.unity"), Is.True);
    }

    // Check that runtime bake connects spawn patrol search rooms and exit.
    [Test]
    public void RuntimeBakeConnectsSpawnPatrolSearchRoomsAndExit()
    {
        // Store the result of Level3GameplayTests.Find using "GhostNavigation" in local navigation.
        Component navigation = Find("GhostNavigation");
        // Update Unity Physics with Transform changes before collision queries.
        Physics.SyncTransforms();
        // Call Level3GameplayTests.Call with navigation, "Awake"; invoke the named gameplay method through
        // reflection, including callbacks not run automatically in Edit Mode.
        Call(navigation, "Awake");
        // Store the first CharacterController component on the result of Level3GameplayTests.Find using
        // "PlayerMovement" in local player.
        CharacterController player = Find("PlayerMovement").GetComponent<CharacterController>();
        // Store player.bounds.center - Vector3.up * player.bounds.extents.y (calculated from the values shown) in
        // local feet.
        Vector3 feet = player.bounds.center - Vector3.up * player.bounds.extents.y;
        // Verify whether NavMesh finds walkable ground near feet; write the result through the out argument
        // against Is.True, "Dwight spawn"; fail the test if the assertion is not met.
        Assert.That(NavMesh.SamplePosition(feet, out NavMeshHit start, 0.75f, NavMesh.AllAreas), Is.True, "Dwight spawn");
        // Store the result of Level3GameplayTests.Find using "RenatoEnemy" in local enemy.
        Component enemy = Find("RenatoEnemy");
        // Call Level3GameplayTests.AssertReachable with world position of start, world position of
        // enemy.transform, "Renato spawn"; require a nearby walkable point and a complete navigation path to the
        // target.
        AssertReachable(start.position, enemy.transform.position, "Renato spawn");
        // Process each point in the result of Level3GameplayTests.Get using enemy, "patrolPoints" viewed as
        // Transform[].
        foreach (Transform point in (Transform[])Get(enemy, "patrolPoints")) AssertReachable(start.position, point.position, point.name);
        // Process each room in the result of Level3GameplayTests.Get using enemy, "rooms" viewed as IEnumerable.
        foreach (object room in (IEnumerable)Get(enemy, "rooms"))
        {
            // Store the result of Level3GameplayTests.Get using room, "searchPosition" viewed as Transform in
            // local point.
            Transform point = (Transform)Get(room, "searchPosition");
            // Call Level3GameplayTests.AssertReachable with world position of start, world position of point,
            // point.name; require a nearby walkable point and a complete navigation path to the target.
            AssertReachable(start.position, point.position, point.name);
            // Set point to the result of Level3GameplayTests.Get using room, "entrance" viewed as Transform.
            point = (Transform)Get(room, "entrance");
            // Call Level3GameplayTests.AssertReachable with world position of start, world position of point,
            // point.name; require a nearby walkable point and a complete navigation path to the target.
            AssertReachable(start.position, point.position, point.name);
        }
        // Store the result of Level3GameplayTests.Get using Find("Level3Flow"), "mainExit" viewed as BoxCollider
        // in local exit.
        BoxCollider exit = (BoxCollider)Get(Find("Level3Flow"), "mainExit");
        // Call Level3GameplayTests.AssertReachable with world position of start, a Vector3 built from
        // exit.bounds.center X component, 0.04, exit.bounds.center Z component, "Main door"; require a nearby
        // walkable point and a complete navigation path to the target.
        AssertReachable(start.position, new Vector3(exit.bounds.center.x, 0.04f, exit.bounds.center.z), "Main door");
        // Store Transform of Find("Level3Key") in local key.
        Transform key = Find("Level3Key").transform;
        // Verify whether NavMesh finds walkable ground near key.position; write the result through the out
        // argument against Is.True, "Key pickup floor"; fail the test if the assertion is not met.
        Assert.That(NavMesh.SamplePosition(key.position, out NavMeshHit keyFloor, 1.5f, NavMesh.AllAreas), Is.True, "Key pickup floor");
        // Call Level3GameplayTests.AssertReachable with world position of start, world position of keyFloor, "Key
        // pickup"; require a nearby walkable point and a complete navigation path to the target.
        AssertReachable(start.position, keyFloor.position, "Key pickup");
    }

    // Require a nearby walkable point and a complete navigation path to the target.
    // from comes from the test/helper caller; it supplies the from used below.
    // to comes from the test/helper caller; it supplies the to used below.
    // label comes from the test/helper caller; it supplies the label used below.
    private static void AssertReachable(Vector3 from, Vector3 to, string label)
    {
        // Verify whether NavMesh finds walkable ground near to; write the result through the out argument against
        // Is.True, label; fail the test if the assertion is not met.
        Assert.That(NavMesh.SamplePosition(to, out NavMeshHit hit, 0.75f, NavMesh.AllAreas), Is.True, label);
        // Store a new NavMeshPath instance in local path.
        NavMeshPath path = new NavMeshPath();
        // Verify whether NavMesh found a navigation route using the supplied target and path object against
        // Is.True, label; fail the test if the assertion is not met.
        Assert.That(NavMesh.CalculatePath(from, hit.position, NavMesh.AllAreas, path), Is.True, label);
        // Verify path.status against the result of Is.EqualTo using NavMeshPathStatus.PathComplete, label; fail
        // the test if the assertion is not met.
        Assert.That(path.status, Is.EqualTo(NavMeshPathStatus.PathComplete), label);
    }

    // Check that solid walls block sight but triggers do not.
    // wallCount comes from NUnit TestCase arguments and selects this test input.
    [TestCase(1)]
    [TestCase(40)]
    public void SolidWallsBlockSightButTriggersDoNot(int wallCount)
    {
        // Store the result of Level3GameplayTests.Find using "RenatoEnemy" in local enemy.
        Component enemy = Find("RenatoEnemy");
        // Store the result of Level3GameplayTests.Get using enemy, "player" viewed as Transform in local player.
        Transform player = (Transform)Get(enemy, "player");
        // Set enemy.transform.position to a Vector3 built from 1000, 1000, 1000.
        enemy.transform.position = new Vector3(1000, 1000, 1000);
        // Set player.position to enemy.transform.position + new Vector3(0, 1.5f, 4) (calculated from the values
        // shown).
        player.position = enemy.transform.position + new Vector3(0, 1.5f, 4);
        // Store a new array/object initialized with the entries specified here in local walls.
        Collider[] walls = new Collider[wallCount];
        // Repeat this block while i is below wallCount; start from int i = 0.
        for (int i = 0; i < wallCount; i++)
        {
            // Store the result of GameObject.CreatePrimitive using PrimitiveType.Cube in local wall.
            GameObject wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            // Call SceneManager.MoveGameObjectToScene using wall, level.
            SceneManager.MoveGameObjectToScene(wall, level);
            // Set wall.transform.position to enemy.transform.position + new Vector3(0, 1.5f, 1f + i * 0.05f)
            // (calculated from the values shown).
            wall.transform.position = enemy.transform.position + new Vector3(0, 1.5f, 1f + i * 0.05f);
            // Set wall.transform.localScale to a Vector3 built from 3, 3, 0.02.
            wall.transform.localScale = new Vector3(3, 3, 0.02f);
            // Store the first Collider component on wall in walls[i].
            walls[i] = wall.GetComponent<Collider>();
        }
        // Update Unity Physics with Transform changes before collision queries.
        Physics.SyncTransforms();
        // Store reflection metadata for method "HasClearSight" on enemy.GetType() in local sight.
        MethodInfo sight = enemy.GetType().GetMethod("HasClearSight");
        // Verify Invoke the reflected method sight on enemy with arguments null. against the result of Is.EqualTo
        // using false; fail the test if the assertion is not met.
        Assert.That(sight.Invoke(enemy, null), Is.EqualTo(false));
        // Process each wall in walls.
        foreach (Collider wall in walls) wall.isTrigger = true;
        // Update Unity Physics with Transform changes before collision queries.
        Physics.SyncTransforms();
        // Verify Invoke the reflected method sight on enemy with arguments null. against the result of Is.EqualTo
        // using true; fail the test if the assertion is not met.
        Assert.That(sight.Invoke(enemy, null), Is.EqualTo(true));
    }

    // Check that renato audio crossfades with pursuit and fades out after catch.
    [Test]
    public void RenatoAudioCrossfadesWithPursuitAndFadesOutAfterCatch()
    {
        // Store the result of Level3GameplayTests.Find using "RenatoAudio" in local audio.
        Component audio = Find("RenatoAudio");
        // Store the result of Level3GameplayTests.Find using "RenatoEnemy" in local enemy.
        Component enemy = Find("RenatoEnemy");
        // Verify the result of Level3GameplayTests.Get using audio, "whistleClip" against Is.Not.Null; fail the
        // test if the assertion is not met.
        Assert.That(Get(audio, "whistleClip"), Is.Not.Null);
        // Verify the result of Level3GameplayTests.Get using audio, "chaseClip" against Is.Not.Null; fail the test
        // if the assertion is not met.
        Assert.That(Get(audio, "chaseClip"), Is.Not.Null);
        // Call Level3GameplayTests.Call with audio, "Awake"; invoke the named gameplay method through reflection,
        // including callbacks not run automatically in Edit Mode.
        Call(audio, "Awake");
        // Store the result of Level3GameplayTests.Get using audio, "whistleSource" viewed as AudioSource in local
        // whistle.
        AudioSource whistle = (AudioSource)Get(audio, "whistleSource");
        // Store the result of Level3GameplayTests.Get using audio, "chaseSource" viewed as AudioSource in local
        // chase.
        AudioSource chase = (AudioSource)Get(audio, "chaseSource");
        // Store the result of Level3GameplayTests.Get using audio, "ambienceSource" viewed as AudioSource in local
        // ambience.
        AudioSource ambience = (AudioSource)Get(audio, "ambienceSource");
        // Verify whistle against the result of Is.Not.SameAs using chase; fail the test if the assertion is not
        // met.
        Assert.That(whistle, Is.Not.SameAs(chase));
        // Verify whistle.spatialBlend against the result of Is.EqualTo using 1f; fail the test if the assertion is
        // not met.
        Assert.That(whistle.spatialBlend, Is.EqualTo(1f));
        // Verify chase.spatialBlend against Is.Zero; fail the test if the assertion is not met.
        Assert.That(chase.spatialBlend, Is.Zero);
        // Verify whistle.outputAudioMixerGroup.name against the result of Is.EqualTo using "SFXVolume"; fail the
        // test if the assertion is not met.
        Assert.That(whistle.outputAudioMixerGroup.name, Is.EqualTo("SFXVolume"));
        // Verify chase.outputAudioMixerGroup.name against the result of Is.EqualTo using "MusicVolume"; fail the
        // test if the assertion is not met.
        Assert.That(chase.outputAudioMixerGroup.name, Is.EqualTo("MusicVolume"));
        // Store reflection metadata for method "UpdateMix" on audio.GetType() in local mix.
        MethodInfo mix = audio.GetType().GetMethod("UpdateMix", BindingFlags.Instance | BindingFlags.NonPublic);
        // Store the callback/condition defined here in local tick.
        Action<float> tick = dt => mix.Invoke(audio, new object[] { dt });
        // Store the result of Level3GameplayTests.Find using "RenatoAnimation" in local animation.
        Component animation = Find("RenatoAnimation");
        // Store reflection metadata for property "IsWalking" on animation.GetType() in local walking.
        PropertyInfo walking = animation.GetType().GetProperty("IsWalking");
        // Call Level3GameplayTests.tick using 2.
        tick(2f);
        // Verify whistle.volume against Is.Zero, "Stationary Renato must not whistle."; fail the test if the
        // assertion is not met.
        Assert.That(whistle.volume, Is.Zero, "Stationary Renato must not whistle.");
        // Invoke the reflected method walking.GetSetMethod(true) on animation with arguments a new array/object
        // initialized with the entries specified here.
        walking.GetSetMethod(true).Invoke(animation, new object[] { true });
        // Call Level3GameplayTests.tick using 2.
        tick(2f);
        // Verify whistle.volume against the result of Is.EqualTo(0.7f).Within using 0.001f; fail the test if the
        // assertion is not met.
        Assert.That(whistle.volume, Is.EqualTo(0.7f).Within(0.001f));
        // Verify chase.volume against Is.Zero; fail the test if the assertion is not met.
        Assert.That(chase.volume, Is.Zero);
        // Store reflection metadata for field "<State>k__BackingField" on enemy.GetType() in local state.
        FieldInfo state = enemy.GetType().GetField("<State>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic);
        // Write the result of Enum.Parse using state.FieldType, "Chase" through reflection on enemy.
        state.SetValue(enemy, Enum.Parse(state.FieldType, "Chase"));
        // Call Level3GameplayTests.tick using 1.
        tick(1f);
        // Verify whistle.volume against the result of Is.InRange using 0.01f, 0.69f; fail the test if the
        // assertion is not met.
        Assert.That(whistle.volume, Is.InRange(0.01f, 0.69f));
        // Verify chase.volume against the result of Is.InRange using 0.01f, 0.64f; fail the test if the assertion
        // is not met.
        Assert.That(chase.volume, Is.InRange(0.01f, 0.64f));
        // Call Level3GameplayTests.tick using 1.
        tick(1f);
        // Verify chase.volume against the result of Is.EqualTo(0.65f).Within using 0.001f; fail the test if the
        // assertion is not met.
        Assert.That(chase.volume, Is.EqualTo(0.65f).Within(0.001f));
        // Verify whistle.volume against Is.Zero; fail the test if the assertion is not met.
        Assert.That(whistle.volume, Is.Zero);
        // Verify ambience.volume against the result of Is.EqualTo(0.04f).Within using 0.001f; fail the test if the
        // assertion is not met.
        Assert.That(ambience.volume, Is.EqualTo(0.04f).Within(0.001f));
        // Write the result of Enum.Parse using state.FieldType, "Investigate" through reflection on enemy.
        state.SetValue(enemy, Enum.Parse(state.FieldType, "Investigate"));
        // Call Level3GameplayTests.tick using 1.
        tick(1f);
        // Verify chase.volume against the result of Is.InRange using 0.01f, 0.64f, "Losing pursuit fades rather
        // than cuts the music."; fail the test if the assertion is not met.
        Assert.That(chase.volume, Is.InRange(0.01f, 0.64f), "Losing pursuit fades rather than cuts the music.");
        // Call Level3GameplayTests.tick using 1.
        tick(1f);
        // Verify chase.volume against Is.Zero; fail the test if the assertion is not met.
        Assert.That(chase.volume, Is.Zero);
        // Verify whistle.volume against the result of Is.EqualTo(0.7f).Within using 0.001f; fail the test if the
        // assertion is not met.
        Assert.That(whistle.volume, Is.EqualTo(0.7f).Within(0.001f));
        // Invoke the reflected method walking.GetSetMethod(true) on animation with arguments a new array/object
        // initialized with the entries specified here.
        walking.GetSetMethod(true).Invoke(animation, new object[] { false });
        // Process each roomState in a new array/object initialized with the entries specified here.
        foreach (string roomState in new[] { "Crouch", "Search", "Stand" })
        {
            // Write the result of Enum.Parse using state.FieldType, roomState through reflection on enemy.
            state.SetValue(enemy, Enum.Parse(state.FieldType, roomState));
            // Call Level3GameplayTests.tick using 2.
            tick(2f);
            // Verify whistle.volume against Is.Zero, roomState; fail the test if the assertion is not met.
            Assert.That(whistle.volume, Is.Zero, roomState);
        }
        // Call Level3GameplayTests.Call with the result of Level3GameplayTests.Find using "PlayerAudio", "Awake";
        // invoke the named gameplay method through reflection, including callbacks not run automatically in Edit
        // Mode.
        Call(Find("PlayerAudio"), "Awake");
        // Call Level3GameplayTests.Call with the result of Level3GameplayTests.Find using "Level3Flow", "Caught";
        // invoke the named gameplay method through reflection, including callbacks not run automatically in Edit
        // Mode.
        Call(Find("Level3Flow"), "Caught");
        // Verify Unity gameplay clock multiplier (0 freezes gameplay time) against Is.Zero; fail the test if the
        // assertion is not met.
        Assert.That(Time.timeScale, Is.Zero);
        // Call Level3GameplayTests.tick using 2.
        tick(2f);
        // Verify whistle.volume against Is.Zero; fail the test if the assertion is not met.
        Assert.That(whistle.volume, Is.Zero);
        // Verify chase.volume against Is.Zero; fail the test if the assertion is not met.
        Assert.That(chase.volume, Is.Zero);
        // Verify ambience.volume against Is.Zero; fail the test if the assertion is not met.
        Assert.That(ambience.volume, Is.Zero);
    }

    // Check that physical movement cannot pass through walls even without anav mesh.
    // deltaTime comes from NUnit TestCase arguments and selects this test input.
    [TestCase(0.02f)]
    [TestCase(0.5f)]
    public void PhysicalMovementCannotPassThroughWallsEvenWithoutANavMesh(float deltaTime)
    {
        // Store the result of Level3GameplayTests.Find using "RenatoEnemy" in local enemy.
        Component enemy = Find("RenatoEnemy");
        // Call Level3GameplayTests.Call with enemy, "Awake"; invoke the named gameplay method through reflection,
        // including callbacks not run automatically in Edit Mode.
        Call(enemy, "Awake");
        // Store the first CharacterController component on enemy in local body.
        CharacterController body = enemy.GetComponent<CharacterController>();
        // Verify body against Is.Not.Null; fail the test if the assertion is not met.
        Assert.That(body, Is.Not.Null);
        // Verify body.isTrigger against Is.False; fail the test if the assertion is not met.
        Assert.That(body.isTrigger, Is.False);
        // Verify enemy.GetComponent<NavMeshAgent>().updatePosition against Is.False; fail the test if the
        // assertion is not met.
        Assert.That(enemy.GetComponent<NavMeshAgent>().updatePosition, Is.False);
        // Disable body by changing its component enabled flag.
        body.enabled = false;
        // Set enemy.transform.position to a Vector3 built from 1000, 1000.05, 1000.
        enemy.transform.position = new Vector3(1000, 1000.05f, 1000);
        // Enable body by changing its component enabled flag.
        body.enabled = true;
        // Store the result of GameObject.CreatePrimitive using PrimitiveType.Cube in local floor.
        GameObject floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
        // Call SceneManager.MoveGameObjectToScene using floor, level.
        SceneManager.MoveGameObjectToScene(floor, level);
        // Set floor.transform.position to a Vector3 built from 1000, 999.9, 1000.
        floor.transform.position = new Vector3(1000, 999.9f, 1000);
        // Set floor.transform.localScale to a Vector3 built from 100, 0.2, 100.
        floor.transform.localScale = new Vector3(100, 0.2f, 100);
        // Store the result of GameObject.CreatePrimitive using PrimitiveType.Cube in local wall.
        GameObject wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
        // Call SceneManager.MoveGameObjectToScene using wall, level.
        SceneManager.MoveGameObjectToScene(wall, level);
        // Set wall.transform.position to a Vector3 built from 1000, 1001.5, 1002.
        wall.transform.position = new Vector3(1000, 1001.5f, 1002);
        // Set wall.transform.localScale to a Vector3 built from 4, 3, 0.2.
        wall.transform.localScale = new Vector3(4, 3, 0.2f);
        // Update Unity Physics with Transform changes before collision queries.
        Physics.SyncTransforms();
        // Store reflection metadata for method "MoveBody" on enemy.GetType() in local move.
        MethodInfo move = enemy.GetType().GetMethod("MoveBody", BindingFlags.Instance | BindingFlags.NonPublic);
        // Store the result of Mathf.CeilToInt using 2f divided by deltaTime in local steps.
        int steps = Mathf.CeilToInt(2f / deltaTime);
        // Repeat this block while i is below steps; start from int i = 0.
        for (int i = 0; i < steps; i++) move.Invoke(enemy, new object[] { Vector3.forward * 6f, deltaTime });
        // Verify enemy.transform.position Z component against the result of Is.LessThan using 1001.8f, "Solid wall
        // must stop the capsule, including long frames."; fail the test if the assertion is not met.
        Assert.That(enemy.transform.position.z, Is.LessThan(1001.8f), "Solid wall must stop the capsule, including long frames.");
        // Set wall.GetComponent<Collider>().isTrigger to true.
        wall.GetComponent<Collider>().isTrigger = true;
        // Update Unity Physics with Transform changes before collision queries.
        Physics.SyncTransforms();
        // Repeat this block while i is below steps; start from int i = 0.
        for (int i = 0; i < steps; i++) move.Invoke(enemy, new object[] { Vector3.forward * 6f, deltaTime });
        // Verify enemy.transform.position Z component against the result of Is.GreaterThan using 1002.5f, "Trigger
        // volumes must remain passable."; fail the test if the assertion is not met.
        Assert.That(enemy.transform.position.z, Is.GreaterThan(1002.5f), "Trigger volumes must remain passable.");
    }

    // Check that key pickup has araycast target and unlocks only for dwight.
    [Test]
    public void KeyPickupHasARaycastTargetAndUnlocksOnlyForDwight()
    {
        // Store the result of Level3GameplayTests.Find using "Level3Key" in local key.
        Component key = Find("Level3Key");
        // Store the result of Level3GameplayTests.Find using "Level3Flow" in local flow.
        Component flow = Find("Level3Flow");
        // Verify the result of Level3GameplayTests.Get using key, "flow" against the result of Is.SameAs using
        // flow; fail the test if the assertion is not met.
        Assert.That(Get(key, "flow"), Is.SameAs(flow));
        // Call Level3GameplayTests.Call with key, "Awake"; invoke the named gameplay method through reflection,
        // including callbacks not run automatically in Edit Mode.
        Call(key, "Awake");
        // Store the first BoxCollider component on key in local pickup.
        BoxCollider pickup = key.GetComponent<BoxCollider>();
        // Verify pickup.isTrigger against Is.True; fail the test if the assertion is not met.
        Assert.That(pickup.isTrigger, Is.True);
        // Verify pickup.size X component against the result of Is.GreaterThan using 0f; fail the test if the
        // assertion is not met.
        Assert.That(pickup.size.x, Is.GreaterThan(0f));
        // Update Unity Physics with Transform changes before collision queries.
        Physics.SyncTransforms();
        // Store pickup world bounding box in local bounds.
        Bounds bounds = pickup.bounds;
        // Verify the ray hits a collider within the specified range/layers; store collider, point and distance in
        // _ against Is.True; fail the test if the assertion is not met.
        Assert.That(pickup.Raycast(new Ray(bounds.center + Vector3.up * 2f, Vector3.down), out _, 3f), Is.True);
        // Call Level3GameplayTests.Call with flow, "Escape"; invoke the named gameplay method through reflection,
        // including callbacks not run automatically in Edit Mode.
        Call(flow, "Escape");
        // Verify value read through reflection from flow against the result of Is.EqualTo using false; fail the
        // test if the assertion is not met.
        Assert.That(flow.GetType().GetProperty("Finished").GetValue(flow), Is.EqualTo(false));
        // Verify Unity gameplay clock multiplier (0 freezes gameplay time) against the result of Is.EqualTo using
        // 1f; fail the test if the assertion is not met.
        Assert.That(Time.timeScale, Is.EqualTo(1f));
        // The overlap fallback must also leave controls active without the key.
        // Store the result of Level3GameplayTests.Find using "PlayerMovement" in local player.
        Component player = Find("PlayerMovement");
        // Set player.transform.position to ((BoxCollider)Get(flow, "mainExit")).bounds center.
        player.transform.position = ((BoxCollider)Get(flow, "mainExit")).bounds.center;
        // Update Unity Physics with Transform changes before collision queries.
        Physics.SyncTransforms();
        // Call Level3GameplayTests.Call with flow, "LateUpdate"; invoke the named gameplay method through
        // reflection, including callbacks not run automatically in Edit Mode.
        Call(flow, "LateUpdate");
        // Verify value read through reflection from flow against the result of Is.EqualTo using false; fail the
        // test if the assertion is not met.
        Assert.That(flow.GetType().GetProperty("Finished").GetValue(flow), Is.EqualTo(false));
        // Store reflection metadata for method "CollectKey" on flow.GetType() in local collect.
        MethodInfo collect = flow.GetType().GetMethod("CollectKey");
        // Verify Invoke the reflected method collect on flow with arguments new object[] { null }. against the
        // result of Is.EqualTo using false; fail the test if the assertion is not met.
        Assert.That(collect.Invoke(flow, new object[] { null }), Is.EqualTo(false));
        // Store a new GameObject named "Other player" in local stranger.
        GameObject stranger = new GameObject("Other player");
        // Call SceneManager.MoveGameObjectToScene using stranger, level.
        SceneManager.MoveGameObjectToScene(stranger, level);
        // Store a the result of Level3GameplayTests.Gameplay using "PlayerMovement" component attached to stranger
        // in local otherPlayer.
        Component otherPlayer = stranger.AddComponent(Gameplay("PlayerMovement"));
        // Verify Invoke the reflected method collect on flow with arguments new object[] { otherPlayer }. against
        // the result of Is.EqualTo using false; fail the test if the assertion is not met.
        Assert.That(collect.Invoke(flow, new object[] { otherPlayer }), Is.EqualTo(false));
        // Set Unity gameplay clock multiplier to zero; scaled movement/timers stop, while code using real time can
        // continue.
        Time.timeScale = 0f;
        // Verify Invoke the reflected method collect on flow with arguments new object[] { player }. against the
        // result of Is.EqualTo using false; fail the test if the assertion is not met.
        Assert.That(collect.Invoke(flow, new object[] { player }), Is.EqualTo(false));
        // Restore Unity gameplay clock multiplier to its normal rate so movement and scaled timers can advance.
        Time.timeScale = 1f;
        // Store the result of Level3GameplayTests.Find using "Inventory" in local inventory.
        Component inventory = Find("Inventory");
        // Store the result of Level3GameplayTests.Get using inventory, "items" viewed as IList in local items.
        IList items = (IList)Get(inventory, "items");
        // Store items[0] (entry read from items) in local flashlight.
        object flashlight = items[0];
        // Store the result of Level3GameplayTests.Get using flow, "keyItem" in local keyItem.
        object keyItem = Get(flow, "keyItem");
        // Store the result of Level3GameplayTests.Get using keyItem, "itemIcon" viewed as Sprite in local keyIcon.
        Sprite keyIcon = (Sprite)Get(keyItem, "itemIcon");
        // Verify keyIcon against Is.Not.Null; fail the test if the assertion is not met.
        Assert.That(keyIcon, Is.Not.Null);
        // Verify the result of AssetDatabase.GetAssetPath using keyIcon against the result of Is.EqualTo using
        // "Assets/Scenes/Buttons-Icons/key icon.png"; fail the test if the assertion is not met.
        Assert.That(AssetDatabase.GetAssetPath(keyIcon), Is.EqualTo("Assets/Scenes/Buttons-Icons/key icon.png"));
        // Verify number of entries in items against the result of Is.EqualTo using 1, "The key must not appear
        // before collection or after retry."; fail the test if the assertion is not met.
        Assert.That(items.Count, Is.EqualTo(1), "The key must not appear before collection or after retry.");
        // Store the result of Level3GameplayTests.Get using inventory, "selectedIndex" viewed as int in local
        // selected.
        int selected = (int)Get(inventory, "selectedIndex");
        // A rejected inventory addition must not grant escape or remove the pickup.
        // Store the result of Level3GameplayTests.Get using inventory, "maxSlots" viewed as int in local capacity.
        int capacity = (int)Get(inventory, "maxSlots");
        // Call Level3GameplayTests.Set with inventory, "maxSlots", number of entries in items; assign a named
        // field through reflection to arrange the test scenario.
        Set(inventory, "maxSlots", items.Count);
        // Verify Invoke the reflected method collect on flow with arguments new object[] { player }. against the
        // result of Is.EqualTo using false; fail the test if the assertion is not met.
        Assert.That(collect.Invoke(flow, new object[] { player }), Is.EqualTo(false));
        // Verify value read through reflection from flow against the result of Is.EqualTo using false; fail the
        // test if the assertion is not met.
        Assert.That(flow.GetType().GetProperty("HasKey").GetValue(flow), Is.EqualTo(false));
        // Verify key.gameObject own activation flag against Is.True; fail the test if the assertion is not met.
        Assert.That(key.gameObject.activeSelf, Is.True);
        // Verify the result of Level3GameplayTests.Get using Find("ObjectiveItem"), "primaryObjective" against the
        // result of Is.EqualTo using "Find the key, then escape through the main door."; fail the test if the
        // assertion is not met.
        Assert.That(Get(Find("ObjectiveItem"), "primaryObjective"), Is.EqualTo("Find the key, then escape through the main door."));
        // Call Level3GameplayTests.Set with inventory, "maxSlots", capacity; assign a named field through
        // reflection to arrange the test scenario.
        Set(inventory, "maxSlots", capacity);
        // Verify Invoke the reflected method collect on flow with arguments new object[] { player }. against the
        // result of Is.EqualTo using true; fail the test if the assertion is not met.
        Assert.That(collect.Invoke(flow, new object[] { player }), Is.EqualTo(true));
        // Verify number of entries in items against the result of Is.EqualTo using 2; fail the test if the
        // assertion is not met.
        Assert.That(items.Count, Is.EqualTo(2));
        // Verify items[0] (entry read from items) against the result of Is.SameAs using flashlight; fail the test
        // if the assertion is not met.
        Assert.That(items[0], Is.SameAs(flashlight));
        // Verify items[1] (entry read from items) against the result of Is.SameAs using keyItem; fail the test if
        // the assertion is not met.
        Assert.That(items[1], Is.SameAs(keyItem));
        // Verify the result of Level3GameplayTests.Get using inventory, "selectedIndex" against the result of
        // Is.EqualTo using selected; fail the test if the assertion is not met.
        Assert.That(Get(inventory, "selectedIndex"), Is.EqualTo(selected));
        // Store the result of Level3GameplayTests.Get using Get(inventory, "hotbarUI"), "slotIcons" viewed as
        // IList in local icons.
        IList icons = (IList)Get(Get(inventory, "hotbarUI"), "slotIcons");
        // Verify enabled state of ((Image)icons[1]) against Is.True; fail the test if the assertion is not met.
        Assert.That(((Image)icons[1]).enabled, Is.True);
        // Verify ((Image)icons[1]).sprite against the result of Is.SameAs using keyIcon; fail the test if the
        // assertion is not met.
        Assert.That(((Image)icons[1]).sprite, Is.SameAs(keyIcon));
        // Verify Invoke the reflected method collect on flow with arguments new object[] { player }. against the
        // result of Is.EqualTo using false; fail the test if the assertion is not met.
        Assert.That(collect.Invoke(flow, new object[] { player }), Is.EqualTo(false));
        // Verify value read through reflection from flow against the result of Is.EqualTo using true; fail the
        // test if the assertion is not met.
        Assert.That(flow.GetType().GetProperty("HasKey").GetValue(flow), Is.EqualTo(true));
        // Verify number of entries in items against the result of Is.EqualTo using 2, "Repeated collection must
        // not duplicate the key."; fail the test if the assertion is not met.
        Assert.That(items.Count, Is.EqualTo(2), "Repeated collection must not duplicate the key.");
        // Verify the result of Level3GameplayTests.Get using Find("ObjectiveItem"), "primaryObjective" against the
        // result of Is.EqualTo using "Escape through the main door."; fail the test if the assertion is not met.
        Assert.That(Get(Find("ObjectiveItem"), "primaryObjective"), Is.EqualTo("Escape through the main door."));
    }

    // Check that level3 sprint boost uses the stamina boundary.
    // stamina comes from NUnit TestCase arguments and selects this test input.
    // speed comes from NUnit TestCase arguments and selects this test input.
    [TestCase(100f, 5f)]
    [TestCase(45f, 5f)]
    [TestCase(44.99f, 5.5f)]
    [TestCase(1f, 5.5f)]
    public void Level3SprintBoostUsesTheStaminaBoundary(float stamina, float speed)
    {
        // Store the result of Level3GameplayTests.Find using "PlayerMovement" in local player.
        Component player = Find("PlayerMovement");
        // Call Level3GameplayTests.Set with player, "currentStamina", stamina; assign a named field through
        // reflection to arrange the test scenario.
        Set(player, "currentStamina", stamina);
        // Store reflection metadata for method "GetMovementSpeed" on player.GetType() in local movement.
        MethodInfo movement = player.GetType().GetMethod("GetMovementSpeed");
        // Verify Invoke the reflected method movement on player with arguments new object[] { true }. against the
        // result of Is.EqualTo using speed; fail the test if the assertion is not met.
        Assert.That(movement.Invoke(player, new object[] { true }), Is.EqualTo(speed));
        // Verify Invoke the reflected method movement on player with arguments new object[] { false }. against the
        // result of Is.EqualTo using 3f; fail the test if the assertion is not met.
        Assert.That(movement.Invoke(player, new object[] { false }), Is.EqualTo(3f));
        // Call Level3GameplayTests.Set with player, "useLowStaminaSprintBoost", false; assign a named field
        // through reflection to arrange the test scenario.
        Set(player, "useLowStaminaSprintBoost", false);
        // Verify Invoke the reflected method movement on player with arguments new object[] { true }. against the
        // result of Is.EqualTo using 5f, "Other scenes retain their configured sprint speed."; fail the test if
        // the assertion is not met.
        Assert.That(movement.Invoke(player, new object[] { true }), Is.EqualTo(5f), "Other scenes retain their configured sprint speed.");
    }

    // Check that regaining focus cannot lock the cursor when controls are disabled or frozen.
    // enabled comes from NUnit TestCase arguments and selects this test input.
    // scale comes from NUnit TestCase arguments and selects this test input.
    [TestCase(false, 1f)]
    [TestCase(true, 0f)]
    public void RegainingFocusCannotLockTheCursorWhenControlsAreDisabledOrFrozen(bool enabled, float scale)
    {
        // Store the result of Level3GameplayTests.Find using "MouseLook" viewed as Behaviour in local look.
        Behaviour look = (Behaviour)Find("MouseLook");
        // Copy enabled (argument from the caller) into look.enabled.
        look.enabled = enabled;
        // Copy scale (argument from the caller) into Time.timeScale.
        Time.timeScale = scale;
        // Release Unity pointer capture so menu buttons can be clicked.
        Cursor.lockState = CursorLockMode.None;
        // Show Unity mouse pointer while UI needs clicks.
        Cursor.visible = true;
        // Invoke the reflected method look.GetType().GetMethod("OnApplicationFocus", BindingFlags.Instance |
        // BindingFlags.NonPublic) on look with arguments a new array/object initialized with the entries specified
        // here.
        look.GetType().GetMethod("OnApplicationFocus", BindingFlags.Instance | BindingFlags.NonPublic)
            .Invoke(look, new object[] { true });
        // Verify Unity mouse-pointer capture mode against the result of Is.EqualTo using CursorLockMode.None; fail
        // the test if the assertion is not met.
        Assert.That(Cursor.lockState, Is.EqualTo(CursorLockMode.None));
        // Verify Unity mouse-pointer visibility against Is.True; fail the test if the assertion is not met.
        Assert.That(Cursor.visible, Is.True);
    }

    // Check that catch locks controls and prevents escape or repeated loss.
    [Test]
    public void CatchLocksControlsAndPreventsEscapeOrRepeatedLoss()
    {
        // Store the result of Level3GameplayTests.Find using "Level3Flow" in local flow.
        Component flow = Find("Level3Flow");
        // Store the result of Level3GameplayTests.Find using "LoseSceneController" in local lose.
        Component lose = Find("LoseSceneController");
        // Store the result of Level3GameplayTests.Get using lose, "gameplayUIToHide" viewed as GameObject[] in
        // local gameplayUI.
        GameObject[] gameplayUI = (GameObject[])Get(lose, "gameplayUIToHide");
        // Verify gameplayUI against the result of Has.Length.EqualTo using 6; fail the test if the assertion is
        // not met.
        Assert.That(gameplayUI, Has.Length.EqualTo(6));
        // Process each ui in gameplayUI.
        foreach (GameObject ui in gameplayUI) Assert.That(ui.activeInHierarchy, Is.True);
        // Store the result of Level3GameplayTests.Get using flow, "caughtCamera" viewed as Camera in local camera.
        Camera camera = (Camera)Get(flow, "caughtCamera");
        // Verify the result of camera.transform.Find using "Caught face spotlight" against Is.Null; fail the test
        // if the assertion is not met.
        Assert.That(camera.transform.Find("Caught face spotlight"), Is.Null);
        // Call Level3GameplayTests.Call with the result of Level3GameplayTests.Find using "PlayerAudio", "Awake";
        // invoke the named gameplay method through reflection, including callbacks not run automatically in Edit
        // Mode.
        Call(Find("PlayerAudio"), "Awake");
        // Call Level3GameplayTests.Call with flow, "Caught"; invoke the named gameplay method through reflection,
        // including callbacks not run automatically in Edit Mode.
        Call(flow, "Caught");
        // Store the result of Level3GameplayTests.Get using lose, "loseCanvas" viewed as GameObject in local
        // canvas.
        GameObject canvas = (GameObject)Get(lose, "loseCanvas");
        // Verify whether canvas and its parent objects are active against Is.True; fail the test if the assertion
        // is not met.
        Assert.That(canvas.activeInHierarchy, Is.True);
        // Process each ui in gameplayUI.
        foreach (GameObject ui in gameplayUI) Assert.That(ui.activeInHierarchy, Is.False, ui.name);
        // Verify the result of ((Button)Get(lose, "tryAgainButton")).IsInteractable against Is.True; fail the test
        // if the assertion is not met.
        Assert.That(((Button)Get(lose, "tryAgainButton")).IsInteractable(), Is.True);
        // Verify the result of ((Button)Get(lose, "exitButton")).IsInteractable against Is.True; fail the test if
        // the assertion is not met.
        Assert.That(((Button)Get(lose, "exitButton")).IsInteractable(), Is.True);
        // Verify Unity gameplay clock multiplier (0 freezes gameplay time) against Is.Zero; fail the test if the
        // assertion is not met.
        Assert.That(Time.timeScale, Is.Zero);
        // Verify enabled state of ((Behaviour)Get(flow, "player")) against Is.False; fail the test if the
        // assertion is not met.
        Assert.That(((Behaviour)Get(flow, "player")).enabled, Is.False);
        // Verify enabled state of ((Behaviour)Get(flow, "renato")) against Is.False; fail the test if the
        // assertion is not met.
        Assert.That(((Behaviour)Get(flow, "renato")).enabled, Is.False);
        // Verify ((Button)Get(flow, "pauseButton")).interactable against Is.False; fail the test if the assertion
        // is not met.
        Assert.That(((Button)Get(flow, "pauseButton")).interactable, Is.False);
        // Store the first Animator component on or beneath ((Component)Get(flow, "renato")) in local animator.
        Animator animator = ((Component)Get(flow, "renato")).GetComponentInChildren<Animator>();
        // Store world position of animator.GetBoneTransform(HumanBodyBones.Head) in local face.
        Vector3 face = animator.GetBoneTransform(HumanBodyBones.Head).position;
        // Verify the distance between camera.transform.position and face against the result of
        // Is.EqualTo(0.65f).Within using 0.001f; fail the test if the assertion is not met.
        Assert.That(Vector3.Distance(camera.transform.position, face), Is.EqualTo(0.65f).Within(0.001f));
        // Verify the result of Vector3.Dot using camera.transform.forward, (face -
        // camera.transform.position).normalized against the result of Is.GreaterThan using 0.999f; fail the test
        // if the assertion is not met.
        Assert.That(Vector3.Dot(camera.transform.forward, (face - camera.transform.position).normalized), Is.GreaterThan(0.999f));
        // Verify camera.fieldOfView against the result of Is.EqualTo using 40f; fail the test if the assertion is
        // not met.
        Assert.That(camera.fieldOfView, Is.EqualTo(40f));
        // Store the first Light component on the result of camera.transform.Find using "Caught face spotlight" in
        // local faceLight.
        Light faceLight = camera.transform.Find("Caught face spotlight").GetComponent<Light>();
        // Verify faceLight.type against the result of Is.EqualTo using LightType.Spot; fail the test if the
        // assertion is not met.
        Assert.That(faceLight.type, Is.EqualTo(LightType.Spot));
        // Verify enabled state of faceLight against Is.True; fail the test if the assertion is not met.
        Assert.That(faceLight.enabled, Is.True);
        // Verify faceLight.intensity against the result of Is.GreaterThan using 0f; fail the test if the assertion
        // is not met.
        Assert.That(faceLight.intensity, Is.GreaterThan(0f));
        // Store face minus world position of faceLight.transform in local lightToFace.
        Vector3 lightToFace = face - faceLight.transform.position;
        // Verify length of lightToFace against the result of Is.LessThan using faceLight.range; fail the test if
        // the assertion is not met.
        Assert.That(lightToFace.magnitude, Is.LessThan(faceLight.range));
        // Verify the result of Vector3.Dot using faceLight.transform.forward, lightToFace.normalized against the
        // result of Is.GreaterThan using 0.999f; fail the test if the assertion is not met.
        Assert.That(Vector3.Dot(faceLight.transform.forward, lightToFace.normalized), Is.GreaterThan(0.999f));
        // Store the result of Level3GameplayTests.Get using flow, "player" viewed as Component in local player.
        Component player = (Component)Get(flow, "player");
        // Verify enabled state of ((Light)Get(player, "flashlight")) against Is.False; fail the test if the
        // assertion is not met.
        Assert.That(((Light)Get(player, "flashlight")).enabled, Is.False);
        // Verify canvas.transform.Find("Background").GetComponent<Image>().color opacity component against the
        // result of Is.EqualTo using 0.5f; fail the test if the assertion is not met.
        Assert.That(canvas.transform.Find("Background").GetComponent<Image>().color.a, Is.EqualTo(0.5f));
        // Verify Invoke the reflected method flow.GetType().GetMethod("CollectKey") on flow with arguments new
        // object[] { Get(flow, "player") }. against the result of Is.EqualTo using false; fail the test if the
        // assertion is not met.
        Assert.That(flow.GetType().GetMethod("CollectKey").Invoke(flow, new object[] { Get(flow, "player") }), Is.EqualTo(false));
        // Deactivate canvas.
        canvas.SetActive(false);
        // Call Level3GameplayTests.Call with flow, "Caught"; invoke the named gameplay method through reflection,
        // including callbacks not run automatically in Edit Mode.
        Call(flow, "Caught");
        // Call Level3GameplayTests.Call with flow, "Escape"; invoke the named gameplay method through reflection,
        // including callbacks not run automatically in Edit Mode.
        Call(flow, "Escape");
        // Verify canvas own activation flag against Is.False, "Repeated loss must not reactivate UI."; fail the
        // test if the assertion is not met.
        Assert.That(canvas.activeSelf, Is.False, "Repeated loss must not reactivate UI.");
        // Verify the result of camera.GetComponentsInChildren<Light>().Count using l => l.name == "Caught face
        // spotlight" against the result of Is.EqualTo using 1; fail the test if the assertion is not met.
        Assert.That(camera.GetComponentsInChildren<Light>().Count(l => l.name == "Caught face spotlight"), Is.EqualTo(1));
        // Verify value read through reflection from flow against the result of Is.EqualTo using true; fail the
        // test if the assertion is not met.
        Assert.That(flow.GetType().GetProperty("Finished").GetValue(flow), Is.EqualTo(true));
    }
}
