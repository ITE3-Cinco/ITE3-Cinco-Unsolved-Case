using System;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.SceneManagement;
using UnityEngine.Video;

public class GameplayIntegrationTests
{
    private readonly List<GameObject> objects = new List<GameObject>();
    private float timeScale;
    private CursorLockMode cursorLock;
    private bool cursorVisible;
    private Scene testScene;
    private Scene previousScene;
    private static Type Gameplay(string name) => Type.GetType(name + ", Assembly-CSharp", true);
    private static object Call(Component target, string method, params object[] args) => target.GetType()
        .GetMethod(method, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance).Invoke(target, args);
    private static void Set(Component target, string field, object value) => target.GetType()
        .GetField(field, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance).SetValue(target, value);

    private GameObject Create(string name)
    {
        GameObject go = new GameObject(name);
        objects.Add(go);
        return go;
    }

    [SetUp]
    public void SetUp()
    {
        timeScale = Time.timeScale;
        cursorLock = Cursor.lockState;
        cursorVisible = Cursor.visible;
        Time.timeScale = 1f;
        previousScene = SceneManager.GetActiveScene();
        testScene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
        SceneManager.SetActiveScene(testScene);
    }

    [TearDown]
    public void TearDown()
    {
        foreach (GameObject go in objects)
            if (go != null) UnityEngine.Object.DestroyImmediate(go);
        objects.Clear();
        EditorSceneManager.CloseScene(testScene, true);
        if (previousScene.IsValid()) SceneManager.SetActiveScene(previousScene);
        Time.timeScale = timeScale;
        Cursor.lockState = cursorLock;
        Cursor.visible = cursorVisible;
    }

    [Test]
    public void LoseScreenClosesPauseAndCannotBeResumedThroughPauseCallbacks()
    {
        Component pause = Create("Pause manager").AddComponent(Gameplay("PauseManager"));
        GameObject panel = Create("Pause panel");
        GameObject button = Create("Pause button");
        Set(pause, "pauseMenuPanel", panel);
        Set(pause, "gameplayPauseButton", button);
        Call(pause, "PauseGame");
        Assert.That(panel.activeSelf, Is.True);

        GameObject losePanel = Create("Lose panel");
        Component lose = losePanel.AddComponent(Gameplay("LoseSceneController"));
        Call(lose, "ShowLoseScreen");
        Assert.That(((Behaviour)pause).enabled, Is.False);
        Assert.That(panel.activeSelf, Is.False);
        Assert.That(button.activeSelf, Is.False);
        Call(pause, "PauseGame");
        Call(pause, "ResumeGame");
        Assert.That(Time.timeScale, Is.Zero);
        Assert.That(losePanel.activeSelf, Is.True);
        Assert.That(Cursor.visible, Is.True);
        Assert.That(Cursor.lockState, Is.EqualTo(CursorLockMode.None));
    }

    [Test]
    public void PauseCannotOverrideAnotherFrozenGameplayState()
    {
        Component pause = Create("Pause manager").AddComponent(Gameplay("PauseManager"));
        GameObject panel = Create("Pause panel");
        panel.SetActive(false);
        Set(pause, "pauseMenuPanel", panel);
        Time.timeScale = 0f;
        Call(pause, "PauseGame");
        Assert.That(panel.activeSelf, Is.False);
        Assert.That(Time.timeScale, Is.Zero);
    }

    [TestCase(true)]
    [TestCase(false)]
    public void InteractionPassesThroughUnrelatedTriggersButNotSolidWalls(bool trigger)
    {
        Component interaction = Create("Interaction").AddComponent(Gameplay("PlayerInteraction"));
        GameObject obstacle = Create("Obstacle");
        obstacle.transform.position = new Vector3(1000f, 1000f, 1001f);
        obstacle.AddComponent<BoxCollider>().isTrigger = trigger;
        GameObject pickup = Create("Pickup");
        pickup.transform.position = new Vector3(1000f, 1000f, 1002f);
        pickup.AddComponent<BoxCollider>().isTrigger = true;
        Component item = pickup.AddComponent(Gameplay("Interactable"));
        Physics.SyncTransforms();
        object found = Call(interaction, "FindInteractable",
            new Ray(new Vector3(1000f, 1000f, 1000f), Vector3.forward), Physics.DefaultRaycastLayers);
        if (trigger) Assert.That(found, Is.SameAs(item));
        else Assert.That(found, Is.Null);
    }

    [Test]
    public void RuntimeCaughtAudioUsesTheSameSfxMixerAsFootsteps()
    {
        GameObject player = Create("Player audio");
        AudioSource footsteps = player.AddComponent<AudioSource>();
        AudioMixer mixer = AssetDatabase.LoadAssetAtPath<AudioMixer>("Assets/Audio/AudioManager/MainMixer.mixer");
        footsteps.outputAudioMixerGroup = mixer.FindMatchingGroups("SFXVolume")[0];
        Component audio = player.AddComponent(Gameplay("PlayerAudio"));
        Call(audio, "Awake");
        AudioSource[] sources = player.GetComponents<AudioSource>();
        Assert.That(sources.Length, Is.EqualTo(2));
        Assert.That(sources[1].outputAudioMixerGroup, Is.SameAs(footsteps.outputAudioMixerGroup));
    }

    [TestCase("MainMenu")]
    [TestCase("OpeningCutscene")]
    public void EveryVideoAudioTrackUsesTheMusicMixer(string sceneName)
    {
        Scene scene = EditorSceneManager.OpenScene("Assets/Scenes/" + sceneName + ".unity", OpenSceneMode.Additive);
        try
        {
            int videos = 0;
            foreach (GameObject root in scene.GetRootGameObjects())
            foreach (VideoPlayer video in root.GetComponentsInChildren<VideoPlayer>(true))
            {
                videos++;
                Assert.That(video.audioOutputMode, Is.EqualTo(VideoAudioOutputMode.AudioSource), video.name);
                for (ushort track = 0; track < video.controlledAudioTrackCount; track++)
                {
                    AudioSource source = video.GetTargetAudioSource(track);
                    Assert.That(source, Is.Not.Null, video.name);
                    Assert.That(source.outputAudioMixerGroup, Is.Not.Null, video.name);
                    Assert.That(source.outputAudioMixerGroup.name, Is.EqualTo("MusicVolume"), video.name);
                }
            }
            Assert.That(videos, Is.GreaterThan(0));
        }
        finally { EditorSceneManager.CloseScene(scene, true); }
    }
}
