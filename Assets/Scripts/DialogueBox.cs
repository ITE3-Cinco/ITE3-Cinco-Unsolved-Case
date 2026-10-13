// Purpose: Builds and displays the speaker, dialogue and continue hint on screen.
// Use: Created by a conversation owner such as Level2NoteNpc when dialogue is first needed.
// How it works: A shared set tracks visible boxes so interaction prompts can hide during conversations.

// System.Collections.Generic supplies typed lists, sets and dictionaries for stored data.
using System.Collections.Generic;
// TextMesh Pro supplies the text components, fonts and text-mesh information used here.
using TMPro;
// UnityEngine supplies GameObjects, components, transforms, time, math and physics used here.
using UnityEngine;
// Unity UI supplies canvas widgets such as images, sliders and buttons.
using UnityEngine.UI;

/// <summary>A scene-local dialogue panel, created on demand by its conversation owner.</summary>
public class DialogueBox : MonoBehaviour
{
    // Track all open boxes so player prompts know when dialogue already occupies the screen.
    // visibleBoxes: shared set of visible DialogueBox objects used by IsOpen. Shared across instances; scene
    // reload alone does not reset static state.
    private static readonly HashSet<DialogueBox> visibleBoxes = new HashSet<DialogueBox>();
    // IsOpen is computed when read: return number of entries in visibleBoxes is above 0.
    public static bool IsOpen => visibleBoxes.Count > 0;

    // panel: dialogue background object containing the text children. Updated by BuildPanel.
    private GameObject panel;
    // canvas: Canvas controlling this overlay visibility. BuildPanel retrieves it from the first Canvas component
    // on overlay.
    private Canvas canvas;
    // speakerText: TMP text receiving the current speaker name. BuildPanel retrieves it from the result of
    // DialogueBox.CreateText using "Speaker", a Vector2 built from 24f, -58f, a Vector2 built from -24f, -16f.
    private TMP_Text speakerText;
    // dialogueText: TMP text receiving the dialogue line and pagination. BuildPanel retrieves it from the result
    // of DialogueBox.CreateText using "Dialogue", a Vector2 built from 24f, 48f, a Vector2 built from -24f, -66f.
    private TMP_Text dialogueText;
    // hintText: TMP text receiving the continue/close hint. BuildPanel retrieves it from the result of
    // DialogueBox.CreateText using "Continue", a Vector2 built from 24f, 12f, a Vector2 built from -24f, 40f.
    private TMP_Text hintText;
    // runtimeFont: TMP font created from the supplied Font at runtime. BuildPanel retrieves it from the result of
    // TMP_FontAsset.CreateFontAsset using typewriterFont.
    private TMP_FontAsset runtimeFont;

    /// <summary>Create the panel if needed, fill its text and show the first page of this line.</summary>
    // speaker is a string argument supplied by Level2NoteNpc.ShowDialogue.
    // line is a string argument supplied by Level2NoteNpc.ShowDialogue.
    // hint is a string argument supplied by Level2NoteNpc.ShowDialogue.
    // typewriterFont is a Font argument supplied by Level2NoteNpc.ShowDialogue.
    public void Show(string speaker, string line, string hint, Font typewriterFont)
    {
        // If panel is missing, call DialogueBox.BuildPanel with typewriterFont; build a screen overlay with a
        // background and three text areas sized for different screens.
        if (panel == null) BuildPanel(typewriterFont);
        // Copy speaker (argument from the caller) into speakerText.text.
        speakerText.text = speaker;
        // Copy line (argument from the caller) into dialogueText.text.
        dialogueText.text = line;
        // Set dialogueText.pageToDisplay to 1.
        dialogueText.pageToDisplay = 1;
        // Copy hint (argument from the caller) into hintText.text.
        hintText.text = hint;
        // Activate panel.
        panel.SetActive(true);
        // Call DialogueBox.UpdateVisibility; keep IsOpen in sync with whether this panel can actually be seen.
        UpdateVisibility();
    }

    /// <summary>Show or hide the canvas while keeping the current conversation text.</summary>
    // visible is a bool argument supplied by Level2NoteNpc.LateUpdate.
    public void SetVisible(bool visible)
    {
        // If canvas exists, copy visible (argument from the caller) into canvas.enabled.
        if (canvas != null) canvas.enabled = visible;
        // Call DialogueBox.UpdateVisibility; keep IsOpen in sync with whether this panel can actually be seen.
        UpdateVisibility();
    }

    /// <summary>Close the panel and remove it from the list of visible conversations.</summary>
    public void Hide()
    {
        // If panel exists, deactivate panel.
        if (panel != null) panel.SetActive(false);
        // Remove this from visibleBoxes.
        visibleBoxes.Remove(this);
    }

