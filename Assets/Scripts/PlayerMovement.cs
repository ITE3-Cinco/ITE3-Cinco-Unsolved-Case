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

    [Header("Stamina Settings")]
    public float maxStamina = 100f;
    public float staminaDrainRate = 15f;
    public float staminaRegenRate = 14.28f;

    [Header("Stamina State")]
    public float currentStamina;
    private bool isExhausted = false;

    [Header("UI Reference")]
    public Slider staminaSlider;

    [Header("Flashlight Settings")]
    public Light flashlight;                 // The Light component on the flashlight
    public ItemData flashlightItemData;      // The ItemData asset for the flashlight
    public bool hasFlashlight = false;       // Becomes true when picked up
    private bool isFlashlightOn = false;     // Remembered toggle state

    // Public property so the pickup script can enable it
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
        if (inventory == null) inventory = GetComponentInParent<Inventory>();

        currentStamina = maxStamina;

        if (staminaSlider != null)
        {
            staminaSlider.maxValue = maxStamina;
            staminaSlider.value = currentStamina;
        }

        // Ensure light is off at start
        isFlashlightOn = false;
        if (flashlight != null)
        {
            flashlight.enabled = false;
        }

        // Sync held item state on start (in case the flashlight is already in the hotbar)
        if (inventory != null)
        {
            UpdateHeldItem(inventory.GetSelectedItem());
        }
    }

    void Update()
    {
        if (Keyboard.current == null) return;

        // 1. Reset downward velocity when grounded
        if (controller.isGrounded && velocity.y < 0)
        {
            velocity.y = -2f;
        }

        // 2. Read movement input
        float x = 0f;
        float z = 0f;

        if (Keyboard.current.dKey.isPressed || Keyboard.current.rightArrowKey.isPressed) x += 1f;
        if (Keyboard.current.aKey.isPressed || Keyboard.current.leftArrowKey.isPressed) x -= 1f;
        if (Keyboard.current.wKey.isPressed || Keyboard.current.upArrowKey.isPressed) z += 1f;
        if (Keyboard.current.sKey.isPressed || Keyboard.current.downArrowKey.isPressed) z -= 1f;

        Vector3 moveInput = new Vector3(x, 0f, z).normalized;

        bool isMoving = moveInput.magnitude > 0;
        bool shiftPressed = Keyboard.current.leftShiftKey.isPressed;

        // 3. Handle Stamina & Exhaustion Recovery
        if (isExhausted && currentStamina >= maxStamina * 0.2f)
        {
            isExhausted = false;
        }

        bool canSprint = shiftPressed && isMoving && currentStamina > 0 && !isExhausted;
        float currentSpeed = canSprint ? runSpeed : walkSpeed;

        if (canSprint)
        {
            currentStamina -= staminaDrainRate * Time.deltaTime;
            if (currentStamina <= 0)
            {
                currentStamina = 0;
                isExhausted = true;
            }
        }
        else
        {
            if (currentStamina < maxStamina)
            {
                currentStamina += staminaRegenRate * Time.deltaTime;
                currentStamina = Mathf.Clamp(currentStamina, 0, maxStamina);
            }
        }

        if (staminaSlider != null)
        {
            staminaSlider.value = currentStamina;
        }

        // 4. Horizontal Movement
        Vector3 move = transform.right * moveInput.x + transform.forward * moveInput.z;
        controller.Move(move * currentSpeed * Time.deltaTime);

        // 5. Jump Check
        if (Keyboard.current.spaceKey.wasPressedThisFrame && controller.isGrounded)
        {
            velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
        }

        // 6. Flashlight Toggle
        //    Only works when:
        //      - Player has picked up the flashlight
        //      - The flashlight item is the currently selected hotbar slot
        if (Keyboard.current.fKey.wasPressedThisFrame && flashlight != null && hasFlashlight)
        {
            bool flashlightSlotSelected = (inventory == null)
                || (inventory.GetSelectedItem() == flashlightItemData);

            if (flashlightSlotSelected)
            {
                isFlashlightOn = !isFlashlightOn;
                flashlight.enabled = isFlashlightOn;
            }
        }

        // 7. Apply Gravity
        velocity.y += gravity * Time.deltaTime;
        controller.Move(velocity * Time.deltaTime);
    }

    /// <summary>
    /// Called by Inventory whenever the selected hotbar slot changes.
    /// Turns the flashlight off when switching away, restores its state when switching back.
    /// </summary>
    public void UpdateHeldItem(ItemData currentItem)
    {
        if (flashlight == null) return;

        if (!hasFlashlight || currentItem == null || currentItem != flashlightItemData)
        {
            // Switched to an empty slot or a different item -> flashlight off
            flashlight.enabled = false;
            return;
        }

        // Switched back to the flashlight slot -> restore previous toggle state
        flashlight.enabled = isFlashlightOn;
    }

    /// <summary>
    /// Call this from your pickup script when the player collects the flashlight.
    /// Adds it to the inventory and enables the ability to toggle it.
    /// </summary>
    public void PickUpFlashlight()
    {
        hasFlashlight = true;

        if (inventory != null && flashlightItemData != null)
        {
            inventory.AddItem(flashlightItemData); // AddItem internally calls SelectSlot -> UpdateHeldItem
        }
        else
        {
            // Fallback: no inventory, just allow toggling with F
            Debug.LogWarning("Flashlight picked up, but Inventory or flashlightItemData is missing!");
        }
    }
}