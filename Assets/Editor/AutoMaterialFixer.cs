using UnityEngine;
using UnityEditor;
using System.IO;

public class AutoMaterialFixer : MonoBehaviour
{
    [MenuItem("Tools/Ninja Runner/Auto Fix Materials (Gắn ảnh tự động)")]
    public static void FixMaterials()
    {
        string[] matGuids = AssetDatabase.FindAssets("t:Material");
        int fixedCount = 0;

        foreach (string guid in matGuids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            
            // Chỉ tìm các Material bị lỗi Trắng bốc (chưa có Base Map)
            if (mat != null && mat.shader.name.Contains("Universal Render Pipeline"))
            {
                // --- CÁCH SỬA LỖI ĐẶC TRỊ CHO MÔ HÌNH ANIME ---
                
                // 1. CHUYỂN SANG UNLIT SHADER: 
                // Mô hình Anime sẽ cực kỳ xấu nếu dùng Lit Shader (bóng đổ gắt, áo đen sì).
                // Chuyển sang Unlit sẽ giúp màu sắc rực rỡ và sáng đúng y như trong Blender 100%!
                Shader unlitShader = Shader.Find("Universal Render Pipeline/Unlit");
                if (unlitShader != null)
                {
                    mat.shader = unlitShader;
                }

                // 2. SỬA LỖI LỖ RÁCH / MẢNG ĐEN TRÊN VÁY:
                // Các mảng đen rách đó thực chất là lớp "Inverted Hull" (Lớp viền Cel-shading ảo của model Anime).
                // Do trước đó ta bật chế độ Render Face: Both (Vẽ cả 2 mặt) nên lớp viền ảo này bị lòi ra ngoài đâm xuyên qua quần áo.
                // Giải pháp: Phải đổi lại thành Render Face: Front (Cull Back) để giấu lớp viền đen này đi!
                mat.SetFloat("_Cull", 2); // 2 = Render Front Face Only (Cull Back)
                
                // Tắt Alpha Clipping để tránh lủng lỗ thêm
                if (mat.HasProperty("_AlphaClip")) mat.SetFloat("_AlphaClip", 0f);
                mat.DisableKeyword("_ALPHATEST_ON");

                EditorUtility.SetDirty(mat);
                fixedCount++;

                // Quét để nối ảnh tự động (nếu tên khớp)
                if (mat.HasProperty("_BaseMap") && mat.GetTexture("_BaseMap") == null)
                {
                    string[] texGuids = AssetDatabase.FindAssets(mat.name + " t:Texture2D");
                    foreach (string texGuid in texGuids)
                    {
                        string texPath = AssetDatabase.GUIDToAssetPath(texGuid);
                        string texName = Path.GetFileNameWithoutExtension(texPath);
                        
                        if (texName.Equals(mat.name, System.StringComparison.OrdinalIgnoreCase))
                        {
                            Texture2D tex = AssetDatabase.LoadAssetAtPath<Texture2D>(texPath);
                            mat.SetTexture("_BaseMap", tex);
                            break;
                        }
                    }
                }
            }
        }
        AssetDatabase.SaveAssets();
        Debug.Log("Đã xử lý xong (Fix tàng hình + Nối ảnh) cho " + fixedCount + " Materials!");
    }
}
