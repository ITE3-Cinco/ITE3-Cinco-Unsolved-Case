// Purpose: checks checkpoint collection and inventory restoration.
// Use: run in Unity Test Runner > Edit Mode; this script is not attached to a game object.
// How it works: arrange a scene or objects, invoke behavior, assert results, then clean up.

// System supplies .NET types, delegates and attributes such as Serializable.
using System;
// System.Collections supplies IEnumerator so coroutines can pause and resume across frames.
using System.Collections;
// System.Collections.Generic supplies typed lists, sets and dictionaries for stored data.
using System.Collections.Generic;
// Reflection reads fields and invokes gameplay methods by name in Editor tests.
using System.Reflection;
// NUnit supplies test attributes and assertions used by Unity Edit Mode tests.
using NUnit.Framework;
// UnityEngine supplies GameObjects, components, transforms, time, math and physics used here.
using UnityEngine;
// Unity SceneManagement supplies scene data, scene-loaded events and scene-loading calls.
using UnityEngine.SceneManagement;
// Unity Editor scene APIs open, close and mark scenes for saving.
using UnityEditor.SceneManagement;

public class CollectionProgressTests
{
    // scene: Unity scene opened by test setup and closed during teardown. Updated by SetUp.
    private Scene scene;
    // previousScene: previous scene. SetUp retrieves it from the result of SceneManager.GetActiveScene.
    private Scene previousScene;
    // objects: objects. Initialized from a new list with no entries.
    private readonly List<UnityEngine.Object> objects = new List<UnityEngine.Object>();
    // Resolve a gameplay class by name from Unity Assembly-CSharp for reflection-based checks.
    // name comes from the test/helper caller; it supplies the type or field name requested.
    // Return the .NET type named name plus ", Assembly-CSharp" (including its assembly name) to the caller.
    private static Type Gameplay(string name) => Type.GetType(name + ", Assembly-CSharp", true);
    // Invoke the named gameplay method through reflection, including callbacks not run automatically in Edit Mode.
    // type comes from the test/helper caller; it supplies the gameplay type name to find.
    // method comes from the test/helper caller; it supplies the method name to invoke.
    // Return Invoke the reflected method Gameplay(type).GetMethod(method, BindingFlags.Public |
    // BindingFlags.Static) on no reference (null) with arguments args. to the caller.
    private static object Call(string type, string method, params object[] args) =>
        Gameplay(type).GetMethod(method, BindingFlags.Public | BindingFlags.Static).Invoke(null, args);
    // Total is computed when read: return value read through reflection from null viewed as int.
    private static int Total => (int)Gameplay("CollectionProgress").GetProperty("TotalCollected").GetValue(null);

    // Create an inactive test object and keep track of it for cleanup.
    // type comes from the test/helper caller; it supplies the gameplay type name to find.
    // name comes from the test/helper caller; it supplies the type or field name requested.
    private Component Create(string type, string name)
    {
        // Store a new GameObject named name in local go.
        GameObject go = new GameObject(name);
        // Call SceneManager.MoveGameObjectToScene using go, scene.
        SceneManager.MoveGameObjectToScene(go, scene);
        // Deactivate go.
        go.SetActive(false);
        // Add go to objects.
        objects.Add(go);
        // Return a the result of CollectionProgressTests.Gameplay using type component attached to go to the
        // method caller.
        return go.AddComponent(Gameplay(type));
    }

    // Before each test, prepare the required objects or scene and save shared state for cleanup.
    [SetUp]
    public void SetUp()
    {
        // Call CollectionProgressTests.Call with "RespawnPoint", "ResetCheckpoint"; invoke the named gameplay
        // method through reflection, including callbacks not run automatically in Edit Mode.
        Call("RespawnPoint", "ResetCheckpoint");
        // Store the result of SceneManager.GetActiveScene in previousScene.
        previousScene = SceneManager.GetActiveScene();
        // Store the result of EditorSceneManager.NewScene using NewSceneSetup.EmptyScene, NewSceneMode.Additive in
        // scene.
        scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
    }

