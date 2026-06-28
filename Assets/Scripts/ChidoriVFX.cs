using UnityEngine;

public class ChidoriVFX : MonoBehaviour
{
    void Start()
    {
        // TẠO HỆ THỐNG TIA LỬA ĐIỆN NHẤP NHÁY
        ParticleSystem ps = gameObject.AddComponent<ParticleSystem>();
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear); // Sửa lỗi Duration
        
        var main = ps.main;
        main.duration = 1f;
        main.loop = true;
        main.startLifetime = 0.3f; // Chớp tắt cực nhanh (Giật giật)
        main.startSpeed = 8f; // Bắn ra xung quanh
        main.startSize = 0.1f; // Sợi sét mảnh
        main.startColor = new Color(0f, 0.8f, 1f, 1f); // Xanh Cyan chói lóa
        main.simulationSpace = ParticleSystemSimulationSpace.Local;

        var emission = ps.emission;
        emission.rateOverTime = 80f; // Số lượng tia sét nhiều

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = 0.3f;

        // BẬT HIỆU ỨNG VỆT KÉO DÀI (TRAILS) ĐỂ TẠO RA ĐƯỜNG ZIG ZAG
        var trails = ps.trails;
        trails.enabled = true;
        trails.ratio = 1f; // Mọi hạt đều có đuôi
        trails.lifetimeMultiplier = 0.4f;
        trails.minVertexDistance = 0.05f;

        // BẬT MODULE NOISE ĐỂ TIA SÉT BỊ NHIỄU SÓNG HỖN LOẠN
        var noise = ps.noise;
        noise.enabled = true;
        noise.strength = 5f; // Bẻ gập cực mạnh
        noise.frequency = 6f; // Rung liên hồi
        noise.scrollSpeed = 15f;

        var renderer = ps.GetComponent<ParticleSystemRenderer>();
        Material trailMat = new Material(Shader.Find("Legacy Shaders/Particles/Additive"));
        trailMat.mainTexture = CreateSoftTexture();
        renderer.material = trailMat;
        renderer.trailMaterial = trailMat;

        // TẠO ĐÈN CHỚP
        Light pointLight = gameObject.AddComponent<Light>();
        pointLight.type = LightType.Point;
        pointLight.color = new Color(0f, 0.8f, 1f);
        pointLight.range = 8f;
        
        // Gắn thuật toán nhấp nháy đèn
        gameObject.AddComponent<ChidoriBlink>();

        ps.Play(); // Phát lại sau khi đã cấu hình xong
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

public class ChidoriBlink : MonoBehaviour
{
    private Light myLight;
    void Start() { myLight = GetComponent<Light>(); }
    void Update()
    {
        if (myLight != null)
            myLight.intensity = Random.Range(1f, 6f); // Cường độ ánh sáng thay đổi điên cuồng mỗi khung hình
    }
}
