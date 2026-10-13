// Purpose: Changes a menu button tint and visual position as the pointer interacts.
// Use: Attach to a UI Image; assign a child visualTarget when the button belongs to a layout group.
// How it works: Pointer events store hover and press state; frame updates blend toward the matching appearance.

// UnityEngine supplies GameObjects, components, transforms, time, math and physics used here.
using UnityEngine;
// Unity UI supplies canvas widgets such as images, sliders and buttons.
using UnityEngine.UI;
// Unity EventSystem supplies pointer callbacks and event data for UI interactions.
using UnityEngine.EventSystems;

[RequireComponent(typeof(Image))]
public class ButtonColorStateEffect : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
{
    // defaultGreen: Default state: Pastel / Sage Green. Assigned in the scene/prefab Inspector; the initializer
    // below is the default.
    [Header("Color States")]
    [Tooltip("Default state: Pastel / Sage Green.")]
    [SerializeField] private Color defaultGreen = new Color(0.62f, 0.85f, 0.68f, 1f);

    // hoverBlue: Hover state: Pastel Ice Blue. Assigned in the scene/prefab Inspector; the initializer below is
    // the default.
    [Tooltip("Hover state: Pastel Ice Blue.")]
    [SerializeField] private Color hoverBlue = new Color(0.68f, 0.85f, 0.95f, 1f);

    // pressedRed: Pressed state: Soft Crimson / Red. Assigned in the scene/prefab Inspector; the initializer below
    // is the default.
    [Tooltip("Pressed state: Soft Crimson / Red.")]
    [SerializeField] private Color pressedRed = new Color(0.9f, 0.35f, 0.35f, 1f);

    // colorTransitionSpeed: rate at which the button tint approaches its target color. Assigned in the
    // scene/prefab Inspector; the initializer below is the default.
    [Header("Transition Settings")]
    [SerializeField] private float colorTransitionSpeed = 12f;

    // enableFlicker: whether random tint changes are applied during frame updates. Assigned in the scene/prefab
    // Inspector; the initializer below is the default.
    [Header("Horror Flicker Effect")]
    [SerializeField] private bool enableFlicker = true;
    // flickerIntensity: maximum scale of the button random color variation. Assigned in the scene/prefab
    // Inspector; the initializer below is the default.
    [Range(0f, 0.2f)] [SerializeField] private float flickerIntensity = 0.08f;

    // visualTarget: Assign the child visual Image object here so it can float without breaking the Layout Group.
    // Awake retrieves it from the first RectTransform component on this GameObject. Inspector assignment may
    // supply its initial value.
    [Header("Hover Float Settings")]
    [Tooltip("Assign the child visual Image object here so it can float without breaking the Layout Group.")]
    [SerializeField] private RectTransform visualTarget;
    // floatDistance: vertical UI offset applied while the pointer hovers over the button. Assigned in the
    // scene/prefab Inspector; the initializer below is the default.
    [SerializeField] private float floatDistance = 8f;
    // floatSpeed: rate at which the visual approaches its hover/rest position. Assigned in the scene/prefab
    // Inspector; the initializer below is the default.
    [SerializeField] private float floatSpeed = 10f;

    // buttonImage: UI Image receiving the button state tint. Awake retrieves it from the first Image component on
    // this GameObject.
    private Image buttonImage;
    // originalVisualPos: visual anchored position saved at startup for restoring the resting button. Awake sets it
    // from visualTarget.anchoredPosition.
    private Vector3 originalVisualPos;
    // targetVisualPos: desired resting or hovered position used by the frame interpolation. Awake sets it from
    // originalVisualPos.
    private Vector3 targetVisualPos;

    // isHovered: pointer-inside flag set by pointer enter/exit callbacks. Updated by OnPointerEnter,
    // OnPointerExit.
    private bool isHovered = false;
    // isPressed: pointer-held flag set by pointer down/up callbacks. Updated by OnPointerExit, OnPointerDown,
    // OnPointerUp.
    private bool isPressed = false;

