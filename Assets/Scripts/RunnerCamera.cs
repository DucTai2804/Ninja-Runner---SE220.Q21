using UnityEngine;

public class RunnerCamera : MonoBehaviour
{
    public Transform target;
    [Header("Manual Tuning (Giống hệt Three.js)")]
    public Vector3 positionOffset = new Vector3(0, 6f, -9f); 
    public Vector3 lookAtOffset = new Vector3(0, 2.5f, 15f); 
    public float followSpeed = 15f;

    [Header("Susanoo Camera Tuning")]
    public float normalFov = 60f;
    public float susanooFov = 90f;

    [Header("Fog Settings (Giữ sương ở cố định tít xa)")]
    public bool enableDynamicFog = true;
    public Color fogColor = new Color(0.53f, 0.67f, 0.8f); // Tương đương mã màu 0x88aacc bên Threejs
    public float normalFogStart = 30f;
    public float normalFogEnd = 200f; // Khoảng cách nhìn tối đa mặc định

    [Header("Orbit Controls")]
    public float mouseSensitivity = 3f;
    private float orbitYaw = 0f;
    private float orbitPitch = 0f;
    private float currentX = 0f;

    private Vector3 initialPositionOffset;
    private Vector3 initialLookAtOffset;
    private float startY;
    private Camera cam;

    // Biến cho Susanoo Mode
    private Vector3 targetPositionOffset;
    private Vector3 targetLookAtOffset;
    private bool isSusanooCamera = false;

    // Các thông số chuẩn xác 100% từ Three.js (được hardcode để tránh Inspector lưu nháp giá trị cũ)
    private Vector3 exactSusanooPosition = new Vector3(0, 56.22f, -14.34f);
    private Vector3 exactSusanooLookAt = new Vector3(0, 31.25f, 17.45f);

    public void SetSusanooMode(bool isActive)
    {
        isSusanooCamera = isActive;
        if (!isActive)
        {
            targetPositionOffset = initialPositionOffset;
            targetLookAtOffset = initialLookAtOffset;
        }
        else
        {
            // Reset góc xoay Orbit khi vào chế độ Susanoo để tránh chóng mặt
            orbitYaw = 0f;
            orbitPitch = 0f;
            
            // Ép buộc sử dụng thông số chuẩn của Three.js
            targetPositionOffset = exactSusanooPosition;
            targetLookAtOffset = exactSusanooLookAt;
        }
    }

    void Start()
    {
        cam = GetComponent<Camera>();
        
        initialPositionOffset = positionOffset;
        initialLookAtOffset = lookAtOffset;
        targetPositionOffset = positionOffset;
        targetLookAtOffset = lookAtOffset;

        if (target != null)
        {
            startY = target.position.y; 
            currentX = target.position.x * 0.7f;
            Vector3 centerPos = new Vector3(currentX, target.position.y, target.position.z);
            transform.position = centerPos + positionOffset;
            transform.LookAt(centerPos + lookAtOffset);
        }
        
        if (enableDynamicFog)
        {
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogColor = fogColor;
            
            // Ép buộc ghi đè lại phòng trường hợp giá trị cũ 120m bị lưu chết trong Inspector
            normalFogStart = 30f;
            normalFogEnd = 200f;
        }
    }

    void LateUpdate()
    {
        if (target == null) return;

        // Bỏ logic khóa cứng ở hàm Update trước đây.
        // Chỉ mượt mà đuổi theo (Lerp) targetPositionOffset.
        // Do đó khi người chơi Scroll (Zoom) hoặc Middle Mouse (Pan), giá trị thay đổi sẽ được giữ lại!
        float lerpSpeed = isSusanooCamera ? 3f : 5f;
        positionOffset = Vector3.Lerp(positionOffset, targetPositionOffset, Time.unscaledDeltaTime * lerpSpeed);
        lookAtOffset = Vector3.Lerp(lookAtOffset, targetLookAtOffset, Time.unscaledDeltaTime * lerpSpeed);

        // --- CẬP NHẬT FOV ---
        if (cam != null)
        {
            float targetFov = isSusanooCamera ? susanooFov : normalFov;
            cam.fieldOfView = Mathf.Lerp(cam.fieldOfView, targetFov, Time.unscaledDeltaTime * lerpSpeed);
        }

        // --- CẬP NHẬT SƯƠNG MÙ THÔNG MINH ---
        if (enableDynamicFog)
        {
            // Đo đạc xem Camera hiện tại đang lùi xa ra đằng sau bao nhiêu so với lúc bình thường (Sasuke)
            float pullBackDistance = Vector3.Distance(positionOffset, initialPositionOffset);
            
            // Bù trừ khoảng cách đó vào Sương mù, để Sương mù luôn đứng yên 1 chỗ (tít ngoài xa)
            // chứ không "ăn" ngược vào nhân vật khi Camera lùi xa như lỗi ở bản Three.js
            RenderSettings.fogStartDistance = normalFogStart + pullBackDistance;
            RenderSettings.fogEndDistance = normalFogEnd + pullBackDistance + (isSusanooCamera ? 100f : 0f);
        }

        currentX = Mathf.Lerp(currentX, target.position.x * 0.7f, Time.unscaledDeltaTime * 6f);
        
        Vector3 centerPos = new Vector3(currentX, startY, target.position.z);

        UnityEngine.InputSystem.Mouse mouse = UnityEngine.InputSystem.Mouse.current;
        UnityEngine.InputSystem.Keyboard keyboard = UnityEngine.InputSystem.Keyboard.current;

        if (mouse != null) 
        {
            if (mouse.rightButton.isPressed)
            {
                orbitYaw += mouse.delta.x.ReadValue() * mouseSensitivity * 0.1f;
                orbitPitch -= mouse.delta.y.ReadValue() * mouseSensitivity * 0.1f;
                orbitPitch = Mathf.Clamp(orbitPitch, -45f, 80f);
            }

            float scroll = mouse.scroll.y.ReadValue();
            if (Mathf.Abs(scroll) > 0.01f)
            {
                targetPositionOffset *= (1f - scroll * 0.005f);
            }

            if (mouse.middleButton.isPressed)
            {
                float panX = -mouse.delta.x.ReadValue() * mouseSensitivity * 0.01f;
                float panY = -mouse.delta.y.ReadValue() * mouseSensitivity * 0.01f;
                
                Vector3 panMovement = transform.right * panX + transform.up * panY;
                targetPositionOffset += panMovement;
                targetLookAtOffset += panMovement;
            }
        }

        if (keyboard != null && keyboard.rKey.wasPressedThisFrame)
        {
            orbitYaw = 0f;
            orbitPitch = 0f;
            if (isSusanooCamera)
            {
                targetPositionOffset = exactSusanooPosition;
                targetLookAtOffset = exactSusanooLookAt;
            }
            else
            {
                targetPositionOffset = initialPositionOffset;
                targetLookAtOffset = initialLookAtOffset;
            }
        }

        Vector3 lookTarget = centerPos + lookAtOffset;
        Vector3 offsetFromTarget = positionOffset - lookAtOffset;
        
        Quaternion orbitRotation = Quaternion.Euler(orbitPitch, orbitYaw, 0);
        Vector3 finalPosition = lookTarget + orbitRotation * offsetFromTarget;

        transform.position = finalPosition;
        transform.LookAt(lookTarget);
    }
}
