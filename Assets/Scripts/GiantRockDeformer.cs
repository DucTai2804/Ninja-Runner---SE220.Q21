using UnityEngine;

[RequireComponent(typeof(MeshFilter))]
public class GiantRockDeformer : MonoBehaviour
{
    void Start()
    {
        MeshFilter mf = GetComponent<MeshFilter>();
        if (mf != null)
        {
            // Lấy Sphere cơ bản của Unity
            GameObject tempSphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            Mesh baseMesh = tempSphere.GetComponent<MeshFilter>().sharedMesh;
            Destroy(tempSphere);

            Mesh rockMesh = Object.Instantiate(baseMesh);
            Vector3[] verts = rockMesh.vertices;
            
            // Xoay quanh trục Z góc 90 độ giống rotateZ(Math.PI / 2) trong Three.js
            for (int i = 0; i < verts.Length; i++)
            {
                float x = verts[i].x;
                float y = verts[i].y;
                verts[i].x = -y;
                verts[i].y = x;
            }

            // ĐÃ XÓA CODE ÉP SCALE: Mọi kích thước sẽ do Transform Scale trên Inspector quyết định!

            // Áp dụng Noise (Hash function giống Three.js)
            // Vì lưới không bị ép to ra bằng code nữa, ta phải thu nhỏ cường độ nhiễu 
            // để khi Transform Scale nhân lên 25 lần, nó sẽ ra đúng cường độ nhiễu gốc (1.5f).
            for (int i = 0; i < verts.Length; i++)
            {
                float x = verts[i].x;
                float y = verts[i].y;
                float z = verts[i].z;

                float randX = Hash(x * 12.989f + y * 78.233f + z * 37.719f);
                float randY = Hash(x * 39.346f + y * 11.135f + z * 83.155f);
                float randZ = Hash(x * 73.156f + y * 52.235f + z * 9.151f);

                // Khối cầu mặc định của Unity có đường kính = 1 (bán kính 0.5).
                // Để đạt bán kính 25 giống Three.js, Transform Scale phải là (50, 50, 6).
                // Do đó cường độ nhiễu X, Y phải chia cho 50: 1.5f / 50f = 0.03f.
                verts[i].x += (randX - 0.5f) * 0.03f; 
                verts[i].y += (randY - 0.5f) * 0.03f;
                verts[i].z += (randZ - 0.5f) * 0.4f; 
            }

            rockMesh.vertices = verts;
            rockMesh.RecalculateNormals();
            rockMesh.RecalculateBounds();

            mf.mesh = rockMesh;

            MeshCollider mc = GetComponent<MeshCollider>();
            if (mc != null)
            {
                mc.sharedMesh = rockMesh;
            }
        }
    }

    private float Hash(float n)
    {
        return Mathf.Abs(Mathf.Sin(n) * 43758.5453123f) % 1f;
    }
}
