using System.Collections;
using UnityEngine;

public class NarutoClone : MonoBehaviour
{
    public float runSpeed = 10f;
    private Animator anim;
    
    private bool isJumping = false;
    private float jumpDuration = 0.8f;
    private float extraJumpHeight = 2.0f;

    void Start()
    {
        anim = GetComponent<Animator>();
        if (anim == null) anim = GetComponentInChildren<Animator>();
    }

    void Update()
    {
        if (WorldManager.Instance == null || ClashManager.Instance != null && ClashManager.Instance.IsClashing()) 
            return; 

        // Phân thân chạy thẳng giống bản thể
        float totalSpeed = WorldManager.Instance.currentSpeed + runSpeed;
        transform.position += Vector3.back * totalSpeed * Time.deltaTime;

        if (!isJumping)
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
