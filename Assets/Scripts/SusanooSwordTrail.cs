using UnityEngine;
using System.Collections.Generic;

public class SusanooSwordTrail : MonoBehaviour
{
    private Mesh mesh;
    private MeshFilter mf;
    private MeshRenderer mr;
    
    private Vector3 tipLocal;
    private Vector3 hiltLocal;

    private float trailTimer = 0f;
    // Rút ngắn lại để vệt chém gọn gàng hơn, không bị kéo thành một vòng cung quá dài
    private float trailDuration = 0.10f;  

    struct TrailPoint
    {
        public Vector3 tipPosition;
        public Vector3 hiltPosition;
        public float timeCreated;
    }

    private List<TrailPoint> points = new List<TrailPoint>();

    // TỐI ƯU HÓA: Dùng List tái sử dụng để chặn rác bộ nhớ (Garbage Collection) mỗi khung hình
    private List<Vector3> verticesList = new List<Vector3>();
    private List<Vector2> uvsList = new List<Vector2>();
    private List<Color> colorsList = new List<Color>();
    private List<int> trianglesList = new List<int>();

    void Start()
    {
        MeshFilter targetMf = GetComponent<MeshFilter>();
        hiltLocal = Vector3.zero;
        tipLocal = Vector3.forward;

        if (targetMf != null && targetMf.sharedMesh != null)
        {
            Vector3 center = targetMf.sharedMesh.bounds.center;
            float maxZ = targetMf.sharedMesh.bounds.max.z;
            
            // BÍ QUYẾT TẠO HÌNH LƯỠI LIỀM NINJA STORM 4:
            // Tuyệt đối không bắt đầu vệt chém từ chuôi kiếm (0f) vì sẽ tạo ra hình quạt khổng lồ thô kệch!
            // Vệt chém phủ khoảng 40% ngoài cùng của kiếm
            hiltLocal = new Vector3(center.x, center.y, maxZ * 0.60f); 
            tipLocal = new Vector3(center.x, center.y, maxZ * 1.05f); 
            
            if (maxZ <= 0) tipLocal.z = 1f; 
        }

        // TẠO OBJECT RIÊNG ĐỂ GIỮ CUSTOM MESH
        GameObject trailObj = new GameObject("CustomSwordTrailMesh");
        trailObj.transform.SetParent(this.transform, false);
        trailObj.transform.localPosition = Vector3.zero;
        trailObj.transform.localRotation = Quaternion.identity;

        mf = trailObj.AddComponent<MeshFilter>();
        mr = trailObj.AddComponent<MeshRenderer>();

        mesh = new Mesh();
        mf.mesh = mesh;

        // NÂNG CẤP ĐỒ HỌA: Dùng Shader Phát Sáng (Additive) để tạo hiệu ứng Năng lượng Chakra
        Shader additiveShader = Shader.Find("Legacy Shaders/Particles/Additive");
        if (additiveShader == null) additiveShader = Shader.Find("Mobile/Particles/Additive"); // Fallback
        if (additiveShader == null) additiveShader = Shader.Find("Sprites/Default"); // Tệ nhất thì xài lại bản cũ
        
        Material trailMat = new Material(additiveShader);
        trailMat.mainTexture = CreateSoftTexture();
        mr.material = trailMat;
        mr.sortingOrder = 10;
    }

    public void ActivateTrail(float duration)
    {
        // BÍ QUYẾT DIỆT LỖI GAI NHỌN: 
        // Nếu bắt đầu một nhát chém MỚI, phải xóa sạch dữ liệu của nhát chém cũ.
        // Tránh tình trạng nhát chém mới nối một đường thẳng khổng lồ vào nhát chém cũ đang tàn dư!
        if (trailTimer <= 0)
        {
            points.Clear();
            if (mesh != null) mesh.Clear();
        }
        trailTimer = duration;
    }

    private bool wasEmitting = true; // Kích hoạt chạy hàm lần đầu tiên để reset hạt về dạng tròn!

