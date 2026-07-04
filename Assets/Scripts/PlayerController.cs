using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody))]
public class PlayerController : MonoBehaviour
{
    [Header("Lane Settings")]
    public float laneDistance = 5.0f; // Mở rộng làn đường thành 5m để vừa với mô hình Sasuke to và cục đá to
    public float switchSpeed = 15.0f; // Tốc độ lướt qua lại
    public float leanMultiplier = 5.0f; // Hệ số nghiêng khi chuyển làn
    private int currentLane = 1; // 0: Trái, 1: Giữa, 2: Phải
    private Vector3 targetPosition;

    [Header("Animation Timers (Giống hệt Three.js)")]
    public float jumpDuration = 0.8f; 
    public float slideDuration = 0.8f;
    
    [Header("Extra Physics")]
    public float extraJumpHeight = 2.0f; // Tự động cộng dồn độ cao này lên trên độ cao của Animation
    
    private Rigidbody rb;
    private Animator anim;
    
    private bool isJumping = false;
    private bool isSliding = false;
    [HideInInspector] public bool isInvincible = false;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        anim = GetComponentInChildren<Animator>(); 
        targetPosition = transform.position;
    }

    void Update()
    {
        HandleInput();
        MoveLane();
    }

    void HandleInput()
    {
        if (Keyboard.current == null) return;

        // Chuyển làn (Trái / Phải)
        if (Keyboard.current.leftArrowKey.wasPressedThisFrame || Keyboard.current.aKey.wasPressedThisFrame)
        {
            if (currentLane > 0) currentLane--;
        }
        else if (Keyboard.current.rightArrowKey.wasPressedThisFrame || Keyboard.current.dKey.wasPressedThisFrame)
        {
            if (currentLane < 2) currentLane++;
        }

        // Nhảy (Lên / W) - Dùng isPressed để cho phép đè phím nhảy liên tục
        if ((Keyboard.current.upArrowKey.isPressed || Keyboard.current.wKey.isPressed) && !isJumping && !isSliding)
        {
            StartCoroutine(JumpRoutine());
        }

        // Trượt (Xuống / S) - Dùng isPressed để cho phép đè phím trượt liên tục
        if ((Keyboard.current.downArrowKey.isPressed || Keyboard.current.sKey.isPressed) && !isJumping && !isSliding)
        {
            StartCoroutine(SlideRoutine());
        }
    }

    void MoveLane()
    {
        // 1. Tính toán tọa độ X mục tiêu
        float targetX = (currentLane - 1) * laneDistance; 
        
        // 3. Di chuyển dứt khoát giống hệ thống cũ
        float newX = Mathf.Lerp(transform.position.x, targetX, Time.deltaTime * switchSpeed);
        transform.position = new Vector3(newX, transform.position.y, transform.position.z);

        // 4. Hiệu ứng nghiêng người (Lean effect) y hệt bản Three.js
        if (anim != null)
        {
            // Giảm hệ số nhân và giới hạn góc nghiêng tối đa ở mức 25 độ để không bị lố
            float rawLean = (targetX - transform.position.x) * -leanMultiplier; 
            float leanAngle = Mathf.Clamp(rawLean, -25f, 25f);
            
            Quaternion targetRotation = Quaternion.Euler(0, 0, leanAngle);
            anim.transform.localRotation = Quaternion.Lerp(anim.transform.localRotation, targetRotation, Time.deltaTime * 15f);
        }
    }

    IEnumerator JumpRoutine()
    {
        isJumping = true;
        if (anim != null) anim.SetTrigger("Jump");
        
        float elapsed = 0f;
        float startY = transform.position.y; // Lưu lại cao độ mặt đường (Thường là 0)

        // Vòng lặp liên tục đẩy nhân vật lên cao theo quỹ đạo Parabola (Hình Sin)
        while (elapsed < jumpDuration)
        {
            elapsed += Time.deltaTime;
            float progress = elapsed / jumpDuration; // Chạy từ 0 đến 1

            // Bơm thêm độ cao phụ trợ cộng dồn với chuyển động của Xương
            float bonusY = Mathf.Sin(progress * Mathf.PI) * extraJumpHeight;
            
            transform.position = new Vector3(transform.position.x, startY + bonusY, transform.position.z);
            
            yield return null; // Chờ sang khung hình (Frame) tiếp theo
        }
        
        // Đảm bảo đáp đất chính xác ở cao độ gốc sau khi hết thời gian
        transform.position = new Vector3(transform.position.x, startY, transform.position.z);
        isJumping = false;
    }

    IEnumerator SlideRoutine()
    {
        isSliding = true;
        if (anim != null) anim.SetTrigger("Slide");

        // Hẹn giờ y hệt state.slideTimer trong Three.js
        yield return new WaitForSeconds(slideDuration);

        isSliding = false;
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.GetComponent<CoinLogic>() != null)
        {
            other.gameObject.SetActive(false);
            if (UIManager.Instance != null)
            {
                UIManager.Instance.coins++;
                UIManager.Instance.score += 100f; // 1 coin = 100 score
            }
            return; // Đã ăn tiền xong thì thoát khỏi hàm, không xét đụng vật cản nữa
        }

        // --- XỬ LÝ VA CHẠM VỚI BOSSS NARUTO ---
        NarutoBoss boss = other.GetComponentInParent<NarutoBoss>();
        if (boss != null || other.name.Contains("Naruto"))
        {
            if (boss != null && boss.isUsingRasengan)
            {
                if (isInvincible && SkillManager.Instance != null && !SkillManager.Instance.IsSusanooActive()) // Dùng Chidori
                {
                    Debug.Log("⚔️ CLASH KÍCH HOẠT: CHIDORI VS RASENGAN!");
                    if (ClashManager.Instance != null)
                    {
                        ClashManager.Instance.StartClash(boss.gameObject);
                    }
                    return;
                }
                else
                {
                    Debug.Log("Thua cuộc vì hứng trọn Rasengan mà không dùng Chidori!");
                    if (GameManager.Instance != null) GameManager.Instance.GameOver();
                    return;
                }
            }
            else
            {
                // Chạm vào Naruto lúc bình thường (không Rasengan) -> Tương tự bẫy
                if (isInvincible && SkillManager.Instance != null && !SkillManager.Instance.IsSusanooActive()) 
                {
                    other.gameObject.SetActive(false); // Chidori xuyên qua phá hủy Naruto
                    Debug.Log("Chidori tiêu diệt Naruto bản thể (không Rasengan)!");
                    return;
                }
            }
        }
        
        // --- XỬ LÝ PHÂN THÂN NARUTO (Clone) ---
        if (other.GetComponentInParent<NarutoClone>() != null || other.name.Contains("NarutoClone"))
        {
            if (isInvincible && SkillManager.Instance != null && !SkillManager.Instance.IsSusanooActive()) 
            {
                Destroy(other.transform.root.gameObject); // Chidori tiêu diệt clone
                Debug.Log("Chidori tiêu diệt Phân thân!");
                return;
            }
            else if (SkillManager.Instance != null && SkillManager.Instance.IsSusanooActive())
            {
                return; // Susanoo Body/Sword sẽ lo
            }
            else
            {
                Debug.Log("Đâm trúng phân thân! Game Over.");
                if (GameManager.Instance != null) GameManager.Instance.GameOver();
                return;
            }
        }

        // --- XỬ LÝ BẪY THƯỜNG ---
        if (other.CompareTag("Obstacle") || other.name.Contains("GiantRockSlide") || other.name.Contains("GiantRockJump") || other.name.Contains("MountainWall"))
        {
            if (isInvincible)
            {
                // Nếu đang bật Susanoo, bỏ qua va chạm vì SusanooBodyCollider và SusanooSwordCollider sẽ lo!
                if (SkillManager.Instance != null && SkillManager.Instance.IsSusanooActive())
                {
                    return; 
                }
                
                // Nếu đang bật Chidori (isInvincible = true nhưng không phải Susanoo)
                // Chiêu 2 (Chidori) phá được mọi thứ TRỪ Vách núi
                if (other.name.Contains("MountainWall"))
                {
                    Debug.Log("Sasuke Hit Mountain Wall during Chidori! Game Over.");
                    if (GameManager.Instance != null)
                    {
                        GameManager.Instance.GameOver();
                    }
                    return;
                }

                other.gameObject.SetActive(false);
                Debug.Log("Obstacle Destroyed by Chidori!");
            }
            else
            {
                Debug.Log("Sasuke Hit Obstacle! Game Over.");
                if (GameManager.Instance != null)
                {
                    GameManager.Instance.GameOver();
                }
            }
        }
    }

    public void ForceJump()
    {
        // Ép nhân vật nhảy vật lý lên
        if (!isJumping && !isSliding)
        {
            StartCoroutine(JumpRoutine());
        }
    }

    public void FallFromHeight(float startHeight)
    {
        StartCoroutine(FallRoutine(startHeight));
    }

    IEnumerator FallRoutine(float startHeight)
    {
        isJumping = true; // Khóa phím nhảy của người chơi trong lúc rơi
        transform.position = new Vector3(transform.position.x, startHeight, transform.position.z);
        
        if (anim != null) anim.CrossFadeInFixedTime("Armature|jump", 0.1f); // Dùng tư thế nhảy làm tư thế rơi

        float velocityY = 0f;
        float gravity = 60f; // Gia tốc rơi cực mạnh

        while (transform.position.y > 0)
        {
            velocityY -= gravity * Time.deltaTime;
            float newY = transform.position.y + velocityY * Time.deltaTime;
            
            if (newY <= 0)
            {
                newY = 0;
                transform.position = new Vector3(transform.position.x, newY, transform.position.z);
                break;
            }
            
            transform.position = new Vector3(transform.position.x, newY, transform.position.z);
            yield return null;
        }

        // Chạm đất
        isJumping = false;
        if (anim != null) anim.CrossFadeInFixedTime("Armature|run", 0.1f); // Ép về lại dáng chạy ngay lập tức để không bị lặp animation nhảy
    }
}
