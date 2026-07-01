using UnityEngine;

public class SusanooVFX : MonoBehaviour
{
    private ParticleSystem guardEmberPS;
    private Mesh guardEmberMesh;
    private Vector3 guardCentroid;

    private Transform targetBone;
    private Transform spineBone;
    private GameObject auraParticles;
    
    private float scaleFactor = 1f;

    void Start()
    {
        // 1. TÍNH TOÁN BÙ TRỪ TỶ LỆ CỦA FBX (THƯỜNG BỊ THU NHỎ 0.01 LẦN)
        if (transform.lossyScale.y > 0)
        {
            scaleFactor = 1f / transform.lossyScale.y; // VD: Nếu Susanoo scale 0.01, factor = 100
        }

        SetupSusanooShader();
    }

    void SetupSusanooShader()
    {
        // 1. TÌM VÀ THAY THẾ SHADER CHO TOÀN BỘ NHÂN VẬT
        Shader energyShader = Shader.Find("Custom/SusanooEnergyFlow");
        if (energyShader != null)
        {
            SkinnedMeshRenderer[] smrs = GetComponentsInChildren<SkinnedMeshRenderer>();
            int globalMatIndex = 0; // Biến đếm thứ tự Material để nhận diện Cánh (Vật liệu đầu tiên)
            
            foreach (var meshRenderer in smrs)
            {
                    // BÍ QUYẾT TỐI THƯỢNG: Đóng băng tọa độ gốc (Rest Pose) vào UV2 (kênh TEXCOORD1)
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

                    // Quét toàn bộ vật liệu
                    Material[] mats = meshRenderer.materials;
                    for (int i = 0; i < mats.Length; i++)
                    {
                        Material mat = mats[i];
                        mat.shader = energyShader;
                        
                        // Truyền màu gốc sang Base Color của Shader mới
                        if (mat.HasProperty("_Color"))
                            mat.SetColor("_BaseColor", mat.color);
                        
                        // Khóa cứng thông số như người dùng yêu cầu
                        mat.SetFloat("_NoiseScale", 10f); 
                        mat.SetFloat("_FlowSpeed", 6f); 
                        
                        // NHẬN DIỆN CHÍNH XÁC VẬT LIỆU CỦA CÁNH
                        string matName = mat.name.ToLower();
                        // Cánh là material có tên chuẩn "t_chr2130_wing_c"
                        bool isWing = matName.Contains("t_chr2130_wing_c");
                        
                        if (isWing) {
                            // Cánh: Sáng hơn một chút (-0.15)
                            mat.SetColor("_Color2", new Color(0.737f, 0.550f, 0.901f, 1.0f));
                        } else {
                            // Các Material còn lại (Thân, Kiếm): Ma mị và tối tăm hơn (-0.4)
                            mat.SetColor("_Color2", new Color(0.469f, 0.350f, 0.574f, 1.0f));
                        }
                        
                        mat.SetFloat("_CoreColorStrength", 1.5f);
                        
                        // Kích hoạt texture gốc nếu có
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
                        
                        globalMatIndex++;
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
        
        // Khóa cứng tỷ lệ thu phóng quỹ đạo khói (Y, Z) theo mức chuẩn
        auraParticles.transform.localScale = new Vector3(1f, 0.5f, 1f);
        
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
            // Khóa cứng độ mờ (Opacity) chuẩn
            new GradientAlphaKey[] { new GradientAlphaKey(0.0f, 0.0f), new GradientAlphaKey(0.2f, 0.2f), new GradientAlphaKey(0.0f, 1.0f) }
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

        SetupSwordVFX();
        SetupBodyLightDots();
        SetupSwordLightDots();

        ps.Play(); 
    }

    void SetupSwordLightDots()
    {
        Transform blade = FindBoneByName(this.transform, new string[] { "sword_blade" });
        if (blade != null)
        {
            GameObject swordDots = new GameObject("SusanooSwordLightDots");
            swordDots.transform.SetParent(blade, false);
            swordDots.transform.localPosition = Vector3.zero;
            swordDots.transform.localRotation = Quaternion.identity;
            swordDots.transform.localScale = Vector3.one;

            ParticleSystem ps = swordDots.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            
            var main = ps.main;
            main.duration = 1.0f;
            main.loop = true;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.3f, 0.6f);
            
            // ĐỒNG BỘ KÍCH THƯỚC VÀ SCALING MODE VỚI ĐỐM SÁNG CƠ THỂ
            main.startSize = new ParticleSystem.MinMaxCurve(1.5f * scaleFactor, 2.5f * scaleFactor); 
            main.scalingMode = ParticleSystemScalingMode.Shape;
            
            main.startColor = new Color(1f, 119f/255f, 1f, 1f); 
            main.simulationSpace = ParticleSystemSimulationSpace.World; 
            main.maxParticles = 500;
            
            var emission = ps.emission;
            emission.rateOverTime = 0f; 
            
            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.MeshRenderer;
            MeshRenderer mr = blade.GetComponent<MeshRenderer>();
            if (mr == null) mr = blade.GetComponentInChildren<MeshRenderer>();
            if (mr != null) {
                shape.meshRenderer = mr;
                shape.meshShapeType = ParticleSystemMeshShapeType.Triangle;
            } else {
                shape.shapeType = ParticleSystemShapeType.Box;
                shape.scale = new Vector3(0.2f, 5f, 0.2f); 
                shape.position = new Vector3(0, 2.5f, 0);
            }
            
            var inheritVel = ps.inheritVelocity;
            inheritVel.enabled = false;
            
            main.startSpeed = new ParticleSystem.MinMaxCurve(3f * scaleFactor, 8f * scaleFactor);
            
            var vel = ps.velocityOverLifetime;
            vel.enabled = false;
            
            var renderer = ps.GetComponent<ParticleSystemRenderer>();
            renderer.renderMode = ParticleSystemRenderMode.Billboard; 
            
            Shader addShader = Shader.Find("Custom/AdditiveParticle");
            if (addShader == null) addShader = Shader.Find("Legacy Shaders/Particles/Additive");
            Material dotMat = new Material(addShader);
            dotMat.mainTexture = CreateSoftTexture();
            renderer.material = dotMat;
            
            ps.Play();
        }
    }

    void SetupBodyLightDots()
    {
        GameObject bodyDots = new GameObject("SusanooBodyLightDots");
        bodyDots.transform.SetParent(this.transform, false);
        bodyDots.transform.localPosition = Vector3.zero;
        bodyDots.transform.localRotation = Quaternion.identity;
        bodyDots.transform.localScale = Vector3.one;
        
        ParticleSystem ps = bodyDots.AddComponent<ParticleSystem>();
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        
        var main = ps.main;
        main.duration = 1.0f;
        main.loop = true;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.8f, 1.2f);
        // Tốc độ gốc y hệt Khói
        main.startSpeed = new ParticleSystem.MinMaxCurve(5f, 9f); 
        main.startSize = new ParticleSystem.MinMaxCurve(1.5f * scaleFactor, 2.5f * scaleFactor); 
        main.startColor = new Color(1f, 119f/255f, 1f, 1f); 
        main.simulationSpace = ParticleSystemSimulationSpace.World; 
        
        main.scalingMode = ParticleSystemScalingMode.Shape;
        
        var emission = ps.emission;
        emission.rateOverTime = 150f; 
        
        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Cone;
        shape.angle = 25f;
        shape.radius = 5.5f * scaleFactor; 
        
        // BÍ QUYẾT: Trục Z của Susanoo là hướng về phía trước mặt. 
        // Xoay Nón ngửa lên trời (Local Y = -90 độ trục X).
        // Đặt Y = 5 để tâm phát hạt nằm đúng vị trí vùng bụng/ngực, tránh bị bay quá cao lên trên đầu.
        shape.position = new Vector3(0, 5f * scaleFactor, -1f * scaleFactor); 
        shape.rotation = new Vector3(-90f, 0f, 0f);
        
        var vel = ps.velocityOverLifetime;
        vel.enabled = true;
        vel.space = ParticleSystemSimulationSpace.World; 
        // Lực World Y y hệt Khói giúp đốm sáng bốc lên không trung
        vel.x = new ParticleSystem.MinMaxCurve(-1.5f, 1.5f);
        vel.y = new ParticleSystem.MinMaxCurve(3.0f, 7.0f);
        vel.z = new ParticleSystem.MinMaxCurve(-1.5f, 1.5f);
        
        var renderer = ps.GetComponent<ParticleSystemRenderer>();
        renderer.renderMode = ParticleSystemRenderMode.Billboard; 
        
        Shader addShader = Shader.Find("Custom/AdditiveParticle");
        if (addShader == null) addShader = Shader.Find("Legacy Shaders/Particles/Additive");
        Material dotMat = new Material(addShader);
        dotMat.mainTexture = CreateSoftTexture();
        renderer.material = dotMat;
        
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

    Vector3 CalculateCentroid(Mesh mesh)
    {
        if (mesh == null) return Vector3.zero;
        Vector3[] vertices = mesh.vertices;
        if (vertices.Length == 0) return Vector3.zero;
        
        Vector3 centroid = Vector3.zero;
        foreach (Vector3 v in vertices)
        {
            centroid += v;
        }
        return centroid / vertices.Length;
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
                auraParticles.transform.rotation = Quaternion.LookRotation(spineDir);
            }
        }
    }

