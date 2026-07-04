using UnityEngine;

/// <summary>
/// Sửa lỗi Sasuke "điếc sáng": AutoMaterialFixer đã ép toàn bộ Material sang Unlit Shader
/// (Shader không phản ứng với ánh sáng), khiến Sasuke không có bóng đổ và không sáng/tối.
/// 
/// Script này chuyển Sasuke về Lit Shader (có chiếu sáng) và thiết lập các thuộc tính
/// giống hệt Three.js character.js: metalness=0, roughness=0.8, side=DoubleSide.
/// </summary>
public class NormalFixer : MonoBehaviour
{
    void Start()
    {
        Renderer[] renderers = GetComponentsInChildren<Renderer>();
        Shader litShader = Shader.Find("Universal Render Pipeline/Lit");

        foreach (Renderer r in renderers)
        {
            // Bỏ qua các VFX
            if (r.gameObject.name.Contains("VFX") || r.gameObject.name.Contains("Particle")) continue;

            // BẬT đổ bóng (castShadow = true trong Three.js)
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
            // TẮT nhận bóng (receiveShadows = false)
            // ĐÂY CHÍNH LÀ NGUYÊN NHÂN KHIẾN LƯNG BỊ ĐEN! Mặt trời chiếu thẳng đứng 90 độ
            // sẽ khiến đầu và bờ vai Sasuke đổ một cái bóng đen sì lên chính cái lưng và ngực của cậu ấy.
            // Phải tắt nhận bóng để ánh sáng chiếu tới đều đặn.
            r.receiveShadows = false;

            foreach (Material m in r.materials)
            {
                // Lưu lại Texture gốc TRƯỚC KHI đổi Shader (tránh mất ảnh)
                Texture baseTex = null;
                if (m.HasProperty("_BaseMap")) baseTex = m.GetTexture("_BaseMap");
                if (baseTex == null) baseTex = m.mainTexture;
                
                Color baseColor = Color.white;
                if (m.HasProperty("_BaseColor")) baseColor = m.GetColor("_BaseColor");

                // CHUYỂN TỪ UNLIT SANG LIT (Để Sasuke phản ứng với ánh sáng)
                if (litShader != null && m.shader.name.Contains("Unlit"))
                {
                    m.shader = litShader;
                }

                // Gán lại Texture SAU KHI đổi Shader
                if (baseTex != null)
                {
                    m.SetTexture("_BaseMap", baseTex);
                    m.mainTexture = baseTex;
                }
                m.SetColor("_BaseColor", baseColor);

                // Thiết lập thuộc tính vật lý giống hệt Three.js character.js
                if (m.HasProperty("_Metallic")) m.SetFloat("_Metallic", 0f);
                if (m.HasProperty("_Smoothness")) m.SetFloat("_Smoothness", 0.2f); // 1 - roughness(0.8) = 0.2

                // BẬT LẠI DOUBLE SIDE (Cull Off) để quần áo không bị tàng hình, không bị lòi khớp xương ra ngoài.
                if (m.HasProperty("_Cull")) m.SetFloat("_Cull", 0f); 
                
                // ĐÂY LÀ CHÌA KHÓA: Kích hoạt tính năng tự động lật Normal của Unity cho mặt sau.
                // Khi áo bị ngược Normal, mặt ngoài biến thành mặt sau. Unity sẽ tự động lật Normal
                // của mặt ngoài hướng ra ngoài (chỉ lên trời), giúp ánh sáng mặt trời chiếu vào lưng sáng rực rỡ!
                m.EnableKeyword("_DOUBLE_SIDED_NORMALS");
            }
        }
    }
}
