using UnityEngine;

public class GroundScroller : MonoBehaviour
{
    [Header("Ground Settings")]
    public float planeWidth = 250f;
    public float planeLength = 200f;
    public Texture groundTexture;
    
    private GameObject[] groundPlanes;

    void Start()
    {
        Material groundMat = null;
#if UNITY_EDITOR
        groundMat = UnityEditor.AssetDatabase.LoadAssetAtPath<Material>("Assets/GroundMat.mat");
#endif

        if (groundMat == null)
        {
            groundMat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        }

        // Đổi thẳng Shader của vật liệu sang Simple Lit (Blinn-Phong) thay vì Lit (PBR)
        Shader simpleLit = Shader.Find("Universal Render Pipeline/Simple Lit");
        if (simpleLit != null) groundMat.shader = simpleLit;

        // Bắt buộc gán lại Texture SAU KHI đổi shader để Unity không làm mất ảnh (chống trắng xóa)
        if (groundTexture != null)
        {
            groundMat.mainTexture = groundTexture;
            groundMat.SetTexture("_BaseMap", groundTexture); 
        }
        // Khuếch đại màu trắng lên 1.5 lần để làm tông màu mặt đất/làn đường sáng hơn
        groundMat.color = new Color(1.5f, 1.5f, 1.5f); 

        // TẮT Emission đi vì Simple Lit + Ánh sáng 3.0 đã quá đủ chói lóa.
        // Cố tình nhồi thêm Emission 70% sẽ biến mọi thứ thành màu trắng xóa!
        groundMat.DisableKeyword("_EMISSION");
        groundMat.SetColor("_EmissionColor", Color.black); 

        // Tiling để lặp lại texture cho tự nhiên
        groundMat.mainTextureScale = new Vector2(20f, 20f);

        // Plane mặc định của Unity có kích thước 10x10. Cần scale lại.
        Vector3 scale = new Vector3(planeWidth / 10f, 1f, planeLength / 10f);

        groundPlanes = new GameObject[3];
        for (int i = 0; i < 3; i++)
        {
            GameObject plane = GameObject.CreatePrimitive(PrimitiveType.Plane);
            plane.name = "GroundSegment_" + i;
            plane.transform.SetParent(this.transform);
            plane.transform.localScale = scale;
            
            // Xếp 3 tấm liên tiếp nhau dọc trục Z VỀ PHÍA TRƯỚC (Z Dương)
            plane.transform.position = new Vector3(0, -0.01f * i, i * planeLength);
            
            plane.GetComponent<Renderer>().material = groundMat;
            
            groundPlanes[i] = plane;
        }
    }

    void Update()
    {
        if (WorldManager.Instance == null) return;

        float moveDistance = WorldManager.Instance.currentSpeed * Time.deltaTime;

        for (int i = 0; i < groundPlanes.Length; i++)
        {
            // Cuộn mặt đất lùi về sau (về hướng -Z)
            groundPlanes[i].transform.position += Vector3.back * moveDistance;

            // Nếu tấm này trôi hoàn toàn qua khỏi lưng camera (Z < -planeLength)
            if (groundPlanes[i].transform.position.z < -planeLength)
            {
                // Đẩy nó lên phía trước (bù cho 3 tấm)
                groundPlanes[i].transform.position += Vector3.forward * (3 * planeLength);
            }
        }
    }
}
