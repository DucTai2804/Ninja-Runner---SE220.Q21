using System.Collections.Generic;
using UnityEngine;

public class TerrainManager : MonoBehaviour
{
    public static TerrainManager Instance;

    public Texture mountainTexture;
    public Material mountainMaterial;
    
    private List<GameObject> lowHills = new List<GameObject>();
    private List<GameObject> bigMountains = new List<GameObject>();

    private const int TOTAL_LOW_HILLS = 28; // 14 trái, 14 phải
    private const int TOTAL_BIG_MOUNTAINS = 16; // 8 trái, 8 phải

    private float speedDistance = 0f;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    void Start()
    {
#if UNITY_EDITOR
        if (mountainMaterial == null)
        {
            mountainMaterial = UnityEditor.AssetDatabase.LoadAssetAtPath<Material>("Assets/RockMat.mat");
        }
#endif

        if (mountainMaterial == null)
        {
            mountainMaterial = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        }

        // Đổi thẳng Shader của vật liệu sang Simple Lit (Blinn-Phong) thay vì Lit (PBR)
        Shader simpleLit = Shader.Find("Universal Render Pipeline/Simple Lit");
        if (simpleLit != null) mountainMaterial.shader = simpleLit;

        // Bắt buộc gán lại Texture SAU KHI đổi shader để Unity không làm mất ảnh
        if (mountainTexture != null)
        {
            mountainMaterial.mainTexture = mountainTexture;
            mountainMaterial.SetTexture("_BaseMap", mountainTexture);
        }
        mountainMaterial.color = Color.white; 
        mountainMaterial.mainTextureScale = new Vector2(3f, 3f);

        // TẮT Emission đi vì Simple Lit + Ánh sáng 3.0 đã quá đủ chói lóa.
        mountainMaterial.DisableKeyword("_EMISSION");
        mountainMaterial.SetColor("_EmissionColor", Color.black); 

        GenerateTerrain();
    }

    void GenerateTerrain()
    {
        // 4 loại Cone giống Three.js
        Mesh mesh0 = CreateCone(60, 140, 4);
        Mesh mesh1 = CreateCone(70, 120, 5);
        Mesh mesh2 = CreateCone(50, 160, 3);
        Mesh mesh3 = CreateCone(25, 40, 4); // Low hill

        // --- TẠO ĐỒI NHỎ (LOW HILLS) ---
        for (int i = 0; i < TOTAL_LOW_HILLS; i++)
        {
            bool isLeft = i < 14;
            int localIndex = isLeft ? i : (i - 14);

            float zSpacing = 400f / 14f;
            float zPos = 15f + localIndex * zSpacing; // Sửa dấu - thành + để sinh ra phía TRƯỚC mặt

            float xPos = 25f + (3f + Random.Range(0f, 2f)); // Bán kính 25 + margin 3~5m
            if (isLeft) xPos = -xPos;

            GameObject go = new GameObject("LowHill_" + i);
            go.transform.SetParent(this.transform);
            
            MeshFilter mf = go.AddComponent<MeshFilter>();
            mf.mesh = mesh3;
            
            MeshRenderer mr = go.AddComponent<MeshRenderer>();
            mr.material = mountainMaterial;
            mr.receiveShadows = true;

            go.transform.position = new Vector3(xPos, -5f, zPos);
            go.transform.rotation = Quaternion.Euler(0, Random.Range(0, 360f), 0);

            lowHills.Add(go);
        }

        // --- TẠO NÚI LỚN (BIG MOUNTAINS) ---
        for (int i = 0; i < TOTAL_BIG_MOUNTAINS; i++)
        {
            bool isLeft = i < 8;
            int localIndex = isLeft ? i : (i - 8);

            float zSpacing = 400f / 8f;
            float zPos = 15f + localIndex * zSpacing; // Sửa dấu - thành + để sinh ra phía TRƯỚC mặt

            int type = localIndex % 3;
            float baseRadius = type == 0 ? 60f : (type == 1 ? 70f : 50f);
            
            float xPos = baseRadius + (35f + Random.Range(0f, 15f)); 
            if (isLeft) xPos = -xPos;

            GameObject go = new GameObject("BigMountain_" + i);
            go.transform.SetParent(this.transform);
            
            MeshFilter mf = go.AddComponent<MeshFilter>();
            mf.mesh = type == 0 ? mesh0 : (type == 1 ? mesh1 : mesh2);
            
            MeshRenderer mr = go.AddComponent<MeshRenderer>();
            mr.material = mountainMaterial;
            mr.receiveShadows = true;

            go.transform.position = new Vector3(xPos, -10f, zPos);
            go.transform.rotation = Quaternion.Euler(0, Random.Range(0, 360f), 0);
            
            go.SetActive(false); // Ẩn mặc định
            bigMountains.Add(go);
        }
    }

