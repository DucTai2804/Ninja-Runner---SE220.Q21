using UnityEngine;

public class MoveObject : MonoBehaviour
{
    [Tooltip("Vị trí Z phía sau Camera để tự động tắt object")]
    public float destroyZ = -10f; 

    void Update()
    {
        // Kiểm tra xem WorldManager đã có chưa
        if (WorldManager.Instance == null) return;

        // Di chuyển vật thể lùi về phía Camera (Trục Z âm)
        // Vector3.back = (0, 0, -1)
        transform.position += Vector3.back * WorldManager.Instance.currentSpeed * Time.deltaTime;

        // Nếu bay qua khỏi lưng Camera thì ẩn đi (Đưa về Object Pool)
        if (transform.position.z < destroyZ)
        {
            gameObject.SetActive(false);
        }
    }
}
