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
    public void ImportedEnvironmentBuildsNavigationAroundWalls()
    {
        GameObject environment = Create("Imported environment");
        environment.SetActive(true);
        GameObject floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
        objects.Add(floor);
        floor.transform.SetParent(environment.transform);
        floor.transform.position = new Vector3(0f, -.5f, 0f);
        floor.transform.localScale = new Vector3(12f, 1f, 12f);
        UnityEngine.Object.DestroyImmediate(floor.GetComponent<Collider>());
        GameObject wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
        objects.Add(wall);
        wall.transform.SetParent(environment.transform);
        wall.transform.position = new Vector3(0f, 1.5f, 0f);
        wall.transform.localScale = new Vector3(1f, 3f, 4f);
        UnityEngine.Object.DestroyImmediate(wall.GetComponent<Collider>());
        GameObject navigationObject = Create("Navigation");
        Component navigation = navigationObject.AddComponent(Gameplay("GhostNavigation"));
        Set(navigation, "environmentRoot", environment.transform);
        Set(navigation, "addMissingMeshColliders", true);
        Set(navigation, "voxelSize", .1f);
        Call(navigation, "Awake");

        try
        {
            Assert.That(floor.GetComponent<MeshCollider>().sharedMesh,
                Is.SameAs(floor.GetComponent<MeshFilter>().sharedMesh));
            Assert.That(wall.GetComponent<MeshCollider>().sharedMesh,
                Is.SameAs(wall.GetComponent<MeshFilter>().sharedMesh));
            Assert.That(Physics.Raycast(new Vector3(-3f, 1f, 0f), Vector3.right, 6f), Is.True);
            NavMeshQueryFilter filter = new NavMeshQueryFilter { agentTypeID = 0, areaMask = NavMesh.AllAreas };
            Assert.That(NavMesh.SamplePosition(new Vector3(-3f, 0f, 0f), out NavMeshHit start, 1f, filter), Is.True);
            Assert.That(NavMesh.SamplePosition(new Vector3(3f, 0f, 0f), out NavMeshHit end, 1f, filter), Is.True);
            NavMeshPath path = new NavMeshPath();
            Assert.That(NavMesh.CalculatePath(start.position, end.position, filter, path), Is.True);
            Assert.That(path.status, Is.EqualTo(NavMeshPathStatus.PathComplete));
            Assert.That(path.corners.Length, Is.GreaterThan(2), "The route must go around the wall.");
        }
        finally
        {
            // This inactive Edit Mode component does not receive OnDestroy.
            BindingFlags flags = BindingFlags.NonPublic | BindingFlags.Instance;
            NavMeshDataInstance instance = (NavMeshDataInstance)navigation.GetType()
                .GetField("instance", flags).GetValue(navigation);
            if (instance.valid) instance.Remove();
            FieldInfo dataField = navigation.GetType().GetField("data", flags);
            UnityEngine.Object.DestroyImmediate((NavMeshData)dataField.GetValue(navigation));
            dataField.SetValue(navigation, null);
        }
    }

    [Test]
    public void WallPreventsCatchUntilPlayerHasClearContact()
    {
        GameObject canvas = Create("Lose Canvas");
        Component lose = canvas.AddComponent(Gameplay("LoseSceneController"));
        GameObject player = Create("Player");
        player.transform.position = new Vector3(1f, 0f, 0f);
        player.AddComponent<CharacterController>();
        Component movement = player.AddComponent(Gameplay("PlayerMovement"));
        Component look = player.AddComponent(Gameplay("MouseLook"));
        player.SetActive(true);
        GameObject ghost = Create("Ghost");
        NavMeshAgent agent = ghost.AddComponent<NavMeshAgent>();
        agent.enabled = false;
        agent.height = 2f;
        Component enemy = ghost.AddComponent(Gameplay("GhostEnemy"));
        Set(enemy, "playerTransform", player.transform);
        Set(enemy, "loseSceneController", lose);
        Call(enemy, "Awake");
        GameObject wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
        objects.Add(wall);
        wall.transform.position = new Vector3(.5f, 1f, 0f);
        wall.transform.localScale = new Vector3(.1f, 3f, 3f);
        Physics.SyncTransforms();
        Call(enemy, "Update");
        Assert.That(canvas.activeSelf, Is.False, "A nearby player behind a wall must not be caught.");
        Assert.That(Time.timeScale, Is.EqualTo(1f));

        wall.SetActive(false);
        Physics.SyncTransforms();
        Call(enemy, "Update");
        Assert.That(canvas.activeSelf, Is.True);
        Assert.That(Time.timeScale, Is.Zero);
        Assert.That(((Behaviour)movement).enabled, Is.False);
        Assert.That(((Behaviour)look).enabled, Is.False);
        Assert.That(Cursor.lockState, Is.EqualTo(CursorLockMode.None));
        Assert.That(Cursor.visible, Is.True);
    }

    [TestCase(0f)]
    [TestCase(5f)]
    [TestCase(10f)]
    public void BrightnessLoadsSavedPreferenceWithoutMenuOrScaledTime(float saved)
    {
        bool hadPreference = PlayerPrefs.HasKey("Brightness");
        float previous = PlayerPrefs.GetFloat("Brightness", 5f);
        GameObject go = Create("Standalone brightness volume");
        Volume volume = go.AddComponent<Volume>();
        Component brightness = go.AddComponent(Gameplay("SceneBrightness"));
        try
        {
            PlayerPrefs.SetFloat("Brightness", saved);
            Time.timeScale = 0f;
            Call(brightness, "OnEnable");
            Assert.That(volume.profile.TryGet(out ColorAdjustments actual), Is.True);
            Assert.That(actual.postExposure.value, Is.EqualTo(saved / 10f * 4f - 2f).Within(.0001f));
            Assert.That(actual.postExposure.overrideState, Is.True);
            Assert.That(actual.contrast.overrideState, Is.False, "Brightness must not override unrelated grading.");
            Assert.That(volume.sharedProfile, Is.Null, "A standalone scene needs no authored profile.");
        }
        finally
        {
            if (hadPreference) PlayerPrefs.SetFloat("Brightness", previous);
            else PlayerPrefs.DeleteKey("Brightness");
            UnityEngine.Object.DestroyImmediate(volume.profile);
        }
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