    void SetupSwordVFX()
    {
        SetupSwordGuardVFX();
        SetupSwordBladeVFX();
    }

    void SetupSwordGuardVFX()
    {
        // 1. CHẮN KIẾM (Sword Guard) - CHỈ CÒN LẠI HỆ THỐNG HẠT LỬA
        Transform guard = FindBoneByName(this.transform, new string[] { "sword_guard" });
        if (guard != null)
        {
            Renderer r = guard.GetComponent<Renderer>();
            if (r != null)
            {
                // ẨN MÔ HÌNH CHẮN KIẾM GỐC!
                r.enabled = false;
                
                // DỌN DẸP RÁC TỪ CÁC LẦN TRƯỚC
                if (r.materials.Length > 1) {
                    Material[] cleanMats = new Material[1];
                    cleanMats[0] = r.materials[0]; 
                    r.materials = cleanMats;
                }
                
                // GỌI HỆ THỐNG HẠT LỬA BỌC QUANH CHẮN KIẾM! (Chế độ độc lập dành riêng cho chắn kiếm)
                CreateGuardEmberParticles(guard);
            }
        }
    }

    void SetupSwordBladeVFX()
    {
        Shader coreShader = Shader.Find("Custom/SwordCore");
        Shader auraShader = Shader.Find("Custom/SwordAura");
        if (coreShader == null || auraShader == null) return;
        Material coreMat = new Material(coreShader);
        Material auraMat = new Material(auraShader);

        // 2. LƯỠI KIẾM (Sword Blade)
        Transform blade = FindBoneByName(this.transform, new string[] { "sword_blade" });
        if (blade != null)
        {
            Renderer r = blade.GetComponent<Renderer>();
            if (r != null)
            {
                MeshFilter mf = blade.GetComponent<MeshFilter>();
                float maxRadius = 0.05f;
                Vector3 center = Vector3.zero;
                float boundsMinZ = 0.0f;
                if (mf != null && mf.sharedMesh != null) {
                    maxRadius = Mathf.Max(mf.sharedMesh.bounds.extents.x, mf.sharedMesh.bounds.extents.y);
                    center = mf.sharedMesh.bounds.center;
                    boundsMinZ = mf.sharedMesh.bounds.min.z;
                }
                
                // ẨN KIẾM GỐC ĐI NHƯ CŨ ĐỂ HIỂN THỊ HÀO QUANG:
                Renderer originalRend = blade.GetComponent<Renderer>();
                if (originalRend != null)
                {
                    originalRend.enabled = false; 
                }

                // Gộp 2 lớp vào 1 cục (auraObj) để dễ quản lý
                GameObject auraObj = new GameObject("SusanooAura");
                auraObj.transform.SetParent(blade, false);
                // ÉP BUỘC ĐỒNG BỘ HÓA TỌA ĐỘ VÀ TỶ LỆ VỚI XƯƠNG GỐC: Tránh lỗi thanh kiếm biến thành khổng lồ
                auraObj.transform.localPosition = Vector3.zero;
                auraObj.transform.localRotation = Quaternion.identity;
                auraObj.transform.localScale = Vector3.one;
                
                // KHÔI PHỤC LẠI THÔNG SỐ SHADER BỊ MẤT ĐỂ TẠO SÓNG GỢN:
                auraMat.SetFloat("_MeshScale", 2.0f); // Phóng to lớp vỏ
                auraMat.SetFloat("_BoundsMinZ", boundsMinZ); 
                auraMat.SetFloat("_Thickness", maxRadius); 
                auraMat.SetVector("_BoundsCenter", new Vector4(center.x, center.y, center.z, 0));

                // --- Pass 1: Lõi Năng Lượng (Bên trong) ---
                GameObject coreObj = new GameObject("CoreWhite");
                coreObj.transform.SetParent(auraObj.transform, false);
                coreObj.transform.localPosition = Vector3.zero;
                coreObj.transform.localRotation = Quaternion.identity;
                coreObj.transform.localScale = Vector3.one;

                MeshFilter coreMf = coreObj.AddComponent<MeshFilter>();
                coreMf.sharedMesh = mf.sharedMesh;
                MeshRenderer coreMr = coreObj.AddComponent<MeshRenderer>();
                Material coreMat_dynamic = new Material(auraMat);
                // Khôi phục thông số cho Lõi
                coreMat_dynamic.SetColor("_Color", new Color(1.0f, 1.0f, 1.0f, 1.0f));
                coreMat_dynamic.SetFloat("_MeshScale", 1.0f); 
                coreMat_dynamic.SetFloat("_ZStretch", 0.0f); 
                coreMr.material = coreMat_dynamic;

                // --- Pass 2: Vỏ Năng Lượng Tím (Bên ngoài) ---
                GameObject shellObj = new GameObject("ShellPurple");
                shellObj.transform.SetParent(auraObj.transform, false);
                MeshFilter shellMf = shellObj.AddComponent<MeshFilter>();
                shellMf.sharedMesh = mf.sharedMesh;
                MeshRenderer shellMr = shellObj.AddComponent<MeshRenderer>();
                shellMr.material = auraMat;
                
                // ĐÃ BẬT LẠI DÒNG CHẢY NĂNG LƯỢNG KÉP (TRẮNG VÀ TÍM)
                // (Tôi đã xóa lệnh ẩn auraObj ở đây)

                // DỌN DẸP RÁC TỪ CÁC LẦN TRƯỚC: Nếu thanh kiếm đang bị dính 2 Material (do code cũ lưu vào Scene), ta gọt đi chỉ chừa lại Material gốc đầu tiên!
                if (r.materials.Length > 1) {
                    Material[] cleanMats = new Material[1];
                    cleanMats[0] = r.materials[0]; // Chỉ giữ lại vật liệu gốc (vỏ kiếm)
                    r.materials = cleanMats;
                }

                // GỌI LẠI HỆ THỐNG HẠT LỬA! (Vừa nãy tôi lỡ tay gạch nhầm dòng này)
                CreateBladeEmberParticles(blade);
            }
        }
        
        // 3. CHUÔI KIẾM (Sword Hilt) - Bọc lại năng lượng
        Transform hilt = FindBoneByName(this.transform, new string[] { "sword_hilt" });
        if (hilt != null)
        {
            Renderer r = hilt.GetComponent<Renderer>();
            if (r != null)
            {
                foreach (Material mat in r.materials)
                {
                    if (mat.shader.name != "Custom/SusanooEnergyFlow")
                    {
                        Shader energyShader = Shader.Find("Custom/SusanooEnergyFlow");
                        if (energyShader != null) mat.shader = energyShader;
                    }
                    
                    if (mat.HasProperty("_FlowSpeed"))
                    {
                        float currentSpeed = mat.GetFloat("_FlowSpeed");
                        mat.SetFloat("_FlowSpeed", -Mathf.Abs(currentSpeed)); // Ép chảy ngược lại
                        
                        // Chỉnh Cường độ sáng (Intensity) theo yêu cầu
                        Color color1 = new Color(0.627f, 0.329f, 0.992f, 1.0f);
                        Color color2 = new Color(1.0f, 1.0f, 1.0f, 1.0f);
                        mat.SetColor("_Color1", color1 * 1.4f);
                        mat.SetColor("_Color2", color2 * 2.0f);
                        
                        // Chỉnh Noise Scale lên 10
                        mat.SetFloat("_NoiseScale", 10f);
                    }
                }
            }
        }
// TẠO HIỆU ỨNG TÀN TRO BAY LƠ LỬNG
    }

