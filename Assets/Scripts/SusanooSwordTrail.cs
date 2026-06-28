using UnityEngine;
using System.Collections.Generic;

public class SusanooSwordTrail : MonoBehaviour
{
    private Mesh mesh;
    private MeshFilter mf;
    private MeshRenderer mr;
    
    private Vector3 tipLocal;
    private Vector3 hiltLocal;

    private float trailTimer = 0f;
    // Tăng nhẹ lên 0.15s để dải ngoài cùng vươn dài và đẹp hơn
    private float trailDuration = 0.15f;  

    struct TrailPoint
    {
        public Vector3 tipPosition;
        public Vector3 hiltPosition;
        public float timeCreated;
    }

    private List<TrailPoint> points = new List<TrailPoint>();

    // TỐI ƯU HÓA: Dùng List tái sử dụng để chặn rác bộ nhớ (Garbage Collection) mỗi khung hình
    private List<Vector3> verticesList = new List<Vector3>();
    private List<Vector2> uvsList = new List<Vector2>();
    private List<Color> colorsList = new List<Color>();
    private List<int> trianglesList = new List<int>();

    void Start()
    {
        MeshFilter targetMf = GetComponent<MeshFilter>();
        hiltLocal = Vector3.zero;
        tipLocal = Vector3.forward;

        if (targetMf != null && targetMf.sharedMesh != null)
        {
            Vector3 center = targetMf.sharedMesh.bounds.center;
            float maxZ = targetMf.sharedMesh.bounds.max.z;
            
            // BÍ QUYẾT TẠO HÌNH LƯỠI LIỀM NINJA STORM 4:
            // Tuyệt đối không bắt đầu vệt chém từ chuôi kiếm (0f) vì sẽ tạo ra hình quạt khổng lồ thô kệch!
            // Vệt chém phủ khoảng 40% ngoài cùng của kiếm
            hiltLocal = new Vector3(center.x, center.y, maxZ * 0.60f); 
            tipLocal = new Vector3(center.x, center.y, maxZ * 1.05f); 
            
            if (maxZ <= 0) tipLocal.z = 1f; 
        }

        // TẠO OBJECT RIÊNG ĐỂ GIỮ CUSTOM MESH
        GameObject trailObj = new GameObject("CustomSwordTrailMesh");
        trailObj.transform.SetParent(this.transform, false);
        trailObj.transform.localPosition = Vector3.zero;
        trailObj.transform.localRotation = Quaternion.identity;

        mf = trailObj.AddComponent<MeshFilter>();
        mr = trailObj.AddComponent<MeshRenderer>();

        mesh = new Mesh();
        mf.mesh = mesh;

        // NÂNG CẤP ĐỒ HỌA: Dùng Shader Phát Sáng (Additive) để tạo hiệu ứng Năng lượng Chakra
        Shader additiveShader = Shader.Find("Legacy Shaders/Particles/Additive");
        if (additiveShader == null) additiveShader = Shader.Find("Mobile/Particles/Additive"); // Fallback
        if (additiveShader == null) additiveShader = Shader.Find("Sprites/Default"); // Tệ nhất thì xài lại bản cũ
        
        Material trailMat = new Material(additiveShader);
        trailMat.mainTexture = CreateSoftTexture();
        mr.material = trailMat;
        mr.sortingOrder = 10;
    }

    public void ActivateTrail(float duration)
    {
        // BÍ QUYẾT DIỆT LỖI GAI NHỌN: 
        // Nếu bắt đầu một nhát chém MỚI, phải xóa sạch dữ liệu của nhát chém cũ.
        // Tránh tình trạng nhát chém mới nối một đường thẳng khổng lồ vào nhát chém cũ đang tàn dư!
        if (trailTimer <= 0)
        {
            points.Clear();
            if (mesh != null) mesh.Clear();
        }
        trailTimer = duration;
    }

    void LateUpdate()
    {
        bool emitting = false;
        if (trailTimer > 0)
        {
            trailTimer -= Time.deltaTime;
            emitting = true;
        }

        if (emitting)
        {
            points.Add(new TrailPoint()
            {
                tipPosition = transform.TransformPoint(tipLocal),
                hiltPosition = transform.TransformPoint(hiltLocal),
                timeCreated = Time.time
            });
        }

        // Hủy các điểm đã quá tuổi thọ (Tạo hiệu ứng đuôi mờ dần)
        while (points.Count > 0 && Time.time - points[0].timeCreated > trailDuration)
        {
            points.RemoveAt(0);
        }

        if (!emitting && points.Count == 0)
        {
            mesh.Clear();
            return;
        }

        BuildMesh();
    }

