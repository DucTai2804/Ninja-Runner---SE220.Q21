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
                // Dùng Lit Shader nguyên bản để mô hình nhận bóng đổ và ánh sáng mặc định của Unity
                Shader litShader = Shader.Find("Universal Render Pipeline/Lit");
                if (litShader != null)
                {
                    mat.shader = litShader;
                }

                // Cấu hình vật lý cơ bản để không bị hút sáng
                if (mat.HasProperty("_Metallic")) mat.SetFloat("_Metallic", 0f);
                if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", 0.2f);

                // MÔ HÌNH ĐÃ ĐƯỢC FIX NÊN TA THOẢI MÁI BẬT CULL 0!
                // Giờ đây ta không còn sợ lớp viền đen (Inverted Hull) phá bĩnh nữa.
                // Việc dùng Cull 0 sẽ giúp vẽ cả mặt trong ống tay áo, che kín 100% khớp xương!
                mat.SetFloat("_Cull", 0f); 
                
                // Trả về mặc định, không cần lật Normal vì bạn đã tự fix ở file gốc rồi!
                mat.SetFloat("_DoubleSidedEnable", 0f); 
                mat.DisableKeyword("_DOUBLE_SIDED_NORMALS");

                // Giữ Alpha Clipping để váy không bị lỗi tàng hình
                if (mat.HasProperty("_AlphaClip")) mat.SetFloat("_AlphaClip", 1f);
                mat.EnableKeyword("_ALPHATEST_ON");

                EditorUtility.SetDirty(mat);
                fixedCount++;

                // Quét để nối ảnh tự động (nếu tên khớp)
                if (mat.HasProperty("_BaseMap") && mat.GetTexture("_BaseMap") == null)
                {
                    // 1. Nếu shader gốc có _MainTex (thường là do FBX tự nhúng), hãy copy sang _BaseMap
                    if (mat.HasProperty("_MainTex") && mat.GetTexture("_MainTex") != null)
                    {
                        mat.SetTexture("_BaseMap", mat.GetTexture("_MainTex"));
                    }
                    else
                    {
                        // 2. Thử tìm texture rời theo tên
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
        }
        AssetDatabase.SaveAssets();
        Debug.Log("Đã xử lý xong (Fix tàng hình + Nối ảnh) cho " + fixedCount + " Materials!");
    }
}
