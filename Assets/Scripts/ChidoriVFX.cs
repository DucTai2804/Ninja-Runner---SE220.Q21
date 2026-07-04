using UnityEngine;

public class ChidoriVFX : MonoBehaviour
{
    [Header("=== QUẢ CẦU LÕI ===")]
    public float coreInnerSize = 0.06f;
    public float coreOuterSize = 0.12f;

    [Header("=== BRANCHING LIGHTNING (Sét phụ) ===")]
    public float minThickness = 0.02f;
    public float maxThickness = 0.2f; // Cập nhật theo ảnh
    public float lightningLifetime = 0.6f;
    public float emissionRate = 200f;
    public float brNoiseStrength = 7f; // Cập nhật theo ảnh
    public float brNoiseFrequency = 1f;
    [Range(4f, 25f)] public float brSpeed = 18.5f;

    [Header("=== CORE LIGHTNING (Sét chính) ===")]
    public float coreRadius = 0.12f;
    public float coreMinSize = 1f;
    public float coreMaxSize = 1f;
    public float coreLifetime = 0.03f;
    public float coreEmissionRate = 200f;
    public float coreNoiseStrength = 10f;
    public float coreNoiseFrequency = 8f;
    [Range(0f, 25f)] public float coreSpeed = 11f;

    private Transform targetBone;
    private Vector3 positionOffset = Vector3.zero;
    private ParticleSystem psBranching;
    private ParticleSystem psCore;
    private Transform innerSphereT;
    private Transform coreSphereT;

    void OnEnable()
    {
        transform.localScale = Vector3.one; // Khôi phục kích thước khi được bật lại
        if (psBranching != null) psBranching.transform.localScale = Vector3.one;
        if (psCore != null) psCore.transform.localScale = Vector3.one;
        
        if (innerSphereT != null) 
        {
            Material innerMat = innerSphereT.GetComponent<Renderer>().material;
            Color innerColor = innerMat.GetColor("_TintColor");
            innerColor.a = 1f;
            innerMat.SetColor("_TintColor", innerColor);
        }
        if (coreSphereT != null) 
        {
            Material outerMat = coreSphereT.GetComponent<Renderer>().material;
            Color outerColor = outerMat.GetColor("_TintColor");
            outerColor.a = 0.8f;
            outerMat.SetColor("_TintColor", outerColor);
        }
    }

