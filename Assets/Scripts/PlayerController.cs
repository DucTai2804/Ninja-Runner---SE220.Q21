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
        // Nhận diện va chạm với chướng ngại vật
        if (other.CompareTag("Obstacle"))
        {
            if (isInvincible)
            {
                // Nếu đang bật Susanoo hoặc Chidori, hất văng/xóa chướng ngại vật
                other.gameObject.SetActive(false);
                Debug.Log("Obstacle Destroyed by Skill!");
            }
            else
            {
                // Bị đụng khi không có kỹ năng bảo vệ
                Debug.Log("Sasuke Hit Obstacle! Game Over.");
                if (GameManager.Instance != null)
                {
                    GameManager.Instance.GameOver();
                }
            }
        }
    }
}
