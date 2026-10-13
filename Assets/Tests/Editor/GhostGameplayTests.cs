// Purpose: checks ghost catches, navigation and brightness behavior.
// Use: run in Unity Test Runner > Edit Mode; this script is not attached to a game object.
// How it works: arrange a scene or objects, invoke behavior, assert results, then clean up.

// System supplies .NET types, delegates and attributes such as Serializable.
using System;
// System.Collections.Generic supplies typed lists, sets and dictionaries for stored data.
using System.Collections.Generic;
// Reflection reads fields and invokes gameplay methods by name in Editor tests.
using System.Reflection;
// NUnit supplies test attributes and assertions used by Unity Edit Mode tests.
using NUnit.Framework;
// UnityEngine supplies GameObjects, components, transforms, time, math and physics used here.
using UnityEngine;
// Unity rendering supplies Volume/profile types used for camera effects.
using UnityEngine.Rendering;
// URP supplies ColorAdjustments and other pipeline-specific camera settings.
using UnityEngine.Rendering.Universal;
// Unity AI supplies NavMesh agents, paths and walkable-surface building/query tools.
using UnityEngine.AI;

public class GhostGameplayTests
{
    // objects: objects. Initialized from a new list with no entries.
    private readonly List<GameObject> objects = new List<GameObject>();
    // timeScale: time scale. SetUp sets it from Unity gameplay clock multiplier (0 freezes gameplay time).
    private float timeScale;
    // cursorLock: cursor lock. SetUp sets it from Unity mouse-pointer capture mode.
    private CursorLockMode cursorLock;
    // cursorVisible: cursor visible. SetUp sets it from Unity mouse-pointer visibility.
    private bool cursorVisible;

    // Resolve a gameplay class by name from Unity Assembly-CSharp for reflection-based checks.
    // Gameplay currently lives in Unity's predefined Assembly-CSharp. Test
    // assemblies access it by reflection instead of changing project assembly layout.
    // name comes from the test/helper caller; it supplies the type or field name requested.
    // Return the .NET type named name plus ", Assembly-CSharp" (including its assembly name) to the caller.
    private static Type Gameplay(string name) => Type.GetType(name + ", Assembly-CSharp", true);
    // Assign a named field through reflection to arrange the test scenario.
    // component comes from the test/helper caller; it supplies the component to inspect or invoke.
    // field comes from the test/helper caller; it supplies the field name to access.
    // value comes from the test/helper caller; it supplies the value to assign.
    // Write value through reflection on component.
    private static void Set(Component component, string field, object value) =>
        component.GetType().GetField(field).SetValue(component, value);
    // Invoke the named gameplay method through reflection, including callbacks not run automatically in Edit Mode.
    // component comes from the test/helper caller; it supplies the component to inspect or invoke.
    // method comes from the test/helper caller; it supplies the method name to invoke.
    // Invoke the reflected method component.GetType().GetMethod(method, BindingFlags.Public |
    // BindingFlags.NonPublic | BindingFlags.Instance) on component with arguments args.
    private static void Call(Component component, string method, params object[] args) =>
        component.GetType().GetMethod(method, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
            .Invoke(component, args);

    // Create an inactive test object and keep track of it for cleanup.
    // name comes from the test/helper caller; it supplies the type or field name requested.
    private GameObject Create(string name)
    {
        // Store a new GameObject named name in local go.
        GameObject go = new GameObject(name);
        // Deactivate go.
        go.SetActive(false);
        // Add go to objects.
        objects.Add(go);
        // Return go to the method caller.
        return go;
    }

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
    }

    // After each test, remove temporary objects or scenes and restore saved shared state.
    [TearDown]
    public void TearDown()
    {
        // Process each go in objects.
        foreach (GameObject go in objects) UnityEngine.Object.DestroyImmediate(go);
        // Remove all entries from objects.
        objects.Clear();
        // Copy timeScale cached by this component into Time.timeScale.
        Time.timeScale = timeScale;
        // Copy cursorLock cached by this component into Cursor.lockState.
        Cursor.lockState = cursorLock;
        // Copy cursorVisible cached by this component into Cursor.visible.
        Cursor.visible = cursorVisible;
    }

