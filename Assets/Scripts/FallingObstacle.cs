using UnityEngine;

public class FallingObstacle : MonoBehaviour
{
    [Header("Falling Logic")]
    public bool isFalling = false;
    public float startY = 100f; // Bắt đầu ở độ cao 100m
    private float targetY; // Lưu lại tọa độ chuẩn trên mặt đất
    public float triggerZ = 60f; // Khi vật thể trôi đến mốc Z này thì bắt đầu thả rơi
    public float fallSpeed = 250f; // Tốc độ rơi sấm sét y hệt Three.js

    private bool hasTriggered = false;

    // Hàm này được LevelSpawner gọi ngay sau khi vừa sinh đá ra
    public void Setup(bool isFallingVariant)
    {
        isFalling = isFallingVariant;
        hasTriggered = false;
        
        if (isFalling)
        {
            // LevelSpawner vừa đặt đá chuẩn xác xuống mặt đất, ta lưu tọa độ này lại làm Đích đến
            targetY = transform.position.y; 
            
            // Lập tức đưa tảng đá lên tận chín tầng mây
            Vector3 pos = transform.position;
            pos.y = startY; 
            transform.position = pos;
        }
    }

    void Update()
    {
        if (!isFalling) return;

        // Bắt đầu dội bom khi tảng đá bay vào vùng Trigger
        if (!hasTriggered && transform.position.z <= triggerZ)
        {
            hasTriggered = true;
        }

        // Thực hiện hành động rơi
        if (hasTriggered && transform.position.y > targetY)
        {
            Vector3 pos = transform.position;
            pos.y -= fallSpeed * Time.deltaTime;
            
            if (pos.y < targetY) 
            {
                pos.y = targetY;
                isFalling = false; // Ngừng rơi, vừa vặn khít mặt đất!
                
                // (Tương lai có thể thêm hàm rung màn hình Camera tại đây!)
            }
            transform.position = pos;
        }
    }
}
