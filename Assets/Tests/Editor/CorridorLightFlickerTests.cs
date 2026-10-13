// Purpose: checks light and panel flicker timing and restoration.
// Use: run in Unity Test Runner > Edit Mode; this script is not attached to a game object.
// How it works: arrange a scene or objects, invoke behavior, assert results, then clean up.

// System supplies .NET types, delegates and attributes such as Serializable.
using System;
// Reflection reads fields and invokes gameplay methods by name in Editor tests.
using System.Reflection;
// NUnit supplies test attributes and assertions used by Unity Edit Mode tests.
using NUnit.Framework;
// UnityEngine supplies GameObjects, components, transforms, time, math and physics used here.
using UnityEngine;

public class CorridorLightFlickerTests
{
    // root: root. Updated by SetUp.
    private GameObject root;
    // flicker: flicker. SetUp retrieves it from a the .NET type named "CorridorLightFlicker, Assembly-CSharp"
    // (including its assembly name) component attached to root.
    private Component flicker;
    // fixture: fixture. SetUp retrieves it from a Light component attached to fixtureObject.
    private Light fixture;
    // flashlight: separate test Light that corridor flicker must leave unchanged. SetUp retrieves it from a Light
    // component attached to flashlightObject.
    private Light flashlight;
    // panel: panel. SetUp retrieves it from a MeshRenderer component attached to panelObject.
    private Renderer panel;
    // material: material. Updated by SetUp.
    private Material material;
    // randomState: random state. SetUp sets it from UnityEngine.Random.state.
    private UnityEngine.Random.State randomState;
    // BaseColor: cached shader ID for the base-color property. Shared across instances; scene reload alone does
    // not reset static state.
    private static readonly int BaseColor = Shader.PropertyToID("_BaseColor");
    // Invoke the named gameplay method through reflection, including callbacks not run automatically in Edit Mode.
    // target comes from the test/helper caller; it supplies the object to inspect or change.
    // method comes from the test/helper caller; it supplies the method name to invoke.
    // Invoke the reflected method target.GetType() .GetMethod(method, BindingFlags.Instance |
    // BindingFlags.NonPublic) on target with arguments args.
    private static void Call(Component target, string method, params object[] args) => target.GetType()
        .GetMethod(method, BindingFlags.Instance | BindingFlags.NonPublic).Invoke(target, args);
    // Read a named field through reflection so assertions can inspect gameplay state.
    // name comes from the test/helper caller; it supplies the type or field name requested.
    // Return value read through reflection from flicker to the caller.
    private object Get(string name) => flicker.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).GetValue(flicker);
    // Assign a named field through reflection to arrange the test scenario.
    // name comes from the test/helper caller; it supplies the type or field name requested.
    // value comes from the test/helper caller; it supplies the value to assign.
    // Write value through reflection on flicker.
    private void Set(string name, object value) => flicker.GetType().GetField(name,
        BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic).SetValue(flicker, value);

    // Before each test, prepare the required objects or scene and save shared state for cleanup.
    [SetUp]
    public void SetUp()
    {
        // Set randomState to UnityEngine.Random.state.
        randomState = UnityEngine.Random.state;
        // Call UnityEngine.Random.InitState using 123.
        UnityEngine.Random.InitState(123);
        // Set root to a new GameObject named "Test corridor".
        root = new GameObject("Test corridor");
        // Deactivate root.
        root.SetActive(false);
        // Store a new GameObject named "Fixture" in local fixtureObject.
        var fixtureObject = new GameObject("Fixture");
        // Parent fixtureObject.transform beneath Transform of root.
        fixtureObject.transform.SetParent(root.transform);
        // Store a Light component attached to fixtureObject in fixture.
        fixture = fixtureObject.AddComponent<Light>();
        // Set fixture.intensity to 4.
        fixture.intensity = 4f;
        // Store a new GameObject named "Flashlight" in local flashlightObject.
        var flashlightObject = new GameObject("Flashlight");
        // Parent flashlightObject.transform beneath Transform of root.
        flashlightObject.transform.SetParent(root.transform);
        // Store a Light component attached to flashlightObject in flashlight.
        flashlight = flashlightObject.AddComponent<Light>();
        // Set flashlight.intensity to 200.
        flashlight.intensity = 200f;
        // Store a new GameObject named "Panel" in local panelObject.
        var panelObject = new GameObject("Panel");
        // Parent panelObject.transform beneath Transform of root.
        panelObject.transform.SetParent(root.transform);
        // Store a MeshRenderer component attached to panelObject in panel.
        panel = panelObject.AddComponent<MeshRenderer>();
        // Store the result of Shader.Find using "Universal Render Pipeline/Unlit" in local shader.
        Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
        // Verify shader against Is.Not.Null; fail the test if the assertion is not met.
        Assert.That(shader, Is.Not.Null);
        // Set material to a new Material initialized with shader.
        material = new Material(shader) { name = "light" };
        // Set material color property BaseColor from Color.white.
        material.SetColor(BaseColor, Color.white);
        // Copy material cached by this component into panel.sharedMaterial.
        panel.sharedMaterial = material;
        // Store a new MaterialPropertyBlock instance in local original.
        var original = new MaterialPropertyBlock();
        // Set original numeric property "_TestProperty" from 7.
        original.SetFloat("_TestProperty", 7f);
        // Apply original overrides to panel material slot 0.
        panel.SetPropertyBlock(original, 0);
        // Store a the .NET type named "CorridorLightFlicker, Assembly-CSharp" (including its assembly name)
        // component attached to root in flicker.
        flicker = root.AddComponent(Type.GetType("CorridorLightFlicker, Assembly-CSharp", true));
        // Call CorridorLightFlickerTests.Set with "environmentRoot", Transform of root; assign a named field
        // through reflection to arrange the test scenario.
        Set("environmentRoot", root.transform);
        // Call CorridorLightFlickerTests.Set with "lights", a new array/object initialized with the entries
        // specified here; assign a named field through reflection to arrange the test scenario.
        Set("lights", new[] { fixture });
        // Call CorridorLightFlickerTests.Call with flicker, "OnEnable"; invoke the named gameplay method through
        // reflection, including callbacks not run automatically in Edit Mode.
        Call(flicker, "OnEnable");
    }

    // After each test, remove temporary objects or scenes and restore saved shared state.
    [TearDown]
    public void TearDown()
    {
        // If flicker exists, call CorridorLightFlickerTests.Call with flicker, "OnDisable"; invoke the named
        // gameplay method through reflection, including callbacks not run automatically in Edit Mode.
        if (flicker != null) Call(flicker, "OnDisable");
        // Remove root immediately during Editor cleanup.
        UnityEngine.Object.DestroyImmediate(root);
        // Remove material immediately during Editor cleanup.
        UnityEngine.Object.DestroyImmediate(material);
        // Copy randomState cached by this component into UnityEngine.Random.state.
        UnityEngine.Random.state = randomState;
    }

    // Check that burst dips light and panel together then recovers without changing flashlight.
    [Test]
    public void BurstDipsLightAndPanelTogetherThenRecoversWithoutChangingFlashlight()
    {
        // Call CorridorLightFlickerTests.Set with "remainingTime", 0; assign a named field through reflection to
        // arrange the test scenario.
        Set("remainingTime", 0f);
        // Call CorridorLightFlickerTests.Call with flicker, "Advance", 0.01; invoke the named gameplay method
        // through reflection, including callbacks not run automatically in Edit Mode.
        Call(flicker, "Advance", 0.01f);
        // Verify fixture.intensity against the result of Is.InRange using 0.2f, 1.4f; fail the test if the
        // assertion is not met.
        Assert.That(fixture.intensity, Is.InRange(0.2f, 1.4f));
        // Store a new MaterialPropertyBlock instance in local block.
        var block = new MaterialPropertyBlock();
        // Copy renderer property overrides from panel into block for material slot 0.
        panel.GetPropertyBlock(block, 0);
        // Verify block.GetColor(BaseColor) red component against the result of Is.EqualTo(0.35f *
        // fixture.intensity / 4f).Within using 0.0001f; fail the test if the assertion is not met.
        Assert.That(block.GetColor(BaseColor).r, Is.EqualTo(0.35f * fixture.intensity / 4f).Within(0.0001f));
        // Verify flashlight.intensity against the result of Is.EqualTo using 200f; fail the test if the assertion
        // is not met.
        Assert.That(flashlight.intensity, Is.EqualTo(200f));
        // Verify color property BaseColor read from material against the result of Is.EqualTo using Color.white;
        // fail the test if the assertion is not met.
        Assert.That(material.GetColor(BaseColor), Is.EqualTo(Color.white));
        // Repeat while the result of CorridorLightFlickerTests.Get using "remainingPulses" viewed as int is above
        // 0.
        while ((int)Get("remainingPulses") > 0)
        {
            // Call CorridorLightFlickerTests.Set with "remainingTime", 0; assign a named field through reflection
            // to arrange the test scenario.
            Set("remainingTime", 0f);
            // Call CorridorLightFlickerTests.Call with flicker, "Advance", 0.01; invoke the named gameplay method
            // through reflection, including callbacks not run automatically in Edit Mode.
            Call(flicker, "Advance", 0.01f);
        }
        // Verify fixture.intensity against the result of Is.EqualTo using 4f; fail the test if the assertion is
        // not met.
        Assert.That(fixture.intensity, Is.EqualTo(4f));
        // Verify the result of CorridorLightFlickerTests.Get using "dimmed" viewed as bool against Is.False; fail
        // the test if the assertion is not met.
        Assert.That((bool)Get("dimmed"), Is.False);
        // Verify the result of CorridorLightFlickerTests.Get using "remainingTime" viewed as float against the
        // result of Is.InRange using 2.5f, 6f; fail the test if the assertion is not met.
        Assert.That((float)Get("remainingTime"), Is.InRange(2.5f, 6f));
    }

    // Check that scaled time freeze and disable preserve source state.
    [Test]
    public void ScaledTimeFreezeAndDisablePreserveSourceState()
    {
        // Call CorridorLightFlickerTests.Set with "remainingTime", 0; assign a named field through reflection to
        // arrange the test scenario.
        Set("remainingTime", 0f);
        // Call CorridorLightFlickerTests.Call with flicker, "Advance", 0.01; invoke the named gameplay method
        // through reflection, including callbacks not run automatically in Edit Mode.
        Call(flicker, "Advance", 0.01f);
        // Store fixture.intensity in local intensity.
        float intensity = fixture.intensity;
        // Store the result of CorridorLightFlickerTests.Get using "remainingTime" viewed as float in local
        // remaining.
        float remaining = (float)Get("remainingTime");
        // Call CorridorLightFlickerTests.Call with flicker, "Advance", 0; invoke the named gameplay method through
        // reflection, including callbacks not run automatically in Edit Mode.
        Call(flicker, "Advance", 0f);
        // Verify fixture.intensity against the result of Is.EqualTo using intensity; fail the test if the
        // assertion is not met.
        Assert.That(fixture.intensity, Is.EqualTo(intensity));
        // Verify the result of CorridorLightFlickerTests.Get using "remainingTime" viewed as float against the
        // result of Is.EqualTo using remaining; fail the test if the assertion is not met.
        Assert.That((float)Get("remainingTime"), Is.EqualTo(remaining));
        // Call CorridorLightFlickerTests.Call with flicker, "OnDisable"; invoke the named gameplay method through
        // reflection, including callbacks not run automatically in Edit Mode.
        Call(flicker, "OnDisable");
        // Verify fixture.intensity against the result of Is.EqualTo using 4f; fail the test if the assertion is
        // not met.
        Assert.That(fixture.intensity, Is.EqualTo(4f));
        // Store a new MaterialPropertyBlock instance in local block.
        var block = new MaterialPropertyBlock();
        // Copy renderer property overrides from panel into block for material slot 0.
        panel.GetPropertyBlock(block, 0);
        // Verify numeric property "_TestProperty" read from block against the result of Is.EqualTo using 7f; fail
        // the test if the assertion is not met.
        Assert.That(block.GetFloat("_TestProperty"), Is.EqualTo(7f));
        // Verify the result of block.HasColor using BaseColor against Is.False; fail the test if the assertion is
        // not met.
        Assert.That(block.HasColor(BaseColor), Is.False);
        // Call CorridorLightFlickerTests.Call with flicker, "OnEnable"; invoke the named gameplay method through
        // reflection, including callbacks not run automatically in Edit Mode.
        Call(flicker, "OnEnable");
        // Verify fixture.intensity against the result of Is.EqualTo using 4f; fail the test if the assertion is
        // not met.
        Assert.That(fixture.intensity, Is.EqualTo(4f));
    }
}
