using UnityEngine;

public class FireballVFX : MonoBehaviour
{
    [Header("Gắn file fire.jpg vào đây!")]
    public Texture2D fireTexture;

    private Transform aura1, aura2, aura3, core;

    void Start()
    {
        // ===== 1. LÕI — SphereGeometry(0.8) =====
        core = GameObject.CreatePrimitive(PrimitiveType.Sphere).transform;
        core.SetParent(this.transform);
        core.localPosition = Vector3.zero;
        core.localScale = new Vector3(1.6f, 1.6f, 1.6f); // đường kính = 2 × 0.8
        Destroy(core.GetComponent<Collider>());
        Material coreMat = new Material(Shader.Find("Unlit/Texture"));
        if (fireTexture != null) coreMat.mainTexture = fireTexture;
        coreMat.color = Color.white; // 0xffffff
        core.GetComponent<Renderer>().material = coreMat;

        // ===== 2. AURA =====
        // BẮT BUỘC dùng Additive để tạo hiệu ứng phát sáng (Glow). 
        // Lửa là ánh sáng, không thể dùng Alpha Blended (sẽ thành đá đặc).
        Material auraMat = new Material(Shader.Find("Legacy Shaders/Particles/Additive"));
        if (fireTexture != null) auraMat.mainTexture = fireTexture;
        // Tăng mạnh Green để màu chuyển sang Vàng Cam sáng rực (Hot Yellow-Orange)
        auraMat.SetColor("_TintColor", new Color(1f, 0.8f, 0f, 0.8f));
        aura1 = CreateAura(auraMat);
        aura2 = CreateAura(auraMat);
        aura3 = CreateAura(auraMat);

        // ===== 3. HẠT TÀN — ĐƠN GIẢN NHƯ THREE.JS =====
        GameObject particleObj = new GameObject("FireParticles");
        particleObj.transform.SetParent(this.transform);
        particleObj.transform.localPosition = Vector3.zero;

        ParticleSystem ps = particleObj.AddComponent<ParticleSystem>();
        var main = ps.main;
        // Giảm lifetime xuống một nửa (0.15-0.3) để vệt tàn ngắn lại và teo nhanh hơn, khớp với Three.js
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.15f, 0.3f);
        main.startSpeed = 0f;
        main.startSize = 1.0f; // Kích thước chuẩn
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        // Xoay ban đầu chỉ trục Z (giống Three.js: rotation.z = random * PI * 2)
        main.startRotation3D = false;
        main.startRotation = new ParticleSystem.MinMaxCurve(0f, 360f);

        // Tăng vọt số lượng hạt tàn: 300 hạt/s (tương đương 5 hạt mỗi frame ở 60fps)
        // Tạo cảm giác đuôi lửa dày đặc và dữ dội hơn
        var emission = ps.emission;
        emission.rateOverTime = 300f;

        // Vùng sinh: Mở rộng ra 1.2 (to hơn quả cầu) để lòi ra ngoài
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
        rot.enabled = true;
        rot.separateAxes = true;
        rot.x = 172f;
        rot.y = 172f;
        rot.z = 172f;

        // Render mesh thập nhị diện
        var rend = ps.GetComponent<ParticleSystemRenderer>();
        rend.renderMode = ParticleSystemRenderMode.Mesh;
        rend.mesh = CreateTrueDodecahedron();

        // BẮT BUỘC dùng Additive. Chấp nhận việc nó có thể hơi mờ trên nền quá sáng
        // vì đó là nguyên lý vật lý của ánh sáng.
        Material pMat = new Material(Shader.Find("Legacy Shaders/Particles/Additive"));
        if (fireTexture != null) pMat.mainTexture = fireTexture;
        // Bơm Green và Alpha lên cao để hạt tàn rực rỡ và đè bẹp màu xanh của nền
        pMat.SetColor("_TintColor", new Color(1f, 0.7f, 0f, 0.9f));
        pMat.SetInt("_Cull", (int)UnityEngine.Rendering.CullMode.Off); // DoubleSide
        rend.material = pMat;

        // ===== 4. ÁNH SÁNG — PointLight(0xff4500, 15, 40) =====
        Light pl = gameObject.AddComponent<Light>();
        pl.type = LightType.Point;
        pl.color = new Color(1f, 0.27f, 0f);
        pl.range = 40f;
        pl.intensity = 15f;
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
        float f = Time.deltaTime * 60f; // Chuyển sang tốc độ per-frame @60fps

        // Core: rotation.x -= 0.2
        if (core) core.Rotate(-0.2f * f * Mathf.Rad2Deg, 0, 0, Space.Self);

        // Aura1: x-=0.15, y+=0.2
        if (aura1) aura1.Rotate(-0.15f * f * Mathf.Rad2Deg, 0.2f * f * Mathf.Rad2Deg, 0, Space.Self);
        // Aura2: y-=0.15, z+=0.2
        if (aura2) aura2.Rotate(0, -0.15f * f * Mathf.Rad2Deg, 0.2f * f * Mathf.Rad2Deg, Space.Self);
        // Aura3: z-=0.15, x+=0.2
        if (aura3) aura3.Rotate(0.2f * f * Mathf.Rad2Deg, 0, -0.15f * f * Mathf.Rad2Deg, Space.Self);

        // Pulse
        float s = 2.4f;
        if (aura1) aura1.localScale = new Vector3(
            (1f + Random.Range(0f, 0.15f)),
            (1f + Random.Range(0f, 0.15f)) * 1.1f,
            (1f + Random.Range(0f, 0.15f)) * 0.9f) * s;
        if (aura2) aura2.localScale = new Vector3(
            (1f + Random.Range(0f, 0.15f)) * 0.9f,
            (1f + Random.Range(0f, 0.15f)),
            (1f + Random.Range(0f, 0.15f)) * 1.1f) * s;
        if (aura3) aura3.localScale = new Vector3(
            (1f + Random.Range(0f, 0.15f)) * 1.1f,
            (1f + Random.Range(0f, 0.15f)) * 0.9f,
            (1f + Random.Range(0f, 0.15f))) * s;
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
