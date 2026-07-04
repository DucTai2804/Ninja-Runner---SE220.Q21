using UnityEngine;

public class FallingObstacle : MonoBehaviour
{
    [Header("Falling Logic")]
    public bool isFalling = false;
    public float startY = 150f; // Bắt đầu ở độ cao 150m (siêu cao)
    private float targetY; 
    
    private float initialZ; // Lưu vị trí Z lúc vừa sinh ra để tính vận tốc rơi
    private float currentRotSpeedX = 0f;
    private float currentRotSpeedY = 0f;

    public void Setup(bool isFallingVariant)
    {
        isFalling = isFallingVariant;
        
        // Ép cứng độ cao xuất phát lên cực cao (600m) để ép tốc độ rơi phải thật nhanh 
        // dù thiên thạch rơi liên tục từ xa thay vì rơi ngắt quãng như Three.js
        startY = 600f; 

        if (isFalling)
        {
            targetY = transform.position.y;
        
            Vector3 pos = transform.position;
            pos.y = startY;
            transform.position = pos;
            
            initialZ = pos.z; // Ghi nhớ khoảng cách tổng
            isFalling = true;
        }
    }

    void Update()
    {
        if (WorldManager.Instance == null) return;

        // Bắt đầu rơi ngay lập tức từ trên cao xuống
        if (isFalling && transform.position.y > targetY)
        {
            // Tốc độ rơi = Quãng đường Y / Tổng quãng đường Z ban đầu
            float dropRate = (startY - targetY) / initialZ; 
            float actualDrop = dropRate * WorldManager.Instance.currentSpeed * Time.deltaTime;
            
            Vector3 pos = transform.position;
            pos.y -= actualDrop;
            
            // Xoay đa hướng (giảm tốc độ xoay đi một chút vì giờ thời gian rơi dài hơn Three.js)
            currentRotSpeedX = -1000f; 
            currentRotSpeedY = -800f;
            transform.Rotate(currentRotSpeedX * Time.deltaTime, currentRotSpeedY * Time.deltaTime, 0f, Space.Self);

            if (pos.y <= targetY) 
            {
                pos.y = targetY;
                isFalling = false; // Ngừng rơi, khít mặt đất!
            }
            transform.position = pos;
        }
        else if (!isFalling && (currentRotSpeedX != 0 || currentRotSpeedY != 0))
        {
            // Quán tính: Xoay chậm dần khi đã chạm đất
            // Giảm 7% mỗi khung hình (0.93) đúng chuẩn bản gốc
            float friction = Mathf.Pow(0.93f, Time.deltaTime * 60f);
            currentRotSpeedX *= friction; 
            currentRotSpeedY *= friction; 
            transform.Rotate(currentRotSpeedX * Time.deltaTime, currentRotSpeedY * Time.deltaTime, 0f, Space.Self);
            
            if (Mathf.Abs(currentRotSpeedX) < 10f && Mathf.Abs(currentRotSpeedY) < 10f)
            {
                currentRotSpeedX = 0f;
                currentRotSpeedY = 0f;
            }
        }
    }
}
