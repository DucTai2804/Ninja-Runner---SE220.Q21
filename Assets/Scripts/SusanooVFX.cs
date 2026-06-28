using UnityEngine;

public class SusanooVFX : MonoBehaviour
{
    void Start()
    {
        // 1. TÍNH TOÁN BÙ TRỪ TỶ LỆ CỦA FBX (THƯỜNG BỊ THU NHỎ 0.01 LẦN)
        float scaleFactor = 1f;
        if (transform.lossyScale.y > 0)
        {
            scaleFactor = 1f / transform.lossyScale.y; // VD: Nếu Susanoo scale 0.01, factor = 100
        }

        // 2. ÁNH SÁNG TÍM MA MỊ
        Light pointLight = gameObject.AddComponent<Light>();
        pointLight.type = LightType.Point;
        pointLight.color = new Color(0.6f, 0f, 1f); 
        pointLight.range = 20f * scaleFactor; 
        pointLight.intensity = 3f; // Giảm cường độ sáng chói

        // 3. KHÓI VÀ TÀN LỬA LINH HỒN (Bao bọc toàn thân)
        GameObject auraParticles = new GameObject("SusanooSmoke");
        auraParticles.transform.SetParent(this.transform, false);
        
        // Đẩy luồng khói lên ngang ngực để nó xả khói bao trọn từ đầu gối lên đỉnh đầu
        auraParticles.transform.localPosition = new Vector3(0, 6f * scaleFactor, 0); 
        
        ParticleSystem ps = auraParticles.AddComponent<ParticleSystem>();
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear); 

        var main = ps.main;
        main.duration = 2f;
        main.loop = true;
        main.startLifetime = 3f; // Khói tồn tại lâu hơn chút để tản mạn
        
        main.startSpeed = 3f * scaleFactor; // Bay chậm lại, lượn lờ hơn
        main.startSize = 8f * scaleFactor; // Giảm một nửa kích thước hạt khói
        
        main.startColor = new Color(1f, 119f/255f, 1f, 0.5f); // Tăng độ đậm lên 50%
        main.simulationSpace = ParticleSystemSimulationSpace.World; 
        main.scalingMode = ParticleSystemScalingMode.Hierarchy;

        var emission = ps.emission;
        emission.rateOverTime = 15f; // Giảm số lượng khói sinh ra (Từ 60 xuống 15)

        // Vùng sinh hạt là một hình nón mở rộng bao bọc cơ thể
        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Cone;
        shape.radius = 6f * scaleFactor;
        shape.angle = 25f;

        // Độ mờ duy trì ở mức 50% (0.5f) ở lưng chừng và nhạt dần
        var colorOverLifetime = ps.colorOverLifetime;
        colorOverLifetime.enabled = true;
        Gradient grad = new Gradient();
        grad.SetKeys(
            new GradientColorKey[] { new GradientColorKey(new Color(1f, 119f/255f, 1f), 0.0f), new GradientColorKey(Color.magenta, 1.0f) },
            new GradientAlphaKey[] { new GradientAlphaKey(0.0f, 0.0f), new GradientAlphaKey(0.5f, 0.3f), new GradientAlphaKey(0.0f, 1.0f) }
        );
        colorOverLifetime.color = grad;

        // Khói to dần khi bay lên
        var sizeOverLifetime = ps.sizeOverLifetime;
        sizeOverLifetime.enabled = true;
        AnimationCurve curve = new AnimationCurve(new Keyframe(0f, 0.5f), new Keyframe(1f, 2f)); // Phóng to gấp 4 lần
        sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1.5f, curve);

        var renderer = ps.GetComponent<ParticleSystemRenderer>();
        // Dùng Sprites/Default: Shader an toàn nhất của Unity, tương thích 100% với URP và hỗ trợ mờ trong suốt hoàn hảo
        Material auraMat = new Material(Shader.Find("Sprites/Default"));
        auraMat.mainTexture = CreateSoftTexture();
        renderer.material = auraMat;

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
