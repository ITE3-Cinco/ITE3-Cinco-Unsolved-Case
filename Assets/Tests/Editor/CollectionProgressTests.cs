using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEditor.SceneManagement;

public class CollectionProgressTests
{
    private Scene scene;
    private Scene previousScene;
    private readonly List<UnityEngine.Object> objects = new List<UnityEngine.Object>();
    private static Type Gameplay(string name) => Type.GetType(name + ", Assembly-CSharp", true);
    private static object Call(string type, string method, params object[] args) =>
        Gameplay(type).GetMethod(method, BindingFlags.Public | BindingFlags.Static).Invoke(null, args);
    private static int Total => (int)Gameplay("CollectionProgress").GetProperty("TotalCollected").GetValue(null);

    private Component Create(string type, string name)
    {
        GameObject go = new GameObject(name);
        SceneManager.MoveGameObjectToScene(go, scene);
        go.SetActive(false);
        objects.Add(go);
        return go.AddComponent(Gameplay(type));
    }

    [SetUp]
    public void SetUp()
    {
        Call("RespawnPoint", "ResetCheckpoint");
        previousScene = SceneManager.GetActiveScene();
        scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
    }

    [TearDown]
    public void TearDown()
    {
        Call("RespawnPoint", "ResetCheckpoint");
        foreach (UnityEngine.Object obj in objects)
            if (obj != null) UnityEngine.Object.DestroyImmediate(obj);
        objects.Clear();
        EditorSceneManager.CloseScene(scene, true);
        if (previousScene.IsValid()) SceneManager.SetActiveScene(previousScene);
    }

    [Test]
    public void RetryRestoresSnapshotInsteadOfLaterPickupsAndCanRepeat()
    {
        Component first = Create("Interactable", "Same name");
        Component second = Create("Interactable", "Same name");
        string scene = first.gameObject.scene.path;
        Call("RespawnPoint", "BeginRun", scene);
        Call("CollectionProgress", "Record", first);
        Call("CollectionProgress", "Record", first);
        Assert.That(Total, Is.EqualTo(1), "Duplicate pickup must not count twice.");
        Call("CollectionProgress", "Save", first.gameObject.scene);
        for (int retry = 0; retry < 2; retry++)
        {
            Call("CollectionProgress", "Record", second);
            Assert.That(Total, Is.EqualTo(2), "Distinct identically named objects must count separately.");
            Call("RespawnPoint", "PrepareForRetry", scene);
            Assert.That(Call("RespawnPoint", "BeginRun", scene), Is.EqualTo(true));
            Assert.That(Total, Is.EqualTo(1));
        }
    }

    [Test]
    public void EscapeCarriesTotalAndEntranceRetryPreservesItButNewRunClearsIt()
    {
        Component item = Create("Interactable", "Evidence");
        Call("RespawnPoint", "BeginRun", "Assets/Scenes/PoliceHQ.unity");
        Call("CollectionProgress", "Record", item);
        Call("CollectionProgress", "PrepareForNextLevel", "Lvl-2");
        Assert.That(Call("RespawnPoint", "BeginRun", "Assets/Scenes/Lvl-2.unity"), Is.EqualTo(false));
        Assert.That(Total, Is.EqualTo(1));
        Assert.That(Gameplay("RespawnPoint").GetField("hasReachedCheckpoint").GetValue(null), Is.EqualTo(false));
        Call("CollectionProgress", "Save", item.gameObject.scene);
        Call("RespawnPoint", "PrepareForRetry", "Assets/Scenes/Lvl-2.unity");
        Assert.That(Call("RespawnPoint", "BeginRun", "Assets/Scenes/Lvl-2.unity"), Is.EqualTo(true));
        Assert.That(Total, Is.EqualTo(1));
        Call("RespawnPoint", "BeginRun", "Assets/Scenes/Lvl-2.unity");
        Assert.That(Total, Is.Zero);
    }

    [Test]
    public void InventorySnapshotIsIndependentOfConsumptionAndRestoration()
    {
        Component inventory = Create("Inventory", "Inventory");
        UnityEngine.Object item = ScriptableObject.CreateInstance(Gameplay("ItemData"));
        objects.Add(item);
        FieldInfo itemsField = inventory.GetType().GetField("items");
        IList items = (IList)itemsField.GetValue(inventory);
        items.Add(item);
        inventory.GetType().GetField("selectedIndex").SetValue(inventory, 2);
        Call("CollectionProgress", "Save", inventory.gameObject.scene);
        for (int retry = 0; retry < 2; retry++)
        {
            ((IList)itemsField.GetValue(inventory)).Clear();
            inventory.GetType().GetField("selectedIndex").SetValue(inventory, 0);
            Call("CollectionProgress", "Restore", inventory.gameObject.scene);
            IList restored = (IList)itemsField.GetValue(inventory);
            Assert.That(restored.Count, Is.EqualTo(1));
            Assert.That(restored[0], Is.SameAs(item));
            Assert.That(inventory.GetType().GetField("selectedIndex").GetValue(inventory), Is.EqualTo(2));
        }
    }

    [Test]
    public void WrongSceneCannotConsumeEscapeOrRetryHistory()
    {
        Component item = Create("Interactable", "Evidence");
        Call("RespawnPoint", "BeginRun", "Assets/Scenes/PoliceHQ.unity");
        Call("CollectionProgress", "Record", item);
        Call("CollectionProgress", "PrepareForNextLevel", "Lvl-2");
        Call("RespawnPoint", "BeginRun", "Assets/Scenes/Level3.unity");
        Assert.That(Total, Is.Zero);
        Call("CollectionProgress", "Record", item);
        Call("RespawnPoint", "PrepareForRetry", "Assets/Scenes/PoliceHQ.unity");
        Assert.That(Total, Is.Zero);
    }
}
