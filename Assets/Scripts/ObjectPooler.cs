using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class PoolItem
{
    public string tag;
    public GameObject prefab;
    public int size; // Số lượng nạp sẵn vào bộ nhớ (VD: 20 cục đá)
}

public class ObjectPooler : MonoBehaviour
{
    public static ObjectPooler Instance;

    public List<PoolItem> pools;
    public Dictionary<string, Queue<GameObject>> poolDictionary;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        InjectDynamicPrefabs();

        poolDictionary = new Dictionary<string, Queue<GameObject>>();

        // Sinh sẵn toàn bộ các GameObject và cho chúng ẩn đi (SetActive = false)
        foreach (PoolItem item in pools)
        {
            Queue<GameObject> objectPool = new Queue<GameObject>();

            for (int i = 0; i < item.size; i++)
            {
                GameObject obj = Instantiate(item.prefab);
                obj.SetActive(false);
                obj.transform.SetParent(this.transform); // Gom vào thư mục cho gọn Hierarchy
                objectPool.Enqueue(obj);
            }

            poolDictionary.Add(item.tag, objectPool);
        }
    }

    // Lấy 1 vật thể ra từ Pool để tái sử dụng
    public GameObject SpawnFromPool(string tag, Vector3 position, Quaternion rotation)
    {
        if (!poolDictionary.ContainsKey(tag))
        {
            Debug.LogWarning("Pool with tag " + tag + " doesn't exist.");
            return null;
        }

        // Rút vật thể cũ nhất ra
        GameObject objectToSpawn = poolDictionary[tag].Dequeue();

        // Kích hoạt lại và đặt vào tọa độ mới
        objectToSpawn.SetActive(true);
        objectToSpawn.transform.position = position;
        objectToSpawn.transform.rotation = rotation;

        // Vứt lại vào cuối hàng chờ để vòng luân hồi tiếp tục
        poolDictionary[tag].Enqueue(objectToSpawn);

        return objectToSpawn;
    }

    private void InjectDynamicPrefabs()
    {
        bool hasGiantRock = false;
        bool hasMountainWall = false;

        foreach (PoolItem item in pools)
        {
            if (item.tag == "GiantRock") hasGiantRock = true;
            if (item.tag == "MountainWall") hasMountainWall = true;
        }

        Material rockMat = null;
#if UNITY_EDITOR
        rockMat = UnityEditor.AssetDatabase.LoadAssetAtPath<Material>("Assets/RockMat.mat");
#endif

        if (!hasGiantRock)
        {
            GameObject giantRock = new GameObject("GiantRockPrefab");
            giantRock.SetActive(false);
            giantRock.AddComponent<MeshFilter>();
            var mr = giantRock.AddComponent<MeshRenderer>();
            if (rockMat != null) mr.material = rockMat;
            giantRock.AddComponent<MeshCollider>();
            giantRock.AddComponent<GiantRockDeformer>();
            giantRock.AddComponent<MoveObject>();
            giantRock.tag = "Obstacle";

            pools.Add(new PoolItem { tag = "GiantRock", prefab = giantRock, size = 15 });
        }

        if (!hasMountainWall)
        {
            GameObject mountainWall = new GameObject("MountainWallPrefab");
            mountainWall.SetActive(false);
            mountainWall.AddComponent<MeshFilter>();
            var mr = mountainWall.AddComponent<MeshRenderer>();
            if (rockMat != null) mr.material = rockMat;
            mountainWall.AddComponent<MeshCollider>();
            mountainWall.AddComponent<MountainWallDeformer>();
            mountainWall.AddComponent<MoveObject>();
            mountainWall.tag = "Obstacle";
            // Kích thước của MountainWall gốc bên Three.js là (100, 40, 100)
            mountainWall.transform.localScale = new Vector3(100f, 40f, 100f);

            pools.Add(new PoolItem { tag = "MountainWall", prefab = mountainWall, size = 10 });
        }
    }
}