    // Check that initially inactive lose canvas remains visible when awake runs.
    [Test]
    public void InitiallyInactiveLoseCanvasRemainsVisibleWhenAwakeRuns()
    {
        // Store the result of GhostGameplayTests.Create using "Lose Canvas" in local canvas.
        GameObject canvas = Create("Lose Canvas");
        // Store a the result of GhostGameplayTests.Gameplay using "LoseSceneController" component attached to
        // canvas in local lose.
        Component lose = canvas.AddComponent(Gameplay("LoseSceneController"));
        // Call GhostGameplayTests.Call with lose, "ShowLoseScreen"; invoke the named gameplay method through
        // reflection, including callbacks not run automatically in Edit Mode.
        Call(lose, "ShowLoseScreen");
        // Edit Mode does not run MonoBehaviour lifecycle callbacks automatically.
        // Reproduce the Awake call caused by activating an inactive controller.
        // Call GhostGameplayTests.Call with lose, "Awake"; invoke the named gameplay method through reflection,
        // including callbacks not run automatically in Edit Mode.
        Call(lose, "Awake");
        // Verify canvas own activation flag against Is.True; fail the test if the assertion is not met.
        Assert.That(canvas.activeSelf, Is.True);
        // Verify Unity gameplay clock multiplier (0 freezes gameplay time) against Is.Zero; fail the test if the
        // assertion is not met.
        Assert.That(Time.timeScale, Is.Zero);
    }

