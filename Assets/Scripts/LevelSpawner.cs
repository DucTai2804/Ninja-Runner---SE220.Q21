using UnityEngine;

public class LevelSpawner : MonoBehaviour
{
    [Header("Spawn Settings")]
    public float laneDistance = 5.0f; // Mở rộng để phù hợp với PlayerController (5m)
    public float spawnZ = 80f; // Tọa độ Z tít đằng xa để sinh chướng ngại vật
    
    // Khoảng cách (mét) giữa các mảng chướng ngại vật
    public float distanceBetweenSpawns = 25f; 
    private float distanceTraveled = 0f;

    void Update()
    {
        if (WorldManager.Instance == null || ObjectPooler.Instance == null) return;

        // Tính quãng đường mà game đã trôi được = Vận tốc * Thời gian
        // Điều này đảm bảo khi game càng nhanh, đá sinh ra càng lẹ, nhịp độ luôn chuẩn xác
        distanceTraveled += WorldManager.Instance.currentSpeed * Time.deltaTime;

        if (distanceTraveled >= distanceBetweenSpawns)
        {
            SpawnObstacles();
            distanceTraveled = 0f; // Reset lại bộ đếm quãng đường
        }
    }

    void SpawnObstacles()
    {
        // 1. Thuật toán chọn 1 đến 2 làn ngẫu nhiên (để luôn có ít nhất 1 làn trống cho Sasuke chạy)
        int[] lanes = { -1, 0, 1 };
        int obstacleCount = Random.Range(1, 3); // Trả về 1 hoặc 2 cục đá

        // Xáo trộn mảng làn đường (Fisher-Yates Shuffle) để không bị trùng lặp
        for (int i = 0; i < lanes.Length; i++)
        {
            int temp = lanes[i];
            int r = Random.Range(i, lanes.Length);
            lanes[i] = lanes[r];
            lanes[r] = temp;
        }

        // 2. Rút đá từ Object Pool ra và đặt vào làn tương ứng
        for (int i = 0; i < obstacleCount; i++)
        {
            float targetX = lanes[i] * laneDistance;
            Vector3 spawnPos = new Vector3(targetX, 0f, spawnZ);
            
            // Gọi tag "Rock" từ ObjectPooler
            GameObject rock = ObjectPooler.Instance.SpawnFromPool("Rock", spawnPos, Quaternion.identity);
            if (rock != null)
            {
                // Tăng kích thước (Scale) lên gấp rưỡi để tạo cảm giác đồ sộ, nguy hiểm hơn!
                // Random 20% cơ hội tảng đá này sẽ biến thành THIÊN THẠCH KHỔNG LỒ RƠI TỪ TRÊN TRỜI
                bool isMeteor = Random.value < 0.2f;

                float scaleXZ = Random.Range(1.2f, 1.6f); 
                float scaleY = Random.Range(1.5f, 2.5f);  
                
                // Nếu là thiên thạch, phóng to kích thước lên gấp đôi cho hoành tráng!
                if (isMeteor) 
                {
                    scaleXZ *= 2.0f;
                    scaleY *= 1.5f;
                }
                
                rock.transform.localScale = new Vector3(scaleXZ, scaleY, scaleXZ);
                
                float baseHeight = 1.2f * scaleY; 
                float sinkingDepth = 0.8f; 
                if (isMeteor) sinkingDepth *= 1.5f; // Lún sâu hơn một chút vì nó quá to
                
                rock.transform.position = new Vector3(targetX, baseHeight - sinkingDepth, spawnZ);

                float randomRotY = Random.Range(0f, 360f); 
                rock.transform.rotation = Quaternion.Euler(0f, randomRotY, 0f);

                // --- GẮN LOGIC RƠI ---
                FallingObstacle fallingScript = rock.GetComponent<FallingObstacle>();
                if (fallingScript == null) 
                {
                    fallingScript = rock.AddComponent<FallingObstacle>();
                }
                // Nếu isMeteor = true, nó sẽ bị ném lên trời. Nếu false, nó vẫn nằm yên dưới đất
                fallingScript.Setup(isMeteor);
            }
        }
    }
}
