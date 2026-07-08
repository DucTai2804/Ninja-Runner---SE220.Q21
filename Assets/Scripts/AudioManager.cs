using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Hệ thống quản lý âm thanh trung tâm, mô phỏng 100% logic từ bản Three.js.
/// Tích hợp 3 tầng chống tiếng rụt rụt (Anti-click/pop):
/// 1. Fade mượt mà bằng Coroutine (thay thế Web Audio API linearRamp)
/// 2. Delayed Stop (chờ volume chạm 0 rồi mới tắt nguồn)
/// 3. PlayOneShot cho các âm thanh one-shot (thay thế BufferSource)
/// </summary>
public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance;

    [Header("=== NHẠC NỀN ===")]
    public AudioClip bgmClip;           // rune_factory_3_ancient_bone.wav
    [Range(0f, 1f)]
    public float bgmVolume = 0.15f;     // Volume 15% giống Three.js

    [Header("=== CHIÊU THỨC SASUKE ===")]
    public AudioClip katonClip;          // katon_goukakyou.wav
    public AudioClip chidoriClip;        // chidori.wav
    public AudioClip susanooActClip;     // susanoo_activation.wav
    public AudioClip susanooFlyClip;     // susanoo_fly.wav (loop)
    public AudioClip susanooSlashClip;   // susanoo_slash.wav
    public AudioClip susanooEndClip;     // susanoo_end.wav

    [Header("=== CHƯỚNG NGẠI VẬT ===")]
    public AudioClip shurikenClip;       // shuriken_extend.wav

    [Header("=== NARUTO BOSS ===")]
    public AudioClip kagebunshinStartClip;  // kage_bunshin_start.wav
    public AudioClip kagebunshinAppearClip; // kage_bunshin_appear.wav
    public AudioClip kagebunshinFadeClip;   // kage_bunshin_fade.wav

    [Header("=== CÀI ĐẶT ===")]
    [Range(0f, 2f)]
    public float susanooSlashVolume = 2.0f; // Khuếch đại x2 giống Three.js
    [Range(0f, 1f)]
    public float susanooFlyMaxVolume = 0.8f;
    [Range(0f, 1f)]
    public float susanooFlyDuckVolume = 0.1f;

    // --- AudioSources ---
    private AudioSource bgmSource;           // Nhạc nền (loop)
    private AudioSource susanooFlySource;     // Tiếng bay Susanoo (loop, cần duck)
    private AudioSource sfxSource;            // SFX one-shot chung (katon, chidori, susanoo_act, susanoo_end)

    // --- Fade Coroutines tracking ---
    private Coroutine bgmFadeCoroutine;
    private Coroutine flyFadeCoroutine;
    private Coroutine narutoStartFadeCoroutine;
    private Coroutine narutoAppearFadeCoroutine;
    private Coroutine katonFadeCoroutine;

    private AudioSource narutoStartSource;
    private AudioSource narutoAppearSource;
    private AudioSource katonSource;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else { Destroy(gameObject); return; }

        // Tạo các AudioSource
        bgmSource = gameObject.AddComponent<AudioSource>();
        bgmSource.playOnAwake = false;
        bgmSource.loop = true;
        bgmSource.volume = 0f;
        bgmSource.clip = bgmClip;

        susanooFlySource = gameObject.AddComponent<AudioSource>();
        susanooFlySource.playOnAwake = false;
        susanooFlySource.loop = true;
        susanooFlySource.volume = 0f;
        susanooFlySource.clip = susanooFlyClip;

        sfxSource = gameObject.AddComponent<AudioSource>();
        sfxSource.playOnAwake = false;
        sfxSource.loop = false;

        narutoStartSource = gameObject.AddComponent<AudioSource>();
        narutoStartSource.playOnAwake = false;
        narutoStartSource.loop = false;

        narutoAppearSource = gameObject.AddComponent<AudioSource>();
        narutoAppearSource.playOnAwake = false;
        narutoAppearSource.loop = false;

        katonSource = gameObject.AddComponent<AudioSource>();
        katonSource.playOnAwake = false;
        katonSource.loop = false;
    }

    void Start()
    {
        // Tự động phát nhạc nền khi game khởi động vì Scene không dùng GameManager
        PlayBGM();
    }

    // ============================================================
    //                     NHẠC NỀN (BGM)
    // ============================================================

    /// <summary>Phát nhạc nền khi game bắt đầu</summary>
    public void PlayBGM()
    {
        if (bgmClip == null) return;
        bgmSource.clip = bgmClip;
        bgmSource.volume = bgmVolume;
        bgmSource.Play();
        Debug.Log("🎵 BGM: Bắt đầu phát nhạc nền");
    }

    /// <summary>Dừng nhạc nền mượt mà (Game Over)</summary>
    public void StopBGM()
    {
        if (bgmSource.isPlaying)
        {
            FadeOut(bgmSource, ref bgmFadeCoroutine, 300f);
        }
    }

    /// <summary>Tiếp tục nhạc nền (Retry)</summary>
    public void ResumeBGM()
    {
        if (bgmClip == null) return;
        bgmSource.clip = bgmClip;
        bgmSource.volume = bgmVolume;
        bgmSource.Play();
    }

    /// <summary>Hiệu ứng Time Dilation cho BGM (Susanoo Cut-in)</summary>
    public void SetBGMSlowmo(bool slow)
    {
        if (slow)
        {
            bgmSource.pitch = 0.3f;
        }
        else
        {
            bgmSource.pitch = 1.0f;
        }
    }

    // ============================================================
    //                  CHIÊU THỨC SASUKE (SFX)
    // ============================================================

    /// <summary>Phát tiếng hét Katon Goukakyou</summary>
    public void PlayKaton()
    {
        if (katonClip == null) return;
        if (katonFadeCoroutine != null) StopCoroutine(katonFadeCoroutine);
        katonSource.clip = katonClip;
        katonSource.volume = 1.0f;
        katonSource.Play();
        Debug.Log("🔥 Audio: Katon Goukakyou!");
    }

    /// <summary>Ngắt tiếng Katon từ từ như lửa tàn (fade 1000ms)</summary>
    public void StopKaton()
    {
        if (katonSource != null && katonSource.isPlaying)
        {
            FadeOut(katonSource, ref katonFadeCoroutine, 1000f);
        }
    }

    /// <summary>Phát tiếng chim Chidori</summary>
    public void PlayChidori()
    {
        if (chidoriClip == null) return;
        sfxSource.PlayOneShot(chidoriClip, 1.0f);
        Debug.Log("⚡ Audio: Chidori!");
    }

    /// <summary>Phát tiếng bùng nổ kích hoạt Susanoo (Cut-in)</summary>
    public void PlaySusanooActivation()
    {
        if (susanooActClip == null) return;
        sfxSource.PlayOneShot(susanooActClip, 1.0f);
        Debug.Log("🟣 Audio: Susanoo Activation!");
    }

    /// <summary>Phát tiếng chém kiếm Susanoo (đè lên nhau, không bị cắt)</summary>
    public void PlaySusanooSlash()
    {
        if (susanooSlashClip == null) return;
        // PlayOneShot cho phép phát đè nhiều instance cùng lúc (giống BufferSource trong Three.js)
        sfxSource.PlayOneShot(susanooSlashClip, susanooSlashVolume);
        Debug.Log("⚔️ Audio: Susanoo Slash!");
    }

    /// <summary>Phát tiếng tan biến khi Susanoo hết hạn</summary>
    public void PlaySusanooEnd()
    {
        if (susanooEndClip == null) return;
        sfxSource.PlayOneShot(susanooEndClip, 1.0f);
        Debug.Log("🟣 Audio: Susanoo End!");
    }

    // ============================================================
    //              TIẾNG BAY SUSANOO (Loop + Duck)
    // ============================================================

    /// <summary>Bắt đầu phát tiếng bay Susanoo (fade-in từ 0 → 0.8)</summary>
    public void StartSusanooFly()
    {
        if (susanooFlyClip == null) return;
        susanooFlySource.clip = susanooFlyClip;
        susanooFlySource.volume = 0f;
        susanooFlySource.Play();
        FadeTo(susanooFlySource, ref flyFadeCoroutine, susanooFlyMaxVolume, 100f);
        Debug.Log("🌪️ Audio: Susanoo Fly bắt đầu (fade-in)");
    }

    /// <summary>Duck tiếng bay xuống 10% khi Susanoo chém kiếm (nhường chỗ cho tiếng chém)</summary>
    public void DuckSusanooFly()
    {
        FadeTo(susanooFlySource, ref flyFadeCoroutine, susanooFlyDuckVolume, 100f);
    }

    /// <summary>Kéo tiếng bay trở lại 80% sau khi chém xong</summary>
    public void UnduckSusanooFly()
    {
        FadeTo(susanooFlySource, ref flyFadeCoroutine, susanooFlyMaxVolume, 150f);
    }

    /// <summary>Tắt tiếng bay mượt mà (fade-out → stop)</summary>
    public void StopSusanooFly()
    {
        if (susanooFlySource.isPlaying)
        {
            FadeOut(susanooFlySource, ref flyFadeCoroutine, 150f);
            Debug.Log("🌪️ Audio: Susanoo Fly dừng (fade-out)");
        }
    }

    // ============================================================
    //                    NARUTO BOSS
    // ============================================================

    /// <summary>Phát tiếng hét khi Naruto múa ấn Kagebunshin</summary>
    public void PlayKagebunshinStart()
    {
        if (kagebunshinStartClip == null) return;
        if (narutoStartFadeCoroutine != null) StopCoroutine(narutoStartFadeCoroutine);
        narutoStartSource.clip = kagebunshinStartClip;
        narutoStartSource.volume = 1.0f;
        narutoStartSource.Play();
        Debug.Log("🍥 Audio: Kagebunshin no Jutsu!");
    }

    /// <summary>Phát tiếng "bụp" khi phân thân xuất hiện</summary>
    public void PlayKagebunshinAppear()
    {
        if (kagebunshinAppearClip == null) return;
        if (narutoAppearFadeCoroutine != null) StopCoroutine(narutoAppearFadeCoroutine);
        narutoAppearSource.clip = kagebunshinAppearClip;
        narutoAppearSource.volume = 1.0f;
        narutoAppearSource.Play();
    }

    /// <summary>Ngắt tiếng Kagebunshin Start và Appear mượt mà</summary>
    public void StopNarutoAudios()
    {
        if (narutoStartSource != null && narutoStartSource.isPlaying)
        {
            FadeOut(narutoStartSource, ref narutoStartFadeCoroutine, 150f);
        }
        if (narutoAppearSource != null && narutoAppearSource.isPlaying)
        {
            FadeOut(narutoAppearSource, ref narutoAppearFadeCoroutine, 150f);
        }
    }

    /// <summary>Phát tiếng bốc hơi khi phân thân biến mất</summary>
    public void PlayKagebunshinFade()
    {
        if (kagebunshinFadeClip == null) return;
        sfxSource.PlayOneShot(kagebunshinFadeClip, 1.0f);
    }

    // ============================================================
    //               CHƯỚNG NGẠI VẬT - PHI TIÊU
    // ============================================================

    /// <summary>
    /// Gắn AudioSource lên phi tiêu để phát tiếng xoay theo khoảng cách.
    /// Gọi hàm này từ ShurikenObstacle.Start() hoặc LevelSpawner.
    /// </summary>
    public AudioSource AttachShurikenAudio(GameObject shurikenObj)
    {
        if (shurikenClip == null) return null;
        AudioSource src = shurikenObj.AddComponent<AudioSource>();
        src.clip = shurikenClip;
        src.loop = true;
        src.volume = 0f;
        src.spatialBlend = 0f; // 2D audio (giống Three.js)
        src.playOnAwake = false;
        return src;
    }

    /// <summary>
    /// Cập nhật volume phi tiêu theo khoảng cách tới camera (giống Three.js).
    /// Gọi mỗi frame từ ShurikenObstacle.Update().
    /// </summary>
    public void UpdateShurikenVolume(AudioSource src, float distanceZ)
    {
        if (src == null) return;
        float absDistance = Mathf.Abs(distanceZ);
        float maxRange = 70f;

        if (absDistance > maxRange)
        {
            if (src.isPlaying)
            {
                src.volume = 0f;
                src.Stop();
            }
            return;
        }

        float vol = (1.0f - absDistance / maxRange) * 0.8f;
        vol = Mathf.Clamp01(vol);

        if (!src.isPlaying && vol > 0.01f)
        {
            src.Play();
        }
        src.volume = vol;
    }

    // ============================================================
    //         CHỐNG TIẾNG RỤT RỤT (Anti-click/pop Engine)
    // ============================================================

    /// <summary>Fade volume đến mức target trong durationMs mili-giây</summary>
    private void FadeTo(AudioSource source, ref Coroutine tracker, float targetVol, float durationMs)
    {
        if (source == null) return;
        if (tracker != null) StopCoroutine(tracker);
        tracker = StartCoroutine(FadeCoroutine(source, targetVol, durationMs / 1000f, false));
    }

    /// <summary>Fade-out về 0 rồi Stop (có delay 50ms buffer giống Three.js)</summary>
    private void FadeOut(AudioSource source, ref Coroutine tracker, float durationMs)
    {
        if (source == null) return;
        if (tracker != null) StopCoroutine(tracker);
        tracker = StartCoroutine(FadeCoroutine(source, 0f, durationMs / 1000f, true));
    }

    /// <summary>
    /// Coroutine fade volume mượt mà frame-by-frame.
    /// stopAfter = true: Sau khi fade xong + 50ms buffer sẽ gọi Stop() (giống Three.js Delayed Stop)
    /// </summary>
    private IEnumerator FadeCoroutine(AudioSource source, float targetVol, float duration, bool stopAfter)
    {
        if (source == null) yield break;
        
        float startVol = source.volume;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime; // Dùng unscaledDeltaTime để fade vẫn hoạt động khi TimeScale = 0
            float t = Mathf.Clamp01(elapsed / duration);
            source.volume = Mathf.Lerp(startVol, targetVol, t);
            yield return null;
        }

        source.volume = targetVol;

        if (stopAfter)
        {
            // Tầng 2: Delayed Stop — Chờ thêm 50ms buffer rồi mới tắt nguồn
            yield return new WaitForSecondsRealtime(0.05f);
            source.Stop();
            source.volume = 0f;
        }
    }
}