    // Check that close contact respects checkpoint and completed story.
    // protectedByCheckpoint comes from NUnit TestCase arguments and selects this test input.
    // level2Quest comes from NUnit TestCase arguments and selects this test input.
    // completed comes from NUnit TestCase arguments and selects this test input.
    [TestCase(false, false, false)]
    [TestCase(true, false, false)]
    [TestCase(false, true, false)]
    [TestCase(false, true, true)]
    public void CloseContactRespectsCheckpointAndCompletedStory(bool protectedByCheckpoint, bool level2Quest, bool completed)
    {
        // Store the result of GhostGameplayTests.Create using "Lose Canvas" in local canvas.
        GameObject canvas = Create("Lose Canvas");
        // Store a the result of GhostGameplayTests.Gameplay using "LoseSceneController" component attached to
        // canvas in local lose.
        Component lose = canvas.AddComponent(Gameplay("LoseSceneController"));
        // Store the result of GhostGameplayTests.Create using "Player" in local player.
        GameObject player = Create("Player");
        // Set player.transform.position to a Vector3 built from 1, 1, 0.
        player.transform.position = new Vector3(1f, 1f, 0f);
        // A CharacterController component attached to player
        player.AddComponent<CharacterController>();
        // Activate player.
        player.SetActive(true);
        // Store the result of GhostGameplayTests.Create using "Ghost" in local ghost.
        GameObject ghost = Create("Ghost");
        // Store a NavMeshAgent component attached to ghost in local agent.
        NavMeshAgent agent = ghost.AddComponent<NavMeshAgent>();
        // Disable agent by changing its component enabled flag.
        agent.enabled = false;
        // Set agent.height to 2.
        agent.height = 2f;
        // Store a the result of GhostGameplayTests.Gameplay using "GhostEnemy" component attached to ghost in
        // local enemy.
        Component enemy = ghost.AddComponent(Gameplay("GhostEnemy"));
        // Call GhostGameplayTests.Set with enemy, "playerTransform", Transform of player; assign a named field
        // through reflection to arrange the test scenario.
        Set(enemy, "playerTransform", player.transform);
        // Call GhostGameplayTests.Set with enemy, "loseSceneController", lose; assign a named field through
        // reflection to arrange the test scenario.
        Set(enemy, "loseSceneController", lose);
        // Call GhostGameplayTests.Set with enemy, "requireNavigation", true; assign a named field through
        // reflection to arrange the test scenario.
        Set(enemy, "requireNavigation", true);
        // If protectedByCheckpoint, run the following block.
        if (protectedByCheckpoint)
            // Write Unity elapsed gameplay time plus 100 through reflection on enemy.
            enemy.GetType().GetField("safeUntil", BindingFlags.NonPublic | BindingFlags.Instance)
                .SetValue(enemy, Time.time + 100f);
        // Call GhostGameplayTests.Call with enemy, "Awake"; invoke the named gameplay method through reflection,
        // including callbacks not run automatically in Edit Mode.
        Call(enemy, "Awake");
        // If level2Quest, run the following block.
        if (level2Quest)
        {
            // Store a the result of GhostGameplayTests.Gameplay using "ArtifactManager" component attached to the
            // result of GhostGameplayTests.Create using "Notes" in local tracker.
            Component tracker = Create("Notes").AddComponent(Gameplay("ArtifactManager"));
            // Store a the result of GhostGameplayTests.Gameplay using "Level2NoteNpc" component attached to the
            // result of GhostGameplayTests.Create using "Woman" in local npc.
            Component npc = Create("Woman").AddComponent(Gameplay("Level2NoteNpc"));
            // Call GhostGameplayTests.Set with npc, "artifacts", tracker; assign a named field through reflection
            // to arrange the test scenario.
            Set(npc, "artifacts", tracker);
            // Call GhostGameplayTests.Set with npc, "whiteLady", enemy; assign a named field through reflection to
            // arrange the test scenario.
            Set(npc, "whiteLady", enemy);
            // Call GhostGameplayTests.Set with enemy, "clueStory", npc; assign a named field through reflection to
            // arrange the test scenario.
            Set(enemy, "clueStory", npc);
            // Call GhostGameplayTests.Call with npc, "OnEnable"; invoke the named gameplay method through
            // reflection, including callbacks not run automatically in Edit Mode.
            Call(npc, "OnEnable");
            // Call GhostGameplayTests.Call with npc, "Interact", a new array/object initialized with the entries
            // specified here; invoke the named gameplay method through reflection, including callbacks not run
            // automatically in Edit Mode.
            Call(npc, "Interact", new object[] { null });
            // Store 3 when completed; otherwise 2 in local count.
            int count = completed ? 3 : 2;
            // Repeat this block while i is below count; start from int i = 0.
            for (int i = 0; i < count; i++)
                // Call GhostGameplayTests.Call with tracker, "CollectArtifact", a Gameplay("Interactable")
                // component attached to Create("Note " + i); invoke the named gameplay method through reflection,
                // including callbacks not run automatically in Edit Mode.
                Call(tracker, "CollectArtifact", Create("Note " + i).AddComponent(Gameplay("Interactable")));
        }
        // Update Unity Physics with Transform changes before collision queries.
        Physics.SyncTransforms();
        // Call GhostGameplayTests.Call with enemy, "Update"; invoke the named gameplay method through reflection,
        // including callbacks not run automatically in Edit Mode.
        Call(enemy, "Update");
        // Store protectedByCheckpoint or completed in local safe.
        bool safe = protectedByCheckpoint || completed;
        // Verify canvas own activation flag against the result of Is.EqualTo using !safe; fail the test if the
        // assertion is not met.
        Assert.That(canvas.activeSelf, Is.EqualTo(!safe));
        // Verify Unity gameplay clock multiplier (0 freezes gameplay time) against the result of Is.EqualTo using
        // safe ? 1f : 0f; fail the test if the assertion is not met.
        Assert.That(Time.timeScale, Is.EqualTo(safe ? 1f : 0f));
    }