    void LateUpdate()
    {
        bool emitting = false;
        if (trailTimer > 0)
        {
            trailTimer -= Time.deltaTime;
            emitting = true;
        }

        // TỰ ĐỘNG CHUYỂN ĐỔI TRẠNG THÁI HẠT: DÃN (KHI CHÉM) VÀ TRÒN (KHI ĐỨNG YÊN)
        if (emitting != wasEmitting)
        {
            wasEmitting = emitting;
            // Dùng transform.root để quét toàn bộ nhân vật (tìm cả hạt của Lưỡi Kiếm lẫn Chắn Kiếm)
            ParticleSystem[] pss = transform.root.GetComponentsInChildren<ParticleSystem>();
            foreach(var ps in pss) 
            {
                if (ps.gameObject.name.Contains("SusanooEmbers")) {
                    var renderer = ps.GetComponent<ParticleSystemRenderer>();
                    var inheritVel = ps.inheritVelocity;
                    var vel = ps.velocityOverLifetime;

                    if (emitting) {
                        // KHI CHÉM: Tắt nhiễu loạn ngẫu nhiên, bật kế thừa gia tốc âm và ép dãn hạt
                        vel.enabled = false; 
                        inheritVel.enabled = true;
                        inheritVel.mode = ParticleSystemInheritVelocityMode.Initial;
                        inheritVel.curveMultiplier = -0.5f; // Lực đẩy lùi mạnh hơn để tạo tia lửa
                        
                        renderer.renderMode = ParticleSystemRenderMode.Stretch;
                        renderer.velocityScale = 0.15f; 
                        renderer.lengthScale = 1.0f;
                    } else {
                        // KHI ĐỨNG YÊN: Bật lại nhiễu loạn để lửa cuộn xoắn tự nhiên, đưa về dạng tròn
                        vel.enabled = true;
                        inheritVel.enabled = false;
                        renderer.renderMode = ParticleSystemRenderMode.Billboard;
                    }
                }
                else if (ps.gameObject.name == "SusanooSwordLightDots") {
                    var emission = ps.emission;
                    if (emitting) {
                        emission.rateOverTime = 300f; // Hạ mật độ theo yêu cầu để đỡ bị một đống dày đặc
                    } else {
                        emission.rateOverTime = 0f;
                    }
                }
            }
        }

        if (emitting)
        {
            points.Add(new TrailPoint()
            {
                tipPosition = transform.TransformPoint(tipLocal),
                hiltPosition = transform.TransformPoint(hiltLocal),
                timeCreated = Time.time
            });
        }

        // Hủy các điểm đã quá tuổi thọ (Tạo hiệu ứng đuôi mờ dần)
        while (points.Count > 0 && Time.time - points[0].timeCreated > trailDuration)
        {
            points.RemoveAt(0);
        }

        if (!emitting && points.Count == 0)
        {
            mesh.Clear();
            return;
        }

        BuildMesh();
    }