    void Update()
    {
        if (WorldManager.Instance == null) return;

        float moveDistance = WorldManager.Instance.currentSpeed * Time.deltaTime;

        // Trôi đồi nhỏ
        foreach (var hill in lowHills)
        {
            hill.transform.position += Vector3.back * moveDistance;
            if (hill.transform.position.z < -15f) // Khi vượt qua lưng camera (Z=-15)
            {
                hill.transform.position += Vector3.forward * 400f; // Bơm trả lại lên đầu (chạy vô hạn)
            }
        }

        // Trôi núi lớn
        foreach (var mountain in bigMountains)
        {
            mountain.transform.position += Vector3.back * moveDistance;
            if (mountain.transform.position.z < -15f)
            {
                mountain.transform.position += Vector3.forward * 400f;
            }
        }
    }

    public void ShowBigMountains(bool show)
    {
        foreach (var m in bigMountains)
        {
            m.SetActive(show);
        }
    }

    // Hàm tạo Mesh Hình Nón giống hệt ConeGeometry của Three.js
    private Mesh CreateCone(float radius, float height, int segments)
    {
        Mesh mesh = new Mesh();

        int numVertices = segments + 2; // Đỉnh chóp + Tâm đáy + Các điểm xung quanh đáy
        Vector3[] vertices = new Vector3[numVertices];
        Vector2[] uvs = new Vector2[numVertices];
        
        // 0: Đỉnh chóp (Top)
        vertices[0] = new Vector3(0, height / 2f, 0);
        uvs[0] = new Vector2(0.5f, 1f);

        // 1: Tâm đáy (Bottom Center)
        vertices[1] = new Vector3(0, -height / 2f, 0);
        uvs[1] = new Vector2(0.5f, 0.5f);

        for (int i = 0; i < segments; i++)
        {
            float angle = (float)i / segments * Mathf.PI * 2f;
            float x = Mathf.Sin(angle) * radius;
            float z = Mathf.Cos(angle) * radius;
            
            vertices[i + 2] = new Vector3(x, -height / 2f, z);
            uvs[i + 2] = new Vector2((float)i / segments, 0f);
        }

        int[] triangles = new int[segments * 6]; // Mỗi phần (segment) có 1 tam giác mặt bên, 1 tam giác mặt đáy
        int triIndex = 0;

        for (int i = 0; i < segments; i++)
        {
            int current = i + 2;
            int next = (i + 1) % segments + 2;

            // Mặt bên (Side) - Hướng ra ngoài (Clockwise)
            triangles[triIndex++] = 0;
            triangles[triIndex++] = current;
            triangles[triIndex++] = next;

            // Mặt đáy (Bottom) - Hướng xuống dưới (Counter-Clockwise)
            triangles[triIndex++] = 1;
            triangles[triIndex++] = next;
            triangles[triIndex++] = current;
        }

        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.uv = uvs;
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();

        // Dịch chuyển tâm xuống đáy thay vì giữa để giống Three.js (ConeGeometry translate Y)
        Vector3[] verts = mesh.vertices;
        for (int i = 0; i < verts.Length; i++)
        {
            verts[i].y += height / 2f; 
        }
        mesh.vertices = verts;
        mesh.RecalculateBounds();

        return mesh;
    }
}
