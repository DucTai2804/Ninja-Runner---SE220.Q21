using UnityEngine;

public class RunnerCamera : MonoBehaviour
{
    public Transform target;
    [Header("Manual Tuning (Giống hệt Three.js)")]
    public Vector3 positionOffset = new Vector3(0, 6f, -9f); // Tọa độ thật trong Three: (0, 6, 9)
    public Vector3 lookAtOffset = new Vector3(0, 2.5f, 15f); // Target thật trong Three: (0, 2.5, -15)
    public float followSpeed = 15f;

    [Header("Orbit Controls")]
    public float mouseSensitivity = 3f;
    private float orbitYaw = 0f;
    private float orbitPitch = 0f;
    private float currentX = 0f;

    private Vector3 initialPositionOffset;
    private Vector3 initialLookAtOffset;
    private float startY;

    void Start()
    {
        initialPositionOffset = positionOffset;
        initialLookAtOffset = lookAtOffset;

        if (target != null)
        {
            startY = target.position.y; // Lưu lại cao độ gốc của mặt đường
            currentX = target.position.x * 0.7f;
            Vector3 centerPos = new Vector3(currentX, target.position.y, target.position.z);
            transform.position = centerPos + positionOffset;
            transform.LookAt(centerPos + lookAtOffset);
        }
    }

    void LateUpdate()
    {
        if (target == null) return;

        // Camera bám theo X với nội suy nhẹ tạo cảm giác tốc độ
        currentX = Mathf.Lerp(currentX, target.position.x * 0.7f, Time.deltaTime * 6f);
        
        // Cố định trục Y (startY) để màn hình không bao giờ bị nảy lên khi nhảy hay trũng xuống khi trượt
        Vector3 centerPos = new Vector3(currentX, startY, target.position.z);

        UnityEngine.InputSystem.Mouse mouse = UnityEngine.InputSystem.Mouse.current;
        UnityEngine.InputSystem.Keyboard keyboard = UnityEngine.InputSystem.Keyboard.current;

        if (mouse != null) 
        {
            // Chuột phải xoay
            if (mouse.rightButton.isPressed)
            {
                orbitYaw += mouse.delta.x.ReadValue() * mouseSensitivity * 0.1f;
                orbitPitch -= mouse.delta.y.ReadValue() * mouseSensitivity * 0.1f;
                orbitPitch = Mathf.Clamp(orbitPitch, -45f, 80f);
            }

            // Lăn chuột để Zoom
            float scroll = mouse.scroll.y.ReadValue();
            if (Mathf.Abs(scroll) > 0.01f)
            {
                positionOffset *= (1f - scroll * 0.005f);
            }

            // Chuột giữa để Pan
            if (mouse.middleButton.isPressed)
            {
                float panX = -mouse.delta.x.ReadValue() * mouseSensitivity * 0.01f;
                float panY = -mouse.delta.y.ReadValue() * mouseSensitivity * 0.01f;
                
                Vector3 panMovement = transform.right * panX + transform.up * panY;
                positionOffset += panMovement;
                lookAtOffset += panMovement;
            }
        }

        // Bấm phím R để Reset Camera
        if (keyboard != null && keyboard.rKey.wasPressedThisFrame)
        {
            orbitYaw = 0f;
            orbitPitch = 0f;
            positionOffset = initialPositionOffset;
            lookAtOffset = initialLookAtOffset;
        }

        Vector3 lookTarget = centerPos + lookAtOffset;
        Vector3 offsetFromTarget = positionOffset - lookAtOffset;
        
        Quaternion orbitRotation = Quaternion.Euler(orbitPitch, orbitYaw, 0);
        Vector3 finalPosition = lookTarget + orbitRotation * offsetFromTarget;

        transform.position = finalPosition;
        transform.LookAt(lookTarget);
    }
}
