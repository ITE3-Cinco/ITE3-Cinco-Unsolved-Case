using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>A scene-local dialogue panel, created on demand by its conversation owner.</summary>
public class DialogueBox : MonoBehaviour
{
    private GameObject panel;
    private Canvas canvas;
    private TMP_Text speakerText;
    private TMP_Text dialogueText;
    private TMP_Text hintText;

    public void Show(string speaker, string line, string hint)
    {
        if (panel == null) BuildPanel();
        speakerText.text = speaker;
        dialogueText.text = line;
        dialogueText.pageToDisplay = 1;
        hintText.text = hint;
        panel.SetActive(true);
    }

    public void SetVisible(bool visible)
    {
        if (canvas != null) canvas.enabled = visible;
    }

    public void Hide()
    {
        if (panel != null) panel.SetActive(false);
    }

    private void OnDisable() => Hide();

    private void BuildPanel()
    {
        GameObject overlay = new GameObject("Dialogue Canvas", typeof(Canvas), typeof(CanvasScaler));
        overlay.transform.SetParent(transform, false);
        canvas = overlay.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 10;
        CanvasScaler scaler = overlay.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        panel = new GameObject("Dialogue Box", typeof(RectTransform), typeof(Image));
        panel.transform.SetParent(overlay.transform, false);
        Image background = panel.GetComponent<Image>();
        background.color = new Color(0.035f, 0.04f, 0.05f, 0.94f);
        background.raycastTarget = false;
        RectTransform rect = background.rectTransform;
        rect.anchorMin = new Vector2(0.12f, 0.17f);
        rect.anchorMax = new Vector2(0.88f, 0.43f);
        rect.offsetMin = rect.offsetMax = Vector2.zero;

        speakerText = CreateText("Speaker", new Vector2(24f, -58f), new Vector2(-24f, -16f));
        speakerText.rectTransform.anchorMin = new Vector2(0f, 1f);
        speakerText.fontSize = 28f;
        speakerText.fontStyle = FontStyles.Bold;
        speakerText.color = new Color(0.95f, 0.8f, 0.5f);
        dialogueText = CreateText("Dialogue", new Vector2(24f, 48f), new Vector2(-24f, -66f));
        dialogueText.enableAutoSizing = true;
        dialogueText.fontSizeMin = 18f;
        dialogueText.fontSizeMax = 28f;
        dialogueText.overflowMode = TextOverflowModes.Page;
        hintText = CreateText("Continue", new Vector2(24f, 12f), new Vector2(-24f, 40f));
        hintText.rectTransform.anchorMax = new Vector2(1f, 0f);
        hintText.fontSize = 20f;
        hintText.alignment = TextAlignmentOptions.BottomRight;
        hintText.color = new Color(0.75f, 0.75f, 0.75f);
    }

    // Show overflow pages before the conversation advances to another speaker.
    public bool AdvancePage()
    {
        if (panel == null || !panel.activeSelf) return false;
        dialogueText.ForceMeshUpdate();
        if (dialogueText.pageToDisplay >= dialogueText.textInfo.pageCount) return false;
        dialogueText.pageToDisplay++;
        return true;
    }

    private TMP_Text CreateText(string label, Vector2 min, Vector2 max)
    {
        GameObject child = new GameObject(label, typeof(RectTransform), typeof(TextMeshProUGUI));
        child.transform.SetParent(panel.transform, false);
        TMP_Text text = child.GetComponent<TextMeshProUGUI>();
        text.raycastTarget = false;
        text.richText = false;
        text.color = Color.white;
        text.rectTransform.anchorMin = Vector2.zero;
        text.rectTransform.anchorMax = Vector2.one;
        text.rectTransform.offsetMin = min;
        text.rectTransform.offsetMax = max;
        return text;
    }
}