    // Check that imported environment builds navigation around walls.
    [Test]
    public void ImportedEnvironmentBuildsNavigationAroundWalls()
    {
        // Store the result of GhostGameplayTests.Create using "Imported environment" in local environment.
        GameObject environment = Create("Imported environment");
        // Activate environment.
        environment.SetActive(true);
        // Store the result of GameObject.CreatePrimitive using PrimitiveType.Cube in local floor.
        GameObject floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
        // Add floor to objects.
        objects.Add(floor);
        // Parent floor.transform beneath Transform of environment.
        floor.transform.SetParent(environment.transform);
        // Set floor.transform.position to a Vector3 built from 0, negative .5, 0.
        floor.transform.position = new Vector3(0f, -.5f, 0f);
        // Set floor.transform.localScale to a Vector3 built from 12, 1, 12.
        floor.transform.localScale = new Vector3(12f, 1f, 12f);
        // Remove the first Collider component on floor immediately during Editor cleanup.
        UnityEngine.Object.DestroyImmediate(floor.GetComponent<Collider>());
        // Store the result of GameObject.CreatePrimitive using PrimitiveType.Cube in local wall.
        GameObject wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
        // Add wall to objects.
        objects.Add(wall);
        // Parent wall.transform beneath Transform of environment.
        wall.transform.SetParent(environment.transform);
        // Set wall.transform.position to a Vector3 built from 0, 1.5, 0.
        wall.transform.position = new Vector3(0f, 1.5f, 0f);
        // Set wall.transform.localScale to a Vector3 built from 1, 3, 4.
        wall.transform.localScale = new Vector3(1f, 3f, 4f);
        // Remove the first Collider component on wall immediately during Editor cleanup.
        UnityEngine.Object.DestroyImmediate(wall.GetComponent<Collider>());
        // Store the result of GhostGameplayTests.Create using "Navigation" in local navigationObject.
        GameObject navigationObject = Create("Navigation");
        // Store a the result of GhostGameplayTests.Gameplay using "GhostNavigation" component attached to
        // navigationObject in local navigation.
        Component navigation = navigationObject.AddComponent(Gameplay("GhostNavigation"));
        // Call GhostGameplayTests.Set with navigation, "environmentRoot", Transform of environment; assign a named
        // field through reflection to arrange the test scenario.
        Set(navigation, "environmentRoot", environment.transform);
        // Call GhostGameplayTests.Set with navigation, "addMissingMeshColliders", true; assign a named field
        // through reflection to arrange the test scenario.
        Set(navigation, "addMissingMeshColliders", true);
        // Call GhostGameplayTests.Set with navigation, "voxelSize", .1; assign a named field through reflection to
        // arrange the test scenario.
        Set(navigation, "voxelSize", .1f);
        // Call GhostGameplayTests.Call with navigation, "Awake"; invoke the named gameplay method through
        // reflection, including callbacks not run automatically in Edit Mode.
        Call(navigation, "Awake");

        try
        {
            // Verify mesh asset referenced by floor.GetComponent<MeshCollider>() against the result of Is.SameAs
            // using floor.GetComponent<MeshFilter>().sharedMesh; fail the test if the assertion is not met.
            Assert.That(floor.GetComponent<MeshCollider>().sharedMesh,
                Is.SameAs(floor.GetComponent<MeshFilter>().sharedMesh));
            // Verify mesh asset referenced by wall.GetComponent<MeshCollider>() against the result of Is.SameAs
            // using wall.GetComponent<MeshFilter>().sharedMesh; fail the test if the assertion is not met.
            Assert.That(wall.GetComponent<MeshCollider>().sharedMesh,
                Is.SameAs(wall.GetComponent<MeshFilter>().sharedMesh));
            // Verify the ray hits a collider within the specified range/layers against Is.True; fail the test if
            // the assertion is not met.
            Assert.That(Physics.Raycast(new Vector3(-3f, 1f, 0f), Vector3.right, 6f), Is.True);
            // Store a new NavMeshQueryFilter instance in local filter.
            NavMeshQueryFilter filter = new NavMeshQueryFilter { agentTypeID = 0, areaMask = NavMesh.AllAreas };
            // Verify whether NavMesh finds walkable ground near new Vector3(-3f, 0f, 0f); write the result through
            // the out argument against Is.True; fail the test if the assertion is not met.
            Assert.That(NavMesh.SamplePosition(new Vector3(-3f, 0f, 0f), out NavMeshHit start, 1f, filter), Is.True);
            // Verify whether NavMesh finds walkable ground near new Vector3(3f, 0f, 0f); write the result through
            // the out argument against Is.True; fail the test if the assertion is not met.
            Assert.That(NavMesh.SamplePosition(new Vector3(3f, 0f, 0f), out NavMeshHit end, 1f, filter), Is.True);
            // Store a new NavMeshPath instance in local path.
            NavMeshPath path = new NavMeshPath();
            // Verify whether NavMesh found a navigation route using the supplied target and path object against
            // Is.True; fail the test if the assertion is not met.
            Assert.That(NavMesh.CalculatePath(start.position, end.position, filter, path), Is.True);
            // Verify path.status against the result of Is.EqualTo using NavMeshPathStatus.PathComplete; fail the
            // test if the assertion is not met.
            Assert.That(path.status, Is.EqualTo(NavMeshPathStatus.PathComplete));
            // Verify length of path.corners against the result of Is.GreaterThan using 2, "The route must go
            // around the wall."; fail the test if the assertion is not met.
            Assert.That(path.corners.Length, Is.GreaterThan(2), "The route must go around the wall.");
        }
        finally
        {
            // This inactive Edit Mode component does not receive OnDestroy.
            // Store BindingFlags.NonPublic bitwise OR BindingFlags shared instance registered by its
            // initialization code in local flags.
            BindingFlags flags = BindingFlags.NonPublic | BindingFlags.Instance;
            // Store value read through reflection from navigation viewed as NavMeshDataInstance in local instance.
            NavMeshDataInstance instance = (NavMeshDataInstance)navigation.GetType()
                .GetField("instance", flags).GetValue(navigation);
            // If instance.valid, remove  from instance.
            if (instance.valid) instance.Remove();
            // Store reflection metadata for field "data" on navigation.GetType() in local dataField.
            FieldInfo dataField = navigation.GetType().GetField("data", flags);
            // Remove value read through reflection from navigation viewed as NavMeshData immediately during Editor
            // cleanup.
            UnityEngine.Object.DestroyImmediate((NavMeshData)dataField.GetValue(navigation));
            // Write no reference (null) through reflection on navigation.
            dataField.SetValue(navigation, null);
        }
    }