    // After each test, remove temporary objects or scenes and restore saved shared state.
    [TearDown]
    public void TearDown()
    {
        // Call CollectionProgressTests.Call with "RespawnPoint", "ResetCheckpoint"; invoke the named gameplay
        // method through reflection, including callbacks not run automatically in Edit Mode.
        Call("RespawnPoint", "ResetCheckpoint");
        // Process each obj in objects.
        foreach (UnityEngine.Object obj in objects)
            // If obj exists, remove obj immediately during Editor cleanup.
            if (obj != null) UnityEngine.Object.DestroyImmediate(obj);
        // Remove all entries from objects.
        objects.Clear();
        // Close Editor scene scene and remove it from the hierarchy when requested.
        EditorSceneManager.CloseScene(scene, true);
        // If previousScene is a valid initialized Unity handle/object, call SceneManager.SetActiveScene using
        // previousScene.
        if (previousScene.IsValid()) SceneManager.SetActiveScene(previousScene);
    }

    // Check that retry restores snapshot instead of later pickups and can repeat.
    [Test]
    public void RetryRestoresSnapshotInsteadOfLaterPickupsAndCanRepeat()
    {
        // Store the result of CollectionProgressTests.Create using "Interactable", "Same name" in local first.
        Component first = Create("Interactable", "Same name");
        // Store the result of CollectionProgressTests.Create using "Interactable", "Same name" in local second.
        Component second = Create("Interactable", "Same name");
        // Store first.gameObject.scene.path in local scene.
        string scene = first.gameObject.scene.path;
        // Call CollectionProgressTests.Call with "RespawnPoint", "BeginRun", scene; invoke the named gameplay
        // method through reflection, including callbacks not run automatically in Edit Mode.
        Call("RespawnPoint", "BeginRun", scene);
        // Call CollectionProgressTests.Call with "CollectionProgress", "Record", first; invoke the named gameplay
        // method through reflection, including callbacks not run automatically in Edit Mode.
        Call("CollectionProgress", "Record", first);
        // Call CollectionProgressTests.Call with "CollectionProgress", "Record", first; invoke the named gameplay
        // method through reflection, including callbacks not run automatically in Edit Mode.
        Call("CollectionProgress", "Record", first);
        // Verify Total against the result of Is.EqualTo using 1, "Duplicate pickup must not count twice."; fail
        // the test if the assertion is not met.
        Assert.That(Total, Is.EqualTo(1), "Duplicate pickup must not count twice.");
        // Call CollectionProgressTests.Call with "CollectionProgress", "Save", first.gameObject.scene; invoke the
        // named gameplay method through reflection, including callbacks not run automatically in Edit Mode.
        Call("CollectionProgress", "Save", first.gameObject.scene);
        // Repeat this block while retry is below 2; start from int retry = 0.
        for (int retry = 0; retry < 2; retry++)
        {
            // Call CollectionProgressTests.Call with "CollectionProgress", "Record", second; invoke the named
            // gameplay method through reflection, including callbacks not run automatically in Edit Mode.
            Call("CollectionProgress", "Record", second);
            // Verify Total against the result of Is.EqualTo using 2, "Distinct identically named objects must
            // count separately."; fail the test if the assertion is not met.
            Assert.That(Total, Is.EqualTo(2), "Distinct identically named objects must count separately.");
            // Call CollectionProgressTests.Call with "RespawnPoint", "PrepareForRetry", scene; invoke the named
            // gameplay method through reflection, including callbacks not run automatically in Edit Mode.
            Call("RespawnPoint", "PrepareForRetry", scene);
            // Verify the result of CollectionProgressTests.Call using "RespawnPoint", "BeginRun", scene against
            // the result of Is.EqualTo using true; fail the test if the assertion is not met.
            Assert.That(Call("RespawnPoint", "BeginRun", scene), Is.EqualTo(true));
            // Verify Total against the result of Is.EqualTo using 1; fail the test if the assertion is not met.
            Assert.That(Total, Is.EqualTo(1));
        }
    }

