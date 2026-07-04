using UnityEngine;

public class FireballVFX : MonoBehaviour
{
    [Header("Gắn file lavatile.jpg vào đây!")]
    public Texture2D lavaTexture;

    private Transform aura1, aura2, aura3, core;
    private Material coreMatInstance, auraMatInstance;

    void Start()
    {
        // ===== 1. LÕI — SphereGeometry(0.8) =====
        core = GameObject.CreatePrimitive(PrimitiveType.Sphere).transform;
        core.SetParent(this.transform);
        core.localPosition = Vector3.zero;
        core.localScale = new Vector3(1.6f, 1.6f, 1.6f); // đường kính = 2 × 0.8
        Destroy(core.GetComponent<Collider>());
        // LÕI: Tắt lõi theo yêu cầu để chỉ kiểm tra lớp Aura duy nhất.
        core.gameObject.SetActive(false);

        // VỎ AURA: Áp dụng Custom Shader dịch 1:1 từ Three.js
        Material auraMat = new Material(Shader.Find("Custom/FireballFresnel"));
        // Dùng lavatile.jpg cho aura — y hệt Three.js demo
        if (lavaTexture != null) {
            // Three.js: lavaTexture.wrapS = RepeatWrapping; lavaTexture.wrapT = RepeatWrapping;
            lavaTexture.wrapMode = TextureWrapMode.Repeat;
            auraMat.mainTexture = lavaTexture;
        }
        aura1 = CreateAura(auraMat);
        aura2 = CreateAura(auraMat);
        aura3 = CreateAura(auraMat);
        auraMatInstance = auraMat;

        // Chỉ bật duy nhất 1 lớp Aura để quan sát
        aura1.gameObject.SetActive(true);
        aura2.gameObject.SetActive(false);
        aura3.gameObject.SetActive(false);

        // ===== 3. HẠT TÀN =====
        GameObject particleObj = new GameObject("FireParticles");
        particleObj.transform.SetParent(this.transform);
        particleObj.transform.localPosition = Vector3.zero;

        ParticleSystem ps = particleObj.AddComponent<ParticleSystem>();
        var main = ps.main;
        // Giảm lifetime xuống một nửa (0.15-0.3) để vệt tàn ngắn lại và teo nhanh hơn, khớp với Three.js
        main.loop = true;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.15f, 0.3f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(2.0f, 4.0f); 
        main.startSize = new ParticleSystem.MinMaxCurve(1.8f, 3.0f); // Tăng kích thước gấp đôi để bù lại phần viền mờ của hạt 2D
        
        // Trộn lẫn màu sắc: Sinh ra ngẫu nhiên giữa màu Cam Vàng và màu Đỏ Rực
        Color colorOrange = new Color(1f, 0.5f, 0f, 0.8f);
        Color colorRed = new Color(1f, 0.15f, 0f, 0.8f);
        main.startColor = new ParticleSystem.MinMaxGradient(colorOrange, colorRed);
        
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        // Xoay ban đầu chỉ trục Z (giống Three.js: rotation.z = random * PI * 2)
        // Khôi phục 300 hạt tàn/s để đuôi lửa dày và đẹp như cũ
        var emission = ps.emission;
        emission.rateOverTime = 300f;

        // Khôi phục vùng sinh Sphere gốc để đường bay tự nhiên
        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = 1.2f;

        // Bốc lên và tạt sang hai bên (Mô phỏng dao động Sine/Cosine)
        // Biên độ vận tốc lớn giúp hạt văng vọt ra khỏi thân cầu lửa!
        var vel = ps.velocityOverLifetime;
        vel.enabled = true;
        vel.x = new ParticleSystem.MinMaxCurve(-3.0f, 3.0f);
        vel.y = new ParticleSystem.MinMaxCurve(1.0f, 6.0f);
        vel.z = new ParticleSystem.MinMaxCurve(-3.0f, 3.0f);
        vel.space = ParticleSystemSimulationSpace.World;

        // Teo nhỏ dần: Dùng AnimationCurve để hạt to ở gần, nhỏ ở xa
        var sol = ps.sizeOverLifetime;
        sol.enabled = true;
        sol.size = new ParticleSystem.MinMaxCurve(1.0f, AnimationCurve.Linear(0f, 1f, 1f, 0f));

        // Xoay: rotation += delta * 3.0 rad/s = 172°/s
        var rot = ps.rotationOverLifetime;
        rot.enabled = false; // Tắt tính năng xoay 3D để hạt Billboard không bị lật nghiêng thành tờ giấy phẳng
        rot.y = 172f;
        rot.z = 172f;

        // HẠT TÀN: Sử dụng dạng 2D Billboard mờ ảo (Soft Particle) mặc định của Unity
        // Việc không gán Mesh hay Material tùy chỉnh sẽ ép Particle System tự động dùng chất liệu Default-ParticleSystem
        // Đây chính là chất liệu có viền mờ (gradient) lan tỏa rất mềm mại mà bạn thấy ở thanh kiếm Susanoo!
        var rend = ps.GetComponent<ParticleSystemRenderer>();
        rend.renderMode = ParticleSystemRenderMode.Billboard; 

        // Rất tiếc, Unity không cho phép dùng code (Resources.GetBuiltinResource) để lấy ảnh Default-Particle.psd ra.
        // Đó là lý do tại sao ở lần trước tôi phải dùng đoạn mã "phức tạp" bên dưới để tự vẽ ra một tấm ảnh viền mờ!
        // Giờ ta đành phải dùng lại nó để không bị lỗi hình vuông hồng.
        int texSize = 64;
        Texture2D softTex = new Texture2D(texSize, texSize, TextureFormat.RGBA32, false);
        for (int y = 0; y < texSize; y++) {
            for (int x = 0; x < texSize; x++) {
                float dist = Vector2.Distance(new Vector2(x, y), new Vector2(texSize / 2f, texSize / 2f)) / (texSize / 2f);
                float alpha = Mathf.Pow(Mathf.Clamp01(1f - dist), 1.5f);
                softTex.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
            }
        }
        softTex.Apply();

        Material pMat = new Material(Shader.Find("Legacy Shaders/Particles/Additive"));
        pMat.mainTexture = softTex;
        rend.material = pMat;

        // Bỏ phần tự tạo PointLight bằng code vì người dùng đã có PointLight chuẩn trên Prefab
    }

