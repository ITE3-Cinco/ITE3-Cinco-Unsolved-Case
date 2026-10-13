// Purpose: Checks how pause, loss, interaction and audio systems work together.
// Use: Run in Unity Test Runner > Edit Mode; this script is not attached to a scene object.
// How it works: Create temporary objects or open a scene, invoke gameplay methods, then assert and clean up.

// System provides Type, used to locate gameplay classes by name.
using System;
// Generic collections provide the list of temporary GameObjects to clean up after each test.
using System.Collections.Generic;
// Reflection lets Editor tests access gameplay fields and methods in Assembly-CSharp.
using System.Reflection;
// NUnit provides the test attributes and assertions that report success or failure.
using NUnit.Framework;
// UnityEditor provides AssetDatabase for loading the project's audio mixer.
using UnityEditor;
// Editor scene APIs create, open and close isolated test scenes.
using UnityEditor.SceneManagement;
// UnityEngine provides GameObjects, components, physics, time and cursor state.
using UnityEngine;
// Unity audio types let the tests inspect mixer routing.
using UnityEngine.Audio;
// SceneManagement provides Scene values and the current active scene.
using UnityEngine.SceneManagement;
// VideoPlayer exposes the audio output assigned to each video track.
using UnityEngine.Video;

// NUnit discovers the test methods in this class through their Test/TestCase attributes.
public class GameplayIntegrationTests
{
    // Track only objects created by this test, so teardown can destroy them without touching other scenes.
    private readonly List<GameObject> objects = new List<GameObject>();
    // Save the original gameplay clock rate because pause and loss tests change it.
    private float timeScale;
    // Save the mouse capture mode so the test does not leave the Editor cursor locked or unlocked differently.
    private CursorLockMode cursorLock;
    // Save whether the mouse pointer was visible before the test.
    private bool cursorVisible;
    // Hold the empty additive scene used to isolate each test's temporary objects.
    private Scene testScene;
    // Remember which scene was active so teardown can restore it after closing the temporary scene.
    private Scene previousScene;
    // name is a gameplay class supplied by the test; resolve it in Unity's gameplay assembly or throw if missing.
    private static Type Gameplay(string name) => Type.GetType(name + ", Assembly-CSharp", true);
    // target supplies the component, method names its callback, and args carries the callback's arguments.
    // Resolve a public/private instance method and return its result; void methods return null.
    private static object Call(Component target, string method, params object[] args) => target.GetType()
        .GetMethod(method, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance).Invoke(target, args);
    // target supplies the component, field identifies its setting, and value is the reference/state to assign.
    // Set that public/private instance field to arrange the conditions for the next assertion.
    private static void Set(Component target, string field, object value) => target.GetType()
        .GetField(field, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance).SetValue(target, value);

    /// <summary>Create a named object in the active test scene and register it for cleanup.</summary>
    // name is a descriptive test label used in the hierarchy and failure messages.
    private GameObject Create(string name)
    {
        // Create the object in the empty scene selected by SetUp.
        GameObject go = new GameObject(name);
        // Retain its reference so TearDown can destroy it even when an assertion fails.
        objects.Add(go);
        // Give the caller the object to position or attach gameplay components to.
        return go;
    }

    // NUnit runs this setup before every test, including each TestCase variant.
    [SetUp]
    public void SetUp()
    {
        // Cache the clock rate before the test changes pause or loss state.
        timeScale = Time.timeScale;
        // Cache the mouse capture mode before gameplay callbacks change it.
        cursorLock = Cursor.lockState;
        // Cache pointer visibility for restoration after the test.
        cursorVisible = Cursor.visible;
        // Start with normal gameplay time so a previous paused state cannot affect the test.
        Time.timeScale = 1f;
        // Remember the active scene before creating a temporary one.
        previousScene = SceneManager.GetActiveScene();
        // Add an empty scene while keeping existing Editor scenes loaded.
        testScene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
        // New GameObjects created by Create will belong to this isolated scene.
        SceneManager.SetActiveScene(testScene);
    }

    // NUnit runs this cleanup after each test, even when an assertion fails.
    [TearDown]
    public void TearDown()
    {
        // Visit each temporary object registered by Create.
        foreach (GameObject go in objects)
            // Destroy surviving objects immediately because Edit Mode does not use normal frame-end cleanup.
            if (go != null) UnityEngine.Object.DestroyImmediate(go);
        // Remove stale references so the next test starts with an empty cleanup list.
        objects.Clear();
        // Unload the temporary scene and discard any changes made inside it.
        EditorSceneManager.CloseScene(testScene, true);
        // Restore the previous active scene if it still exists.
        if (previousScene.IsValid()) SceneManager.SetActiveScene(previousScene);
        // Restore the clock rate saved before pause/loss callbacks ran.
        Time.timeScale = timeScale;
        // Restore the original mouse capture mode.
        Cursor.lockState = cursorLock;
        // Restore the original pointer visibility.
        Cursor.visible = cursorVisible;
    }