    // Check that escape carries total and entrance retry preserves it but new run clears it.
    [Test]
    public void EscapeCarriesTotalAndEntranceRetryPreservesItButNewRunClearsIt()
    {
        // Store the result of CollectionProgressTests.Create using "Interactable", "Evidence" in local item.
        Component item = Create("Interactable", "Evidence");
        // Call CollectionProgressTests.Call with "RespawnPoint", "BeginRun", "Assets/Scenes/PoliceHQ.unity";
        // invoke the named gameplay method through reflection, including callbacks not run automatically in Edit
        // Mode.
        Call("RespawnPoint", "BeginRun", "Assets/Scenes/PoliceHQ.unity");
        // Call CollectionProgressTests.Call with "CollectionProgress", "Record", item; invoke the named gameplay
        // method through reflection, including callbacks not run automatically in Edit Mode.
        Call("CollectionProgress", "Record", item);
        // Call CollectionProgressTests.Call with "CollectionProgress", "PrepareForNextLevel", "Lvl-2"; invoke the
        // named gameplay method through reflection, including callbacks not run automatically in Edit Mode.
        Call("CollectionProgress", "PrepareForNextLevel", "Lvl-2");
        // Verify the result of CollectionProgressTests.Call using "RespawnPoint", "BeginRun",
        // "Assets/Scenes/Lvl-2.unity" against the result of Is.EqualTo using false; fail the test if the assertion
        // is not met.
        Assert.That(Call("RespawnPoint", "BeginRun", "Assets/Scenes/Lvl-2.unity"), Is.EqualTo(false));
        // Verify Total against the result of Is.EqualTo using 1; fail the test if the assertion is not met.
        Assert.That(Total, Is.EqualTo(1));
        // Verify value read through reflection from null against the result of Is.EqualTo using false; fail the
        // test if the assertion is not met.
        Assert.That(Gameplay("RespawnPoint").GetField("hasReachedCheckpoint").GetValue(null), Is.EqualTo(false));
        // Call CollectionProgressTests.Call with "CollectionProgress", "Save", item.gameObject.scene; invoke the
        // named gameplay method through reflection, including callbacks not run automatically in Edit Mode.
        Call("CollectionProgress", "Save", item.gameObject.scene);
        // Call CollectionProgressTests.Call with "RespawnPoint", "PrepareForRetry", "Assets/Scenes/Lvl-2.unity";
        // invoke the named gameplay method through reflection, including callbacks not run automatically in Edit
        // Mode.
        Call("RespawnPoint", "PrepareForRetry", "Assets/Scenes/Lvl-2.unity");
        // Verify the result of CollectionProgressTests.Call using "RespawnPoint", "BeginRun",
        // "Assets/Scenes/Lvl-2.unity" against the result of Is.EqualTo using true; fail the test if the assertion
        // is not met.
        Assert.That(Call("RespawnPoint", "BeginRun", "Assets/Scenes/Lvl-2.unity"), Is.EqualTo(true));
        // Verify Total against the result of Is.EqualTo using 1; fail the test if the assertion is not met.
        Assert.That(Total, Is.EqualTo(1));
        // Call CollectionProgressTests.Call with "RespawnPoint", "BeginRun", "Assets/Scenes/Lvl-2.unity"; invoke
        // the named gameplay method through reflection, including callbacks not run automatically in Edit Mode.
        Call("RespawnPoint", "BeginRun", "Assets/Scenes/Lvl-2.unity");
        // Verify Total against Is.Zero; fail the test if the assertion is not met.
        Assert.That(Total, Is.Zero);
    }

