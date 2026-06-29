using UnityEngine;

public class SusanooVFX : MonoBehaviour
{
    [Header("Smoke Settings")]
    [Range(0f, 1f)] public float smokeOpacity = 0.2f;
    [Range(0.1f, 2f)] public float smokeHeightScale = 0.5f;

    private Transform targetBone;
    private Transform spineBone;
    private GameObject auraParticles;

    void Start()
    {
        // 1. TÍNH TOÁN BÙ TRỪ TỶ LỆ CỦA FBX (THƯỜNG BỊ THU NHỎ 0.01 LẦN)
        float scaleFactor = 1f;
        if (transform.lossyScale.y > 0)
        {
            scaleFactor = 1f / transform.lossyScale.y; // VD: Nếu Susanoo scale 0.01, factor = 100
        }

        // BÍ QUYẾT TỐI THƯỢNG: ÁP DỤNG SHADER DÒNG CHẢY NĂNG LƯỢNG VÀO TOÀN BỘ CƠ THỂ
        Shader energyShader = Shader.Find("Custom/SusanooEnergyFlow");
        if (energyShader != null)
        {
            SkinnedMeshRenderer[] smrs = GetComponentsInChildren<SkinnedMeshRenderer>();
            foreach (var meshRenderer in smrs)
            {
                    // BÍ QUYẾT TỐI THƯỢNG: Đóng băng tọa độ gốc (Rest Pose) vào UV2 (kênh TEXCOORD1)
                    // Việc này giúp Shader luôn có tọa độ 3D chuẩn xác bất kể khi cánh đập hay xương di chuyển!
                    Mesh mesh = meshRenderer.sharedMesh;
                    if (mesh != null)
                    {
                        if (mesh.isReadable)
                        {
                            System.Collections.Generic.List<Vector3> verts = new System.Collections.Generic.List<Vector3>();
                            mesh.GetVertices(verts);
                            mesh.SetUVs(1, verts); // Lưu vào kênh UV2 (TEXCOORD1)
                        }
                        else
                        {
                            Debug.LogWarning("Mesh is not readable! Noise might swim. Please enable Read/Write in FBX.");
                        }
                    }

                    // Gọi .materials sẽ tự động tạo bản sao (instance) an toàn cho vật liệu
                    foreach (Material mat in meshRenderer.materials)
                    {
                        mat.shader = energyShader;
                        
                        // Kích hoạt texture gốc nếu có (giữ lại vân áo giáp)
                        if (mat.HasProperty("_MainTex") && mat.GetTexture("_MainTex") != null)
                        {
                            mat.SetFloat("_HasMap", 1.0f);
                            mat.SetFloat("_ModelScale", scaleFactor);
                        }
                        else
                        {
                            mat.SetFloat("_HasMap", 0.0f);
                            mat.SetFloat("_ModelScale", scaleFactor);
                        }
                    }
            }
        }

        // 2. ÁNH SÁNG TÍM MA MỊ (Bê nguyên thông số PointLight từ ThreeJS sang)
        Light pointLight = gameObject.AddComponent<Light>();
        pointLight.type = LightType.Point;
        pointLight.color = new Color(0.54f, 0f, 1f); // Mã màu 0x8A00FF chuẩn xác
        pointLight.range = 120f; // Bán kính 120 giống hệt ThreeJS
        pointLight.intensity = 8f; // Ép cường độ hắt sáng mạnh lên sàn nhà
        pointLight.shadows = LightShadows.None; // Tắt đổ bóng để tránh lỗi tràn bộ nhớ Shadow Atlas của Unity

        // 3. TÌM XƯƠNG WAIST VÀ HEAD CHUẨN XÁC
        // Quét toàn bộ GameObject thay vì chỉ SkinnedMesh để không bao giờ trượt
        targetBone = FindBoneByName(this.transform, new string[] { "waist", "hips", "pelvis" });
        SkinnedMeshRenderer smr = GetComponentInChildren<SkinnedMeshRenderer>();
        if (targetBone == null && smr != null) targetBone = smr.rootBone;
        if (targetBone == null) targetBone = this.transform;

        spineBone = FindBoneByName(this.transform, new string[] { "head", "neck" });

        // 4. KHÓI VÀ TÀN LỬA LINH HỒN (Bao bọc toàn thân)
        auraParticles = new GameObject("SusanooSmoke");
        auraParticles.transform.SetParent(this.transform, false);
        
        // Đặt hạt khói ở tâm mô hình
        auraParticles.transform.localPosition = Vector3.zero;
        // BÍ QUYẾT 1: KHÔNG XOAY OBJECT. Việc xoay -90 độ lúc trước đã làm trục Y (trục bay lên) bị bẻ ngang ra phía sau!
        // Giữ nguyên identity để trục Y luôn hướng thẳng đứng lên trời.
        auraParticles.transform.localRotation = Quaternion.identity;
        
        // Ép chết tỷ lệ thu phóng quỹ đạo khói (Y, Z) theo ý bạn ngay từ lúc sinh ra để không bị mất khi thoát Play Mode
        auraParticles.transform.localScale = new Vector3(1f, smokeHeightScale, 1f);
        
        ParticleSystem ps = auraParticles.AddComponent<ParticleSystem>();
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear); 

        var main = ps.main;
        main.duration = 2f;
        main.loop = true;
        main.startLifetime = 1.75f; 
        
        // Lực chính: Phóng thẳng theo trục Z của Cone (ôm sát cột sống)
        // Giảm tốc độ từ 10-18 xuống 5-9 để khói chỉ bay đến vừa qua khỏi đầu rồi tan biến, thay vì vọt lên tận trời
        main.startSpeed = new ParticleSystem.MinMaxCurve(5f, 9f); 
        main.startSize = 5f; 
        
