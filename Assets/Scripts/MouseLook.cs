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

    private void Awake()
    {
        targetSensitivity = mouseSensitivity;
    }

    private void Start()
    {
        LockCursor();
    }

    private void Update()
    {
        if (Mouse.current == null || PauseManager.IsGamePaused)
            return;

        if (lockSensitivity)
            mouseSensitivity = targetSensitivity;

        Vector2 delta = Mouse.current.delta.ReadValue();

        if (delta == Vector2.zero)
            return;

        float mouseX = delta.x * (mouseSensitivity * 0.1f);
        float mouseY = delta.y * (mouseSensitivity * 0.1f);

        xRotation -= mouseY;
        xRotation = Mathf.Clamp(xRotation, -90f, 90f);

        transform.localRotation = Quaternion.Euler(xRotation, 0f, 0f);

        if (playerBody != null)
            playerBody.Rotate(Vector3.up * mouseX);
    }

    private void OnApplicationFocus(bool hasFocus)
    {
        if (hasFocus && !PauseManager.IsGamePaused)
            LockCursor();
    }

    private void LockCursor()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }
}