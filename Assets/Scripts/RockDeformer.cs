using UnityEngine;

[RequireComponent(typeof(MeshFilter))]
public class RockDeformer : MonoBehaviour
{
    void Start()
    {
        MeshFilter mf = GetComponent<MeshFilter>();
        if (mf != null)
        {
            // BỎ QUA KHỐI CAPSULE XẤU XÍ!
            // Tự động vẽ ra một khối Thập Nhị Diện (Dodecahedron) giống y hệt Three.js
            Mesh rockMesh = CreateDodecahedron();
            mf.mesh = rockMesh;

            // Gắn lại Hitbox cho vừa khít với khối đá mới
            MeshCollider mc = GetComponent<MeshCollider>();
            if (mc != null)
            {
                mc.sharedMesh = rockMesh;
            }
        }
    }

    Mesh CreateDodecahedron()
    {
        Mesh mesh = new Mesh();
        float p = (1f + Mathf.Sqrt(5f)) / 2f;
        
        Vector3[] baseVerts = {
            new Vector3(-1, p, 0), new Vector3(1, p, 0), new Vector3(-1, -p, 0), new Vector3(1, -p, 0),
            new Vector3(0, -1, p), new Vector3(0, 1, p), new Vector3(0, -1, -p), new Vector3(0, 1, -p),
            new Vector3(p, 0, -1), new Vector3(p, 0, 1), new Vector3(-p, 0, -1), new Vector3(-p, 0, 1)
        };

        int[] baseTris = {
            0, 11, 5,  0, 5, 1,   0, 1, 7,   0, 7, 10,  0, 10, 11,
            3, 9, 4,   3, 4, 2,   3, 2, 6,   3, 6, 8,   3, 8, 9,
            5, 11, 4,  4, 11, 2,  2, 11, 10, 2, 10, 6,  6, 10, 7,
            7, 8, 6,   7, 1, 8,   8, 1, 9,   9, 1, 5,   9, 5, 4
        };

        Vector3[] flatVerts = new Vector3[baseTris.Length];
        Vector2[] uvs = new Vector2[baseTris.Length];
        int[] tris = new int[baseTris.Length];

        float currentRadius = Mathf.Sqrt(1f + p * p);
        float scaleFactor = 1.2f / currentRadius;

        for (int i = 0; i < baseTris.Length; i++)
        {
            Vector3 v = baseVerts[baseTris[i]] * scaleFactor;
            flatVerts[i] = v;
            
            // Spherical UV Mapping 
            Vector3 norm = v.normalized;
            float u = 0.5f + Mathf.Atan2(norm.z, norm.x) / (2f * Mathf.PI);
            float v_tex = 0.5f - Mathf.Asin(norm.y) / Mathf.PI;
            
            uvs[i] = new Vector2(u, v_tex);
            tris[i] = i;
        }

        // --- BÍ QUYẾT TỪ THREE.JS: KHẮC PHỤC LỖI ĐỨT GÃY UV (UV SEAM CORRECTION) ---
        // Tại điểm giáp ranh vòng tròn (ví dụ từ 359 độ nhảy sang 1 độ), tọa độ U sẽ bị tụt đột ngột từ 1.0 về 0.0.
        // Điều này khiến bức ảnh bị ép méo mó dãn dài qua toàn bộ bề mặt của mặt phẳng đó.
        for (int i = 0; i < tris.Length; i += 3)
        {
            Vector2 uv0 = uvs[i];
            Vector2 uv1 = uvs[i + 1];
            Vector2 uv2 = uvs[i + 2];

            // Tìm ra khoảng cách lớn nhất giữa các điểm U
            float maxU = Mathf.Max(uv0.x, Mathf.Max(uv1.x, uv2.x));
            float minU = Mathf.Min(uv0.x, Mathf.Min(uv1.x, uv2.x));

            // Nếu khoảng cách U lớn hơn 0.5, chắc chắn mặt phẳng này đang bị dính vết cắt Seam
            if (maxU - minU > 0.5f)
            {
                // Cộng thêm 1 vào các điểm có U < 0.5 để đẩy chúng sang chu kỳ tiếp theo, loại bỏ sự đứt gãy
                if (uv0.x < 0.5f) uv0.x += 1f;
                if (uv1.x < 0.5f) uv1.x += 1f;
                if (uv2.x < 0.5f) uv2.x += 1f;
            }

            // Nhân Tiling (Scale Texture) để vân đá sắc nét hơn
            uvs[i] = uv0 * 3f;
            uvs[i + 1] = uv1 * 3f;
            uvs[i + 2] = uv2 * 3f;
        }

        mesh.vertices = flatVerts;
        mesh.triangles = tris;
        mesh.uv = uvs;
        mesh.RecalculateNormals(); 
        mesh.RecalculateBounds();
        return mesh;
    }
}
