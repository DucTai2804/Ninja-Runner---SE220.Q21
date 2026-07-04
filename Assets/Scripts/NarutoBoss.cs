using System.Collections;
using UnityEngine;

public class NarutoBoss : MonoBehaviour
{
    public float runSpeed = 10f;
    public float rasenganSpeedBoost = 40f;
    
    private float baseRunSpeed;
    private Animator anim;
    
    [Header("Skills")]
    public GameObject clonePrefab; // Cần kéo prefab NarutoClone vào đây
    public GameObject rasenganSphere; // Cần tự tạo 1 Sphere màu xanh lam làm con của tay trái, rồi kéo vào đây
    
    public bool isUsingRasengan = false;
    
    private float actionCooldown = 3f;
    private float actionTimer = 0f;
    
    private bool isJumping = false;
    private float jumpDuration = 0.8f;
    private float extraJumpHeight = 2.0f;

    void Start()
    {
        anim = GetComponent<Animator>();
        if (anim == null) anim = GetComponentInChildren<Animator>();
        
        baseRunSpeed = runSpeed;
        actionTimer = actionCooldown;

        if (rasenganSphere != null)
        {
            rasenganSphere.SetActive(false);
        }
    }

    void Update()
    {
        if (WorldManager.Instance == null || ClashManager.Instance != null && ClashManager.Instance.IsClashing()) 
            return; // Dừng chạy khi đang Clash

        // Naruto chạy ngược chiều với mặt đất (tức là chạy về phía Sasuke)
        float totalSpeed = WorldManager.Instance.currentSpeed + runSpeed;
        transform.position += Vector3.back * totalSpeed * Time.deltaTime;

        // Xử lý AI tung chiêu
        if (!isUsingRasengan && !isJumping)
        {
            actionTimer -= Time.deltaTime;
            if (actionTimer <= 0)
            {
                actionTimer = actionCooldown;
                ChooseAction();
            }
            
            // Tỉ lệ nhảy khi đến gần Sasuke (khoảng 30m)
            if (transform.position.z < 30f && transform.position.z > 25f && Random.value < 0.3f)
            {
                StartCoroutine(JumpRoutine());
            }
        }

        // Tự hủy khi ra sau camera
        if (transform.position.z < -20f)
        {
            gameObject.SetActive(false);
        }
    }

    void ChooseAction()
    {
        if (Random.value < 0.5f)
        {
            StartCoroutine(KagebunshinRoutine());
        }
        else
        {
            StartCoroutine(RasenganRoutine());
        }
    }

    IEnumerator KagebunshinRoutine()
    {
        if (anim != null) anim.CrossFadeInFixedTime("kagebunshin", 0.1f);
        
        yield return new WaitForSeconds(0.5f); // Chờ animation kết ấn

        // Sinh ra 2 phân thân ở 2 làn bên
        float laneDistance = 5f; // Bằng với PlayerController
        Vector3 leftPos = new Vector3(-laneDistance, transform.position.y, transform.position.z);
        Vector3 rightPos = new Vector3(laneDistance, transform.position.y, transform.position.z);
        
        if (clonePrefab != null)
        {
            Instantiate(clonePrefab, leftPos, transform.rotation);
            Instantiate(clonePrefab, rightPos, transform.rotation);
        }

        yield return new WaitForSeconds(0.5f);
        if (anim != null) anim.CrossFadeInFixedTime("run", 0.1f);
    }

    IEnumerator RasenganRoutine()
    {
        isUsingRasengan = true;
        if (anim != null) anim.CrossFadeInFixedTime("rasengan", 0.1f);
        
        if (rasenganSphere != null) 
        {
            rasenganSphere.SetActive(true);
            // Mở rộng Rasengan siêu to khổng lồ để quét cả 3 làn
            rasenganSphere.transform.localScale = new Vector3(10f, 10f, 10f); 
        }

        yield return new WaitForSeconds(1.5f); // Tụ lực

        // Phóng nhanh về phía Sasuke
        runSpeed += rasenganSpeedBoost;
        if (anim != null) anim.CrossFadeInFixedTime("run", 0.1f);
    }

    IEnumerator JumpRoutine()
    {
        isJumping = true;
        if (anim != null) anim.CrossFadeInFixedTime("jump", 0.1f);
        
        float elapsed = 0f;
        float startY = transform.position.y; 

        while (elapsed < jumpDuration)
        {
            if (ClashManager.Instance != null && ClashManager.Instance.IsClashing())
            {
                yield return null; // Pause animation nếu đang clash
                continue;
            }

            elapsed += Time.deltaTime;
            float progress = elapsed / jumpDuration; 
            float bonusY = Mathf.Sin(progress * Mathf.PI) * extraJumpHeight;
            
            transform.position = new Vector3(transform.position.x, startY + bonusY, transform.position.z);
            yield return null;
        }

        isJumping = false;
        transform.position = new Vector3(transform.position.x, startY, transform.position.z);
        if (anim != null && !isUsingRasengan) anim.CrossFadeInFixedTime("run", 0.1f);
    }
    
    // Hàm này được gọi khi Hỏa Cầu (Fireball) đâm trúng
    public void Die()
    {
        // Thêm Particle khói bụi chỗ này nếu cần
        Debug.Log("Naruto đã bị tiêu diệt bởi Hỏa cầu!");
        gameObject.SetActive(false);
    }
}