    // A loss must close pause controls and remain frozen even if an old pause button callback fires.
    [Test]
    public void LoseScreenClosesPauseAndCannotBeResumedThroughPauseCallbacks()
    {
        // Create a real PauseManager component resolved from the gameplay assembly.
        Component pause = Create("Pause manager").AddComponent(Gameplay("PauseManager"));
        // Create the pause panel that PauseManager will show or hide.
        GameObject panel = Create("Pause panel");
        // Create the gameplay pause button that should disappear after loss.
        GameObject button = Create("Pause button");
        // Assign the panel just as a scene Inspector reference would.
        Set(pause, "pauseMenuPanel", panel);
        // Assign the in-game button to the manager under test.
        Set(pause, "gameplayPauseButton", button);
        // Invoke the pause action without needing a physical key press.
        Call(pause, "PauseGame");
        // Confirm pause actually opened before checking the loss transition.
        Assert.That(panel.activeSelf, Is.True);

        // Create the UI object that represents the loss screen.
        GameObject losePanel = Create("Lose panel");
        // Attach the real loss controller so its pause-locking behavior runs.
        Component lose = losePanel.AddComponent(Gameplay("LoseSceneController"));
        // Trigger loss while the pause menu is already open.
        Call(lose, "ShowLoseScreen");
        // Loss must disable the pause component to stop further normal input handling.
        Assert.That(((Behaviour)pause).enabled, Is.False);
        // The pause panel must no longer cover the loss screen.
        Assert.That(panel.activeSelf, Is.False);
        // The gameplay pause button must also be hidden.
        Assert.That(button.activeSelf, Is.False);
        // Simulate a stale UI callback trying to pause after the loss.
        Call(pause, "PauseGame");
        // Simulate a stale resume callback, which must not unfreeze a lost game.
        Call(pause, "ResumeGame");
        // The game must remain frozen after both callbacks.
        Assert.That(Time.timeScale, Is.Zero);
        // The loss UI must remain visible for retry or menu selection.
        Assert.That(losePanel.activeSelf, Is.True);
        // A visible pointer is required to use the loss screen buttons.
        Assert.That(Cursor.visible, Is.True);
        // Mouse capture must be released for those buttons.
        Assert.That(Cursor.lockState, Is.EqualTo(CursorLockMode.None));
    }

    // Pause must respect an existing freeze imposed by another gameplay system.
    [Test]
    public void PauseCannotOverrideAnotherFrozenGameplayState()
    {
        // Create the pause controller in the isolated scene.
        Component pause = Create("Pause manager").AddComponent(Gameplay("PauseManager"));
        // Create a panel whose visibility reveals whether pause was accepted.
        GameObject panel = Create("Pause panel");
        // Begin with the panel hidden, as it is during ordinary gameplay.
        panel.SetActive(false);
        // Wire the panel into the controller under test.
        Set(pause, "pauseMenuPanel", panel);
        // Freeze the clock independently of PauseManager, as an ending or loss system would.
        Time.timeScale = 0f;
        // Ask PauseManager to pause an already frozen game.
        Call(pause, "PauseGame");
        // It must not open its panel or claim the other system's frozen state.
        Assert.That(panel.activeSelf, Is.False);
        // The original freeze must remain in effect.
        Assert.That(Time.timeScale, Is.Zero);
    }

    // Run the same raycast layout with a harmless trigger and then with a solid blocking collider.
    [TestCase(true)]
    [TestCase(false)]
    // trigger comes from TestCase: true makes the obstacle a trigger; false makes it a solid wall.
    public void InteractionPassesThroughUnrelatedTriggersButNotSolidWalls(bool trigger)
    {
        // Create the interaction component whose target-selection helper is invoked below.
        Component interaction = Create("Interaction").AddComponent(Gameplay("PlayerInteraction"));
        // Create an obstacle between the ray origin and the pickup.
        GameObject obstacle = Create("Obstacle");
        // Place it far from existing scene objects so unrelated colliders do not affect this ray.
        obstacle.transform.position = new Vector3(1000f, 1000f, 1001f);
        // The TestCase value chooses whether the ray should pass through or stop here.
        obstacle.AddComponent<BoxCollider>().isTrigger = trigger;
        // Create the target object one unit beyond the obstacle.
        GameObject pickup = Create("Pickup");
        // Align it with the forward ray while keeping it behind the obstacle.
        pickup.transform.position = new Vector3(1000f, 1000f, 1002f);
        // Use a trigger collider so targeting can find the pickup without making it a physical wall.
        pickup.AddComponent<BoxCollider>().isTrigger = true;
        // Attach Interactable so a successful target search returns this component.
        Component item = pickup.AddComponent(Gameplay("Interactable"));
        // Update physics immediately after positioning objects; no gameplay frame has run in Edit Mode.
        Physics.SyncTransforms();
        // Invoke the helper with a forward ray and default layers; found receives its selected Interactable.
        object found = Call(interaction, "FindInteractable",
            new Ray(new Vector3(1000f, 1000f, 1000f), Vector3.forward), Physics.DefaultRaycastLayers);
        // A harmless trigger should allow selection of the pickup behind it.
        if (trigger) Assert.That(found, Is.SameAs(item));
        // A solid wall should prevent the pickup from being selected.
        else Assert.That(found, Is.Null);
    }