    void CreateGuardEmberParticles(Transform guard)
    {
        if (guard.Find("SusanooEmbersGuard") != null) return;
        Mesh sharedMesh = guard.GetComponent<MeshFilter>().sharedMesh;
        GameObject pObj = new GameObject("SusanooEmbersGuard");
        pObj.transform.SetParent(guard, false);
        pObj.transform.localPosition = Vector3.zero;
        pObj.transform.localRotation = Quaternion.identity;
        pObj.transform.localScale = Vector3.one; 
        
        ParticleSystem ps = pObj.AddComponent<ParticleSystem>();
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        var main = ps.main;
        main.duration = 1.0f;
        main.loop = true;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.15f, 0.3f);
        main.startSpeed = 0f; 
        main.scalingMode = ParticleSystemScalingMode.Shape; 
        main.startSize = new ParticleSystem.MinMaxCurve(0.4f, 0.8f); 
        main.startColor = new Color(2.0f, 0.5f, 4.0f, 0.2f); 
        main.simulationSpace = ParticleSystemSimulationSpace.Local; 
        main.maxParticles = 5000;
        
        var emission = ps.emission;
        emission.rateOverTime = 5000f; 
        
        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Mesh;
        shape.mesh = sharedMesh;
        shape.meshShapeType = ParticleSystemMeshShapeType.Triangle;
        
