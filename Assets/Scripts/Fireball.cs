using UnityEngine;

public class Fireball : MonoBehaviour
{
    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Obstacle"))
        {
            // Tắt chướng ngại vật (Hoặc có thể sinh hiệu ứng nổ nhỏ ở đây)
            other.gameObject.SetActive(false);
            
            // Hủy luôn quả cầu lửa sau khi đâm trúng
            Destroy(gameObject);
        }
    }
}
