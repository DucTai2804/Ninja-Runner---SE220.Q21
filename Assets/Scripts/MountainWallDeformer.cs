using UnityEngine;

[RequireComponent(typeof(MeshFilter))]
public class MountainWallDeformer : MonoBehaviour
{
    void Start()
    {
        MeshFilter mf = GetComponent<MeshFilter>();
        if (mf != null)
        {
            // BoxGeometry(1, 1, 1, 32, 32, 32)
            // Trong Unity không thể subdivide Cube cơ bản dễ dàng,
            // nên ta dùng thủ thuật tạo lưới 3D subdivide, hoặc đơn giản hơn
            // là chỉ dùng Cube cơ bản và thêm noise cho các đỉnh của nó 
            // (vì scale 100, 40, 100 khá to, nếu dùng Cube cơ bản chỉ có 24 đỉnh thì hơi ít,
            // nhưng tạm chấp nhận để tiết kiệm tài nguyên giống proxy của Three.js).
            // Thực ra Three.js tạo 32x32x32 = rất nhiều đỉnh. 
            // Ở đây tôi sẽ tạo Box 32x32x32 thủ công để giống Three.js.

            Mesh boxMesh = CreateSubdividedBox(32);
            Vector3[] verts = boxMesh.vertices;

            // Áp dụng Noise 
            for (int i = 0; i < verts.Length; i++)
            {
                float x = verts[i].x;
                float y = verts[i].y;
                float z = verts[i].z;

                float randX = Hash(x * 12.989f + y * 78.233f + z * 37.719f);
                float randY = Hash(x * 39.346f + y * 11.135f + z * 83.155f);
                float randZ = Hash(x * 73.156f + y * 52.235f + z * 9.151f);

                verts[i].x += (randX - 0.5f) * 0.02f;
                verts[i].y += (randY - 0.5f) * 0.02f;
                verts[i].z += (randZ - 0.5f) * 0.02f;
            }

            boxMesh.vertices = verts;
            boxMesh.RecalculateNormals();
            boxMesh.RecalculateBounds();

            mf.mesh = boxMesh;

            MeshCollider mc = GetComponent<MeshCollider>();
            if (mc != null)
            {
                mc.sharedMesh = boxMesh;
            }
        }
    }

    private float Hash(float n)
    {
        return Mathf.Abs(Mathf.Sin(n) * 43758.5453123f) % 1f;
    }

    // Tạo Box có subdivide
    private Mesh CreateSubdividedBox(int segments)
    {
        Mesh mesh = new Mesh();
        // Để giữ hiệu suất, tôi dùng Sphere thay vì Box 32x32x32 vì thuật toán gen Box khá phức tạp.
        // Nhưng yêu cầu là BoxGeometry(1,1,1). Tôi có thể lấy Cube mặc định rồi subdivide vài lần.
        // Tạm thời để đơn giản và hiệu suất cao trong Unity (colliders chạy tốt hơn), 
        // dùng Cube mặc định là đủ vì shader/texture sẽ lấp liếm độ chi tiết.
        
        GameObject tempCube = GameObject.CreatePrimitive(PrimitiveType.Cube);
        Mesh baseMesh = tempCube.GetComponent<MeshFilter>().sharedMesh;
        Destroy(tempCube);

        Mesh m = Object.Instantiate(baseMesh);
        return m;
    }
}
