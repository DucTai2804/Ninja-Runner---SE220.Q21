using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;

public class HitboxDebugger : MonoBehaviour
{
    private bool showHitboxes = false;
    private List<GameObject> visualObjects = new List<GameObject>();
    private Material debugMaterial;
    private Material triggerMaterial;

    void Start()
    {
        // Sử dụng Shader chuẩn của URP hỗ trợ trong suốt
        Shader urpShader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
        
        debugMaterial = new Material(urpShader);
        debugMaterial.SetColor("_BaseColor", new Color(0f, 1f, 0f, 0.4f)); 
        debugMaterial.SetFloat("_Surface", 1); // Transparent
        
        triggerMaterial = new Material(urpShader);
        triggerMaterial.SetColor("_BaseColor", new Color(1f, 0f, 0f, 0.4f)); 
        triggerMaterial.SetFloat("_Surface", 1); // Transparent
    }

    void Update()
    {
        if (Keyboard.current != null && Keyboard.current.hKey.wasPressedThisFrame)
        {
            showHitboxes = !showHitboxes;
            
            if (showHitboxes) GenerateHitboxVisuals();
            else ClearHitboxVisuals();
            
            Debug.Log("Hitbox Debugger: " + (showHitboxes ? "ON" : "OFF"));
        }

        // Cập nhật liên tục để bắt kịp các cục đá mới sinh ra và theo sát vị trí
        if (showHitboxes)
        {
            GenerateHitboxVisuals();
        }
    }

    void GenerateHitboxVisuals()
    {
        ClearHitboxVisuals(); // Tránh tạo trùng lặp

        // Tìm tất cả các Collider đang có trên bản đồ
        Collider[] colliders = FindObjectsByType<Collider>(FindObjectsSortMode.None);
        foreach (Collider col in colliders)
        {
            GameObject visual = null;

            // Dựng lồng tương ứng với từng loại hình học
            if (col is BoxCollider box)
            {
                visual = GameObject.CreatePrimitive(PrimitiveType.Cube);
                visual.transform.SetParent(col.transform, false);
                visual.transform.localPosition = box.center;
                visual.transform.localScale = box.size;
            }
            else if (col is CapsuleCollider cap)
            {
                visual = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                visual.transform.SetParent(col.transform, false);
                visual.transform.localPosition = cap.center;
                // Capsule mặc định của Unity cao 2, bán kính 0.5 nên phải chia tỷ lệ
                visual.transform.localScale = new Vector3(cap.radius * 2f, cap.height / 2f, cap.radius * 2f);
                
                // Nếu capsule xoay ngang hoặc dọc
                if (cap.direction == 0) visual.transform.localRotation = Quaternion.Euler(0, 0, 90);
                else if (cap.direction == 2) visual.transform.localRotation = Quaternion.Euler(90, 0, 0);
            }
            else if (col is SphereCollider sph)
            {
                visual = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                visual.transform.SetParent(col.transform, false);
                visual.transform.localPosition = sph.center;
                visual.transform.localScale = Vector3.one * sph.radius * 2f;
            }

            if (visual != null)
            {
                // Xóa bỏ thành phần vật lý của cái lồng mờ để nó không tông vào ai cả
                Destroy(visual.GetComponent<Collider>());
                
                // Đổ màu xanh/đỏ
                MeshRenderer renderer = visual.GetComponent<MeshRenderer>();
                renderer.material = col.isTrigger ? triggerMaterial : debugMaterial;
                
                // Không để cái lồng tạo ra bóng (Shadow)
                renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

                visualObjects.Add(visual);
            }
        }
    }

    void ClearHitboxVisuals()
    {
        foreach (var obj in visualObjects)
        {
            if (obj != null) Destroy(obj);
        }
        visualObjects.Clear();
    }
}
