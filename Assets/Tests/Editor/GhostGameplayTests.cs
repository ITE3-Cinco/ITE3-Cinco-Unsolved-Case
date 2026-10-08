using System;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.AI;

public class GhostGameplayTests
{
    private readonly List<GameObject> objects = new List<GameObject>();
    private float timeScale;
    private CursorLockMode cursorLock;
    private bool cursorVisible;

    // Gameplay currently lives in Unity's predefined Assembly-CSharp. Test
    // assemblies access it by reflection instead of changing project assembly layout.
    private static Type Gameplay(string name) => Type.GetType(name + ", Assembly-CSharp", true);
    private static void Set(Component component, string field, object value) =>
        component.GetType().GetField(field).SetValue(component, value);
    private static void Call(Component component, string method, params object[] args) =>
        component.GetType().GetMethod(method, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
            .Invoke(component, args);

    private GameObject Create(string name)
    {
        GameObject go = new GameObject(name);
        go.SetActive(false);
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
    }

    [TearDown]
    public void TearDown()
    {
        foreach (GameObject go in objects) UnityEngine.Object.DestroyImmediate(go);
        objects.Clear();
        Time.timeScale = timeScale;
        Cursor.lockState = cursorLock;
        Cursor.visible = cursorVisible;
    }

    [Test]
    public void InitiallyInactiveLoseCanvasRemainsVisibleWhenAwakeRuns()
    {
        GameObject canvas = Create("Lose Canvas");
        Component lose = canvas.AddComponent(Gameplay("LoseSceneController"));
        Call(lose, "ShowLoseScreen");
        // Edit Mode does not run MonoBehaviour lifecycle callbacks automatically.
        // Reproduce the Awake call caused by activating an inactive controller.
        Call(lose, "Awake");
        Assert.That(canvas.activeSelf, Is.True);
        Assert.That(Time.timeScale, Is.Zero);
    }

    [TestCase(false)]
    [TestCase(true)]
    public void ClosePlayerIsCaughtBeforeArtifactGateEvenWithoutNavigation(bool protectedByCheckpoint)
    {
        GameObject canvas = Create("Lose Canvas");
        Component lose = canvas.AddComponent(Gameplay("LoseSceneController"));
        GameObject player = Create("Player");
        player.transform.position = new Vector3(1f, 1f, 0f);
        player.AddComponent<CharacterController>();
        player.SetActive(true);
        GameObject ghost = Create("Ghost");
        NavMeshAgent agent = ghost.AddComponent<NavMeshAgent>();
        agent.enabled = false;
        agent.height = 2f;
        Component enemy = ghost.AddComponent(Gameplay("GhostEnemy"));
        Set(enemy, "playerTransform", player.transform);
        Set(enemy, "loseSceneController", lose);
        Set(enemy, "requireNavigation", true);
        if (protectedByCheckpoint)
            enemy.GetType().GetField("safeUntil", BindingFlags.NonPublic | BindingFlags.Instance)
                .SetValue(enemy, Time.time + 100f);
        Call(enemy, "Awake");
        Physics.SyncTransforms();
        Call(enemy, "Update");
        Assert.That(canvas.activeSelf, Is.EqualTo(!protectedByCheckpoint));
        Assert.That(Time.timeScale, Is.EqualTo(protectedByCheckpoint ? 1f : 0f));
    }

    [Test]
    public void BrightnessPreservesAuthoredExposureWithoutAccumulating()
    {
        GameObject go = Create("Brightness Volume");
        Volume volume = go.AddComponent<Volume>();
        VolumeProfile profile = ScriptableObject.CreateInstance<VolumeProfile>();
        ColorAdjustments authored = profile.Add<ColorAdjustments>(true);
        authored.postExposure.value = -.7f;
        volume.sharedProfile = profile;
        Component brightness = go.AddComponent(Gameplay("SceneBrightness"));
        try
        {
            foreach (float slider in new[] { 0f, 5f, 10f, 10f, 5f })
            {
                Call(brightness, "Apply", slider);
                Assert.That(volume.profile.TryGet(out ColorAdjustments actual), Is.True);
                Assert.That(actual.postExposure.value, Is.EqualTo(-.7f + (slider / 10f * 4f - 2f)).Within(.0001f));
                Assert.That(actual.postExposure.overrideState, Is.True);
                Assert.That(authored.postExposure.value, Is.EqualTo(-.7f));
            }
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(volume.profile);
            UnityEngine.Object.DestroyImmediate(profile);
        }
    }
}