        // Đồng bộ màu gốc của khói giống hệt với Energy Color (Màu Tím Pastel)
        main.startColor = new Color(160f/255f, 84f/255f, 253f/255f, 1f); 
        
        main.simulationSpace = ParticleSystemSimulationSpace.Local; 
        // BÍ QUYẾT: Đổi từ Hierarchy sang Shape! 
        // Giờ đây khi bạn dùng phím R để thu phóng (Scale) vùng phát khói (giới hạn quỹ đạo Y, Z), 
        // các hạt khói tròn vẫn giữ nguyên hình dáng 100% mà KHÔNG BAO GIỜ BỊ MÓP MÉO!
        main.scalingMode = ParticleSystemScalingMode.Shape;

        var emission = ps.emission;
        emission.rateOverTime = 120f; 

        // BÍ QUYẾT 3: CĂN CHỈNH KHUNG HÌNH TRỤ (CYLINDER)
        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Cone;
        shape.radius = 5.5f * scaleFactor; 
        shape.angle = 0f; 
        
        // Kéo gốc của ống trụ lùi sâu xuống tận -7 dọc theo trục Z (hướng xuống chân).
        // Điều này đảm bảo toàn bộ đôi chân Susanoo đều được đắm chìm trong làn khói ma mị!
        shape.position = new Vector3(0, 0, -7f * scaleFactor);

        // Lực phụ: Dù bay theo cột sống, khói vẫn sẽ hơi bốc nhẹ lên trời (World Y)
        var vel = ps.velocityOverLifetime;
        vel.enabled = true;
        vel.space = ParticleSystemSimulationSpace.World;
        vel.x = new ParticleSystem.MinMaxCurve(0f, 0f);
        vel.y = new ParticleSystem.MinMaxCurve(3.0f, 7.0f); 
        vel.z = new ParticleSystem.MinMaxCurve(0f, 0f);

        // Độ mờ: Xóa bỏ lõi trắng rực rỡ ở chân, đồng bộ toàn bộ khói thành màu tím mờ ảo
        var colorOverLifetime = ps.colorOverLifetime;
        colorOverLifetime.enabled = true;
        Gradient grad = new Gradient();
        grad.SetKeys(
            new GradientColorKey[] { new GradientColorKey(new Color(0.6f, 0.2f, 1.0f), 0.0f), new GradientColorKey(new Color(0.4f, 0f, 0.8f), 1.0f) },
            // Liên kết thẳng với thanh trượt smokeOpacity trên Inspector
            new GradientAlphaKey[] { new GradientAlphaKey(0.0f, 0.0f), new GradientAlphaKey(smokeOpacity, 0.2f), new GradientAlphaKey(0.0f, 1.0f) }
        );
        colorOverLifetime.color = grad;

        // Khói to dần lên 2.5x khi bay lên để tạo thành một khối hào quang khổng lồ
        // Khói chỉ phình to nhẹ (1.5x) khi bay lên để giữ form dáng ôm sát cơ thể
        var sizeOverLifetime = ps.sizeOverLifetime;
        sizeOverLifetime.enabled = true;
        AnimationCurve curve = new AnimationCurve(new Keyframe(0f, 1f), new Keyframe(1f, 1.5f)); 
        sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1.0f, curve);

        var renderer = ps.GetComponent<ParticleSystemRenderer>();
        // NÂNG CẤP ĐỒ HỌA: Dùng Additive Shader chuyên dụng vừa tạo để khói tuyệt đối không bị dính viền đen
        Shader addShader = Shader.Find("Custom/AdditiveParticle");
        
        Material auraMat = new Material(addShader);
        auraMat.mainTexture = CreateSoftTexture();
        renderer.material = auraMat;

        ps.Play(); 
    }

    // Thuật toán quét đa năng tìm xương theo danh sách tên
    Transform FindBoneByName(Transform current, string[] names)
    {
        string n = current.name.ToLower();
        foreach (string name in names)
        {
            if (n.Contains(name)) return current;
        }
            
        foreach (Transform child in current)
        {
            Transform found = FindBoneByName(child, names);
            if (found != null) return found;
        }
        return null;
    }

    void LateUpdate()
    {
        if (auraParticles != null && targetBone != null)
        {
            auraParticles.transform.position = targetBone.position;

            Vector3 spineDir = this.transform.up; 
            
            if (spineBone != null)
            {
                // Vector nối từ WAIST lên HEAD có độ nghiêng tuyệt đối chuẩn xác
                spineDir = (spineBone.position - targetBone.position).normalized;
            }
            
            if (spineDir != Vector3.zero)
            {
                // BÍ QUYẾT TỐI THƯỢNG: Hình nón (Cone) phun khói dọc theo trục Z của nó!
                // Vì vậy, ta phải dùng LookRotation để ép trục Z hướng thẳng theo Cột sống (spineDir).
                // Lỗi "thẳng đuột" lúc trước là do dùng FromToRotation để xoay trục Y, khiến Cone bị sai trục!
                auraParticles.transform.rotation = Quaternion.LookRotation(spineDir);
            }
        }
    }

    Texture2D CreateSoftTexture()
    {
        int size = 64;
        Texture2D tex = new Texture2D(size, size, TextureFormat.ARGB32, false);
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dist = Vector2.Distance(new Vector2(x, y), new Vector2(size/2f, size/2f));
                float alpha = Mathf.Clamp01(1f - (dist / (size/2f)));
                tex.SetPixel(x, y, new Color(1f, 1f, 1f, Mathf.Pow(alpha, 1.5f)));
            }
        }
        tex.Apply();
        return tex;
    }
}