    void BuildMesh()
    {
        if (points.Count < 2)
        {
            mesh.Clear();
            return;
        }

        int widthSegments = 60; // Lưới độ phân giải cực cao để vẽ nhiễu (Noise) mượt mà
        int numVerticesPerSlice = widthSegments + 1;
        
        // Tái sử dụng bộ nhớ thay vì tạo mảng mới (Zero Allocation)
        verticesList.Clear();
        uvsList.Clear();
        colorsList.Clear();
        trianglesList.Clear();

        // TUYỆT KỸ UNREAL ENGINE: Nhiễu ngẫu nhiên (Perlin Noise Erosion)
        // Thay vì sóng răng cưa đều đặn, ta dùng nhiễu để tạo ra các vệt xói mòn hữu cơ (Organic).
        // Vệt chém là 1 lưới ĐẶC NGUYÊN KHỐI (Không có rãnh hở ở thân).

        for (int i = 0; i < points.Count; i++)
        {
            float age = Time.time - points[i].timeCreated;
            float relativePos = points.Count > 1 ? (float)i / (points.Count - 1) : 1f;
            
            // Vuốt toàn bộ vệt chém cong nhẹ về phía mũi kiếm (Tạo hình lưỡi liềm tổng thể)
            float globalWidthMult = Mathf.Pow(relativePos, 0.5f); 

            Vector3 baseHilt = mf.transform.InverseTransformPoint(points[i].hiltPosition);
            Vector3 baseTip = mf.transform.InverseTransformPoint(points[i].tipPosition);
            
            // Đáy của vệt chém bị bóp cong về phía Mũi Kiếm
            Vector3 dynamicHilt = Vector3.Lerp(baseTip, baseHilt, globalWidthMult);

            for (int w = 0; w <= widthSegments; w++)
            {
                float tWidth = (float)w / widthSegments; // 0 = Inner (Chuôi), 1 = Outer (Mũi)
                
                // --- BÍ QUYẾT TẠO SƯƠNG KHÓI NHƯ UNREAL ENGINE ---
                // Dùng Perlin Noise tạo xói mòn ngẫu nhiên, không đều đặn như Sine wave!
                // relativePos * 3f tạo ra sự kéo giãn nhẹ dọc theo chiều dài, giống như khói bị gió cuốn
                float noise = Mathf.PerlinNoise(tWidth * 15f, relativePos * 3f); 
                float wave = Mathf.Pow(noise, 2f); // Tương phản hóa nhiễu
                
                // Tuổi thọ cơ bản: Ngoài sống 0.10s, Trong bốc hơi ở 0.02s
                float baseDuration = Mathf.Lerp(0.02f, trailDuration, tWidth);
                
                // Áp dụng nhiễu: Đáy nhiễu chết cực nhanh (0.1x), Đỉnh sống thọ (1.0x)
                float waveInfluence = Mathf.Pow(1f - tWidth, 0.5f); 
                float durationMultiplier = Mathf.Lerp(1f, Mathf.Lerp(0.1f, 1.0f, wave), waveInfluence);
                
                float finalDuration = baseDuration * durationMultiplier;

                // Tính toán Alpha dựa trên tuổi thọ CỦA RIÊNG ĐIỂM ẢNH NÀY!
                float timeAlpha = 1f - (age / finalDuration);
                if (timeAlpha < 0) timeAlpha = 0;
                
                float localAlpha = timeAlpha * Mathf.Pow(relativePos, 0.5f);

                // HỆ MÀU VOLUMETRIC (Chuẩn AAA): Lõi trắng siêu mảnh, Hào quang tím rực rỡ
                Color deepPurple = new Color(3.5f, 0f, 6.0f, localAlpha); // Tím neon cực gắt
                Color hotWhite = new Color(5f, 5f, 5f, localAlpha); // Trắng cháy sáng
                
                // Lũy thừa 12 ép màu Trắng CHỈ xuất hiện ở 5% ngoài cùng, 95% còn lại là luồng khí tím
                float whiteSharpness = 12f;
                Color vColor = Color.Lerp(deepPurple, hotWhite, Mathf.Pow(tWidth, whiteSharpness)); 

                if (i == points.Count - 1 && trailTimer > 0) vColor = Color.white;

                verticesList.Add(Vector3.Lerp(dynamicHilt, baseTip, tWidth));
                uvsList.Add(new Vector2(relativePos, tWidth));
                colorsList.Add(vColor);
            }
            
            if (i < points.Count - 1)
            {
                for (int w = 0; w < widthSegments; w++)
                {
                    int v0 = (i * numVerticesPerSlice) + w;
                    int v1 = v0 + 1;
                    int v2 = v0 + numVerticesPerSlice;
                    int v3 = v2 + 1;

                    trianglesList.Add(v0);
                    trianglesList.Add(v1);
                    trianglesList.Add(v2);
                    trianglesList.Add(v1);
                    trianglesList.Add(v3);
                    trianglesList.Add(v2);
                }
            }
        }

        mesh.Clear();
        mesh.SetVertices(verticesList);
        mesh.SetUVs(0, uvsList);
        mesh.SetColors(colorsList);
        mesh.SetTriangles(trianglesList, 0);
        mesh.RecalculateBounds();
    }

    Texture2D CreateSoftTexture()
    {
        int size = 64;
        Texture2D tex = new Texture2D(size, size, TextureFormat.ARGB32, false);
        
        // Khóa WrapMode để chặn tuyệt đối hiện tượng viền đen (UV Bleeding) ở các mép
        tex.wrapMode = TextureWrapMode.Clamp; 
        
        for (int y = 0; y < size; y++)
        {
            // y = 0 (chuôi kiếm), y = 63 (mũi kiếm)
            // Chuôi kiếm mờ dần vào không khí, nhưng Mũi Kiếm phải RỰC SÁNG VÀ SẮC LẸM
            float alphaY = (y / (float)(size - 1)); 
            for (int x = 0; x < size; x++)
            {
                tex.SetPixel(x, y, new Color(1f, 1f, 1f, Mathf.Pow(alphaY, 0.5f)));
            }
        }
        tex.Apply();
        return tex;
    }
}
