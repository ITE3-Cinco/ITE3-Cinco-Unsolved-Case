using UnityEngine;
using UnityEngine.InputSystem;

public class MouseLook : MonoBehaviour
{
    [Header("Sensitivity Settings")]
    [Range(0.1f, 10f)]
    public float mouseSensitivity = 2f;

    [Tooltip("If checked, ignores external scripts trying to change sensitivity at runtime.")]
    public bool lockSensitivity = true;

    public Transform playerBody;
    private float xRotation = 0f;
    private float targetSensitivity;

    void Awake()
    {
        targetSensitivity = mouseSensitivity;
    }

    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void Update()
    {
        if (Mouse.current == null) return;

        // Force sensitivity to stay at Inspector value if locked
        if (lockSensitivity)
        {
            mouseSensitivity = targetSensitivity;
        }

        Vector2 delta = Mouse.current.delta.ReadValue();
        if (delta == Vector2.zero) return;

        // Apply controlled scaling
        float mouseX = delta.x * (mouseSensitivity * 0.1f);
        float mouseY = delta.y * (mouseSensitivity * 0.1f);

        // Pitch (Up/Down)
        xRotation -= mouseY;
        xRotation = Mathf.Clamp(xRotation, -90f, 90f);

        transform.localRotation = Quaternion.Euler(xRotation, 0f, 0f);

        // Yaw (Left/Right)
        if (playerBody != null)
        {
            playerBody.Rotate(Vector3.up * mouseX);
        }
    }

    private void OnApplicationFocus(bool hasFocus)
    {
        if (hasFocus)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }
}