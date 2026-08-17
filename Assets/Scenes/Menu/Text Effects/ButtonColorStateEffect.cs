using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

[RequireComponent(typeof(Image))]
public class ButtonColorStateEffect : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
{
    [Header("Color States")]
    [Tooltip("Default state: Pastel / Sage Green.")]
    [SerializeField] private Color defaultGreen = new Color(0.62f, 0.85f, 0.68f, 1f);

    [Tooltip("Hover state: Pastel Ice Blue.")]
    [SerializeField] private Color hoverBlue = new Color(0.68f, 0.85f, 0.95f, 1f);

    [Tooltip("Pressed state: Soft Crimson / Red.")]
    [SerializeField] private Color pressedRed = new Color(0.9f, 0.35f, 0.35f, 1f);

    [Header("Transition Settings")]
    [SerializeField] private float colorTransitionSpeed = 12f;

    [Header("Horror Flicker Effect")]
    [SerializeField] private bool enableFlicker = true;
    [Range(0f, 0.2f)] [SerializeField] private float flickerIntensity = 0.08f;

    [Header("Hover Float Settings")]
    [Tooltip("Assign the child visual Image object here so it can float without breaking the Layout Group.")]
    [SerializeField] private RectTransform visualTarget;
    [SerializeField] private float floatDistance = 8f;
    [SerializeField] private float floatSpeed = 10f;

    private Image buttonImage;
    private Vector3 originalVisualPos;
    private Vector3 targetVisualPos;

    private bool isHovered = false;
    private bool isPressed = false;

    private void Awake()
    {
        buttonImage = GetComponent<Image>();
        
        // Use the assigned visual target or default to this object's transform
        if (visualTarget == null)
        {
            visualTarget = GetComponent<RectTransform>();
        }

        originalVisualPos = visualTarget.anchoredPosition;
        targetVisualPos = originalVisualPos;
    }

    private void Update()
    {
        // 1. Float only the visual element (does not affect Vertical Layout Group)
        if (visualTarget != null)
        {
            visualTarget.anchoredPosition = Vector3.Lerp(visualTarget.anchoredPosition, targetVisualPos, Time.deltaTime * floatSpeed);
        }

        if (buttonImage == null) return;

        // 2. State Colors
        Color targetColor = defaultGreen;
        if (isPressed) targetColor = pressedRed;
        else if (isHovered) targetColor = hoverBlue;

        // 3. Subtle Flicker
        if (enableFlicker)
        {
            float noise = (Random.value - 0.5f) * flickerIntensity;
            targetColor.r = Mathf.Clamp01(targetColor.r + noise);
            targetColor.g = Mathf.Clamp01(targetColor.g + noise);
            targetColor.b = Mathf.Clamp01(targetColor.b + noise);
        }

        // 4. Color Lerp
        buttonImage.color = Color.Lerp(buttonImage.color, targetColor, Time.deltaTime * colorTransitionSpeed);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        isHovered = true;
        targetVisualPos = originalVisualPos + new Vector3(0f, floatDistance, 0f);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        isHovered = false;
        isPressed = false;
        targetVisualPos = originalVisualPos;
    }

    public void OnPointerDown(PointerEventData eventData) => isPressed = true;
    public void OnPointerUp(PointerEventData eventData) => isPressed = false;
}