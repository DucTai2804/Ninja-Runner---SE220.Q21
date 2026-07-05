using System.Collections;
using System.Collections.Generic;
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

    private List<GameObject> activeClones = new List<GameObject>();

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

    void OnDisable()
    {
        // Khi Boss chết hoặc bị tắt, dọn dẹp sạch sẽ toàn bộ phân thân
        foreach(var c in activeClones)
        {
            if (c != null) Destroy(c);
        }
        activeClones.Clear();
    }

    void Update()
    {
        if (WorldManager.Instance == null || ClashManager.Instance != null && ClashManager.Instance.IsClashing()) 
            return; 

        float totalSpeed = WorldManager.Instance.currentSpeed + runSpeed;
        transform.position += Vector3.back * totalSpeed * Time.deltaTime;

        if (!isUsingRasengan && !isJumping)
        {
            actionTimer -= Time.deltaTime;
            if (actionTimer <= 0)
            {
                actionTimer = actionCooldown;
                StartCoroutine(FormationRoutine());
            }
        }

        if (transform.position.z < -20f)
        {
            gameObject.SetActive(false);
        }
    }

    IEnumerator FormationRoutine()
    {
        float waitTime = 1.0f;
        if (anim != null) 
        {
            anim.CrossFadeInFixedTime("kagebunshin", 0.1f);
            yield return null;
            yield return null;
            AnimatorStateInfo info = anim.GetCurrentAnimatorStateInfo(0);
            if (info.IsName("kagebunshin")) waitTime = info.length;
        }
        
        yield return new WaitForSeconds(waitTime);

        // Sinh đội hình
        bool isRasengan = Random.value < 0.5f; // 50% ra đội hình Tử Thần
        float laneDistance = 5f;
        Vector3 leftPos = new Vector3(-laneDistance, transform.position.y, transform.position.z);
        Vector3 rightPos = new Vector3(laneDistance, transform.position.y, transform.position.z);

        if (isRasengan)
        {
            // BẢN THỂ DÙNG RASENGAN
            isUsingRasengan = true;
            if (anim != null) anim.CrossFadeInFixedTime("rasengan", 0.1f);
            if (rasenganSphere != null) rasenganSphere.SetActive(true);
            runSpeed += rasenganSpeedBoost;

            // HÀNG 1: 2 PHÂN THÂN CẦM RASENGAN
            SpawnClone(leftPos, true, false, rasenganSpeedBoost);
            SpawnClone(rightPos, true, false, rasenganSpeedBoost);

            // HÀNG 2: 3 PHÂN THÂN NHẢY THEO SAU
            Vector3 backOffset = new Vector3(0, 0, 10f); // Lùi lại 10m phía sau
            SpawnClone(leftPos + backOffset, false, true, rasenganSpeedBoost);
            SpawnClone(transform.position + backOffset, false, true, rasenganSpeedBoost);
            SpawnClone(rightPos + backOffset, false, true, rasenganSpeedBoost);
        }
        else
        {
            // ĐỘI HÌNH CHẠY BÌNH THƯỜNG
            if (anim != null) anim.CrossFadeInFixedTime("run", 0.1f);
            SpawnClone(leftPos, false, false, 0f);
            SpawnClone(rightPos, false, false, 0f);
        }
    }

    void SpawnClone(Vector3 pos, bool useRasengan, bool constantJump, float speedBoost)
    {
        if (clonePrefab == null) return;
        GameObject cloneObj = Instantiate(clonePrefab, pos, transform.rotation);
        activeClones.Add(cloneObj);
        
        NarutoClone cloneScript = cloneObj.GetComponent<NarutoClone>();
        if (cloneScript != null)
        {
            if (useRasengan) cloneScript.ActivateRasengan(speedBoost);
            else if (constantJump) cloneScript.ActivateContinuousJump(speedBoost);
            else cloneScript.runSpeed += speedBoost; // Đảm bảo tốc độ luôn khớp bản thể
        }
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
