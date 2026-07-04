using UnityEngine;
using System.Collections.Generic;

public class LevelSpawner : MonoBehaviour
{
    [Header("Spawn Settings")]
    public float laneDistance = 5.0f; // Mở rộng để phù hợp với PlayerController (5m)
    public float spawnZ = 400f; // Tọa độ Z cực xa (y hệt Three.js) để sinh bẫy ở chân trời
    
    // Khoảng cách (mét) giữa các mảng chướng ngại vật
    public float distanceBetweenSpawns = 25f; 
    private float distanceTraveled = 0f;

    [Header("Debug")]
    public bool debugMode = false;
    [Range(1, 9)] public int forcePattern = 7;
    private bool lastDebugMode = false;
    private int lastForcePattern = -1;

    [Header("Three.js Sync Settings")]
    public float giantRockRadius = 25f; // Bán kính thực tế của tảng đá khổng lồ
    public float giantRockThickness = 3f; // Độ dày của tảng đá khổng lồ
    
    [Header("Shuriken Settings")]
    public float shurikenHeight = 2.5f; 
    public float shurikenExtraSpeed = 24f; 
    public float shurikenRotationSpeed = 1718f; 
    
    [Tooltip("Góc xoay ép phi tiêu nằm ngang. Mặc định xoay 90 độ trục X")]
    public Vector3 shurikenSpawnRotation = new Vector3(90f, 0f, 0f);

    private GameObject coinPrefab;

    private float currentRequiredDistance = 55f;

    void Awake()
    {
        spawnZ = 400f; 
        distanceBetweenSpawns = 55f; 
        currentRequiredDistance = distanceBetweenSpawns;
    }

    void Start()
    {
        CreateCoinPrefab();
        PreSpawnInitialTraps();
    }

    private void PreSpawnInitialTraps()
    {
        // Sinh trước một loạt bẫy từ khoảng cách 80m đến 350m để người chơi thấy bẫy ngay lập tức
        float[] initialDistances = { 80f, 150f, 220f, 290f, 360f };
        float originalZ = spawnZ;
        
        foreach (float z in initialDistances)
        {
            spawnZ = z;
            SpawnObstacles();
        }
        
        spawnZ = originalZ; // Trả lại 400f cho vòng lặp game
    }

    private void CreateCoinPrefab()
    {
        coinPrefab = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        coinPrefab.name = "CoinPrefab";
        coinPrefab.transform.localScale = new Vector3(2.0f, 0.2f, 2.0f); // Phóng to đồng xu
        coinPrefab.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
        
        Renderer renderer = coinPrefab.GetComponent<Renderer>();
        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null) shader = Shader.Find("Standard");
        
        Material mat = new Material(shader);
        mat.color = new Color(1f, 0.84f, 0f); // Vàng gold
        
        if (mat.HasProperty("_Metallic")) mat.SetFloat("_Metallic", 0.8f);
        if (mat.HasProperty("_Glossiness")) mat.SetFloat("_Glossiness", 0.9f);
        if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", 0.9f);
        
        renderer.material = mat;
        
        Collider col = coinPrefab.GetComponent<Collider>();
        col.isTrigger = true;
        
        coinPrefab.AddComponent<CoinLogic>();
        coinPrefab.AddComponent<MoveObject>();
        
        // Bơm thẳng vào Object Pooler với số lượng 50 đồng xu dự trữ
        if (ObjectPooler.Instance != null && !ObjectPooler.Instance.poolDictionary.ContainsKey("Coin"))
        {
            Queue<GameObject> coinPool = new Queue<GameObject>();
            for (int i = 0; i < 50; i++)
            {
                GameObject obj = Instantiate(coinPrefab);
                obj.SetActive(false);
                obj.transform.SetParent(ObjectPooler.Instance.transform);
                coinPool.Enqueue(obj);
            }
            ObjectPooler.Instance.poolDictionary.Add("Coin", coinPool);
        }
        
