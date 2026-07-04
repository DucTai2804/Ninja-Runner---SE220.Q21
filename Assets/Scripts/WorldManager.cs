using UnityEngine;

public class WorldManager : MonoBehaviour
{
    public static WorldManager Instance;

    [Header("Speed Settings")]
    public float baseSpeed = 15f;       // Tốc độ ban đầu
    public float maxSpeed = 40f;        // Tốc độ tối đa
    public float acceleration = 0.2f;   // Gia tốc (Tăng độ khó dần đều)

    [HideInInspector] 
    public float currentSpeed;

    [Header("Lighting Settings")]
    [Range(0f, 3f)]
    public float sunIntensity = 1.0f;
    [Range(0f, 2f)]
    public float ambientIntensityMultiplier = 1.0f;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
        
        currentSpeed = baseSpeed;

        // Tự động thêm hệ thống quản lý môi trường nếu chưa có trên scene
        if (FindObjectOfType<GroundScroller>() == null)
        {
            GameObject groundObj = new GameObject("GroundSystem");
            groundObj.AddComponent<GroundScroller>();
        }

        if (FindObjectOfType<TerrainManager>() == null)
        {
            GameObject terrainObj = new GameObject("TerrainSystem");
            terrainObj.AddComponent<TerrainManager>();
        }

        ApplyThreeJsLighting();
    }

    private void ApplyThreeJsLighting()
    {
        // Tăng khoảng cách vẽ bóng (Mặc định Unity chỉ vẽ bóng trong bán kính 50m)
        // Lưu ý: Nếu bóng vẫn không hiện ra xa như 200m, thì do Asset URP đang ghi đè.
        // Cần vào Project Settings -> Graphics -> URP Asset để sửa Shadow Distance thành 200.
        QualitySettings.shadowDistance = 200f;

        // 1. Chỉnh góc máy quay và màu nền Camera (Bầu trời)
        Camera cam = Camera.main;
        if (cam != null)
        {
            // Đồng bộ góc phối cảnh y hệt Three.js (core.js)
            cam.fieldOfView = 60f;
            cam.transform.position = new Vector3(0f, 6f, -9f); // Z = 9 bên Threejs tương đương -9 bên Unity
            cam.transform.LookAt(new Vector3(0f, 0f, 10f));    // LookAt -10 bên Threejs tương đương 10 bên Unity
            
            cam.clearFlags = CameraClearFlags.SolidColor;
            ColorUtility.TryParseHtmlString("#88AACC", out Color skyColor);
            cam.backgroundColor = skyColor;
        }

        // 2. Chỉnh Directional Light (Ánh sáng mặt trời) — Giữ nguyên 89.9 độ
        Light[] lights = FindObjectsOfType<Light>();
        foreach (var l in lights)
        {
            if (l.type == LightType.Directional)
            {
                ColorUtility.TryParseHtmlString("#FFEEDD", out Color sunColor);
                l.color = sunColor;
                l.intensity = sunIntensity; 
                l.transform.rotation = Quaternion.Euler(89.9f, 0f, 0f); 
                l.shadows = LightShadows.Soft; 
                break;
            }
        }

        // 3. Chỉnh Ánh sáng môi trường (Tương đương HemisphereLight + AmbientLight bên Three.js)
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight; // Gradient
        
        ColorUtility.TryParseHtmlString("#FFFFFF", out Color skyAmbient);
        RenderSettings.ambientSkyColor = skyAmbient * ambientIntensityMultiplier; 
        
        ColorUtility.TryParseHtmlString("#99AA99", out Color equatorAmbient);
        RenderSettings.ambientEquatorColor = equatorAmbient * ambientIntensityMultiplier;

        ColorUtility.TryParseHtmlString("#445544", out Color groundAmbient);
        RenderSettings.ambientGroundColor = groundAmbient * ambientIntensityMultiplier;
        
        DynamicGI.UpdateEnvironment(); // Cập nhật lại bầu trời để Ambient Light có tác dụng ngay lập tức
    }

    void Update()
    {
        // Tự động tăng tốc độ game theo thời gian
        if (currentSpeed < maxSpeed)
        {
            currentSpeed += acceleration * Time.deltaTime;
        }
    }
}
