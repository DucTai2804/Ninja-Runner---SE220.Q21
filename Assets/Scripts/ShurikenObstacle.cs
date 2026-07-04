using UnityEngine;

public class ShurikenObstacle : MonoBehaviour
{
    // Thông số có thể tinh chỉnh từ bên ngoài (LevelSpawner sẽ gán vào)
    [HideInInspector] public float extraSpeed = 24f; 
    [HideInInspector] public float rotationSpeed = 1718f; 
    
    // Nếu phi tiêu bị lật 90 độ, bạn có thể chỉnh bù góc tại đây (ví dụ: X = 90)
    public Vector3 meshRotationOffset = Vector3.zero;

    void Start()
    {
        // Áp dụng góc xoay bù trừ nếu model gốc bị lật khi ném vào ObjectPool
        if (meshRotationOffset != Vector3.zero) {
            transform.GetChild(0).localRotation = Quaternion.Euler(meshRotationOffset); // Cần bọc mesh vào 1 object con
        }
    }

    void Update()
    {
        // Phi tiêu đứng thẳng đối diện người chơi thì phải xoay quanh trục Z (giống đĩa cưa)
        transform.Rotate(0, 0, rotationSpeed * Time.deltaTime, Space.Self);

        // Lao nhanh về phía người chơi (Người chơi chạy hướng tới, phi tiêu lao ngược lại)
        // Vì MoveObject đã trừ dần Z theo currentSpeed, ta chỉ cần trừ thêm extraSpeed
        if (WorldManager.Instance != null && WorldManager.Instance.currentSpeed > 0)
        {
            transform.position -= new Vector3(0, 0, extraSpeed * Time.deltaTime);
        }
    }
}
