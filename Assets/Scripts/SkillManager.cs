using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class SkillManager : MonoBehaviour
{
    public static SkillManager Instance;

    [Header("Skill 1: Fireball")]
    public GameObject fireballPrefab;
    public Transform fireballSpawnPoint;
    public float fireballCooldown = 3f;
    [Range(10f, 100f)]
    public float fireballSpeed = 25f; 
    private float lastFireballTime = -10f;

    [Header("Skill 2: Chidori")]
    public GameObject chidoriVFX;
    public float chidoriDuration = 5f; 
    public float chidoriSpeedMultiplier = 2.5f;
    public float chidoriCooldown = 15f;
    private float lastChidoriTime = -20f;

    [Header("Skill 3: Susanoo")]
    public GameObject susanooModel; 
    public float susanooDuration = 10f;
    public float susanooCooldown = 30f;
    public float susanooPenaltyTime = 2.0f;
    public Texture2D dualEyesTexture; // Chuyển thành Texture2D để kéo thả thẳng file ảnh vào không lỗi
    private float lastSusanooTime = -50f;
    private Animator susanooAnim;
    private bool isSusanooActive = false;
    private float susanooDurationRemaining = 0f;

    // --- CÁC HÀM GETTER CHO UI ---
    public float GetFireballCooldownRatio() { return Mathf.Clamp01((Time.time - lastFireballTime) / fireballCooldown); }
    public float GetChidoriCooldownRatio() { return Mathf.Clamp01((Time.time - lastChidoriTime) / chidoriCooldown); }
    public float GetSusanooCooldownRatio() { return Mathf.Clamp01((Time.time - lastSusanooTime) / susanooCooldown); }

    private PlayerController player;
    private Animator sasukeAnim;
    private RunnerCamera runnerCam;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private float lastSusanooAttackTime = -1f;

    void Start()
    {
        player = GetComponent<PlayerController>();
        sasukeAnim = GetComponentInChildren<Animator>();
        runnerCam = Camera.main.GetComponent<RunnerCamera>();

        if (chidoriVFX) chidoriVFX.SetActive(false);
        
        if (susanooModel) 
        {
            susanooAnim = susanooModel.GetComponent<Animator>();
            if (susanooAnim == null) susanooAnim = susanooModel.GetComponentInChildren<Animator>(true);
            
            SetupSusanooHitboxes(); // Gắn hitbox vật lý cho Susanoo!

            susanooModel.SetActive(false);
        }

        // Khởi tạo hệ thống ánh sáng cho chiêu thức
        AddSkillLights();
    }

    [Header("Susanoo Hitboxes (World Size)")]
    public Vector3 susanooBodySize = new Vector3(10f, 15f, 10f); // Tăng kích thước mặc định lên to hơn
    public Vector3 susanooBodyCenter = new Vector3(0, 7.5f, 0);
    public Vector3 susanooSwordSize = new Vector3(1.5f, 8f, 1.5f);
    public Vector3 susanooSwordCenter = new Vector3(0, 4f, 0);

    private void SetupSusanooHitboxes()
    {
        if (susanooModel == null) return;
        
        // 1. Hitbox cho Thân Thể Susanoo
        Vector3 bodyLossy = susanooModel.transform.lossyScale;
        BoxCollider bodyCol = susanooModel.AddComponent<BoxCollider>();
        bodyCol.isTrigger = true;
        // Chia tỷ lệ lossyScale để số liệu nhập trên Inspector đúng bằng kích thước mét ngoài đời
        bodyCol.center = new Vector3(susanooBodyCenter.x / bodyLossy.x, susanooBodyCenter.y / bodyLossy.y, susanooBodyCenter.z / bodyLossy.z); 
        bodyCol.size = new Vector3(susanooBodySize.x / bodyLossy.x, susanooBodySize.y / bodyLossy.y, susanooBodySize.z / bodyLossy.z); 
        
        Rigidbody bodyRb = susanooModel.AddComponent<Rigidbody>();
        bodyRb.isKinematic = true;
        susanooModel.AddComponent<SusanooBodyCollider>();

        // 2. Hitbox cho Lưỡi Kiếm
        // Dò trực tiếp tên "sword_blade" trên Hierarchy
        Transform swordBlade = FindBoneByName(susanooModel.transform, "sword_blade");
        if (swordBlade != null)
        {
            BoxCollider swordCol = swordBlade.GetComponent<BoxCollider>();
            if (swordCol == null) swordCol = swordBlade.gameObject.AddComponent<BoxCollider>();
            
            swordCol.isTrigger = true;
            
            // XÓA BỎ VIỆC CAN THIỆP THỦ CÔNG VÀO KÍCH THƯỚC (SIZE/CENTER)!
            // Vì người dùng tạo kiếm bằng một khối Cube, BoxCollider mặc định đã ôm khít hoàn hảo vào lưới (Mesh) của khối Cube đó.
            // Việc chia cho lossyScale siêu nhỏ (0.005) ở bản trước đã làm Collider bị phình to hàng trăm lần.
            
            Rigidbody swordRb = swordBlade.GetComponent<Rigidbody>();
            if (swordRb == null) swordRb = swordBlade.gameObject.AddComponent<Rigidbody>();
            swordRb.isKinematic = true;
            
            if (swordBlade.GetComponent<SusanooSwordCollider>() == null)
                swordBlade.gameObject.AddComponent<SusanooSwordCollider>();
                
            Debug.Log("Found sword_blade in Hierarchy for Hitbox!");
        }
        else
        {
            Debug.LogWarning("Could not find sword_blade GameObject for Hitbox!");
        }
    }

    private Transform FindBoneByName(Transform parent, string nameContains)
    {
        foreach (Transform child in parent)
        {
            if (child.name.ToLower().Contains(nameContains.ToLower()))
            {
                return child;
            }
            Transform found = FindBoneByName(child, nameContains);
            if (found != null) return found;
        }
        return null;
    }

    void Update()
    {
        if (Keyboard.current == null || WorldManager.Instance == null) return;
        Keyboard keyboard = Keyboard.current;
        if (keyboard == null || WorldManager.Instance == null) return;

        bool press1 = keyboard.digit1Key.wasPressedThisFrame || keyboard.numpad1Key.wasPressedThisFrame;
        bool press2 = keyboard.digit2Key.wasPressedThisFrame || keyboard.numpad2Key.wasPressedThisFrame;
        bool press3 = keyboard.digit3Key.wasPressedThisFrame || keyboard.numpad3Key.wasPressedThisFrame;

        if (press1 && Time.time - lastFireballTime >= fireballCooldown)
        {
            if (UIManager.Instance != null) UIManager.Instance.AnimateSkillButton(1);
            CastFireball();
        }

        if (press2 && Time.time - lastChidoriTime >= chidoriCooldown)
        {
            if (UIManager.Instance != null) UIManager.Instance.AnimateSkillButton(2);
            StartCoroutine(CastChidori());
        }

        if (press3)
        {
            if (!isSusanooActive)
            {
                if (Time.time >= lastSusanooTime + susanooCooldown) 
                {
                    if (UIManager.Instance != null) UIManager.Instance.AnimateSkillButton(3);
                    StartCoroutine(CastSusanoo());
                }
            }
            else
            {
                if (susanooAnim != null) susanooAnim.SetTrigger("Attack");
                lastSusanooAttackTime = Time.time; // Cập nhật thời điểm vung kiếm!
                
                if (susanooModel != null)
                {
                    SusanooSwordTrail trailScript = susanooModel.GetComponentInChildren<SusanooSwordTrail>();
                    if (trailScript != null) trailScript.ActivateTrail(0.5f);
                }
            }
        }
    }

    public bool IsSusanooActive()
    {
        return isSusanooActive;
    }

    public bool IsSlashing()
    {
        float t = Time.time - lastSusanooAttackTime;
        // Chém trúng địch từ giây 0.23 đến 0.50 y như Three.js!
        return (t >= 0.23f && t <= 0.50f);
    }

    public void ReduceSusanooTime()
    {
        if (isSusanooActive)
        {
            susanooDurationRemaining -= susanooPenaltyTime;
            if (susanooDurationRemaining < 0) susanooDurationRemaining = 0;
            Debug.Log("Susanoo Time Reduced! Remaining: " + susanooDurationRemaining);
        }
    }

    void CastFireball()
    {
        lastFireballTime = Time.time;
        if (sasukeAnim != null) sasukeAnim.CrossFadeInFixedTime("Armature|fireball", 0.1f);
        StartCoroutine(CastFireballRoutine());
    }

    IEnumerator CastFireballRoutine()
    {
        yield return new WaitForSeconds(2.0f);
        if (fireballPrefab && fireballSpawnPoint)
        {
            GameObject fb = Instantiate(fireballPrefab, fireballSpawnPoint.position, Quaternion.identity);
            StartCoroutine(FireballMoveRoutine(fb));
        }
    }

    IEnumerator FireballMoveRoutine(GameObject fb)
    {
        while (fb != null)
        {
            float safeDeltaTime = Mathf.Min(Time.deltaTime, 0.033f);
            fb.transform.position += Vector3.forward * fireballSpeed * safeDeltaTime;
            yield return null;
        }
    }

    IEnumerator CastChidori()
    {
        lastChidoriTime = Time.time;
        if (sasukeAnim != null) sasukeAnim.CrossFadeInFixedTime("Armature|chidori", 0.1f);
        if (chidoriVFX) chidoriVFX.SetActive(true);
        if (chidoriLight != null) 
        {
            chidoriLight.enabled = true; // Bật ánh sáng
            Debug.Log("⚡ ChidoriLight: BẬT SÁNG!");
        }

        float originalSpeed = WorldManager.Instance.currentSpeed;
        WorldManager.Instance.currentSpeed = originalSpeed * chidoriSpeedMultiplier;
        if (player) player.isInvincible = true;

        yield return new WaitForSeconds(chidoriDuration);

        WorldManager.Instance.currentSpeed = originalSpeed;
        if (player) player.isInvincible = false;
        
        if (chidoriLight != null) 
        {
            chidoriLight.enabled = false; // Tắt ánh sáng
            Debug.Log("⚡ ChidoriLight: TẮT.");
        }

        if (chidoriVFX)
        {
            ChidoriVFX vfxScript = chidoriVFX.GetComponent<ChidoriVFX>();
            if (vfxScript != null) vfxScript.FadeOut();
            else chidoriVFX.SetActive(false);
        }
    }

    // Tắt bật lưới của Sasuke một cách an toàn mà không bị hủy Coroutine
    private void SetSasukeMeshesVisible(bool isVisible)
    {
        if (sasukeAnim == null) return;
        Renderer[] renderers = sasukeAnim.GetComponentsInChildren<Renderer>(true);
        foreach (Renderer r in renderers)
        {
            // Bỏ qua Susanoo (nếu Susanoo là con của Sasuke)
            if (susanooModel != null && r.transform.IsChildOf(susanooModel.transform)) continue;
            r.enabled = isVisible;
        }
    }

    private GameObject susanooBarCanvas;
    private RectTransform susanooBarInner;

    private void CreateSusanooBar()
    {
        if (susanooBarCanvas != null) return;

        susanooBarCanvas = new GameObject("SusanooBarCanvas");
        Canvas canvas = susanooBarCanvas.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 900;
        
        CanvasScaler scaler = susanooBarCanvas.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;
        
        // Vùng chứa (Container)
        GameObject container = new GameObject("Container");
        container.transform.SetParent(susanooBarCanvas.transform, false);
        Image bgImg = container.AddComponent<Image>();
        bgImg.color = new Color(0, 0, 0, 0.6f);
        Outline outline = container.AddComponent<Outline>();
        outline.effectColor = new Color(0.66f, 0.2f, 0.66f); // Mã màu #a832a8
        outline.effectDistance = new Vector2(2, -2);
        
        RectTransform rect = container.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(400, 20); 
        rect.anchoredPosition = new Vector2(0, 400); // Đặt ở giữa màn hình bên trên
        
        // Thanh thời gian (Inner)
        GameObject inner = new GameObject("Inner");
        inner.transform.SetParent(container.transform, false);
        Image innerImg = inner.AddComponent<Image>();
        innerImg.color = new Color(0.83f, 0f, 0.98f); // Mã màu #d500f9
        
        susanooBarInner = inner.GetComponent<RectTransform>();
        susanooBarInner.anchorMin = new Vector2(0, 0.5f);
        susanooBarInner.anchorMax = new Vector2(0, 0.5f);
        susanooBarInner.pivot = new Vector2(0, 0.5f); // Neo gốc từ bên trái để thu nhỏ dần về trái
        susanooBarInner.sizeDelta = new Vector2(400, 20);
        susanooBarInner.anchoredPosition = new Vector2(0, 0); // Đã sửa lỗi bị lệch
        
        susanooBarCanvas.SetActive(false);
    }

    IEnumerator CastSusanoo()
    {
        lastSusanooTime = Time.time;
        isSusanooActive = true;
        susanooDurationRemaining = susanooDuration;

        Time.timeScale = 0.3f;
        Time.fixedDeltaTime = 0.02f * Time.timeScale;
        StartCoroutine(ShowDualEyesCutin());

        yield return new WaitForSecondsRealtime(0.5f);

        Time.timeScale = 1.0f;
        Time.fixedDeltaTime = 0.02f;

        SetSasukeMeshesVisible(false);
        
        if (susanooModel) susanooModel.SetActive(true);
        if (susanooLight != null) 
        {
            susanooLight.enabled = true; // Bật vầng hào quang tím
            Debug.Log("🟣 SusanooLight: BẬT SÁNG TOÀN BẢN ĐỒ!");
        }
        
        if (runnerCam != null) runnerCam.SetSusanooMode(true);
        if (TerrainManager.Instance != null) TerrainManager.Instance.ShowBigMountains(true);
        if (player) player.isInvincible = true;

        float originalSpeed = WorldManager.Instance.currentSpeed;
        WorldManager.Instance.currentSpeed = originalSpeed * chidoriSpeedMultiplier;

        // Kích hoạt thanh UI thời gian
        CreateSusanooBar();
        if (susanooBarCanvas) susanooBarCanvas.SetActive(true);

        while (susanooDurationRemaining > 0)
        {
            susanooDurationRemaining -= Time.deltaTime;
            
            // Cập nhật thanh thời gian UI
            if (susanooBarInner)
            {
                float scaleX = Mathf.Clamp01(susanooDurationRemaining / susanooDuration);
                susanooBarInner.localScale = new Vector3(scaleX, 1, 1);
            }
            
            yield return null;
        }

        if (susanooBarCanvas) susanooBarCanvas.SetActive(false);
        WorldManager.Instance.currentSpeed = originalSpeed;
        if (player) player.isInvincible = false;
        
        if (susanooModel) susanooModel.SetActive(false);
        if (susanooLight != null) 
        {
            susanooLight.enabled = false; // Tắt vầng hào quang tím
            Debug.Log("🟣 SusanooLight: TẮT.");
        }
        
        if (TerrainManager.Instance != null) TerrainManager.Instance.ShowBigMountains(false);
        if (runnerCam != null) runnerCam.SetSusanooMode(false);
        
        SetSasukeMeshesVisible(true);
        
        // Bắt đầu rơi tự do từ ngang ngực Susanoo (khoảng 8 mét) xuống đất
        if (player)
        {
            player.FallFromHeight(8.0f);
        }
        
        isSusanooActive = false;
    }

    private Light chidoriLight;
    private Light susanooLight;

    private void AddSkillLights()
    {
        // 1. Ánh sáng xanh chớp giật cho Chidori
        if (chidoriLight == null)
        {
            GameObject lightObj = new GameObject("ChidoriLight");
            lightObj.transform.SetParent(this.transform); // Gắn trực tiếp vào Sasuke để tránh lỗi Scale của VFX
            lightObj.transform.localPosition = new Vector3(0, 1.5f, 0); // Nằm ngang hông Sasuke
            chidoriLight = lightObj.AddComponent<Light>();
            chidoriLight.type = LightType.Point;
            chidoriLight.color = new Color(0f, 0.8f, 1f); // Xanh dương sấm sét
            chidoriLight.range = 40f; 
            chidoriLight.intensity = 100f; // Theo yêu cầu
            chidoriLight.enabled = false; // Tắt mặc định
            Debug.Log("💡 Đã khởi tạo thành công đèn ChidoriLight gắn trên Sasuke.");
        }

        // 2. Ánh sáng tím khổng lồ cho Susanoo
        if (susanooLight == null)
        {
            GameObject lightObj = new GameObject("SusanooLight");
            lightObj.transform.SetParent(this.transform); // Gắn trực tiếp vào Sasuke
            lightObj.transform.localPosition = new Vector3(0, 8f, 0); // Phát sáng từ lồng ngực Susanoo
            susanooLight = lightObj.AddComponent<Light>();
            susanooLight.type = LightType.Point;
            susanooLight.color = new Color(0.7f, 0f, 1f); // Tím Susanoo
            susanooLight.range = 200f; // Vùng phủ sáng khổng lồ
            susanooLight.intensity = 1000f; // Theo yêu cầu
            susanooLight.enabled = false; // Tắt mặc định
            Debug.Log("💡 Đã khởi tạo thành công đèn SusanooLight khổng lồ gắn trên Sasuke.");
        }
    }

    IEnumerator ShowDualEyesCutin()
    {
        if (dualEyesTexture == null) yield break;

        GameObject canvasObj = new GameObject("DualEyesCanvas");
        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 999;
        
        CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;

        GameObject imageObj = new GameObject("DualEyesImage");
        imageObj.transform.SetParent(canvasObj.transform, false);
        
        RawImage img = imageObj.AddComponent<RawImage>();
        img.texture = dualEyesTexture;
        
        RectTransform rect = img.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(1600, 800);
        rect.anchoredPosition = Vector2.zero;

        float elapsed = 0f;
        float duration = 0.5f; 
        
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float progress = elapsed / duration;
            
            float scale = Mathf.Lerp(0.8f, 1.2f, progress);
            rect.localScale = new Vector3(scale, scale, scale);
            
            float alpha = 1f;
            if (progress < 0.2f) alpha = progress * 5f; 
            else alpha = 1f - ((progress - 0.2f) * 1.25f); 
            
            img.color = new Color(1, 1, 1, alpha);
            yield return null;
        }

        Destroy(canvasObj);
    }
}
