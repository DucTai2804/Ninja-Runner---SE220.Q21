using UnityEngine;
using UnityEditor;

public class FixSusanooColor : AssetPostprocessor
{
    static void OnPostprocessAllAssets(string[] imported, string[] deleted, string[] moved, string[] movedFrom)
    {
        if (SessionState.GetBool("FixedColor", false)) return;
        SessionState.SetBool("FixedColor", true);

        string[] guids = AssetDatabase.FindAssets("t:Material");
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat != null && mat.shader != null && mat.shader.name == "Custom/SusanooEnergyFlow")
            {
                mat.SetFloat("_NoiseScale", 10f);
                mat.SetFloat("_FlowSpeed", 6f);
                
                Color c2 = mat.GetColor("_Color2");
                float maxColor = Mathf.Max(c2.r, Mathf.Max(c2.g, c2.b));
                if (maxColor > 0.001f)
                {
                    Color baseC2 = new Color(c2.r / maxColor, c2.g / maxColor, c2.b / maxColor, c2.a);
                    mat.SetColor("_Color2", baseC2 * Mathf.Pow(2f, -0.4f));
                }
                EditorUtility.SetDirty(mat);
            }
        }
        AssetDatabase.SaveAssets();
    }
}
