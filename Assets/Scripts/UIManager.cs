using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance;

    [Header("Skill Textures (Kéo 3 ảnh kỹ năng vào đây)")]
    public Texture2D skill1Texture;
    public Texture2D skill2Texture;
    public Texture2D skill3Texture;

    private GameObject hudCanvas;
    private TextMeshProUGUI scoreText;
    private TextMeshProUGUI coinText;
    private TextMeshProUGUI distanceText; // Thêm UI hiển thị số mét đã chạy

    private RectTransform btn1Rect, btn2Rect, btn3Rect;
    private Image mask1, mask2, mask3;

    public float score = 0f;
    public int coins = 0;
    private float totalDistance = 0f; // Lưu trữ quãng đường

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    void Start()
    {
        CreateDynamicHUD();
    }

    void Update()
    {
        // Tự động tăng điểm và quãng đường theo WorldManager
        if (WorldManager.Instance != null && WorldManager.Instance.currentSpeed > 0)
        {
            float dist = WorldManager.Instance.currentSpeed * Time.deltaTime;
            score += dist * 0.5f;
            totalDistance += dist;
        }

        if (scoreText != null) scoreText.text = Mathf.FloorToInt(score).ToString();
        if (coinText != null) coinText.text = coins.ToString();
        if (distanceText != null) distanceText.text = Mathf.FloorToInt(totalDistance).ToString() + "m";

        if (SkillManager.Instance != null)
        {
            if (mask1 != null) mask1.fillAmount = 1f - SkillManager.Instance.GetFireballCooldownRatio();
            if (mask2 != null) mask2.fillAmount = 1f - SkillManager.Instance.GetChidoriCooldownRatio();
            if (mask3 != null) mask3.fillAmount = 1f - SkillManager.Instance.GetSusanooCooldownRatio();
        }
    }

    public void AnimateSkillButton(int skillIndex)
    {
        RectTransform targetBtn = null;
        if (skillIndex == 1) targetBtn = btn1Rect;
        if (skillIndex == 2) targetBtn = btn2Rect;
        if (skillIndex == 3) targetBtn = btn3Rect;

        if (targetBtn != null)
        {
            StartCoroutine(PulseButton(targetBtn));
        }
    }

    private void CreateDynamicHUD()
    {
        hudCanvas = new GameObject("DynamicHUDCanvas");
        Canvas canvas = hudCanvas.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 50;

        CanvasScaler scaler = hudCanvas.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;

        Color goldColor = new Color(241f / 255f, 196f / 255f, 15f / 255f); // #f1c40f
        Vector2 shadowDist = new Vector2(2, -2);
        Color shadowColor = new Color(0, 0, 0, 0.8f);

        // Container cho Score và Coins ở giữa trên cùng
        GameObject hudContainer = new GameObject("HUDContainer");
        hudContainer.transform.SetParent(hudCanvas.transform, false);
        RectTransform cRect = hudContainer.AddComponent<RectTransform>();
        cRect.anchorMin = new Vector2(0.5f, 1f);
        cRect.anchorMax = new Vector2(0.5f, 1f);
        cRect.pivot = new Vector2(0.5f, 1f);
        cRect.anchoredPosition = new Vector2(0, -20); // top: 20px
        cRect.sizeDelta = new Vector2(400, 100);

        // Score Section
        GameObject scoreSection = new GameObject("ScoreSection");
        scoreSection.transform.SetParent(hudContainer.transform, false);
        RectTransform ssRect = scoreSection.AddComponent<RectTransform>();
        ssRect.anchorMin = new Vector2(0.5f, 1); // Đổi về Neo giữa
        ssRect.anchorMax = new Vector2(0.5f, 1);
        ssRect.pivot = new Vector2(1, 1);
        ssRect.anchoredPosition = new Vector2(-20, 0); // Cách giữa 20px (gap 40px)
        ssRect.sizeDelta = new Vector2(150, 80);

        GameObject scoreTitleObj = new GameObject("ScoreTitle");
        scoreTitleObj.transform.SetParent(scoreSection.transform, false);
        TextMeshProUGUI scoreTitle = scoreTitleObj.AddComponent<TextMeshProUGUI>();
        scoreTitle.text = "SCORE";
        scoreTitle.fontSize = 24; 
        scoreTitle.color = goldColor;
        scoreTitle.alignment = TextAlignmentOptions.Top;
        scoreTitle.fontStyle = FontStyles.Bold;
        Outline tOut1 = scoreTitleObj.AddComponent<Outline>();
        tOut1.effectColor = Color.black; tOut1.effectDistance = new Vector2(2, -2);
        RectTransform stRect = scoreTitleObj.GetComponent<RectTransform>();
        stRect.anchorMin = new Vector2(0, 1); stRect.anchorMax = new Vector2(1, 1);
        stRect.pivot = new Vector2(0.5f, 1); stRect.sizeDelta = new Vector2(0, 30); stRect.anchoredPosition = Vector2.zero;

        GameObject scoreTextObj = new GameObject("ScoreText");
        scoreTextObj.transform.SetParent(scoreSection.transform, false);
        scoreText = scoreTextObj.AddComponent<TextMeshProUGUI>();
        scoreText.text = "0";
        scoreText.fontSize = 48; 
        scoreText.color = Color.white;
        scoreText.alignment = TextAlignmentOptions.Bottom;
        scoreText.fontStyle = FontStyles.Bold;
        Outline sOut1 = scoreTextObj.AddComponent<Outline>();
        sOut1.effectColor = Color.black; sOut1.effectDistance = new Vector2(3, -3);
        RectTransform svRect = scoreTextObj.GetComponent<RectTransform>();
        svRect.anchorMin = new Vector2(0, 0); svRect.anchorMax = new Vector2(1, 0);
        svRect.pivot = new Vector2(0.5f, 0); svRect.sizeDelta = new Vector2(0, 50); svRect.anchoredPosition = Vector2.zero;

        // Coin Section
        GameObject coinSection = new GameObject("CoinSection");
        coinSection.transform.SetParent(hudContainer.transform, false);
        RectTransform csRect = coinSection.AddComponent<RectTransform>();
        csRect.anchorMin = new Vector2(0.5f, 1); 
        csRect.anchorMax = new Vector2(0.5f, 1);
        csRect.pivot = new Vector2(0, 1);
        csRect.anchoredPosition = new Vector2(20, 0); 
        csRect.sizeDelta = new Vector2(150, 80);

        GameObject coinTitleObj = new GameObject("CoinTitle");
        coinTitleObj.transform.SetParent(coinSection.transform, false);
        TextMeshProUGUI coinTitle = coinTitleObj.AddComponent<TextMeshProUGUI>();
        coinTitle.text = "COINS";
        coinTitle.fontSize = 24;
        coinTitle.color = goldColor;
        coinTitle.alignment = TextAlignmentOptions.Top;
        coinTitle.fontStyle = FontStyles.Bold;
        Outline tOut2 = coinTitleObj.AddComponent<Outline>();
        tOut2.effectColor = Color.black; tOut2.effectDistance = new Vector2(2, -2);
        RectTransform ctRect = coinTitleObj.GetComponent<RectTransform>();
        ctRect.anchorMin = new Vector2(0, 1); ctRect.anchorMax = new Vector2(1, 1);
        ctRect.pivot = new Vector2(0.5f, 1); ctRect.sizeDelta = new Vector2(0, 30); ctRect.anchoredPosition = Vector2.zero;

        GameObject coinTextObj = new GameObject("CoinText");
        coinTextObj.transform.SetParent(coinSection.transform, false);
        coinText = coinTextObj.AddComponent<TextMeshProUGUI>();
        coinText.text = "0";
        coinText.fontSize = 48;
        coinText.color = goldColor; 
        coinText.alignment = TextAlignmentOptions.Bottom;
        coinText.fontStyle = FontStyles.Bold;
        Outline sOut2 = coinTextObj.AddComponent<Outline>();
        sOut2.effectColor = Color.black; sOut2.effectDistance = new Vector2(3, -3);
        RectTransform cvRect = coinTextObj.GetComponent<RectTransform>();
        cvRect.anchorMin = new Vector2(0, 0); cvRect.anchorMax = new Vector2(1, 0);
        cvRect.pivot = new Vector2(0.5f, 0); cvRect.sizeDelta = new Vector2(0, 50); cvRect.anchoredPosition = Vector2.zero;

        // Skill Buttons (Góc trên trái)
        btn3Rect = CreateSkillButton(hudCanvas.transform, "Skill 3", 3, skill3Texture, out mask3);
        btn2Rect = CreateSkillButton(hudCanvas.transform, "Skill 2", 2, skill2Texture, out mask2);
        btn1Rect = CreateSkillButton(hudCanvas.transform, "Skill 1", 1, skill1Texture, out mask1);
    }

    private Sprite CreateCircleSprite()
    {
        int size = 256;
        Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        texture.filterMode = FilterMode.Bilinear;
        
        Color[] colorData = new Color[size * size];
        float radius = size / 2f;
        Vector2 center = new Vector2(radius, radius);
        
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dist = Vector2.Distance(center, new Vector2(x, y));
                float alpha = Mathf.Clamp01((radius - dist) / 1.5f);
                colorData[y * size + x] = new Color(1f, 1f, 1f, alpha);
            }
        }
        texture.SetPixels(colorData);
        texture.Apply();
        return Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect);
    }

    // Tạo sprite vòng tròn tỏa mờ dần (như Gaussian Blur) để làm Box-shadow
    private Sprite CreateBlurryCircleSprite()
    {
        int size = 256;
        Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        texture.filterMode = FilterMode.Bilinear;
        
        Color[] colorData = new Color[size * size];
        float radius = size / 2f;
        Vector2 center = new Vector2(radius, radius);
        
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dist = Vector2.Distance(center, new Vector2(x, y));
                float alpha = 1f;
                float innerRadius = radius * 0.7f; // Giữ đặc 70% bên trong
                if (dist > innerRadius)
                {
                    // 30% bên ngoài sẽ mờ dần ra không khí (blur)
                    alpha = 1f - ((dist - innerRadius) / (radius - innerRadius));
                    alpha = Mathf.SmoothStep(0f, 1f, alpha);
                }
                if (dist > radius) alpha = 0f;
                colorData[y * size + x] = new Color(1f, 1f, 1f, alpha);
            }
        }
        texture.SetPixels(colorData);
        texture.Apply();
        return Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect);
    }

    private RectTransform CreateSkillButton(Transform parent, string name, int index, Texture2D tex, out Image cooldownMask)
    {
        Sprite circleSprite = CreateCircleSprite();
        Sprite blurrySprite = CreateBlurryCircleSprite(); 

        // Vùng chứa nút (Tăng kích thước lên 100x100)
        GameObject btnObj = new GameObject(name);
        btnObj.transform.SetParent(parent, false);
        RectTransform rect = btnObj.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0, 1f); 
        rect.anchorMax = new Vector2(0, 1f);
        rect.pivot = new Vector2(0, 1f);
        
        // Giữ nguyên khoảng cách (Gap = 20px)
        float topOffset = 20; 
        if (index == 3) topOffset = 20; 
        else if (index == 2) topOffset = 140; // 20 + 100 + 20 = 140
        else if (index == 1) topOffset = 260; // 140 + 100 + 20 = 260
        
        rect.anchoredPosition = new Vector2(20, -topOffset); 
        rect.sizeDelta = new Vector2(100, 100); // Size 100x100

        // Box Shadow 
        GameObject shadowObj = new GameObject("BoxShadow");
        shadowObj.transform.SetParent(btnObj.transform, false);
        Image shadowImg = shadowObj.AddComponent<Image>();
        shadowImg.sprite = blurrySprite; 
        shadowImg.color = new Color(0, 0, 0, 0.7f); 
        RectTransform shadowRect = shadowObj.GetComponent<RectTransform>();
        shadowRect.anchorMin = Vector2.zero;
        shadowRect.anchorMax = Vector2.one;
        shadowRect.sizeDelta = new Vector2(14, 14); // 114x114

        // Viền nút 
        GameObject borderObj = new GameObject("Border");
        borderObj.transform.SetParent(btnObj.transform, false);
        Image borderImg = borderObj.AddComponent<Image>();
        borderImg.sprite = circleSprite;
        borderImg.color = new Color(1f, 1f, 1f, 0.5f);
        RectTransform bRect = borderObj.GetComponent<RectTransform>();
        bRect.anchorMin = Vector2.zero; bRect.anchorMax = Vector2.one;
        bRect.sizeDelta = new Vector2(8, 8); // 108x108 (Dày 4px)

        // Nền đen 
        GameObject bgDarkObj = new GameObject("BgDark");
        bgDarkObj.transform.SetParent(btnObj.transform, false);
        Image bgDark = bgDarkObj.AddComponent<Image>();
        bgDark.sprite = circleSprite;
        bgDark.color = new Color(0.2f, 0.2f, 0.2f); 
        RectTransform bdRect = bgDarkObj.GetComponent<RectTransform>();
        bdRect.anchorMin = Vector2.zero; bdRect.anchorMax = Vector2.one;
        bdRect.sizeDelta = Vector2.zero;

        // Mặt nạ bo tròn
        GameObject maskObj = new GameObject("Mask");
        maskObj.transform.SetParent(btnObj.transform, false);
        Image maskImg = maskObj.AddComponent<Image>();
        maskImg.sprite = circleSprite;
        Mask uiMask = maskObj.AddComponent<Mask>();
        uiMask.showMaskGraphic = false; 
        RectTransform mRect = maskObj.GetComponent<RectTransform>();
        mRect.anchorMin = Vector2.zero; mRect.anchorMax = Vector2.one;
        mRect.sizeDelta = Vector2.zero;

        // Ảnh nền của nút
        GameObject imgObj = new GameObject("Image");
        imgObj.transform.SetParent(maskObj.transform, false);
        if (tex != null)
        {
            RawImage bgImg = imgObj.AddComponent<RawImage>();
            bgImg.texture = tex;
        }
        RectTransform bgRect = imgObj.GetComponent<RectTransform>();
        bgRect.anchorMin = Vector2.zero; bgRect.anchorMax = Vector2.one;
        bgRect.sizeDelta = Vector2.zero;

        // Mask đếm ngược thời gian hồi chiêu
        GameObject cdObj = new GameObject("CooldownMask");
        cdObj.transform.SetParent(maskObj.transform, false);
        cooldownMask = cdObj.AddComponent<Image>();
        cooldownMask.sprite = circleSprite; 
        cooldownMask.color = new Color(0, 0, 0, 0.7f); 
        cooldownMask.type = Image.Type.Filled;
        cooldownMask.fillMethod = Image.FillMethod.Radial360;
        cooldownMask.fillOrigin = (int)Image.Origin360.Top;
        cooldownMask.fillClockwise = false;
        cooldownMask.fillAmount = 0f;
        RectTransform cdRect = cdObj.GetComponent<RectTransform>();
        cdRect.anchorMin = Vector2.zero; cdRect.anchorMax = Vector2.one;
        cdRect.sizeDelta = Vector2.zero;

        // Tâm chung cho phím tắt (để không bị lệch tâm gây méo viền)
        Vector2 keyCenter = new Vector2(-15, 15); // Góc dưới phải

        // Viền trắng mờ cho phím tắt (Nằm dưới)
        GameObject keyBorderObj = new GameObject("KeyBorder");
        keyBorderObj.transform.SetParent(btnObj.transform, false);
        Image keyBorder = keyBorderObj.AddComponent<Image>();
        keyBorder.sprite = circleSprite;
        keyBorder.color = new Color(1f, 1f, 1f, 0.5f);
        RectTransform kbRect = keyBorderObj.GetComponent<RectTransform>();
        kbRect.anchorMin = new Vector2(1, 0); kbRect.anchorMax = new Vector2(1, 0);
        kbRect.pivot = new Vector2(0.5f, 0.5f); // Đặt tâm ở chính giữa!
        kbRect.anchoredPosition = keyCenter; 
        kbRect.sizeDelta = new Vector2(44, 44); // To hơn nền đen 8px
        
        // Hình tròn chứa phím tắt ở góc dưới phải
        GameObject keyBgObj = new GameObject("KeyCircle");
        keyBgObj.transform.SetParent(btnObj.transform, false);
        Image keyBg = keyBgObj.AddComponent<Image>();
        keyBg.sprite = circleSprite;
        keyBg.color = new Color(0, 0, 0, 0.8f);
        RectTransform keyRect = keyBgObj.GetComponent<RectTransform>();
        keyRect.anchorMin = new Vector2(1, 0); 
        keyRect.anchorMax = new Vector2(1, 0);
        keyRect.pivot = new Vector2(0.5f, 0.5f); // Đặt tâm ở chính giữa!
        keyRect.anchoredPosition = keyCenter; // CHUNG TÂM VỚI VIỀN!
        keyRect.sizeDelta = new Vector2(36, 36); 

        // Chữ phím tắt
        GameObject textObj = new GameObject("KeyText");
        textObj.transform.SetParent(keyBgObj.transform, false);
        TextMeshProUGUI txt = textObj.AddComponent<TextMeshProUGUI>();
        txt.text = index.ToString();
        txt.fontSize = 20; // Phóng to chữ phím tắt lên 20
        txt.color = Color.white;
        txt.fontStyle = FontStyles.Bold;
        txt.alignment = TextAlignmentOptions.Center; 
        RectTransform txtRect = textObj.GetComponent<RectTransform>();
        txtRect.anchorMin = Vector2.zero; txtRect.anchorMax = Vector2.one;
        txtRect.sizeDelta = Vector2.zero; txtRect.anchoredPosition = Vector2.zero;

        return rect;
    }

    private IEnumerator PulseButton(RectTransform btn)
    {
        float elapsed = 0f;
        float duration = 0.2f; // scale(1.3) mượt mà nhưng nhanh gọn (0.1s up, 0.1s down)
        Vector3 originalScale = Vector3.one;
        Vector3 targetScale = new Vector3(1.3f, 1.3f, 1.3f); 
        
        Image shadowImg = btn.Find("BoxShadow").GetComponent<Image>();
        Image borderImg = btn.Find("Border").GetComponent<Image>();

        Color normalShadow = new Color(0, 0, 0, 0.7f);
        Color glowShadow = new Color(0f, 229f/255f, 1f, 0.9f); // Cyan mạnh hơn
        Color normalBorder = new Color(1f, 1f, 1f, 0.5f);
        Color cyanBorder = new Color(0f, 229f/255f, 1f, 1f);

        // Bơm to ra và đổi Box-shadow từ Đen sang Cyan mờ
        while (elapsed < duration / 2f)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = elapsed / (duration / 2f);
            btn.localScale = Vector3.Lerp(originalScale, targetScale, t);
            if (shadowImg != null)
            {
                shadowImg.color = Color.Lerp(normalShadow, glowShadow, t);
                shadowImg.rectTransform.sizeDelta = Vector2.Lerp(new Vector2(10, 10), new Vector2(25, 25), t); // Lan tỏa ra nhẹ nhàng hơn!
            }
            if (borderImg != null) borderImg.color = Color.Lerp(normalBorder, cyanBorder, t);
            yield return null;
        }

        elapsed = 0f;
        // Thu nhỏ lại và trả về Box-shadow đen
        while (elapsed < duration / 2f)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = elapsed / (duration / 2f);
            btn.localScale = Vector3.Lerp(targetScale, originalScale, t);
            if (shadowImg != null)
            {
                shadowImg.color = Color.Lerp(glowShadow, normalShadow, t);
                shadowImg.rectTransform.sizeDelta = Vector2.Lerp(new Vector2(25, 25), new Vector2(10, 10), t);
            }
            if (borderImg != null) borderImg.color = Color.Lerp(cyanBorder, normalBorder, t);
            yield return null;
        }
        btn.localScale = originalScale;
        if (shadowImg != null) 
        {
            shadowImg.color = normalShadow;
            shadowImg.rectTransform.sizeDelta = new Vector2(10, 10);
        }
        if (borderImg != null) borderImg.color = normalBorder;
    }
}