    Transform CreateAura(Material mat)
    {
        GameObject a = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        a.transform.SetParent(this.transform);
        a.transform.localPosition = Vector3.zero;
        a.transform.localScale = Vector3.one * 2.4f; // đường kính = 2 × 1.2
        Destroy(a.GetComponent<Collider>());
        a.GetComponent<Renderer>().material = mat;
        return a.transform;
    }

    void Update()
    {
        float f = Time.deltaTime * 60f; 

        // Xoay — giữ nguyên logic cũ cho core và các aura khác
        if (core) core.Rotate(-0.2f * f * Mathf.Rad2Deg, 0, 0, Space.Self);
        
        // aura1: Xoay 90 độ trục X để hướng Cực Nam ra phía trước mặt và Cực Bắc ra phía sau lưng
        // Do UV.y cuộn âm (-0.4), dòng chảy sẽ đi từ Nam lên Bắc (nghĩa là từ Trước ra Sau)
        if (aura1) aura1.localRotation = Quaternion.Euler(90f, 0f, 0f);
        
        if (aura2) aura2.Rotate(0, -0.15f * f * Mathf.Rad2Deg, 0.2f * f * Mathf.Rad2Deg, Space.Self);
        if (aura3) aura3.Rotate(0.2f * f * Mathf.Rad2Deg, 0, -0.15f * f * Mathf.Rad2Deg, Space.Self);

        // Co bóp (Pulsing) mượt mà bằng sóng Sine
        float time = Time.time * 15f;
        float baseScale = 2.4f;
        float pulseAmp = 0.08f;
        
        // Tắt biến dạng co bóp cho aura1
        if (aura1) aura1.localScale = Vector3.one * baseScale;
        
        if (aura2) aura2.localScale = Vector3.one * (1f + Mathf.Sin(time + 2f) * pulseAmp) * baseScale;
        if (aura3) aura3.localScale = Vector3.one * (1f + Mathf.Sin(time + 4f) * pulseAmp) * baseScale;
    }

    Mesh CreateTrueDodecahedron()
    {
        Mesh mesh = new Mesh();
        float p = (1f + Mathf.Sqrt(5f)) / 2f;
        float inv = 1f / p;
        Vector3[] verts = {
            new Vector3(-1,-1,-1), new Vector3(1,-1,-1), new Vector3(-1,1,-1), new Vector3(1,1,-1),
            new Vector3(-1,-1,1), new Vector3(1,-1,1), new Vector3(-1,1,1), new Vector3(1,1,1),
            new Vector3(0,-inv,-p), new Vector3(0,inv,-p), new Vector3(0,-inv,p), new Vector3(0,inv,p),
            new Vector3(-inv,-p,0), new Vector3(inv,-p,0), new Vector3(-inv,p,0), new Vector3(inv,p,0),
            new Vector3(-p,0,-inv), new Vector3(p,0,-inv), new Vector3(-p,0,inv), new Vector3(p,0,inv)
        };
        int[] tris = {
            0,8,1, 0,1,13, 0,13,12,
            0,16,2, 0,2,9, 0,9,8,
            0,12,4, 0,4,18, 0,18,16,
            1,8,9, 1,9,3, 1,3,17,
            1,17,19, 1,19,5, 1,5,13,
            2,16,18, 2,18,6, 2,6,14,
            2,14,15, 2,15,3, 2,3,9,
            3,15,7, 3,7,19, 3,19,17,
            4,12,13, 4,13,5, 4,5,10,
            4,10,11, 4,11,6, 4,6,18,
            5,19,7, 5,7,11, 5,11,10,
            6,11,7, 6,7,15, 6,15,14
        };
        Vector2[] uvs = new Vector2[verts.Length];
        for (int j = 0; j < verts.Length; j++)
        {
            Vector3 v = verts[j].normalized;
            // UV hình cầu chuẩn Three.js để giữ lại viền mờ, tạo độ mềm mại cho ngọn lửa
            uvs[j] = new Vector2(
                0.5f + Mathf.Atan2(v.z, v.x) / (2f * Mathf.PI),
                0.5f + Mathf.Asin(Mathf.Clamp(v.y, -1f, 1f)) / Mathf.PI
            );
            verts[j] = v; // bán kính 1.0
        }
        mesh.vertices = verts;
        mesh.triangles = tris;
        mesh.uv = uvs;
        mesh.RecalculateNormals();
        return mesh;
    }
}