    /// <summary>Close this conversation when its owner is disabled.</summary>
    // Call DialogueBox.Hide; close the panel and remove it from the list of visible conversations.
    private void OnDisable() => Hide();

    /// <summary>Keep IsOpen in sync with whether this panel can actually be seen.</summary>
    private void UpdateVisibility()
    {
        // If isActiveAndEnabled and panel != null and panel and its parent objects are active and enabled state of
        // canvas, run the following block.
        if (isActiveAndEnabled && panel != null && panel.activeInHierarchy && canvas.enabled)
            // Add this to visibleBoxes.
            visibleBoxes.Add(this);
        else
            // Remove this from visibleBoxes.
            visibleBoxes.Remove(this);
    }

    /// <summary>Remove the visibility entry and free any font resources created at runtime.</summary>
    private void OnDestroy()
    {
        // Remove this from visibleBoxes.
        visibleBoxes.Remove(this);
        // If runtimeFont is missing, return now; skip the rest of this method.
        if (runtimeFont == null) return;
        // Process each atlas in runtimeFont.atlasTextures.
        foreach (Texture2D atlas in runtimeFont.atlasTextures)
            // If atlas exists, schedule atlas for destruction at Unity end-of-frame cleanup.
            if (atlas != null) Destroy(atlas);
        // If runtimeFont material reference (Renderer.material may create an instance) exists, schedule
        // runtimeFont material reference (Renderer.material may create an instance) for destruction at Unity
        // end-of-frame cleanup.
        if (runtimeFont.material != null) Destroy(runtimeFont.material);
        // Schedule runtimeFont for destruction at Unity end-of-frame cleanup.
        Destroy(runtimeFont);
    }

    /// <summary>Build a screen overlay with a background and three text areas sized for different screens.</summary>
    // typewriterFont is a Font argument supplied by DialogueBox.Show.
    private void BuildPanel(Font typewriterFont)
    {
        // If typewriterFont exists, store the result of TMP_FontAsset.CreateFontAsset using typewriterFont in
        // runtimeFont.
        if (typewriterFont != null) runtimeFont = TMP_FontAsset.CreateFontAsset(typewriterFont);
        // Store a new GameObject named "Dialogue Canvas" with components the runtime type Canvas, the runtime type
        // CanvasScaler in local overlay.
        GameObject overlay = new GameObject("Dialogue Canvas", typeof(Canvas), typeof(CanvasScaler));
        // Parent overlay.transform beneath the Transform on this component GameObject; keep its local coordinates.
        overlay.transform.SetParent(transform, false);
        // Store the first Canvas component on overlay in canvas.
        canvas = overlay.GetComponent<Canvas>();
        // Set canvas.renderMode to RenderMode.ScreenSpaceOverlay.
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        // Set canvas.sortingOrder to 10.
        canvas.sortingOrder = 10;
        // Store the first CanvasScaler component on overlay in local scaler.
        CanvasScaler scaler = overlay.GetComponent<CanvasScaler>();
        // Set scaler.uiScaleMode to CanvasScaler.ScaleMode.ScaleWithScreenSize.
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        // Set scaler.referenceResolution to a Vector2 built from 1920, 1080.
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        // Set scaler.matchWidthOrHeight to 0.5.
        scaler.matchWidthOrHeight = 0.5f;

        // Place a dark panel near the bottom of the screen; fractional anchors adapt to canvas size.
        // Set panel to a new GameObject named "Dialogue Box" with components the runtime type RectTransform, the
        // runtime type Image.
        panel = new GameObject("Dialogue Box", typeof(RectTransform), typeof(Image));
        // Parent panel.transform beneath Transform of overlay; keep its local coordinates.
        panel.transform.SetParent(overlay.transform, false);
        // Store the first Image component on panel in local background.
        Image background = panel.GetComponent<Image>();
        // Set background.color to a Color built from 0.035, 0.04, 0.05, 0.94.
        background.color = new Color(0.035f, 0.04f, 0.05f, 0.94f);
        // Set background.raycastTarget to false.
        background.raycastTarget = false;
        // Store background.rectTransform in local rect.
        RectTransform rect = background.rectTransform;
        // Set rect.anchorMin to a Vector2 built from 0.18, 0.17.
        rect.anchorMin = new Vector2(0.18f, 0.17f);
        // Set rect.anchorMax to a Vector2 built from 0.82, 0.39.
        rect.anchorMax = new Vector2(0.82f, 0.39f);
        // Set rect.offsetMin to rect.offsetMax = Vector2.zero.
        rect.offsetMin = rect.offsetMax = Vector2.zero;

        // Give the speaker, dialogue and continue hint separate areas so each has its own styling.
        // Store the result of DialogueBox.CreateText using "Speaker", a Vector2 built from 24f, -58f, a Vector2
        // built from -24f, -16f in speakerText.
        speakerText = CreateText("Speaker", new Vector2(24f, -58f), new Vector2(-24f, -16f));
        // Set speakerText.rectTransform.anchorMin to a Vector2 built from 0, 1.
        speakerText.rectTransform.anchorMin = new Vector2(0f, 1f);
        // Set speakerText.fontSize to 28.
        speakerText.fontSize = 28f;
        // Set speakerText.fontStyle to FontStyles.Bold.
        speakerText.fontStyle = FontStyles.Bold;
        // Set speakerText.color to a Color built from 0.95, 0.8, 0.5.
        speakerText.color = new Color(0.95f, 0.8f, 0.5f);
        // Store the result of DialogueBox.CreateText using "Dialogue", a Vector2 built from 24f, 48f, a Vector2
        // built from -24f, -66f in dialogueText.
        dialogueText = CreateText("Dialogue", new Vector2(24f, 48f), new Vector2(-24f, -66f));
        // Set dialogueText.enableAutoSizing to true.
        dialogueText.enableAutoSizing = true;
        // Set dialogueText.fontSizeMin to 18.
        dialogueText.fontSizeMin = 18f;
        // Set dialogueText.fontSizeMax to 28.
        dialogueText.fontSizeMax = 28f;
        // Set dialogueText.overflowMode to TextOverflowModes.Page.
        dialogueText.overflowMode = TextOverflowModes.Page;
        // Store the result of DialogueBox.CreateText using "Continue", a Vector2 built from 24f, 12f, a Vector2
        // built from -24f, 40f in hintText.
        hintText = CreateText("Continue", new Vector2(24f, 12f), new Vector2(-24f, 40f));
        // Set hintText.rectTransform.anchorMax to a Vector2 built from 1, 0.
        hintText.rectTransform.anchorMax = new Vector2(1f, 0f);
        // Set hintText.fontSize to 20.
        hintText.fontSize = 20f;
        // Set hintText.alignment to TextAlignmentOptions.BottomRight.
        hintText.alignment = TextAlignmentOptions.BottomRight;
        // Set hintText.color to a Color built from 0.75, 0.75, 0.75.
        hintText.color = new Color(0.75f, 0.75f, 0.75f);
    }