    /// <summary>Find the button image and remember the visual original position.</summary>
    private void Awake()
    {
        // Store the first Image component on this GameObject in buttonImage.
        buttonImage = GetComponent<Image>();
        
        // Use the assigned visual target or default to this object's transform
        // If visualTarget is missing, run the following block.
        if (visualTarget == null)
        {
            // Store the first RectTransform component on this GameObject in visualTarget.
            visualTarget = GetComponent<RectTransform>();
        }

        // Set originalVisualPos to visualTarget.anchoredPosition.
        originalVisualPos = visualTarget.anchoredPosition;
        // Copy originalVisualPos cached by this component into targetVisualPos.
        targetVisualPos = originalVisualPos;
    }

    /// <summary>Smoothly float the visual, choose its state color and add optional random flicker.</summary>
    private void Update()
    {
        // 1. Float only the visual element (does not affect Vertical Layout Group)
        // If visualTarget exists, run the following block.
        if (visualTarget != null)
        {
            // Store a blend from visualTarget.anchoredPosition to targetVisualPos using fraction Time.deltaTime *
            // floatSpeed in visualTarget.anchoredPosition.
            visualTarget.anchoredPosition = Vector3.Lerp(visualTarget.anchoredPosition, targetVisualPos, Time.deltaTime * floatSpeed);
        }

        // If buttonImage is missing, return now; skip the rest of this method.
        if (buttonImage == null) return;

        // 2. State Colors
        // Keep defaultGreen from this component in local targetColor.
        Color targetColor = defaultGreen;
        // If isPressed, copy pressedRed from this component into targetColor.
        if (isPressed) targetColor = pressedRed;
        else if (isHovered) targetColor = hoverBlue;

        // 3. Subtle Flicker
        // If enableFlicker, run the following block.
        if (enableFlicker)
        {
            // Store (Unity random value from 0 to 1 minus 0.5f) multiplied by flickerIntensity in local noise.
            float noise = (Random.value - 0.5f) * flickerIntensity;
            // Store targetColor.r plus noise limited to the range 0 to 1 in targetColor.r.
            targetColor.r = Mathf.Clamp01(targetColor.r + noise);
            // Store targetColor.g plus noise limited to the range 0 to 1 in targetColor.g.
            targetColor.g = Mathf.Clamp01(targetColor.g + noise);
            // Store targetColor.b plus noise limited to the range 0 to 1 in targetColor.b.
            targetColor.b = Mathf.Clamp01(targetColor.b + noise);
        }

        // 4. Color Lerp
        // Store a blend from buttonImage.color to targetColor using fraction Time.deltaTime * colorTransitionSpeed
        // in buttonImage.color.
        buttonImage.color = Color.Lerp(buttonImage.color, targetColor, Time.deltaTime * colorTransitionSpeed);
    }

    /// <summary>Mark hover active and move the visual target upward.</summary>
    // Unity EventSystem supplies eventData with the pointer position, button and selected UI event information.
    public void OnPointerEnter(PointerEventData eventData)
    {
        // Set isHovered to true.
        isHovered = true;
        // Recalculate targetVisualPos from originalVisualPos + new Vector3(0f, floatDistance, 0f) (calculated from
        // the values shown).
        targetVisualPos = originalVisualPos + new Vector3(0f, floatDistance, 0f);
    }

    /// <summary>Clear hover and press state and return the target to its original position.</summary>
    // Unity EventSystem supplies eventData with the pointer position, button and selected UI event information.
    public void OnPointerExit(PointerEventData eventData)
    {
        // Set isHovered to false.
        isHovered = false;
        // Set isPressed to false.
        isPressed = false;
        // Copy originalVisualPos cached by this component into targetVisualPos.
        targetVisualPos = originalVisualPos;
    }

    /// <summary>Show the pressed color while the pointer button is held.</summary>
    // Unity EventSystem supplies eventData with the pointer position, button and selected UI event information.
    // Set isPressed to true.
    public void OnPointerDown(PointerEventData eventData) => isPressed = true;
    /// <summary>Release the pressed state when the pointer button is released.</summary>
    // Unity EventSystem supplies eventData with the pointer position, button and selected UI event information.
    // Set isPressed to false.
    public void OnPointerUp(PointerEventData eventData) => isPressed = false;
}