        coinPrefab.SetActive(false);
    }

    private int blockedWallSide = -1;
    private float blockedWallRemainingDistance = 0f;

    private float totalDistance = 0f;
    private float nextNarutoDistance = 1500f;
    
    public GameObject narutoPrefab; // Kéo Prefab Naruto vào đây (Inspector)

    void Update()
    {
        if (WorldManager.Instance == null || ObjectPooler.Instance == null) return;
        
        // Dừng sinh chướng ngại vật khi đang đấu chiêu (Clash)
        if (ClashManager.Instance != null && ClashManager.Instance.IsClashing()) return;

        if (debugMode)
        {
            WorldManager.Instance.currentSpeed = 0f;
            
            if (debugMode != lastDebugMode || forcePattern != lastForcePattern)
            {
                MoveObject[] oldTraps = FindObjectsOfType<MoveObject>();
                foreach (var trap in oldTraps)
                {
                    if (trap.CompareTag("Obstacle")) trap.gameObject.SetActive(false);
                }
                
                float originalZ = spawnZ;
                spawnZ = 20f;
                SpawnPattern(forcePattern);
                spawnZ = originalZ;

                lastDebugMode = debugMode;
                lastForcePattern = forcePattern;
            }
            return;
        }
        else if (lastDebugMode)
        {
            WorldManager.Instance.currentSpeed = WorldManager.Instance.baseSpeed;
            lastDebugMode = false;
        }

        float moveDist = WorldManager.Instance.currentSpeed * Time.deltaTime;
        distanceTraveled += moveDist;
        totalDistance += moveDist; // Theo dõi tổng quãng đường chạy được

        // Giảm dần quãng đường bị vách núi chiếm dụng
        if (blockedWallRemainingDistance > 0)
        {
            blockedWallRemainingDistance -= moveDist;
        }

        // --- HỆ THỐNG TRIỆU HỒI BOSS NARUTO ---
        if (totalDistance >= nextNarutoDistance)
        {
            SpawnNarutoBoss();
            nextNarutoDistance += 1500f;
        }

        if (distanceTraveled >= distanceBetweenSpawns)
        {
            // Dọn đường 300m trước khi Boss xuất hiện, không sinh thêm bẫy
            if (totalDistance < nextNarutoDistance - 300f || totalDistance > nextNarutoDistance + 50f) 
            {
                SpawnObstacles();
            }
            distanceTraveled = 0f; 
        }
    }

    void SpawnNarutoBoss()
    {
        Debug.Log("⚠️ CẢNH BÁO: BOSS NARUTO XUẤT HIỆN!");
        if (narutoPrefab != null)
        {
            // Sinh Naruto ở chính giữa làn đường, cách xa 400m
            Vector3 spawnPos = new Vector3(0, 0, spawnZ + 20f); // Spawn xa hơn một chút
            Instantiate(narutoPrefab, spawnPos, Quaternion.Euler(0, 180, 0));
        }
    }

    void SpawnObstacles()
    {
        float rand = Random.value;
        int pattern = 1;

        if (rand < 0.15f) pattern = 1;       
        else if (rand < 0.30f) pattern = 2;  
        else if (rand < 0.45f) pattern = 3;  
        else if (rand < 0.55f) pattern = 4;  
        else if (rand < 0.70f) pattern = 5;  
        else if (rand < 0.75f) pattern = 6;  
        else if (rand < 0.85f) pattern = 7;  
        else if (rand < 0.92f) pattern = 8;  
        else pattern = 9;                    

        // Nếu vách núi vẫn đang chắn đường, CẤM tuyệt đối sinh thêm vách núi hoặc thiên thạch (tránh kẹt hình/xuyên tường)
        // Ép game tự động đổi sang các bẫy nhỏ (1-6) để người chơi vừa chạy né vách núi vừa né bẫy con
        if (blockedWallRemainingDistance > 0 && pattern >= 7)
        {
            pattern = Random.Range(1, 7); 
        }

        SpawnPattern(pattern);
    }

    private bool IsLaneBlocked(int lane)
    {
        if (blockedWallRemainingDistance > 0)
        {
            // Vách trái (0) chiếm làn -1 và 0. Vách phải (1) chiếm làn 1 và 0.
            if (blockedWallSide == 0 && (lane == -1 || lane == 0)) return true;
            if (blockedWallSide == 1 && (lane == 1 || lane == 0)) return true;
        }
        return false;
    }

    private int GetRandomLane()
    {
        if (blockedWallRemainingDistance > 0)
        {
            // Nếu có vách núi, làn ngẫu nhiên DUY NHẤT được phép trả về là làn an toàn
            return (blockedWallSide == 0) ? 1 : -1;
        }
        return Random.Range(-1, 2);
    }

    void SpawnPattern(int patternId)
    {
        if (patternId == 1) SpawnRock(GetRandomLane(), false);
        else if (patternId == 2)
        {
            int emptyLane = GetRandomLane();
            for (int i = -1; i <= 1; i++) {
                if (i != emptyLane) SpawnRock(i, false);
            }
        }
        else if (patternId == 3)
        {
            int[] lanes = { -1, 0, 1 };
            ShuffleArray(lanes);
            for (int i = 0; i < 3; i++) SpawnRock(lanes[i], false, -i * 8f);
        }
        else if (patternId == 4) SpawnShuriken(GetRandomLane());
        else if (patternId == 5)
        {
            SpawnRock(-1, false);
            SpawnShuriken(0);
            SpawnRock(1, false);
        }
        else if (patternId == 6) SpawnRock(GetRandomLane(), true);
        else if (patternId == 7) SpawnGiantRock("GiantRockSlide");
        else if (patternId == 8) SpawnGiantRock("GiantRockJump");
        else if (patternId == 9)
        {
            int side = Random.Range(0, 2); 
            SpawnMountainWall(side);
            
            // Phi tiêu sẽ bay thẳng vào làn an toàn, người chơi phải nhảy hoặc cúi để né
            int safeLane = (side == 0) ? 1 : -1;
            SpawnShuriken(safeLane); 

            // Khóa 2 làn của vách núi trong 140m tiếp theo
            blockedWallSide = side;
            blockedWallRemainingDistance = 140f; 
        }

        // --- RẢI ĐỒNG XU (COINS) ---
        if (Random.value > 0.5f)
        {
            // Lấy làn ngẫu nhiên (nếu vách núi đang tồn tại, hàm này tự động trả về làn an toàn duy nhất)
            int coinLaneIndex = GetRandomLane(); 
            float coinLaneX = coinLaneIndex * laneDistance;
            int numCoins = Random.Range(3, 6);
            
            for (int i = 0; i < numCoins; i++)
            {
                Vector3 spawnPos = new Vector3(coinLaneX, 1.0f, spawnZ + (i * 3f));
                if (ObjectPooler.Instance != null && ObjectPooler.Instance.poolDictionary.ContainsKey("Coin"))
                {
                    GameObject coinObj = ObjectPooler.Instance.SpawnFromPool("Coin", spawnPos, Quaternion.Euler(90f, 0f, 0f));
                }
                else
                {
                    GameObject coinObj = Instantiate(coinPrefab);
                    coinObj.transform.position = spawnPos;
                    coinObj.SetActive(true);
                }
            }
        }
    }

    private void SpawnMountainWall(int side)
    {
        float wallX = (side == 0) ? -49.5f : 49.5f; 
        
        // Lấy tọa độ Y hiện tại của Prefab từ trong Pool để không ghi đè nó
        float currentY = 20f;
        if (ObjectPooler.Instance.poolDictionary.ContainsKey("MountainWall") && ObjectPooler.Instance.poolDictionary["MountainWall"].Count > 0)
        {
            currentY = ObjectPooler.Instance.poolDictionary["MountainWall"].Peek().transform.position.y;
        }

        Vector3 spawnPos = new Vector3(wallX, currentY, spawnZ); 
        ObjectPooler.Instance.SpawnFromPool("MountainWall", spawnPos, Quaternion.identity);
    }

    private void SpawnShuriken(int lane)
    {
        if (IsLaneBlocked(lane)) return; // BẤT KHẢ XÂM PHẠM LÀN VÁCH NÚI

        float targetX = lane * laneDistance;
        Vector3 spawnPos = new Vector3(targetX, shurikenHeight, spawnZ);
        
        // Ép xoay nằm ngang theo thông số đã chỉnh
        GameObject shuriken = ObjectPooler.Instance.SpawnFromPool("Shuriken", spawnPos, Quaternion.Euler(shurikenSpawnRotation));
        if (shuriken != null)
        {
            // Áp dụng tỷ lệ đồng dạng tuyệt đối với Sasuke (5, 5, 5) như bên Three.js
            shuriken.transform.localScale = new Vector3(5f, 5f, 5f);
            
            ShurikenObstacle script = shuriken.GetComponent<ShurikenObstacle>();
            if (script == null) script = shuriken.AddComponent<ShurikenObstacle>();
            
            script.extraSpeed = shurikenExtraSpeed;
            script.rotationSpeed = shurikenRotationSpeed;
        }
    }



    private void ShuffleArray(int[] array)
    {
        for (int i = 0; i < array.Length; i++)
        {
            int temp = array[i];
            int r = Random.Range(i, array.Length);
            array[i] = array[r];
            array[r] = temp;
        }
    }

    private void SpawnGiantRock(string poolTag)
    {
        // Lấy tọa độ Y hiện tại của Prefab từ trong Pool để không ghi đè nó
        float currentY = 0f;
        if (ObjectPooler.Instance.poolDictionary.ContainsKey(poolTag) && ObjectPooler.Instance.poolDictionary[poolTag].Count > 0)
        {
            currentY = ObjectPooler.Instance.poolDictionary[poolTag].Peek().transform.position.y;
        }

        Vector3 spawnPos = new Vector3(0f, currentY, spawnZ);
        ObjectPooler.Instance.SpawnFromPool(poolTag, spawnPos, Quaternion.identity);
    }

    private void SpawnRock(int lane, bool isMeteor, float offsetZ = 0f)
    {
        if (IsLaneBlocked(lane)) return; // BẤT KHẢ XÂM PHẠM LÀN VÁCH NÚI

        float targetX = lane * laneDistance;
        Vector3 spawnPos = new Vector3(targetX, 0f, spawnZ + offsetZ);
        
        GameObject rock = ObjectPooler.Instance.SpawnFromPool("Rock", spawnPos, Quaternion.identity);
        if (rock != null)
        {
            float scaleXZ = Random.Range(1.2f, 1.6f); 
            float scaleY = Random.Range(1.5f, 2.5f);  
            
            rock.transform.localScale = new Vector3(scaleXZ, scaleY, scaleXZ);
            
            float baseHeight = 1.2f * scaleY; 
            float sinkingDepth = 0.8f; 
            if (isMeteor) sinkingDepth *= 1.5f; 
            
            rock.transform.position = new Vector3(targetX, baseHeight - sinkingDepth, spawnZ + offsetZ);
            rock.transform.rotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);

            FallingObstacle fallingScript = rock.GetComponent<FallingObstacle>();
            if (fallingScript == null) fallingScript = rock.AddComponent<FallingObstacle>();
            
            fallingScript.Setup(isMeteor);
        }
    }
}