    // Check that wall prevents catch until player has clear contact.
    [Test]
    public void WallPreventsCatchUntilPlayerHasClearContact()
    {
        // Store the result of GhostGameplayTests.Create using "Lose Canvas" in local canvas.
        GameObject canvas = Create("Lose Canvas");
        // Store a the result of GhostGameplayTests.Gameplay using "LoseSceneController" component attached to
        // canvas in local lose.
        Component lose = canvas.AddComponent(Gameplay("LoseSceneController"));
        // Store the result of GhostGameplayTests.Create using "Player" in local player.
        GameObject player = Create("Player");
        // Set player.transform.position to a Vector3 built from 1, 0, 0.
        player.transform.position = new Vector3(1f, 0f, 0f);
        // A CharacterController component attached to player
        player.AddComponent<CharacterController>();
        // Store a the result of GhostGameplayTests.Gameplay using "PlayerMovement" component attached to player in
        // local movement.
        Component movement = player.AddComponent(Gameplay("PlayerMovement"));
        // Store a the result of GhostGameplayTests.Gameplay using "MouseLook" component attached to player in
        // local look.
        Component look = player.AddComponent(Gameplay("MouseLook"));
        // Activate player.
        player.SetActive(true);
        // Store the result of GhostGameplayTests.Create using "Ghost" in local ghost.
        GameObject ghost = Create("Ghost");
        // Store a NavMeshAgent component attached to ghost in local agent.
        NavMeshAgent agent = ghost.AddComponent<NavMeshAgent>();
        // Disable agent by changing its component enabled flag.
        agent.enabled = false;
        // Set agent.height to 2.
        agent.height = 2f;
        // Store a the result of GhostGameplayTests.Gameplay using "GhostEnemy" component attached to ghost in
        // local enemy.
        Component enemy = ghost.AddComponent(Gameplay("GhostEnemy"));
        // Call GhostGameplayTests.Set with enemy, "playerTransform", Transform of player; assign a named field
        // through reflection to arrange the test scenario.
        Set(enemy, "playerTransform", player.transform);
        // Call GhostGameplayTests.Set with enemy, "loseSceneController", lose; assign a named field through
        // reflection to arrange the test scenario.
        Set(enemy, "loseSceneController", lose);
        // Call GhostGameplayTests.Call with enemy, "Awake"; invoke the named gameplay method through reflection,
        // including callbacks not run automatically in Edit Mode.
        Call(enemy, "Awake");
        // Store the result of GameObject.CreatePrimitive using PrimitiveType.Cube in local wall.
        GameObject wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
        // Add wall to objects.
        objects.Add(wall);
        // Set wall.transform.position to a Vector3 built from .5, 1, 0.
        wall.transform.position = new Vector3(.5f, 1f, 0f);
        // Set wall.transform.localScale to a Vector3 built from .1, 3, 3.
        wall.transform.localScale = new Vector3(.1f, 3f, 3f);
        // Update Unity Physics with Transform changes before collision queries.
        Physics.SyncTransforms();
        // Call GhostGameplayTests.Call with enemy, "Update"; invoke the named gameplay method through reflection,
        // including callbacks not run automatically in Edit Mode.
        Call(enemy, "Update");
        // Verify canvas own activation flag against Is.False, "A nearby player behind a wall must not be caught.";
        // fail the test if the assertion is not met.
        Assert.That(canvas.activeSelf, Is.False, "A nearby player behind a wall must not be caught.");
        // Verify Unity gameplay clock multiplier (0 freezes gameplay time) against the result of Is.EqualTo using
        // 1f; fail the test if the assertion is not met.
        Assert.That(Time.timeScale, Is.EqualTo(1f));

        // Deactivate wall.
        wall.SetActive(false);
        // Update Unity Physics with Transform changes before collision queries.
        Physics.SyncTransforms();
        // Call GhostGameplayTests.Call with enemy, "Update"; invoke the named gameplay method through reflection,
        // including callbacks not run automatically in Edit Mode.
        Call(enemy, "Update");
        // Verify canvas own activation flag against Is.True; fail the test if the assertion is not met.
        Assert.That(canvas.activeSelf, Is.True);
        // Verify Unity gameplay clock multiplier (0 freezes gameplay time) against Is.Zero; fail the test if the
        // assertion is not met.
        Assert.That(Time.timeScale, Is.Zero);
        // Verify enabled state of ((Behaviour)movement) against Is.False; fail the test if the assertion is not
        // met.
        Assert.That(((Behaviour)movement).enabled, Is.False);
        // Verify enabled state of ((Behaviour)look) against Is.False; fail the test if the assertion is not met.
        Assert.That(((Behaviour)look).enabled, Is.False);
        // Verify Unity mouse-pointer capture mode against the result of Is.EqualTo using CursorLockMode.None; fail
        // the test if the assertion is not met.
        Assert.That(Cursor.lockState, Is.EqualTo(CursorLockMode.None));
        // Verify Unity mouse-pointer visibility against Is.True; fail the test if the assertion is not met.
        Assert.That(Cursor.visible, Is.True);
    }