    void BuildMesh()
    {
        if (points.Count < 2)
        {
            mesh.Clear();
            return;
        }

        int widthSegments = 10; 
        int numVerticesPerSlice = widthSegments + 1;
        
        // Tái sử dụng bộ nhớ thay vì tạo mảng mới (Zero Allocation)
        verticesList.Clear();
        uvsList.Clear();
        colorsList.Clear();
        trianglesList.Clear();

        // TUYỆT KỸ NINJA STORM 4 - PHIÊN BẢN HOÀN MỸ: Sợi Năng Lượng Độc Lập (Fibrous Strands)
        // Nâng lên 16 dải để thu hẹp khoảng cách giữa chúng, tạo ra các sợi dày đặc hơn.
        int numRibbons = 16; 

        for (int r = 0; r < numRibbons; r++)
        {
            float tInner = (float)r / numRibbons;
            float tOuter = (float)(r + 1) / numRibbons;
            float tCenter = (tInner + tOuter) / 2f;
            
            // Dải trong cùng bốc hơi cực nhanh (chỉ sống 20% thời gian), dải ngoài cùng sống 100%
            float ribbonDuration = trailDuration * Mathf.Lerp(0.2f, 1.0f, Mathf.Pow(tOuter, 2f));

            for (int i = 0; i < points.Count; i++)
            {
                float age = Time.time - points[i].timeCreated;
                float timeAlpha = 1f - (age / ribbonDuration);
                if (timeAlpha < 0) timeAlpha = 0;

                float relativePos = points.Count > 1 ? (float)i / (points.Count - 1) : 1f;
                // Giảm lũy thừa xuống 0.35 để các dải GIỮ ĐỘ DÀY lâu hơn, khe hở sẽ vô cùng nhỏ 
                // và chỉ thực sự vuốt nhọn sắc lẹm ở khúc đuôi cùng.
                float widthMult = Mathf.Pow(relativePos, 0.35f); 

                Vector3 baseHilt = mf.transform.InverseTransformPoint(points[i].hiltPosition);
                Vector3 baseTip = mf.transform.InverseTransformPoint(points[i].tipPosition);

                // Tọa độ gốc của sợi năng lượng này
                Vector3 ribbonInnerBase = Vector3.Lerp(baseHilt, baseTip, tInner);
                Vector3 ribbonOuterBase = Vector3.Lerp(baseHilt, baseTip, tOuter);
                Vector3 ribbonCenterBase = Vector3.Lerp(baseHilt, baseTip, tCenter);

                // BÍ QUYẾT TRIỆT TIÊU CHỤM MŨI KIẾM: 
                // Mỗi sợi tự vuốt nhọn về TÂM CỦA CHÍNH NÓ! 
                // Ở đầu kiếm, chúng dính chặt vào nhau. Ở đuôi kiếm, chúng tự tách nhau ra thành 10 cái kim nhọn!
                Vector3 dynamicInner = Vector3.Lerp(ribbonCenterBase, ribbonInnerBase, widthMult);
                Vector3 dynamicOuter = Vector3.Lerp(ribbonCenterBase, ribbonOuterBase, widthMult);

                float localAlpha = timeAlpha * Mathf.Pow(relativePos, 0.5f);

                // Màu Chakra Tím rực rỡ và Viền Trắng chói lóa (Fake HDR)
                Color purpleChakra = new Color(2f, 0f, 3f, localAlpha); 
                Color hotWhite = new Color(3f, 3f, 3f, localAlpha);
                
                // Chỉ Sợi ngoài cùng mới có viền trắng sắc lẹm
                Color colorInner = purpleChakra;
                Color colorOuter = (r == numRibbons - 1) ? hotWhite : purpleChakra;

                // Frame mới nhất luôn sáng nhất
                if (i == points.Count - 1 && trailTimer > 0) 
                {
                    colorOuter = Color.white;
                    if (r == numRibbons - 1) colorInner = Color.white;
                }

                // Add 2 vertices cho mặt cắt của Sợi này
                verticesList.Add(dynamicInner);
                verticesList.Add(dynamicOuter);
                
                uvsList.Add(new Vector2(relativePos, tInner));
                uvsList.Add(new Vector2(relativePos, tOuter));
                
                colorsList.Add(colorInner);
                colorsList.Add(colorOuter);

                if (i < points.Count - 1)
                {
                    int v0 = (r * points.Count * 2) + (i * 2);
                    int v1 = v0 + 1;
                    int v2 = v0 + 2;
                    int v3 = v2 + 1;

                    trianglesList.Add(v0);
                    trianglesList.Add(v1);
                    trianglesList.Add(v2);
                    trianglesList.Add(v1);
                    trianglesList.Add(v3);
                    trianglesList.Add(v2);
                }
            }
        }

        mesh.Clear();
        mesh.SetVertices(verticesList);
        mesh.SetUVs(0, uvsList);
        mesh.SetColors(colorsList);
        mesh.SetTriangles(trianglesList, 0);
        mesh.RecalculateBounds();
    }

    Texture2D CreateSoftTexture()
    {
        int size = 64;
        Texture2D tex = new Texture2D(size, size, TextureFormat.ARGB32, false);
        
        // Khóa WrapMode để chặn tuyệt đối hiện tượng viền đen (UV Bleeding) ở các mép
        tex.wrapMode = TextureWrapMode.Clamp; 
        
        for (int y = 0; y < size; y++)
        {
            // y = 0 (chuôi kiếm), y = 63 (mũi kiếm)
            // Chuôi kiếm mờ dần vào không khí, nhưng Mũi Kiếm phải RỰC SÁNG VÀ SẮC LẸM
            float alphaY = (y / (float)(size - 1)); 
            for (int x = 0; x < size; x++)
            {
                tex.SetPixel(x, y, new Color(1f, 1f, 1f, Mathf.Pow(alphaY, 0.5f)));
            }
        }
        tex.Apply();
        return tex;
    }
}
