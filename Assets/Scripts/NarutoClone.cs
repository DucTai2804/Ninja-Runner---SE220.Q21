using System.Collections;
using UnityEngine;

public class NarutoClone : MonoBehaviour
{
    public float runSpeed = 10f;
    private Animator anim;
    
    private bool isJumping = false;
    private float jumpDuration = 0.8f;
    private float extraJumpHeight = 2.0f;

    public bool isUsingRasengan = false;
    private bool isContinuousJump = false;

    public GameObject rasenganSphere; // Kéo thả quả cầu vào đây giống y hệt Boss

    void Awake()
    {
        anim = GetComponent<Animator>();
        if (anim == null) anim = GetComponentInChildren<Animator>();
        
        if (rasenganSphere != null)
        {
            rasenganSphere.SetActive(false); // Ẩn quả cầu đi khi vừa sinh ra
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
        runSpeed += speedBoost;
        if (anim != null) anim.CrossFadeInFixedTime("rasengan", 0.1f);
        
        // 1. Tự động tìm kiếm nếu lỡ quên gán
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

        // 2. Nếu quét nát cả người mà vẫn KHÔNG có quả cầu (do bạn chưa add vào tay Clone) -> Tự nặn ra 1 quả!
        if (rasenganSphere == null)
        {
            Transform leftHand = FindChildByName(transform, "Hand_L");
            if (leftHand == null) leftHand = FindChildByName(transform, "mixamorig:LeftHand"); 
            if (leftHand == null) leftHand = transform; // Cùng đường thì gắn tạm vào người

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
            {
                r.material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            }
            r.material.color = new Color(0f, 0.5f, 1f, 0.9f); 
            
            Collider col = rasenganSphere.GetComponent<Collider>();
            if (col != null) Destroy(col);
        }

        // Bật quả cầu lên
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

    void Update()
    {
        if (WorldManager.Instance == null || ClashManager.Instance != null && ClashManager.Instance.IsClashing()) 
            return; 

        // Phân thân chạy thẳng giống bản thể
        float totalSpeed = WorldManager.Instance.currentSpeed + runSpeed;
        transform.position += Vector3.back * totalSpeed * Time.deltaTime;

        if (!isJumping && !isContinuousJump && !isUsingRasengan)
        {
            // Tỉ lệ nhảy y chang bản thể
            if (transform.position.z < 30f && transform.position.z > 25f && Random.value < 0.3f)
            {
                StartCoroutine(JumpRoutine());
            }
        }

        if (transform.position.z < -20f)
        {
            Destroy(gameObject);
        }
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
                float normalizedTime = elapsed / jumpDuration;
                float height = Mathf.Sin(normalizedTime * Mathf.PI) * extraJumpHeight;
                transform.position = new Vector3(transform.position.x, startY + height, transform.position.z);
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
        if (anim != null) anim.CrossFadeInFixedTime("run", 0.1f);
    }
}