    // Runtime-created caught audio must follow the same SFX volume control as footsteps.
    [Test]
    public void RuntimeCaughtAudioUsesTheSameSfxMixerAsFootsteps()
    {
        // Create the player audio object without relying on a scene prefab.
        GameObject player = Create("Player audio");
        // Supply the existing footstep source that PlayerAudio uses to choose its mixer route.
        AudioSource footsteps = player.AddComponent<AudioSource>();
        // Load the real project mixer so this assertion covers its assigned SFX group.
        AudioMixer mixer = AssetDatabase.LoadAssetAtPath<AudioMixer>("Assets/Audio/AudioManager/MainMixer.mixer");
        // Route footsteps into the first group matching the SFXVolume name.
        footsteps.outputAudioMixerGroup = mixer.FindMatchingGroups("SFXVolume")[0];
        // Attach PlayerAudio, which creates a separate caught-audio source when it initializes.
        Component audio = player.AddComponent(Gameplay("PlayerAudio"));
        // Invoke initialization explicitly in Edit Mode.
        Call(audio, "Awake");
        // Read both audio sources after initialization.
        AudioSource[] sources = player.GetComponents<AudioSource>();
        // There should be the original footstep source and one caught-audio source.
        Assert.That(sources.Length, Is.EqualTo(2));
        // The added source must use exactly the same mixer group, so SFX settings control both sounds.
        Assert.That(sources[1].outputAudioMixerGroup, Is.SameAs(footsteps.outputAudioMixerGroup));
    }

    // Check both video scenes using the same music-routing assertions.
    [TestCase("MainMenu")]
    [TestCase("OpeningCutscene")]
    // sceneName comes from TestCase and selects the saved Unity scene to inspect.
    public void EveryVideoAudioTrackUsesTheMusicMixer(string sceneName)
    {
        // Open the selected scene additively so the isolated test scene remains loaded.
        Scene scene = EditorSceneManager.OpenScene("Assets/Scenes/" + sceneName + ".unity", OpenSceneMode.Additive);
        // Use finally below to close this extra scene even if an assertion fails.
        try
        {
            // Count discovered videos so an empty scene cannot accidentally pass the routing test.
            int videos = 0;
            // Search each top-level object in the loaded scene.
            foreach (GameObject root in scene.GetRootGameObjects())
            // Include inactive children because their video players may be enabled later during transitions.
            foreach (VideoPlayer video in root.GetComponentsInChildren<VideoPlayer>(true))
            {
                // Count this video before checking its output configuration.
                videos++;
                // AudioSource output is required to route video audio through the mixer.
                Assert.That(video.audioOutputMode, Is.EqualTo(VideoAudioOutputMode.AudioSource), video.name);
                // Inspect every controlled audio track, starting at the zero-based first track.
                for (ushort track = 0; track < video.controlledAudioTrackCount; track++)
                {
                    // Read the AudioSource assigned to this video track.
                    AudioSource source = video.GetTargetAudioSource(track);
                    // A missing source would prevent this track from following the configured mixer route.
                    Assert.That(source, Is.Not.Null, video.name);
                    // Require a mixer assignment before reading its name.
                    Assert.That(source.outputAudioMixerGroup, Is.Not.Null, video.name);
                    // Video sound must follow music volume, rather than bypassing the player's settings.
                    Assert.That(source.outputAudioMixerGroup.name, Is.EqualTo("MusicVolume"), video.name);
                }
            }
            // Verify this scene really contains at least one video covered by the assertions.
            Assert.That(videos, Is.GreaterThan(0));
        }
        // Always unload the inspected video scene, discarding test changes.
        finally { EditorSceneManager.CloseScene(scene, true); }
    }
}
