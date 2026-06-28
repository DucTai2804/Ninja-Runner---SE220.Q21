using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class SkillManager : MonoBehaviour
{
    public static SkillManager Instance;

    [Header("Skill 1: Fireball")]
    public GameObject fireballPrefab;
    public Transform fireballSpawnPoint;
    public float fireballCooldown = 3f;
    [Range(10f, 100f)]
    public float fireballSpeed = 25f; // Đã giảm mạnh để tạo cảm giác nặng nề, uy lực hơn
    private float lastFireballTime = -10f;

    [Header("Skill 2: Chidori")]
    public GameObject chidoriVFX;
    public float chidoriDuration = 5f; // Tăng thời gian Chidori lên 5 giây!
    public float chidoriSpeedMultiplier = 2.5f;
    public float chidoriCooldown = 15f;
    private float lastChidoriTime = -20f;

    [Header("Skill 3: Susanoo")]
    public GameObject susanooModel; // Dùng hẳn Model 3D có xương và Animation!
    public float susanooDuration = 10f;
    public float susanooCooldown = 30f;
    private float lastSusanooTime = -50f;
    private Animator susanooAnim;
    private bool isSusanooActive = false;

    private PlayerController player;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    void Start()
    {
        player = GetComponent<PlayerController>();
        if (chidoriVFX) chidoriVFX.SetActive(false);
        
        if (susanooModel) 
        {
            // Thêm true để Unity tìm thấy Animator kể cả khi Susanoo đang bị ẩn (Tắt con mắt)
            susanooAnim = susanooModel.GetComponent<Animator>();
            if (susanooAnim == null) susanooAnim = susanooModel.GetComponentInChildren<Animator>(true);
            
            susanooModel.SetActive(false);
        }
    }

    void Update()
    {
        if (Keyboard.current == null || WorldManager.Instance == null) return;

        // Skill 1: Hỏa Cầu (Phím 1 hoặc Numpad 1)
        if (Keyboard.current.digit1Key.wasPressedThisFrame || Keyboard.current.numpad1Key.wasPressedThisFrame)
        {
            Debug.Log("Pressed Skill 1");
            if (Time.time >= lastFireballTime + fireballCooldown) CastFireball();
            else Debug.Log("Fireball is on cooldown!");
        }

        // Skill 2: Chidori (Phím 2 hoặc Numpad 2)
        if (Keyboard.current.digit2Key.wasPressedThisFrame || Keyboard.current.numpad2Key.wasPressedThisFrame)
        {
            Debug.Log("Pressed Skill 2");
            if (Time.time >= lastChidoriTime + chidoriCooldown) StartCoroutine(CastChidori());
            else Debug.Log("Chidori is on cooldown!");
        }

        // Skill 3: Susanoo (Phím 3 hoặc Numpad 3)
        if (Keyboard.current.digit3Key.wasPressedThisFrame || Keyboard.current.numpad3Key.wasPressedThisFrame)
        {
            if (!isSusanooActive)
            {
                // Bật Susanoo lần đầu
                if (Time.time >= lastSusanooTime + susanooCooldown) 
                {
                    StartCoroutine(CastSusanoo());
                }
                else 
                {
                    Debug.Log("Susanoo is on cooldown!");
                }
            }
            else
            {
                // Đang bật Susanoo -> Bấm phím 3 lần nữa để vung kiếm chém bồi
                if (susanooAnim != null) susanooAnim.SetTrigger("Attack");
                Debug.Log("Susanoo Slash!");
                
                // Trở về code gốc: Bật vệt sáng liên tục trong 1 giây
                if (susanooModel != null)
                {
                    SusanooSwordTrail trailScript = susanooModel.GetComponentInChildren<SusanooSwordTrail>();
                    if (trailScript != null) trailScript.ActivateTrail(0.5f);
                }
            }
        }
    }

    void CastFireball()
    {
        lastFireballTime = Time.time;
        if (fireballPrefab && fireballSpawnPoint)
        {
            // Sinh ra quả cầu lửa tại vị trí bàn tay/trước mặt Sasuke
            GameObject fb = Instantiate(fireballPrefab, fireballSpawnPoint.position, Quaternion.identity);
            
            // Tự viết script di chuyển Hỏa Cầu bay lên phía trước
            StartCoroutine(FireballMoveRoutine(fb));
            
            Destroy(fb, 2.5f); // Tự hủy sau 2.5 giây nếu không trúng gì
        }
    }

    IEnumerator FireballMoveRoutine(GameObject fb)
    {
        while (fb != null)
        {
            // Tránh việc Hỏa Cầu bị dịch chuyển tức thời quá xa ở lần đầu tiên (do lag nạp Shader/Mesh)
            float safeDeltaTime = Mathf.Min(Time.deltaTime, 0.033f); // Tối đa tương đương 30 FPS
            fb.transform.position += Vector3.forward * fireballSpeed * safeDeltaTime;
            yield return null;
        }
    }

    IEnumerator CastChidori()
    {
        lastChidoriTime = Time.time;
        if (chidoriVFX) chidoriVFX.SetActive(true);

        // Bật trạng thái Bất tử và Tăng tốc thế giới ngay lập tức
        float originalSpeed = WorldManager.Instance.currentSpeed;
        WorldManager.Instance.currentSpeed = originalSpeed * chidoriSpeedMultiplier;
        
        if (player) player.isInvincible = true;

        yield return new WaitForSeconds(chidoriDuration);

        // Hết thời gian, trả lại bình thường
        WorldManager.Instance.currentSpeed = originalSpeed;
        if (player) player.isInvincible = false;
        if (chidoriVFX) chidoriVFX.SetActive(false);
    }

    IEnumerator CastSusanoo()
    {
        lastSusanooTime = Time.time;
        isSusanooActive = true;
        
        if (susanooModel) 
        {
            susanooModel.SetActive(true);
            // Susanoo chỉ xuất hiện, không tự động chém (chờ người chơi bấm phím 3 lần nữa)
        }

        // Bật trạng thái bất tử để ủi bay chướng ngại vật
        if (player) player.isInvincible = true;

        yield return new WaitForSeconds(susanooDuration);

        if (player) player.isInvincible = false;
        if (susanooModel) susanooModel.SetActive(false);
        isSusanooActive = false;
    }
}
