using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public class PlayerMovement : MonoBehaviour
{
    private CharacterController controller;

    [Header("Movement Speeds")]
    public float walkSpeed = 6f;
    public float runSpeed = 12f;
    public float gravity = -9.81f;
    public float jumpHeight = 1.5f;

    [Header("Character Controller")]
    [Tooltip("Maximum height the player can step over.")]
    public float stepOffset = 0.5f;
    [Tooltip("Maximum slope the player can walk up.")]
    [Range(0f, 89f)] public float slopeLimit = 60f;
    [Tooltip("Small collision skin that helps prevent the controller from snagging on seams.")]
    public float skinWidth = 0.08f;

    [Header("Stamina Settings")]
    public float maxStamina = 100f;
    public float staminaDrainRate = 15f;
    public float staminaRegenRate = 14.28f;
    public bool useLowStaminaSprintBoost = false;
    public float sprintBoostThreshold = 45f;
    public float lowStaminaRunSpeed = 5.5f;

    [Header("Stamina State")]
    public float currentStamina;
    private bool isExhausted = false;

    [Header("UI Reference")]
    public Slider staminaSlider;

    [Header("Flashlight Settings")]
    public Light flashlight;
    public ItemData flashlightItemData;
    public bool hasFlashlight = false;
    private bool isFlashlightOn = false;

    public bool HasFlashlight
    {
        get { return hasFlashlight; }
        set { hasFlashlight = value; }
    }

    private Vector3 velocity;
    private Inventory inventory;

    void Start()
    {
        controller = GetComponent<CharacterController>();
        inventory = GetComponent<Inventory>();

        if (inventory == null)
            inventory = GetComponentInParent<Inventory>();

        // Keep the controller from getting stuck on small floor/wall seams.
        controller.stepOffset = stepOffset;
        controller.slopeLimit = slopeLimit;
        controller.skinWidth = skinWidth;
        controller.minMoveDistance = 0f;

        currentStamina = maxStamina;

        if (staminaSlider != null)
        {
            staminaSlider.maxValue = maxStamina;
            staminaSlider.value = currentStamina;
        }

        isFlashlightOn = false;

        if (flashlight != null)
            flashlight.enabled = false;

        if (inventory != null)
            UpdateHeldItem(inventory.GetSelectedItem());
    }

    void Update()
    {
        if (SceneTransition.IsLoading) return;
        if (Keyboard.current == null || PauseManager.IsGamePaused || !controller.enabled) return;

        bool wasGrounded = controller.isGrounded;
        if (wasGrounded && velocity.y < 0)
            velocity.y = -2f;

        float x = 0f;
        float z = 0f;

        if (Keyboard.current.dKey.isPressed || Keyboard.current.rightArrowKey.isPressed) x += 1f;
        if (Keyboard.current.aKey.isPressed || Keyboard.current.leftArrowKey.isPressed) x -= 1f;
        if (Keyboard.current.wKey.isPressed || Keyboard.current.upArrowKey.isPressed) z += 1f;
        if (Keyboard.current.sKey.isPressed || Keyboard.current.downArrowKey.isPressed) z -= 1f;

        Vector3 moveInput = new Vector3(x, 0f, z).normalized;

        bool isMoving = moveInput.magnitude > 0;
        bool shiftPressed = Keyboard.current.leftShiftKey.isPressed;

        if (isExhausted && currentStamina >= maxStamina * 0.2f)
            isExhausted = false;

        bool canSprint = shiftPressed && isMoving && currentStamina > 0 && !isExhausted;

        if (canSprint)
        {
            currentStamina -= staminaDrainRate * Time.deltaTime;

            if (currentStamina <= 0)
            {
                currentStamina = 0;
                isExhausted = true;
            }
        }
        else if (currentStamina < maxStamina)
        {
            currentStamina += staminaRegenRate * Time.deltaTime;
            currentStamina = Mathf.Clamp(currentStamina, 0, maxStamina);
        }

        if (staminaSlider != null)
            staminaSlider.value = currentStamina;

        float currentSpeed = GetMovementSpeed(canSprint);

        Vector3 move = transform.right * moveInput.x + transform.forward * moveInput.z;
        // isGrounded describes the previous Move. Check it before moving and
        // combine horizontal motion and gravity so floor contact is not lost
        // between two separate Move calls.
        if (Keyboard.current.spaceKey.wasPressedThisFrame && wasGrounded)
            velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);

        if (Keyboard.current.fKey.wasPressedThisFrame && flashlight != null && hasFlashlight)
        {
            bool flashlightSlotSelected =
                inventory == null || inventory.GetSelectedItem() == flashlightItemData;

            if (flashlightSlotSelected)
            {
                isFlashlightOn = !isFlashlightOn;
                flashlight.enabled = isFlashlightOn;
            }
        }

        velocity.y += gravity * Time.deltaTime;
        CollisionFlags flags = controller.Move((move * currentSpeed + velocity) * Time.deltaTime);

        if ((flags & CollisionFlags.Above) != 0 && velocity.y > 0f)
            velocity.y = 0f;
        else if ((flags & CollisionFlags.Below) != 0 && velocity.y < 0f)
            velocity.y = -2f;
    }

    public float GetMovementSpeed(bool sprinting)
    {
        if (!sprinting) return walkSpeed;
        return useLowStaminaSprintBoost && currentStamina < sprintBoostThreshold
            ? lowStaminaRunSpeed : runSpeed;
    }

    public void ResetVelocity()
    {
        velocity = Vector3.zero;
    }

    public void UpdateHeldItem(ItemData currentItem)
    {
        if (flashlight == null) return;

        if (!hasFlashlight || currentItem == null || currentItem != flashlightItemData)
        {
            flashlight.enabled = false;
            return;
        }

        flashlight.enabled = isFlashlightOn;
    }

    public void PickUpFlashlight()
    {
        hasFlashlight = true;

        if (inventory != null && flashlightItemData != null)
        {
            inventory.AddItem(flashlightItemData);
        }
        else
        {
            Debug.LogWarning("Flashlight picked up, but Inventory or flashlightItemData is missing!");
        }
    }
}
