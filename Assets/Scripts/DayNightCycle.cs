using UnityEngine;

public class DayNightCycle : MonoBehaviour
{
    [Header("Colors (Day)")]
    public Color dayBackground = Color.white;
    public Color dayAmbient = Color.white;
    public Color daySun = Color.white;
    public float dayAmbientIntensity = 0.4f;
    public float daySunIntensity = 1.5f;

    [Header("Colors (Night)")]
    public Color nightBackground = Color.black;
    public Color nightAmbient = Color.white;
    public Color nightMoon = Color.white;
    public float nightAmbientIntensity = 0.4f;
    public float nightMoonIntensity = 0.7f;

    [Header("Cycle Settings")]
    public float dayDuration = 10000f; // 10000 điểm sáng
    public float nightDuration = 3000f; // 3000 điểm tối

    [Header("References")]
    public Light directionalLight; // Kéo Main Directional Light vào đây
    public Camera mainCamera;      // Kéo Main Camera vào đây (hoặc để tự tìm)

    [Header("Ambient Intensity Control")]
    public float originalAmbientIntensity = 1f;

    private bool isNight = false;

    void Start()
    {
        if (mainCamera == null) mainCamera = Camera.main;

        // Lưu lại chính xác thông số ban ngày hiện tại của bạn trong Unity
        if (mainCamera != null)
        {
            // Bắt buộc Camera hiển thị màu trơn (Solid Color) thay vì Skybox để thấy hiệu ứng đổi màu nền
            mainCamera.clearFlags = CameraClearFlags.SolidColor;
            dayBackground = mainCamera.backgroundColor;
        }
        else
        {
            dayBackground = RenderSettings.fogColor;
        }
        
        dayAmbient = RenderSettings.ambientLight;
        originalAmbientIntensity = RenderSettings.ambientIntensity;
        dayAmbientIntensity = originalAmbientIntensity; // Kế thừa luôn độ sáng chuẩn ban ngày
        
        if (directionalLight != null)
        {
            daySun = directionalLight.color;
            daySunIntensity = directionalLight.intensity;
        }

        // Parse màu của ban đêm từ Three.js
        ColorUtility.TryParseHtmlString("#1A2235", out nightBackground);
        ColorUtility.TryParseHtmlString("#7788AA", out nightAmbient);
        ColorUtility.TryParseHtmlString("#88AADD", out nightMoon);
        
        // Cường độ ban đêm cực thấp để tối đen lại
        nightAmbientIntensity = 0.1f; 
        nightMoonIntensity = 0.15f; 
    }

    void Update()
    {
        // Lấy điểm thực tế từ UIManager (nơi cộng điểm hiển thị trên màn hình)
        float score = 0f;
        if (UIManager.Instance != null) 
        {
            score = UIManager.Instance.score;
        }
        else if (GameManager.Instance != null) 
        {
            score = GameManager.Instance.score;
        }

        // Nếu điểm bằng 0 (chưa bắt đầu chơi) thì không cần tính toán chu kỳ
        if (score <= 0f) return;

        float cycleLength = dayDuration + nightDuration;
        float currentCyclePos = score % cycleLength;

        bool wasNight = isNight;
        isNight = (currentCyclePos >= dayDuration);

        // Xác định màu đích (Target Colors)
        Color targetBg = isNight ? nightBackground : dayBackground;
        Color targetAmbient = isNight ? nightAmbient : dayAmbient;
        Color targetDirColor = isNight ? nightMoon : daySun;
        float targetAmbientInt = isNight ? nightAmbientIntensity : dayAmbientIntensity;
        float targetDirInt = isNight ? nightMoonIntensity : daySunIntensity;

        // Tốc độ nội suy mượt mà (Lerp)
        float lerpSpeed = Time.deltaTime * 0.5f;

        // --- Cập nhật màu nền & Sương mù ---
        if (mainCamera != null)
        {
            mainCamera.backgroundColor = Color.Lerp(mainCamera.backgroundColor, targetBg, lerpSpeed);
        }
        RenderSettings.fogColor = Color.Lerp(RenderSettings.fogColor, targetBg, lerpSpeed);

        // --- Cập nhật Ánh sáng môi trường (Ambient) ---
        Color lerpedAmbient = Color.Lerp(RenderSettings.ambientLight, targetAmbient, lerpSpeed);
        RenderSettings.ambientLight = lerpedAmbient;
        
        // Cực kỳ quan trọng: Giảm độ sáng của toàn bộ môi trường (áp dụng cho cả Skybox)
        RenderSettings.ambientIntensity = Mathf.Lerp(RenderSettings.ambientIntensity, targetAmbientInt, lerpSpeed);

        // --- Cập nhật Ánh sáng mặt trời / mặt trăng (Directional Light) ---
        if (directionalLight != null)
        {
            directionalLight.color = Color.Lerp(directionalLight.color, targetDirColor, lerpSpeed);
            directionalLight.intensity = Mathf.Lerp(directionalLight.intensity, targetDirInt, lerpSpeed);
        }
    }
}