    // Check that brightness loads saved preference without menu or scaled time.
    // saved comes from NUnit TestCase arguments and selects this test input.
    [TestCase(0f)]
    [TestCase(5f)]
    [TestCase(10f)]
    public void BrightnessLoadsSavedPreferenceWithoutMenuOrScaledTime(float saved)
    {
        // Store whether PlayerPrefs already contains key "Brightness" in local hadPreference.
        bool hadPreference = PlayerPrefs.HasKey("Brightness");
        // Store saved PlayerPrefs value for "Brightness" (default 5) in local previous.
        float previous = PlayerPrefs.GetFloat("Brightness", 5f);
        // Store the result of GhostGameplayTests.Create using "Standalone brightness volume" in local go.
        GameObject go = Create("Standalone brightness volume");
        // Store a Volume component attached to go in local volume.
        Volume volume = go.AddComponent<Volume>();
        // Store a the result of GhostGameplayTests.Gameplay using "SceneBrightness" component attached to go in
        // local brightness.
        Component brightness = go.AddComponent(Gameplay("SceneBrightness"));
        try
        {
            // Save saved under PlayerPrefs key "Brightness".
            PlayerPrefs.SetFloat("Brightness", saved);
            // Set Unity gameplay clock multiplier to zero; scaled movement/timers stop, while code using real time
            // can continue.
            Time.timeScale = 0f;
            // Call GhostGameplayTests.Call with brightness, "OnEnable"; invoke the named gameplay method through
            // reflection, including callbacks not run automatically in Edit Mode.
            Call(brightness, "OnEnable");
            // Verify whether volume.profile contains the requested profile setting; retrieve it through the out
            // argument against Is.True; fail the test if the assertion is not met.
            Assert.That(volume.profile.TryGet(out ColorAdjustments actual), Is.True);
            // Verify actual.postExposure.value against the result of Is.EqualTo(saved / 10f * 4f - 2f).Within
            // using .0001f; fail the test if the assertion is not met.
            Assert.That(actual.postExposure.value, Is.EqualTo(saved / 10f * 4f - 2f).Within(.0001f));
            // Verify actual.postExposure.overrideState against Is.True; fail the test if the assertion is not met.
            Assert.That(actual.postExposure.overrideState, Is.True);
            // Verify actual.contrast.overrideState against Is.False, "Brightness must not override unrelated
            // grading."; fail the test if the assertion is not met.
            Assert.That(actual.contrast.overrideState, Is.False, "Brightness must not override unrelated grading.");
            // Verify volume.sharedProfile against Is.Null, "A standalone scene needs no authored profile."; fail
            // the test if the assertion is not met.
            Assert.That(volume.sharedProfile, Is.Null, "A standalone scene needs no authored profile.");
        }
        finally
        {
            // If hadPreference, save previous under PlayerPrefs key "Brightness".
            if (hadPreference) PlayerPrefs.SetFloat("Brightness", previous);
            else PlayerPrefs.DeleteKey("Brightness");
            // Remove volume.profile immediately during Editor cleanup.
            UnityEngine.Object.DestroyImmediate(volume.profile);
        }
    }