    // Check that inventory snapshot is independent of consumption and restoration.
    [Test]
    public void InventorySnapshotIsIndependentOfConsumptionAndRestoration()
    {
        // Store the result of CollectionProgressTests.Create using "Inventory", "Inventory" in local inventory.
        Component inventory = Create("Inventory", "Inventory");
        // Store the result of ScriptableObject.CreateInstance using the result of CollectionProgressTests.Gameplay
        // using "ItemData" in local item.
        UnityEngine.Object item = ScriptableObject.CreateInstance(Gameplay("ItemData"));
        // Add item to objects.
        objects.Add(item);
        // Store reflection metadata for field "items" on inventory.GetType() in local itemsField.
        FieldInfo itemsField = inventory.GetType().GetField("items");
        // Store value read through reflection from inventory viewed as IList in local items.
        IList items = (IList)itemsField.GetValue(inventory);
        // Store the caller-supplied ItemData reference in the next inventory slot; this keeps its asset data, not
        // the world pickup object.
        items.Add(item);
        // Write 2 through reflection on inventory.
        inventory.GetType().GetField("selectedIndex").SetValue(inventory, 2);
        // Call CollectionProgressTests.Call with "CollectionProgress", "Save", inventory.gameObject.scene; invoke
        // the named gameplay method through reflection, including callbacks not run automatically in Edit Mode.
        Call("CollectionProgress", "Save", inventory.gameObject.scene);
        // Repeat this block while retry is below 2; start from int retry = 0.
        for (int retry = 0; retry < 2; retry++)
        {
            // Remove all entries from ((IList)itemsField.GetValue(inventory)).
            ((IList)itemsField.GetValue(inventory)).Clear();
            // Write 0 through reflection on inventory.
            inventory.GetType().GetField("selectedIndex").SetValue(inventory, 0);
            // Call CollectionProgressTests.Call with "CollectionProgress", "Restore", inventory.gameObject.scene;
            // invoke the named gameplay method through reflection, including callbacks not run automatically in
            // Edit Mode.
            Call("CollectionProgress", "Restore", inventory.gameObject.scene);
            // Store value read through reflection from inventory viewed as IList in local restored.
            IList restored = (IList)itemsField.GetValue(inventory);
            // Verify number of entries in restored against the result of Is.EqualTo using 1; fail the test if the
            // assertion is not met.
            Assert.That(restored.Count, Is.EqualTo(1));
            // Verify restored[0] (entry read from restored) against the result of Is.SameAs using item; fail the
            // test if the assertion is not met.
            Assert.That(restored[0], Is.SameAs(item));
            // Verify value read through reflection from inventory against the result of Is.EqualTo using 2; fail
            // the test if the assertion is not met.
            Assert.That(inventory.GetType().GetField("selectedIndex").GetValue(inventory), Is.EqualTo(2));
        }
    }

    // Check that wrong scene cannot consume escape or retry history.
    [Test]
    public void WrongSceneCannotConsumeEscapeOrRetryHistory()
    {
        // Store the result of CollectionProgressTests.Create using "Interactable", "Evidence" in local item.
        Component item = Create("Interactable", "Evidence");
        // Call CollectionProgressTests.Call with "RespawnPoint", "BeginRun", "Assets/Scenes/PoliceHQ.unity";
        // invoke the named gameplay method through reflection, including callbacks not run automatically in Edit
        // Mode.
        Call("RespawnPoint", "BeginRun", "Assets/Scenes/PoliceHQ.unity");
        // Call CollectionProgressTests.Call with "CollectionProgress", "Record", item; invoke the named gameplay
        // method through reflection, including callbacks not run automatically in Edit Mode.
        Call("CollectionProgress", "Record", item);
        // Call CollectionProgressTests.Call with "CollectionProgress", "PrepareForNextLevel", "Lvl-2"; invoke the
        // named gameplay method through reflection, including callbacks not run automatically in Edit Mode.
        Call("CollectionProgress", "PrepareForNextLevel", "Lvl-2");
        // Call CollectionProgressTests.Call with "RespawnPoint", "BeginRun", "Assets/Scenes/Level3.unity"; invoke
        // the named gameplay method through reflection, including callbacks not run automatically in Edit Mode.
        Call("RespawnPoint", "BeginRun", "Assets/Scenes/Level3.unity");
        // Verify Total against Is.Zero; fail the test if the assertion is not met.
        Assert.That(Total, Is.Zero);
        // Call CollectionProgressTests.Call with "CollectionProgress", "Record", item; invoke the named gameplay
        // method through reflection, including callbacks not run automatically in Edit Mode.
        Call("CollectionProgress", "Record", item);
        // Call CollectionProgressTests.Call with "RespawnPoint", "PrepareForRetry",
        // "Assets/Scenes/PoliceHQ.unity"; invoke the named gameplay method through reflection, including callbacks
        // not run automatically in Edit Mode.
        Call("RespawnPoint", "PrepareForRetry", "Assets/Scenes/PoliceHQ.unity");
        // Verify Total against Is.Zero; fail the test if the assertion is not met.
        Assert.That(Total, Is.Zero);
    }
}
