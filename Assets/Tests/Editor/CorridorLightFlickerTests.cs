using System;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

public class CorridorLightFlickerTests
{
    private GameObject root;
    private Component flicker;
    private Light fixture;
    private Light flashlight;
    private Renderer panel;
    private Material material;
    private UnityEngine.Random.State randomState;
    private static readonly int BaseColor = Shader.PropertyToID("_BaseColor");
    private static void Call(Component target, string method, params object[] args) => target.GetType()
        .GetMethod(method, BindingFlags.Instance | BindingFlags.NonPublic).Invoke(target, args);
    private object Get(string name) => flicker.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).GetValue(flicker);
    private void Set(string name, object value) => flicker.GetType().GetField(name,
        BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic).SetValue(flicker, value);

    [SetUp]
    public void SetUp()
    {
        randomState = UnityEngine.Random.state;
        UnityEngine.Random.InitState(123);
        root = new GameObject("Test corridor");
        root.SetActive(false);
        var fixtureObject = new GameObject("Fixture");
        fixtureObject.transform.SetParent(root.transform);
        fixture = fixtureObject.AddComponent<Light>();
        fixture.intensity = 4f;
        var flashlightObject = new GameObject("Flashlight");
        flashlightObject.transform.SetParent(root.transform);
        flashlight = flashlightObject.AddComponent<Light>();
        flashlight.intensity = 200f;
        var panelObject = new GameObject("Panel");
        panelObject.transform.SetParent(root.transform);
        panel = panelObject.AddComponent<MeshRenderer>();
        Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
        Assert.That(shader, Is.Not.Null);
        material = new Material(shader) { name = "light" };
        material.SetColor(BaseColor, Color.white);
        panel.sharedMaterial = material;
        var original = new MaterialPropertyBlock();
        original.SetFloat("_TestProperty", 7f);
        panel.SetPropertyBlock(original, 0);
        flicker = root.AddComponent(Type.GetType("CorridorLightFlicker, Assembly-CSharp", true));
        Set("environmentRoot", root.transform);
        Set("lights", new[] { fixture });
        Call(flicker, "OnEnable");
    }

    [TearDown]
    public void TearDown()
    {
        if (flicker != null) Call(flicker, "OnDisable");
        UnityEngine.Object.DestroyImmediate(root);
        UnityEngine.Object.DestroyImmediate(material);
        UnityEngine.Random.state = randomState;
    }

    [Test]
    public void BurstDipsLightAndPanelTogetherThenRecoversWithoutChangingFlashlight()
    {
        Set("remainingTime", 0f);
        Call(flicker, "Advance", 0.01f);
        Assert.That(fixture.intensity, Is.InRange(0.2f, 1.4f));
        var block = new MaterialPropertyBlock();
        panel.GetPropertyBlock(block, 0);
        Assert.That(block.GetColor(BaseColor).r, Is.EqualTo(0.35f * fixture.intensity / 4f).Within(0.0001f));
        Assert.That(flashlight.intensity, Is.EqualTo(200f));
        Assert.That(material.GetColor(BaseColor), Is.EqualTo(Color.white));
        while ((int)Get("remainingPulses") > 0)
        {
            Set("remainingTime", 0f);
            Call(flicker, "Advance", 0.01f);
        }
        Assert.That(fixture.intensity, Is.EqualTo(4f));
        Assert.That((bool)Get("dimmed"), Is.False);
        Assert.That((float)Get("remainingTime"), Is.InRange(2.5f, 6f));
    }

    [Test]
    public void ScaledTimeFreezeAndDisablePreserveSourceState()
    {
        Set("remainingTime", 0f);
        Call(flicker, "Advance", 0.01f);
        float intensity = fixture.intensity;
        float remaining = (float)Get("remainingTime");
        Call(flicker, "Advance", 0f);
        Assert.That(fixture.intensity, Is.EqualTo(intensity));
        Assert.That((float)Get("remainingTime"), Is.EqualTo(remaining));
        Call(flicker, "OnDisable");
        Assert.That(fixture.intensity, Is.EqualTo(4f));
        var block = new MaterialPropertyBlock();
        panel.GetPropertyBlock(block, 0);
        Assert.That(block.GetFloat("_TestProperty"), Is.EqualTo(7f));
        Assert.That(block.HasColor(BaseColor), Is.False);
        Call(flicker, "OnEnable");
        Assert.That(fixture.intensity, Is.EqualTo(4f));
    }
}
