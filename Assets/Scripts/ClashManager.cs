using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class ClashManager : MonoBehaviour
{
    public static ClashManager Instance;

    private GameObject clashCanvas;
    private RectTransform progressBarInner;
    private TMPro.TextMeshProUGUI clashText;

    private bool isClashing = false;
    private float clashTimer = 0f;
    private int spacePresses = 0;
    
    public int pressesRequired = 25; // Cần bấm 25 lần
    public float clashDuration = 3.0f; // Trong 3 giây

    private GameObject narutoRef;

    void Awake()
    {
        if (Instance == null) 
        {
            Instance = this;
            CreateUI();
        }
        else 
        {
            Destroy(gameObject);
        }
    }

    void CreateUI()
    {
        clashCanvas = new GameObject("ClashCanvas");
        Canvas canvas = clashCanvas.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 999;
        
        CanvasScaler scaler = clashCanvas.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        
        GameObject bg = new GameObject("BarBackground");
        bg.transform.SetParent(clashCanvas.transform, false);
        Image bgImg = bg.AddComponent<Image>();
        bgImg.color = new Color(0.2f, 0.2f, 0.2f, 0.8f);
        RectTransform bgRect = bg.GetComponent<RectTransform>();
        bgRect.sizeDelta = new Vector2(600, 40);
        bgRect.anchoredPosition = new Vector2(0, -200); 

        GameObject inner = new GameObject("BarInner");
        inner.transform.SetParent(bg.transform, false);
        Image innerImg = inner.AddComponent<Image>();
        innerImg.color = new Color(1f, 0.8f, 0f, 1f); 
        progressBarInner = inner.GetComponent<RectTransform>();
        progressBarInner.anchorMin = new Vector2(0, 0); // Kéo giãn theo chiều cao
        progressBarInner.anchorMax = new Vector2(0, 1);
        progressBarInner.pivot = new Vector2(0, 0.5f);
        progressBarInner.sizeDelta = new Vector2(600, 0); // Chiều dài 600, cao bằng parent
        progressBarInner.anchoredPosition = new Vector2(0, 0);
        progressBarInner.localScale = new Vector3(0, 1, 1); // Scale X từ 0 đến 1

        GameObject txtObj = new GameObject("InstructionText");
        txtObj.transform.SetParent(clashCanvas.transform, false);
        clashText = txtObj.AddComponent<TMPro.TextMeshProUGUI>();
        clashText.text = "NHẤP PHÍM SPACE LIÊN TỤC!";
        clashText.fontSize = 40;
        clashText.color = Color.red;
        clashText.alignment = TMPro.TextAlignmentOptions.Center;
        clashText.fontStyle = TMPro.FontStyles.Bold;
        RectTransform txtRect = txtObj.GetComponent<RectTransform>();
        txtRect.sizeDelta = new Vector2(800, 100);
        txtRect.anchoredPosition = new Vector2(0, -130);

        clashCanvas.SetActive(false);
    }

    public void StartClash(GameObject naruto)
    {
        if (isClashing) return;
        
        isClashing = true;
        narutoRef = naruto;
        spacePresses = 0;
        clashTimer = clashDuration;

        WorldManager.Instance.currentSpeed = 0f;
        
        clashCanvas.SetActive(true);
        UpdateProgressBar();
        
        StartCoroutine(ClashRoutine());
    }

    void Update()
    {
        if (!isClashing) return;

        if (UnityEngine.InputSystem.Keyboard.current != null && UnityEngine.InputSystem.Keyboard.current.spaceKey.wasPressedThisFrame)
        {
            spacePresses++;
            UpdateProgressBar();
            if (clashText) clashText.color = Random.ColorHSV(0f, 1f, 1f, 1f, 1f, 1f);
        }
    }

    void UpdateProgressBar()
    {
        float ratio = Mathf.Clamp01((float)spacePresses / pressesRequired);
        if (progressBarInner != null)
        {
            progressBarInner.localScale = new Vector3(ratio, 1f, 1f);
        }
    }

    IEnumerator ClashRoutine()
    {
        while (clashTimer > 0)
        {
            clashTimer -= Time.deltaTime;
            yield return null;
        }

        EndClash();
    }

    void EndClash()
    {
        isClashing = false;
        clashCanvas.SetActive(false);

        if (spacePresses >= pressesRequired)
        {
            Debug.Log("CHIDORI THẮNG RASENGAN!");
            
            // Xóa Naruto
            if (narutoRef != null) 
            {
                // Thêm tí hạt bụi (nếu rảnh)
                narutoRef.SetActive(false);
            }
            
            WorldManager.Instance.currentSpeed = WorldManager.Instance.baseSpeed + (WorldManager.Instance.acceleration * Time.timeSinceLevelLoad); 
        }
        else
        {
            Debug.Log("THUA CLASH! GAME OVER");
            if (GameManager.Instance != null) GameManager.Instance.GameOver();
        }
    }

    public bool IsClashing()
    {
        return isClashing;
    }
}