    // Check that brightness preserves authored exposure without accumulating.
    [Test]
    public void BrightnessPreservesAuthoredExposureWithoutAccumulating()
    {
        // Store the result of GhostGameplayTests.Create using "Brightness Volume" in local go.
        GameObject go = Create("Brightness Volume");
        // Store a Volume component attached to go in local volume.
        Volume volume = go.AddComponent<Volume>();
        // Store the result of ScriptableObject.CreateInstance in local profile.
        VolumeProfile profile = ScriptableObject.CreateInstance<VolumeProfile>();
        // Store a new ColorAdjustments setting added to profile with override states set to true in local
        // authored.
        ColorAdjustments authored = profile.Add<ColorAdjustments>(true);
        // Set authored.postExposure.value to negative .7.
        authored.postExposure.value = -.7f;
        // Copy profile into volume.sharedProfile.
        volume.sharedProfile = profile;
        // Store a the result of GhostGameplayTests.Gameplay using "SceneBrightness" component attached to go in
        // local brightness.
        Component brightness = go.AddComponent(Gameplay("SceneBrightness"));
        try
        {
            // Process each slider in a new array/object initialized with the entries specified here.
            foreach (float slider in new[] { 0f, 5f, 10f, 10f, 5f })
            {
                // Call GhostGameplayTests.Call with brightness, "Apply", slider; invoke the named gameplay method
                // through reflection, including callbacks not run automatically in Edit Mode.
                Call(brightness, "Apply", slider);
                // Verify whether volume.profile contains the requested profile setting; retrieve it through the
                // out argument against Is.True; fail the test if the assertion is not met.
                Assert.That(volume.profile.TryGet(out ColorAdjustments actual), Is.True);
                // Verify actual.postExposure.value against the result of Is.EqualTo(-.7f + (slider / 10f * 4f -
                // 2f)).Within using .0001f; fail the test if the assertion is not met.
                Assert.That(actual.postExposure.value, Is.EqualTo(-.7f + (slider / 10f * 4f - 2f)).Within(.0001f));
                // Verify actual.postExposure.overrideState against Is.True; fail the test if the assertion is not
                // met.
                Assert.That(actual.postExposure.overrideState, Is.True);
                // Verify authored.postExposure.value against the result of Is.EqualTo using -.7f; fail the test if
                // the assertion is not met.
                Assert.That(authored.postExposure.value, Is.EqualTo(-.7f));
            }
        }
        finally
        {
            // Remove volume.profile immediately during Editor cleanup.
            UnityEngine.Object.DestroyImmediate(volume.profile);
            // Remove profile immediately during Editor cleanup.
            UnityEngine.Object.DestroyImmediate(profile);
        }
    }
}