    // Show overflow pages before the conversation advances to another speaker.
    /// <summary>Show the next page of a long line before moving to the next dialogue line.</summary>
    public bool AdvancePage()
    {
        // If panel is missing or not (panel own activation flag), return false to the method caller.
        if (panel == null || !panel.activeSelf) return false;
        // Regenerate dialogueText text mesh now so character and vertex information is available.
        dialogueText.ForceMeshUpdate();
        // If dialogueText.pageToDisplay is at least dialogueText.textInfo.pageCount, return false to the method
        // caller.
        if (dialogueText.pageToDisplay >= dialogueText.textInfo.pageCount) return false;
        // Increase dialogueText.pageToDisplay by one, storing the next counter/index value.
        dialogueText.pageToDisplay++;
        // Return true to the method caller.
        return true;
    }

    /// <summary>Create a readable text child and place it inside the dialogue panel.</summary>
    // label is a string argument supplied by DialogueBox.BuildPanel.
    // min is a Vector2 argument supplied by DialogueBox.BuildPanel.
    // max is a Vector2 argument supplied by DialogueBox.BuildPanel.
    private TMP_Text CreateText(string label, Vector2 min, Vector2 max)
    {
        // Store a new GameObject named label with components the runtime type RectTransform, the runtime type
        // TextMeshProUGUI in local child.
        GameObject child = new GameObject(label, typeof(RectTransform), typeof(TextMeshProUGUI));
        // Parent child.transform beneath Transform of panel; keep its local coordinates.
        child.transform.SetParent(panel.transform, false);
        // Store the first TextMeshProUGUI component on child in local text.
        TMP_Text text = child.GetComponent<TextMeshProUGUI>();
        // If runtimeFont exists, copy runtimeFont cached by this component into text.font.
        if (runtimeFont != null) text.font = runtimeFont;
        // Set text.raycastTarget to false.
        text.raycastTarget = false;
        // Set text.richText to false.
        text.richText = false;
        // Set text.color to Color.white.
        text.color = Color.white;
        // Set text.rectTransform.anchorMin to the zero 2D vector.
        text.rectTransform.anchorMin = Vector2.zero;
        // Set text.rectTransform.anchorMax to a 2D vector with both components equal to 1.
        text.rectTransform.anchorMax = Vector2.one;
        // Copy min (argument from the caller) into text.rectTransform.offsetMin.
        text.rectTransform.offsetMin = min;
        // Copy max (argument from the caller) into text.rectTransform.offsetMax.
        text.rectTransform.offsetMax = max;
        // Return text to the method caller.
        return text;
    }
}
