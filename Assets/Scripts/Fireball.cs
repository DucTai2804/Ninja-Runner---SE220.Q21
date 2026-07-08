using UnityEngine;

public class Fireball : MonoBehaviour
{
    [Header("Thời gian tồn tại (giây)")]
    [Range(1f, 10f)]
    public float lifeTime = 2.5f;

    [Header("Số lượng chướng ngại nhỏ tối đa có thể phá hủy")]
    public int maxObstaclesToDestroy = 1;
    private int destroyedCount = 0;

    void Start()
    {
        // 1. Phải có Rigidbody thì OnTiggerEnter mới hoạt động được với các bẫy (đang là Trigger)
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb == null)
        {
            rb = gameObject.AddComponent<Rigidbody>();
            rb.isKinematic = true;
            rb.useGravity = false;
        }

        // 2. Cầu lửa phát sáng rực rỡ (Point Light)
        Light l = GetComponent<Light>();
        if (l == null)
        {
            l = gameObject.AddComponent<Light>();
            l.type = LightType.Point;
            l.color = new Color(1f, 0.4f, 0f); // Cam lửa
            l.range = 60f; // Phạm vi phủ sóng cực rộng
            l.intensity = 150f; // Bơm cường độ sáng lên mức đột biến để tạo luồng sáng rực rỡ
        }

        // 3. Các hiệu ứng hình ảnh (Particle) thường KHÔNG CÓ Collider. Bắt buộc phải gắn thêm!
        Collider col = GetComponent<Collider>();
        if (col == null)
        {
            SphereCollider sphere = gameObject.AddComponent<SphereCollider>();
            sphere.radius = 4.0f; // Tăng lên 4.0f để bao trùm cả một làn đường, không thể trượt được!
            sphere.isTrigger = true;
        }
        else
        {
            col.isTrigger = true;
        }

        // Hẹn giờ tự hủy sau lifeTime giây, dùng chung hàm tách Particle
        Invoke("DestroyFireball", lifeTime);
    }

    void OnTriggerEnter(Collider other)
    {
        Debug.Log("🔥 FIREBALL chạm vào: " + other.name + " | Tag: " + other.tag);

        // Nếu đâm trúng bẫy khổng lồ (vách núi, thiên thạch bự) -> Cầu lửa vỡ nát
        if (other.name.Contains("Giant") || other.name.Contains("MountainWall"))
        {
            Debug.Log("🔥 FIREBALL vỡ nát do chạm trúng vách núi/thiên thạch!");
            DestroyFireball();
            return;
        }

        NarutoBoss boss = other.GetComponentInParent<NarutoBoss>();
        if (boss != null)
        {
            if (boss.isUsingRasengan)
            {
                // Bất kể Bản thể hay Phân thân, nếu có Rasengan thì đều chặn được Hỏa Cầu
                Debug.Log("🔥 FIREBALL bị Rasengan cản lại!");
                DestroyFireball();
                return;
            }
            else
            {
                // Hỏa cầu thiêu rụi Naruto chạy bộ (Bản thể hoặc Phân thân)
                Debug.Log("🔥 FIREBALL đã thiêu rụi " + (boss.isClone ? "Phân thân" : "Bản thể") + "! +500 điểm");
                if (UIManager.Instance != null) UIManager.Instance.score += 500f;
                
                // Diệt tận gốc Object thay vì chỉ tắt Collider (khắc phục lỗi đi xuyên qua)
                if (boss.isClone)
                {
                    Destroy(boss.gameObject);
                }
                else
                {
                    boss.gameObject.SetActive(false);
                }
                
                DestroyFireball();
                return;
            }
        }

        // Nếu đâm trúng bẫy nhỏ (đá con, phi tiêu)
        if (other.CompareTag("Obstacle") || other.name.Contains("Rock") || other.name.Contains("Shuriken"))
        {
            Debug.Log("🔥 FIREBALL đã thiêu rụi bẫy: " + other.name + " +500 điểm");
            if (UIManager.Instance != null) UIManager.Instance.score += 500f;
            
            // Tắt chướng ngại vật (Tắt object cha nếu có)
            if (other.transform.parent != null && other.transform.parent.CompareTag("Obstacle"))
            {
                other.transform.parent.gameObject.SetActive(false);
            }
            else
            {
                other.gameObject.SetActive(false);
            }
            
            destroyedCount++;
            
            // Nếu đã phá đủ chỉ tiêu thì quả cầu lửa tự hủy
            if (destroyedCount >= maxObstaclesToDestroy)
            {
                DestroyFireball();
            }
            return;
        }
    }

    void DestroyFireball()
    {
        // Tránh gọi 2 lần (nếu vừa hết giờ vừa đâm trúng)
        CancelInvoke("DestroyFireball");

        // Gọi âm thanh ngắt lửa từ từ
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.StopKaton();
        }

        // Tách hệ thống Particle ra khỏi quả cầu để nó không bị biến mất ngay lập tức
        ParticleSystem ps = GetComponentInChildren<ParticleSystem>();
        if (ps != null)
        {
            ps.transform.SetParent(null);
            
            // Ngừng phát hạt mới
            var emission = ps.emission;
            emission.enabled = false;
            
            // Tự hủy object Particle sau khi các hạt cuối cùng bay hết vòng đời
            float particleMaxLife = ps.main.startLifetime.constantMax;
            if (particleMaxLife <= 0) particleMaxLife = 1.5f; // Đề phòng trường hợp chưa gán hằng số
            Destroy(ps.gameObject, particleMaxLife);
        }
        
        // Hủy phần quả cầu (lõi và vỏ aura) ngay lập tức
        Destroy(gameObject);
    }
}