    void Start()
    {
        // 1. TÌM CHÍNH XÁC VỊ TRÍ XƯƠNG NGÓN TAY NHƯ THREE.JS
        Animator anim = transform.root.GetComponentInChildren<Animator>();
        if (anim != null)
        {
            if (anim.isHuman)
            {
                targetBone = anim.GetBoneTransform(HumanBodyBones.RightMiddleProximal);
                if (targetBone == null)
                {
                    targetBone = anim.GetBoneTransform(HumanBodyBones.RightHand);
                    positionOffset = new Vector3(0f, -0.12f, 0f); 
                }
            }
            else
            {
                targetBone = FindBoneByName(anim.transform, new string[] { "righthandmiddle1", "right_middle", "r_middle", "hand_r", "r_hand" });
            }
        }

        // ===============================================================
        // QUẢ CẦU TỤ NĂNG LƯỢNG (Giữ nguyên)
        // ===============================================================
        GameObject innerSphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        innerSphere.transform.SetParent(this.transform, false);
        innerSphere.transform.localPosition = Vector3.zero;
        innerSphere.transform.localScale = Vector3.one * coreInnerSize;
        Material innerMat = new Material(Shader.Find("Legacy Shaders/Particles/Additive"));
        innerMat.SetColor("_TintColor", new Color(1f, 1f, 1f, 1f));
        Destroy(innerSphere.GetComponent<Collider>());
        innerSphere.GetComponent<Renderer>().material = innerMat;
        innerSphereT = innerSphere.transform;

        GameObject coreSphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        coreSphere.transform.SetParent(this.transform, false);
        coreSphere.transform.localPosition = Vector3.zero;
        coreSphere.transform.localScale = Vector3.one * coreOuterSize;
        Material coreMat = new Material(Shader.Find("Legacy Shaders/Particles/Additive"));
        coreMat.mainTexture = CreateSoftTexture();
        coreMat.SetColor("_TintColor", new Color(0f, 0.8f, 1f, 0.8f));
        Destroy(coreSphere.GetComponent<Collider>());
        coreSphere.GetComponent<Renderer>().material = coreMat;
        coreSphereT = coreSphere.transform;

        // Material dùng chung cho cả 3 hệ thống Particle
        Material trailMat = new Material(Shader.Find("Legacy Shaders/Particles/Additive"));
        trailMat.mainTexture = CreateSoftTexture();
        trailMat.SetColor("_TintColor", new Color(0f, 0.8f, 1f, 1f));

        // ===============================================================
        // LỚP 1: CORE LIGHTNING — Tia sét chính, dày, sáng trắng, tốc độ cao
        // ===============================================================
        GameObject coreObj = new GameObject("CoreLightning");
        coreObj.transform.SetParent(this.transform, false);
        coreObj.transform.localPosition = Vector3.zero;
        psCore = coreObj.AddComponent<ParticleSystem>();
        psCore.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        // CORE LIGHTNING SETTINGS (Hardcoded)
        var coreMain = psCore.main;
        coreMain.duration = 1f;
        coreMain.loop = true;
        coreMain.startLifetime = new ParticleSystem.MinMaxCurve(coreLifetime * 0.6f, coreLifetime);
        coreMain.startSpeed = new ParticleSystem.MinMaxCurve(coreSpeed * 0.7f, coreSpeed);
        coreMain.startSize = new ParticleSystem.MinMaxCurve(coreMinSize, coreMaxSize);
        coreMain.startColor = new Color(0.85f, 0.95f, 1f, 1f); 
        coreMain.simulationSpace = ParticleSystemSimulationSpace.Local;
        coreMain.maxParticles = 500;

        var coreEmission = psCore.emission;
        coreEmission.rateOverTime = coreEmissionRate;

        var coreShape = psCore.shape;
        coreShape.shapeType = ParticleSystemShapeType.Sphere;
        coreShape.radius = coreRadius;
        coreShape.position = Vector3.zero;

        // Core Trails
        var coreTrails = psCore.trails;
        coreTrails.enabled = true;
        coreTrails.ratio = 1f;
        coreTrails.lifetimeMultiplier = 0.4f;
        coreTrails.minVertexDistance = 0.02f;
        coreTrails.widthOverTrail = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 0.5f, 1f, 1.5f));

        var coreNoise = psCore.noise;
        coreNoise.enabled = true;
        coreNoise.strength = 10f;
        coreNoise.frequency = 8f;
        coreNoise.scrollSpeed = 25f;
        coreNoise.octaveCount = 2;

        var coreSizeOverLife = psCore.sizeOverLifetime;
        coreSizeOverLife.enabled = true;
        coreSizeOverLife.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 0.3f, 1f, 1.8f));

        var coreRenderer = psCore.GetComponent<ParticleSystemRenderer>();
        coreRenderer.material = trailMat;
        coreRenderer.trailMaterial = trailMat;

        psCore.Play();

        // ===============================================================
        // LỚP 2: BRANCHING LIGHTNING — Tia sét phụ, mảnh, xanh nhạt, tỏa rộng
        // ===============================================================
        psBranching = gameObject.AddComponent<ParticleSystem>();
        psBranching.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        
        // BRANCHING LIGHTNING SETTINGS (Hardcoded)
        var brMain = psBranching.main;
        brMain.duration = 1f;
        brMain.loop = true;
        brMain.startLifetime = lightningLifetime;
        brMain.startSpeed = new ParticleSystem.MinMaxCurve(brSpeed * 0.6f, brSpeed);
        brMain.startSize = new ParticleSystem.MinMaxCurve(minThickness, maxThickness);
        brMain.startColor = new Color(0f, 0.8f, 1f, 1f); 
        brMain.simulationSpace = ParticleSystemSimulationSpace.Local;
        brMain.maxParticles = 1000;

        var brEmission = psBranching.emission;
        brEmission.rateOverTime = emissionRate;

        var brShape = psBranching.shape;
        brShape.shapeType = ParticleSystemShapeType.Cone;
        brShape.angle = 30f;
        brShape.radius = 0.06f;
        brShape.position = new Vector3(0f, 0f, -0.6f);
        brShape.length = 3f;

        // Branching Trails
        var brTrails = psBranching.trails;
        brTrails.enabled = true;
        brTrails.ratio = 1f;
        brTrails.lifetimeMultiplier = 0.4f;
        brTrails.minVertexDistance = 0.2f; // Tăng khoảng cách để mượt đường cong, chống gãy góc
        brTrails.widthOverTrail = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 0.4f, 1f, 1.5f));

        var brNoise = psBranching.noise;
        brNoise.enabled = true;
        brNoise.strength = brNoiseStrength;
        brNoise.frequency = brNoiseFrequency;
        brNoise.scrollSpeed = 15f;
        brNoise.octaveCount = 2;
        brNoise.quality = ParticleSystemNoiseQuality.High;

        // Size over Lifetime: mảnh ở gốc → dày khi bay xa
        var brSizeOverLife = psBranching.sizeOverLifetime;
        brSizeOverLife.enabled = true;
        brSizeOverLife.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 0.4f, 1f, 1.5f));

        var brRenderer = psBranching.GetComponent<ParticleSystemRenderer>();
        brRenderer.renderMode = ParticleSystemRenderMode.None; // Ẩn các hạt chấm tròn, chỉ hiện tia sét (trails)
        brRenderer.trailMaterial = trailMat;

        psBranching.Play();

        // ===============================================================
        // LỚP 3: FLASH — Vầng sáng chớp tại điểm tụ (bàn tay)
        // ===============================================================
        GameObject flashObj = new GameObject("ChidoriFlash");
        flashObj.transform.SetParent(this.transform, false);
        flashObj.transform.localPosition = Vector3.zero;
        ParticleSystem psFlash = flashObj.AddComponent<ParticleSystem>();
        psFlash.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        var flashMain = psFlash.main;
        flashMain.duration = 1f;
        flashMain.loop = true;
        flashMain.startLifetime = new ParticleSystem.MinMaxCurve(0.05f, 0.12f);
        flashMain.startSpeed = 0f; // KHÔNG bay — đứng yên tại tâm
        flashMain.startSize = new ParticleSystem.MinMaxCurve(0.3f, 0.7f);
        flashMain.startColor = new Color(0.7f, 0.9f, 1f, 0.9f);
        flashMain.simulationSpace = ParticleSystemSimulationSpace.Local;
        flashMain.maxParticles = 8;

        var flashEmission = psFlash.emission;
        flashEmission.rateOverTime = 0f;
        flashEmission.SetBursts(new ParticleSystem.Burst[] {
            new ParticleSystem.Burst(0f, 2, 3, -1, 0.06f)
        });

        // Tắt Shape để hạt sinh ra đúng tại tâm, không bị trượt ra ngoài
        var flashShape = psFlash.shape;
        flashShape.enabled = false;

        // Size over Lifetime: nở to rồi tắt
        var flashSol = psFlash.sizeOverLifetime;
        flashSol.enabled = true;
        flashSol.size = new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(
            new Keyframe(0f, 0.5f),
            new Keyframe(0.3f, 1f),
            new Keyframe(1f, 0f)
        ));

        // Tắt Velocity, Noise cho Flash — chỉ cần nó đứng yên và chớp
        var flashNoise = psFlash.noise;
        flashNoise.enabled = false;

        // Flash Renderer — dùng Billboard để vầng sáng luôn hướng về camera
        var flashRenderer = psFlash.GetComponent<ParticleSystemRenderer>();
        flashRenderer.renderMode = ParticleSystemRenderMode.Billboard;
        Material flashMat = new Material(Shader.Find("Legacy Shaders/Particles/Additive"));
        flashMat.mainTexture = CreateSoftTexture();
        flashRenderer.material = flashMat;

        psFlash.Play();

        // ===============================================================
        // ĐÈN CHỚP (Giữ nguyên)
        // ===============================================================
        Light pointLight = gameObject.AddComponent<Light>();
        pointLight.type = LightType.Point;
        pointLight.color = new Color(0f, 0.8f, 1f);
        pointLight.range = 8f;
        gameObject.AddComponent<ChidoriBlink>();
    }

    void LateUpdate()
    {
        if (targetBone != null)
        {
            transform.position = targetBone.position;
            
            // Khôi phục lại thuật toán ngắm hướng cánh tay (Dành cho Branching Lightning)
            Animator anim = transform.root.GetComponentInChildren<Animator>();
            if (anim != null && anim.isHuman)
            {
                Transform upperArm = anim.GetBoneTransform(HumanBodyBones.RightUpperArm);
                if (upperArm != null)
                {
                    Vector3 armDirection = (targetBone.position - upperArm.position).normalized;
                    if (armDirection != Vector3.zero)
                    {
                        transform.rotation = Quaternion.LookRotation(armDirection);
                    }
                }
            }
            else
            {
                transform.rotation = transform.root.rotation * Quaternion.Euler(0, 180f, 0);
            }
        }

        // Cập nhật thông số Inspector theo thời gian thực
        if (psBranching != null)
        {
            var main = psBranching.main;
            main.startSize = new ParticleSystem.MinMaxCurve(minThickness, maxThickness);
            main.startLifetime = lightningLifetime;
            main.startSpeed = new ParticleSystem.MinMaxCurve(brSpeed * 0.6f, brSpeed);
            var emission = psBranching.emission;
            emission.rateOverTime = emissionRate;
            var noise = psBranching.noise;
            noise.strength = brNoiseStrength;
            noise.frequency = brNoiseFrequency;
        }
        if (psCore != null)
        {
            var main = psCore.main;
            main.startSize = new ParticleSystem.MinMaxCurve(coreMinSize, coreMaxSize);
            main.startLifetime = new ParticleSystem.MinMaxCurve(coreLifetime * 0.6f, coreLifetime);
            main.startSpeed = new ParticleSystem.MinMaxCurve(coreSpeed * 0.7f, coreSpeed);
            var emission = psCore.emission;
            emission.rateOverTime = coreEmissionRate;
            var noise = psCore.noise;
            noise.strength = coreNoiseStrength;
            noise.frequency = coreNoiseFrequency;
            var shape = psCore.shape;
            shape.radius = coreRadius;
        }
        if (innerSphereT != null) innerSphereT.localScale = Vector3.one * coreInnerSize;
        if (coreSphereT != null) coreSphereT.localScale = Vector3.one * coreOuterSize;
    } // Đóng ngoặc cho hàm LateUpdate()

    // Gọi hàm này từ SkillManager khi hết thời gian duy trì Chidori
    public void FadeOut()
    {
        StartCoroutine(FadeOutRoutine());
    }

    private System.Collections.IEnumerator FadeOutRoutine()
    {
        float fadeOutDuration = 0.5f;
        float elapsed = 0f;

        Material innerMat = innerSphereT.GetComponent<Renderer>().material;
        Material outerMat = coreSphereT.GetComponent<Renderer>().material;

        Color innerColor = innerMat.GetColor("_TintColor");
        Color outerColor = outerMat.GetColor("_TintColor");

        var brTrails = psBranching.trails;
        var coreTrails = psCore.trails;

        float brOriginalWidth = brTrails.widthOverTrail.curveMultiplier;
        float coreOriginalWidth = coreTrails.widthOverTrail.curveMultiplier;

        while (elapsed < fadeOutDuration)
        {
            elapsed += Time.deltaTime;
            float ratio = 1f - (elapsed / fadeOutDuration);
            float safeRatio = Mathf.Max(ratio, 0.01f);
            
            // 1. Dùng Scale để ép chiều dài tia sét tụt giảm tuyệt đối về 0 tại tâm
            psBranching.transform.localScale = Vector3.one * ratio;
            psCore.transform.localScale = Vector3.one * ratio;

            // 2. THUẬT TOÁN BÙ TRỪ: Scale làm mỏng tia sét, ta tăng widthMultiplier lên bấy nhiêu lần
            // -> Tia sét bị ngắn đi nhưng KHÔNG HỀ bị teo nhỏ bề ngang!
            var brCurve = brTrails.widthOverTrail;
            brCurve.curveMultiplier = brOriginalWidth / safeRatio;
            brTrails.widthOverTrail = brCurve;

            var coreCurve = coreTrails.widthOverTrail;
            coreCurve.curveMultiplier = coreOriginalWidth / safeRatio;
            coreTrails.widthOverTrail = coreCurve;

            // 3. Không thu nhỏ quả cầu, chỉ làm mờ dần (Fade Alpha)
            innerColor.a = 1f * ratio;
            outerColor.a = 0.8f * ratio;
            innerMat.SetColor("_TintColor", innerColor);
            outerMat.SetColor("_TintColor", outerColor);

            yield return null;
        }

        // Khôi phục lại trạng thái ban đầu để lần bật sau không bị lỗi
        var brResetCurve = brTrails.widthOverTrail;
        brResetCurve.curveMultiplier = brOriginalWidth;
        brTrails.widthOverTrail = brResetCurve;

        var coreResetCurve = coreTrails.widthOverTrail;
        coreResetCurve.curveMultiplier = coreOriginalWidth;
        coreTrails.widthOverTrail = coreResetCurve;

        psBranching.transform.localScale = Vector3.zero;
        psCore.transform.localScale = Vector3.zero;

        gameObject.SetActive(false); 
    }


    Transform FindBoneByName(Transform current, string[] names)
    {
        string n = current.name.ToLower();
        foreach (string name in names)
        {
            if (n.Contains(name)) return current;
        }
        foreach (Transform child in current)
        {
            Transform found = FindBoneByName(child, names);
            if (found != null) return found;
        }
        return null;
    }

    Texture2D CreateSoftTexture()
    {
        int size = 64;
        Texture2D tex = new Texture2D(size, size, TextureFormat.ARGB32, false);
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dist = Vector2.Distance(new Vector2(x, y), new Vector2(size/2f, size/2f));
                float alpha = Mathf.Clamp01(1f - (dist / (size/2f)));
                tex.SetPixel(x, y, new Color(1f, 1f, 1f, Mathf.Pow(alpha, 1.5f)));
            }
        }
        tex.Apply();
        return tex;
    }
}

public class ChidoriBlink : MonoBehaviour
{
    private Light myLight;
    void Start() { myLight = GetComponent<Light>(); }
    void Update()
    {
        if (myLight != null)
            myLight.intensity = Random.Range(1f, 6f);
    }
}