        // CẤU HÌNH DÀNH RIÊNG CHO CHẮN KIẾM (Khóa cứng scale và tự động định tâm)
        float sx = 1.2f, sy = 1.2f, sz = 1.2f;
        shape.scale = new Vector3(sx, sy, sz);
        
        Vector3 centroid = CalculateCentroid(sharedMesh);
        shape.position = new Vector3(
            -centroid.x * (sx - 1.0f) / sx,
            -centroid.y * (sy - 1.0f) / sy,
            -centroid.z * (sz - 1.0f) / sz
        );
        shape.rotation = Vector3.zero;
        
        var inheritVel = ps.inheritVelocity;
        inheritVel.enabled = true;
        inheritVel.mode = ParticleSystemInheritVelocityMode.Initial;
        inheritVel.curveMultiplier = 1.0f;
        var vel = ps.velocityOverLifetime;
        vel.enabled = true; 
        vel.space = ParticleSystemSimulationSpace.Local; 
        vel.x = new ParticleSystem.MinMaxCurve(-1.5f, 1.5f); 
        vel.y = new ParticleSystem.MinMaxCurve(-1.5f, 1.5f); 
        vel.z = new ParticleSystem.MinMaxCurve(-1.0f, 1.0f); 
        
        var noise = ps.noise;
        noise.enabled = true;
        noise.strength = 0.5f; 
        noise.frequency = 1.0f;
        noise.scrollSpeed = 1.0f;
        var sizeOverLifetime = ps.sizeOverLifetime;
        sizeOverLifetime.enabled = true;
        AnimationCurve curve = new AnimationCurve();
        curve.AddKey(0.0f, 1.0f);
        curve.AddKey(1.0f, 0.0f);
        sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1.0f, curve); 
        
        var renderer = ps.GetComponent<ParticleSystemRenderer>();
        renderer.renderMode = ParticleSystemRenderMode.Stretch; 
        renderer.lengthScale = 2.0f;
        renderer.velocityScale = 0.05f; 
        Material emberMat = new Material(Shader.Find("Legacy Shaders/Particles/Additive")); 
        emberMat.mainTexture = CreateSoftTexture(); 
        emberMat.SetColor("_TintColor", new Color(1f, 0.3f, 1f, 0.8f)); 
        renderer.material = emberMat;
        ps.Play();
    }

    void CreateBladeEmberParticles(Transform blade)
    {
        if (blade.Find("SusanooEmbersBlade") != null) return;
        Mesh sharedMesh = blade.GetComponent<MeshFilter>().sharedMesh;
        GameObject pObj = new GameObject("SusanooEmbersBlade");
        pObj.transform.SetParent(blade, false);
        pObj.transform.localPosition = Vector3.zero;
        pObj.transform.localRotation = Quaternion.identity;
        pObj.transform.localScale = Vector3.one; 
        
        ParticleSystem ps = pObj.AddComponent<ParticleSystem>();
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        var main = ps.main;
        main.duration = 1.0f;
        main.loop = true;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.15f, 0.3f);
        main.startSpeed = 0f; 
        main.scalingMode = ParticleSystemScalingMode.Shape; 
        main.startSize = new ParticleSystem.MinMaxCurve(0.4f, 0.8f); 
        main.startColor = new Color(2.0f, 0.5f, 4.0f, 0.2f); 
        main.simulationSpace = ParticleSystemSimulationSpace.Local; 
        main.maxParticles = 1500;
        
        var emission = ps.emission;
        emission.rateOverTime = 1500f; 
        
        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Mesh;
        shape.mesh = sharedMesh;
        shape.meshShapeType = ParticleSystemMeshShapeType.Triangle;
        
        // CẤU HÌNH DÀNH RIÊNG CHO LƯỠI KIẾM
        shape.scale = new Vector3(1f, 2f, 1.05f);
        shape.position = new Vector3(0f, -0.0003f, 0f);
        shape.rotation = Vector3.zero;
        
        var inheritVel = ps.inheritVelocity;
        inheritVel.enabled = true;
        inheritVel.mode = ParticleSystemInheritVelocityMode.Initial;
        inheritVel.curveMultiplier = 1.0f;
        var vel = ps.velocityOverLifetime;
        vel.enabled = true; 
        vel.space = ParticleSystemSimulationSpace.Local; 
        vel.x = new ParticleSystem.MinMaxCurve(-1.5f, 1.5f); 
        vel.y = new ParticleSystem.MinMaxCurve(-1.5f, 1.5f); 
        vel.z = new ParticleSystem.MinMaxCurve(-1.0f, 1.0f); 
        
        var noise = ps.noise;
        noise.enabled = true;
        noise.strength = 0.5f; 
        noise.frequency = 1.0f;
        noise.scrollSpeed = 1.0f;
        var sizeOverLifetime = ps.sizeOverLifetime;
        sizeOverLifetime.enabled = true;
        AnimationCurve curve = new AnimationCurve();
        curve.AddKey(0.0f, 1.0f);
        curve.AddKey(1.0f, 0.0f);
        sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1.0f, curve); 
        
        var renderer = ps.GetComponent<ParticleSystemRenderer>();
        renderer.renderMode = ParticleSystemRenderMode.Stretch; 
        renderer.lengthScale = 2.0f;
        renderer.velocityScale = 0.05f; 
        Material emberMat = new Material(Shader.Find("Legacy Shaders/Particles/Additive")); 
        emberMat.mainTexture = CreateSoftTexture(); 
        emberMat.SetColor("_TintColor", new Color(1f, 0.3f, 1f, 0.8f)); 
        renderer.material = emberMat;
        ps.Play();
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
