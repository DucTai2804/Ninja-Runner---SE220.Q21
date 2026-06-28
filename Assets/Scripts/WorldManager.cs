using UnityEngine;

public class WorldManager : MonoBehaviour
{
    public static WorldManager Instance;

    [Header("Speed Settings")]
    public float baseSpeed = 15f;       // Tốc độ ban đầu
    public float maxSpeed = 40f;        // Tốc độ tối đa
    public float acceleration = 0.2f;   // Gia tốc (Tăng độ khó dần đều)

    [HideInInspector] 
    public float currentSpeed;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
        
        currentSpeed = baseSpeed;
    }

    void Update()
    {
        // Tự động tăng tốc độ game theo thời gian
        if (currentSpeed < maxSpeed)
        {
            currentSpeed += acceleration * Time.deltaTime;
        }
    }
}
