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
    public GameObject clonePrefab; // Giờ đây chỉ cần kéo chính prefab NarutoBoss vào đây!
    public GameObject rasenganSphere; 
    
    public bool isUsingRasengan = false;
    public bool isFirstSpawn = false; // Nhận từ LevelSpawner
    public bool willUseRasengan = false; // Quyết định trước khi vào tầm nhìn
    public bool forceTestMode = false; // Ngăn không cho Start() ghi đè
    
    [Header("Clone Logic (Auto Handled)")]
    public bool isClone = false;
    private bool isContinuousJump = false;
    private bool hasDecidedJump = false;
    private bool hasDoneFormation = false; // Ngăn gọi Kagebunshin nhiều lần
    
    private float actionCooldown = 3f;
    private float actionTimer = 0f;
    
    private bool isJumping = false;
    private float jumpDuration = 0.8f;
    private float extraJumpHeight = 2.0f;

    private List<GameObject> activeClones = new List<GameObject>();

    void Awake()
    {
        anim = GetComponent<Animator>();
        if (anim == null) anim = GetComponentInChildren<Animator>();
        
        if (anim != null) anim.applyRootMotion = false; // Ngăn chặn animation tự ý di chuyển mô hình (gây lệch làn)
        
        baseRunSpeed = runSpeed;
        actionTimer = actionCooldown;

        if (rasenganSphere != null && !isClone)
        {
            rasenganSphere.SetActive(false);
        }
    }

    void Start()
    {
        if (!isClone && !forceTestMode)
        {
            if (!isFirstSpawn) 
            {
                willUseRasengan = Random.value < 0.4f; // Từ lần thứ 2 trở đi, 40% gọi Rasengan
            }
        }
    }

    void OnDisable()
    {
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.StopNarutoAudios();
            AudioManager.Instance.PlayKagebunshinFade();
        }

        if (isClone) return; 
        
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

        if (!isUsingRasengan && !isContinuousJump)
        {
            // --- LOGIC NHẢY KHI ÁP SÁT DÀNH CHO KAGEBUNSHIN BÌNH THƯỜNG ---
            // Tính toán khoảng cách kích hoạt sao cho đỉnh cú nhảy nằm đúng vị trí Sasuke (z = 0)
            float triggerZ = totalSpeed * (jumpDuration / 2f);
            
            if (transform.position.z <= triggerZ && !hasDecidedJump)
            {
                hasDecidedJump = true;
                if (Random.value < 0.5f) // 50% tỉ lệ nhảy (độc lập cho cả Boss và Clone)
                {
                    StartCoroutine(JumpRoutine());
                }
            }
        }

        if (!isClone)
        {
            // --- LOGIC CỦA BẢN THỂ BÁO (Triệu hồi phân thân) ---
            if (!isUsingRasengan && !isJumping && !hasDoneFormation)
            {
                float triggerDist = willUseRasengan ? 180f : 120f; // Rasengan ở 180m, Kagebunshin thường ở 120m
                if (transform.position.z <= triggerDist)
                {
                    hasDoneFormation = true;
                    StartCoroutine(FormationRoutine());
                }
            }
        }

        if (transform.position.z < -20f)
        {
            if (isClone) Destroy(gameObject);
            else gameObject.SetActive(false);
        }
    }

    IEnumerator FormationRoutine()
    {
        if (anim != null) 
        {
            anim.CrossFadeInFixedTime("kagebunshin", 0.1f);
            if (AudioManager.Instance != null) AudioManager.Instance.PlayKagebunshinStart();
            yield return new WaitForSeconds(0.15f); // Đợi Unity chuyển state xong
            
            // Chờ cho animation Kagebunshin thực sự kết thúc (tránh bị lệch nhịp)
            while (anim.GetCurrentAnimatorStateInfo(0).IsName("kagebunshin") && 
                   anim.GetCurrentAnimatorStateInfo(0).normalizedTime < 0.95f)
            {
                yield return null;
            }
        }
        else
        {
            yield return new WaitForSeconds(1.0f);
        }

        bool isRasengan = willUseRasengan;
        
        float laneDistance = 3.5f;
        
        // Xác định vị trí các làn trống để gọi phân thân
        // Có 3 làn: X = -5, 0, 5
        float currentX = transform.position.x;
        List<float> availableLanes = new List<float> { -laneDistance, 0f, laneDistance };
        
        // Xóa làn mà Bản thể đang đứng (tìm làn gần với currentX nhất để tránh sai số thập phân)
        float closestLane = 0f;
        float minDiff = 999f;
        foreach (float lane in availableLanes)
        {
            if (Mathf.Abs(currentX - lane) < minDiff)
            {
                minDiff = Mathf.Abs(currentX - lane);
                closestLane = lane;
            }
        }
        availableLanes.Remove(closestLane);

        // Ép bản thể đứng chính xác vào giữa làn của nó (đề phòng trước đó có bị trôi)
        transform.position = new Vector3(closestLane, transform.position.y, transform.position.z);

        Vector3 pos1 = new Vector3(availableLanes[0], transform.position.y, transform.position.z);
        Vector3 pos2 = new Vector3(availableLanes[1], transform.position.y, transform.position.z);

        if (isRasengan)
        {
            ActivateRasengan(rasenganSpeedBoost);
            if (AudioManager.Instance != null) AudioManager.Instance.PlayKagebunshinAppear();

            SpawnClone(pos1, true, false, rasenganSpeedBoost);
            SpawnClone(pos2, true, false, rasenganSpeedBoost);

            Vector3 backOffset = new Vector3(0, 0, 10f); 
            // 3 phân thân sau sẽ xuất hiện ở cả 3 làn (bao gồm cả làn bản thể đứng)
            SpawnClone(new Vector3(-laneDistance, transform.position.y, transform.position.z) + backOffset, false, true, rasenganSpeedBoost);
            SpawnClone(new Vector3(0f, transform.position.y, transform.position.z) + backOffset, false, true, rasenganSpeedBoost);
            SpawnClone(new Vector3(laneDistance, transform.position.y, transform.position.z) + backOffset, false, true, rasenganSpeedBoost);
        }
        else
        {
            if (anim != null) anim.CrossFadeInFixedTime("run", 0.1f);
            if (AudioManager.Instance != null) AudioManager.Instance.PlayKagebunshinAppear();
            SpawnClone(pos1, false, false, 0f);
            SpawnClone(pos2, false, false, 0f);
        }
    }

    void SpawnClone(Vector3 pos, bool useRasengan, bool constantJump, float speedBoost)
    {
        if (clonePrefab == null) return;
        GameObject cloneObj = Instantiate(clonePrefab, pos, transform.rotation);
        activeClones.Add(cloneObj);
        
        // Ngăn chặn va chạm vật lý đẩy nhau ra giữa các clone và bản thể
        Collider mainCol = GetComponent<Collider>();
        Collider cloneCol = cloneObj.GetComponent<Collider>();
        if (mainCol != null && cloneCol != null) Physics.IgnoreCollision(mainCol, cloneCol, true);
        
        foreach (GameObject active in activeClones)
        {
            if (active != cloneObj && active != null)
            {
                Collider activeCol = active.GetComponent<Collider>();
                if (activeCol != null && cloneCol != null) Physics.IgnoreCollision(activeCol, cloneCol, true);
            }
        }
        
        NarutoBoss cloneScript = cloneObj.GetComponent<NarutoBoss>();
        if (cloneScript != null)
        {
            cloneScript.isClone = true;
            cloneScript.isUsingRasengan = false; // Xóa trạng thái rác nếu copy từ bản thể runtime
            cloneScript.baseRunSpeed = this.baseRunSpeed; // Đồng bộ gốc tốc độ
            cloneScript.runSpeed = this.baseRunSpeed;
            
            if (useRasengan) cloneScript.ActivateRasengan(speedBoost);
            else if (constantJump) cloneScript.ActivateContinuousJump(speedBoost);
            else cloneScript.runSpeed = cloneScript.baseRunSpeed + speedBoost;
        }
    }

    Transform FindChildByName(Transform parent, string name)
    {
        if (parent.name == name) return parent;
        foreach (Transform child in parent)
        {
            Transform result = FindChildByName(child, name);
            if (result != null) return result;
        }
        return null;
    }

    public void ActivateRasengan(float speedBoost)
    {
        isUsingRasengan = true;
        runSpeed = baseRunSpeed + speedBoost; // Đảm bảo bứt tốc chuẩn tuyệt đối
        if (anim != null) anim.CrossFadeInFixedTime("rasengan", 0.1f);
        
        if (rasenganSphere == null)
        {
            Transform[] children = GetComponentsInChildren<Transform>(true);
            foreach (Transform t in children)
            {
                if (t.name.Contains("Rasengan") || t.name.Contains("Sphere") || t.name.Contains("rasengan"))
                {
                    rasenganSphere = t.gameObject;
                    break;
                }
            }
        }

        if (rasenganSphere == null)
        {
            Transform leftHand = FindChildByName(transform, "Hand_L");
            if (leftHand == null) leftHand = FindChildByName(transform, "mixamorig:LeftHand"); 
            if (leftHand == null) leftHand = transform; 

            rasenganSphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            rasenganSphere.transform.SetParent(leftHand, false);
            
            if (leftHand == transform) 
            {
                rasenganSphere.transform.localPosition = new Vector3(-0.5f, 1.2f, 0.5f);
                rasenganSphere.transform.localScale = new Vector3(0.5f, 0.5f, 0.5f);
            }
            else 
            {
                rasenganSphere.transform.localPosition = Vector3.zero;
                rasenganSphere.transform.localScale = new Vector3(0.25f, 0.25f, 0.25f); 
            }
            
            Renderer r = rasenganSphere.GetComponent<Renderer>();
            if (UnityEngine.Rendering.GraphicsSettings.currentRenderPipeline != null)
                r.material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            r.material.color = new Color(0f, 0.5f, 1f, 0.9f); 
            
            Collider col = rasenganSphere.GetComponent<Collider>();
            if (col != null) Destroy(col);
        }

        if (rasenganSphere != null)
        {
            rasenganSphere.SetActive(true);
        }
    }

    public void ActivateContinuousJump(float speedBoost)
    {
        isContinuousJump = true;
        runSpeed += speedBoost;
        StartCoroutine(ContinuousJumpRoutine());
    }

    IEnumerator ContinuousJumpRoutine()
    {
        while (true)
        {
            if (anim != null) anim.CrossFadeInFixedTime("jump", 0.1f);
            
            float elapsed = 0f;
            float startY = transform.position.y;
            
            while (elapsed < jumpDuration)
            {
                if (ClashManager.Instance != null && ClashManager.Instance.IsClashing())
                {
                    yield return null;
                    continue;
                }
                
                elapsed += Time.deltaTime;
                float progress = elapsed / jumpDuration;
                float bonusY = Mathf.Sin(progress * Mathf.PI) * extraJumpHeight;
                transform.position = new Vector3(transform.position.x, startY + bonusY, transform.position.z);
                
                yield return null;
            }
            transform.position = new Vector3(transform.position.x, startY, transform.position.z);
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
                yield return null; 
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
