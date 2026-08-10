using System;
using System.Collections;
using UnityEngine;

/// <summary>
/// Sistem pusat (Singleton) untuk mengelola semua Audio global: BGM (Musik Latar) dan UI SFX (Suara klik, hover, dll).
/// 
/// Cara Pakai dari script lain:
///   AudioManager.Instance.PlayBGM("NamaLagu");
///   AudioManager.Instance.PlaySFX("NamaSuara");
///   AudioManager.Instance.StopBGM();
///
/// Fitur:
///   - Auto-spawn dari Resources jika belum ada di scene (bisa ngetest dari scene manapun!)
///   - BGM dengan crossfade otomatis saat ganti lagu
///   - Master Volume control untuk BGM dan SFX terpisah
///   - DontDestroyOnLoad (tetap hidup saat pindah scene)
/// </summary>
public class AudioManager : MonoBehaviour
{
    // ========================
    // SINGLETON
    // ========================
    private static AudioManager _instance;

    /// <summary>
    /// Akses Singleton. Jika belum ada di scene, akan otomatis di-spawn dari Resources/AudioManager.
    /// Dengan begini, Mas Nauval bisa ngetest dari scene manapun tanpa harus Play dari Main Menu!
    /// </summary>
    public static AudioManager Instance
    {
        get
        {
            if (_instance == null)
            {
                // Cari dulu di scene, mungkin sudah ada
                _instance = FindAnyObjectByType<AudioManager>();

                // Kalau tetap belum ada, spawn otomatis dari folder Resources
                if (_instance == null)
                {
                    GameObject prefab = Resources.Load<GameObject>("AudioManager");
                    if (prefab != null)
                    {
                        GameObject obj = Instantiate(prefab);
                        obj.name = "AudioManager (Auto-Spawned)";
                        _instance = obj.GetComponent<AudioManager>();
                    }
                    else
                    {
                        // Kalau prefab-nya juga belum dibuat, buat GameObject kosong sebagai fallback
                        Debug.LogWarning("AudioManager: Prefab 'AudioManager' tidak ditemukan di folder Resources! " +
                                         "Membuat AudioManager kosong sebagai fallback.");
                        GameObject obj = new GameObject("AudioManager (Fallback)");
                        _instance = obj.AddComponent<AudioManager>();
                    }
                }
            }
            return _instance;
        }
    }

    // ========================
    // INSPECTOR SETTINGS
    // ========================

    [Header("=== Daftar BGM (Background Music) ===")]
    [Tooltip("Daftar semua lagu latar. Isi Nama dan Clip di Inspector, sisanya otomatis.")]
    [SerializeField] private Sound[] bgmSounds;

    [Header("=== Daftar SFX UI (Suara Klik, Hover, dll) ===")]
    [Tooltip("Daftar semua efek suara untuk UI. Isi Nama dan Clip di Inspector.")]
    [SerializeField] private Sound[] sfxSounds;

    [Header("=== Volume Master ===")]
    [Range(0f, 1f)]
    [Tooltip("Volume master untuk semua BGM (bisa diubah dari Settings menu nanti)")]
    [SerializeField] private float bgmMasterVolume = 1f;

    [Range(0f, 1f)]
    [Tooltip("Volume master untuk semua SFX (bisa diubah dari Settings menu nanti)")]
    [SerializeField] private float sfxMasterVolume = 1f;

    [Header("=== Crossfade Settings ===")]
    [Tooltip("Durasi transisi (fade out lagu lama -> fade in lagu baru) dalam detik")]
    [SerializeField] private float crossfadeDuration = 1.5f;

    // ========================
    // PRIVATE STATE
    // ========================
    private Sound currentBGM;
    private Coroutine crossfadeCoroutine;

    // ========================
    // LIFECYCLE
    // ========================

    private void Awake()
    {
        // Singleton guard: Jika sudah ada instance lain, hancurkan duplikatnya
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }

        _instance = this;
        DontDestroyOnLoad(gameObject);

        // Inisialisasi: Buat AudioSource untuk setiap Sound di daftar BGM dan SFX
        InitializeSounds(bgmSounds);
        InitializeSounds(sfxSounds);
    }

    /// <summary>
    /// Membuat komponen AudioSource untuk setiap entry Sound dan mengatur propertinya sesuai data di Inspector.
    /// </summary>
    private void InitializeSounds(Sound[] sounds)
    {
        if (sounds == null) return;

        foreach (Sound s in sounds)
        {
            if (s.clip == null)
            {
                Debug.LogWarning($"AudioManager: Sound '{s.name}' tidak punya AudioClip! Silakan assign di Inspector.");
                continue;
            }

            s.source = gameObject.AddComponent<AudioSource>();
            s.source.clip = s.clip;
            s.source.volume = s.volume;
            s.source.pitch = s.pitch;
            s.source.loop = s.loop;
            s.source.playOnAwake = false;

            // Suara global (2D), tidak terpengaruh jarak/posisi
            s.source.spatialBlend = 0f;
        }
    }

    // ========================
    // PUBLIC API — BGM
    // ========================

    /// <summary>
    /// Memainkan BGM berdasarkan nama. Jika BGM lain sedang bermain, akan dilakukan crossfade otomatis.
    /// Contoh: AudioManager.Instance.PlayBGM("HutanBGM");
    /// </summary>
    /// <param name="name">Nama BGM yang sudah didaftarkan di Inspector</param>
    public void PlayBGM(string name)
    {
        Sound s = FindSound(bgmSounds, name);
        if (s == null)
        {
            Debug.LogWarning($"AudioManager: BGM dengan nama '{name}' tidak ditemukan! Cek penulisan di Inspector.");
            return;
        }

        // Jika lagu yang sama sudah sedang bermain, tidak perlu diulang
        if (currentBGM != null && currentBGM.name == name && currentBGM.source.isPlaying)
        {
            return;
        }

        // Hentikan crossfade yang sedang berjalan (jika ada)
        if (crossfadeCoroutine != null)
        {
            StopCoroutine(crossfadeCoroutine);
        }

        crossfadeCoroutine = StartCoroutine(CrossfadeBGM(s));
    }

    /// <summary>
    /// Menghentikan BGM yang sedang bermain dengan efek fade out.
    /// </summary>
    public void StopBGM()
    {
        if (currentBGM != null && currentBGM.source.isPlaying)
        {
            if (crossfadeCoroutine != null)
            {
                StopCoroutine(crossfadeCoroutine);
            }
            crossfadeCoroutine = StartCoroutine(FadeOutSound(currentBGM, crossfadeDuration, () =>
            {
                currentBGM = null;
            }));
        }
    }

    /// <summary>
    /// Pause BGM saat ini (bisa di-resume nanti).
    /// </summary>
    public void PauseBGM()
    {
        if (currentBGM != null && currentBGM.source.isPlaying)
        {
            currentBGM.source.Pause();
        }
    }

    /// <summary>
    /// Resume BGM yang sedang di-pause.
    /// </summary>
    public void ResumeBGM()
    {
        if (currentBGM != null && !currentBGM.source.isPlaying)
        {
            currentBGM.source.UnPause();
        }
    }

    // ========================
    // PUBLIC API — SFX
    // ========================

    /// <summary>
    /// Memainkan SFX (efek suara) berdasarkan nama. Cocok untuk suara UI (klik, hover, notif).
    /// Contoh: AudioManager.Instance.PlaySFX("ButtonClick");
    /// </summary>
    /// <param name="name">Nama SFX yang sudah didaftarkan di Inspector</param>
    public void PlaySFX(string name)
    {
        Sound s = FindSound(sfxSounds, name);
        if (s == null)
        {
            Debug.LogWarning($"AudioManager: SFX dengan nama '{name}' tidak ditemukan! Cek penulisan di Inspector.");
            return;
        }

        // PlayOneShot agar bisa overlap (misal: klik berkali-kali cepat)
        s.source.PlayOneShot(s.clip, s.volume * sfxMasterVolume);
    }

    // ========================
    // PUBLIC API — VOLUME SETTINGS (untuk menu Settings nanti)
    // ========================

    /// <summary>
    /// Set master volume untuk BGM (0 = mute, 1 = penuh). Cocok untuk slider di menu Settings.
    /// </summary>
    public void SetBGMVolume(float volume)
    {
        bgmMasterVolume = Mathf.Clamp01(volume);

        // Update volume BGM yang sedang bermain secara realtime
        if (currentBGM != null && currentBGM.source != null)
        {
            currentBGM.source.volume = currentBGM.volume * bgmMasterVolume;
        }
    }

    /// <summary>
    /// Set master volume untuk SFX (0 = mute, 1 = penuh). Cocok untuk slider di menu Settings.
    /// </summary>
    public void SetSFXVolume(float volume)
    {
        sfxMasterVolume = Mathf.Clamp01(volume);
    }

    /// <summary>
    /// Mendapatkan nilai master volume BGM saat ini (untuk menampilkan posisi slider).
    /// </summary>
    public float GetBGMVolume() => bgmMasterVolume;

    /// <summary>
    /// Mendapatkan nilai master volume SFX saat ini (untuk menampilkan posisi slider).
    /// </summary>
    public float GetSFXVolume() => sfxMasterVolume;

    // ========================
    // INTERNAL — CROSSFADE & HELPERS
    // ========================

    /// <summary>
    /// Coroutine untuk melakukan crossfade: fade out lagu lama sambil fade in lagu baru secara bersamaan.
    /// </summary>
    private IEnumerator CrossfadeBGM(Sound newBGM)
    {
        float timer = 0f;
        float targetVolume = newBGM.volume * bgmMasterVolume;

        // Siapkan lagu baru (mulai dari volume 0)
        newBGM.source.volume = 0f;
        newBGM.source.Play();

        Sound oldBGM = currentBGM;
        currentBGM = newBGM;

        while (timer < crossfadeDuration)
        {
            timer += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(timer / crossfadeDuration);

            // Fade in lagu baru
            newBGM.source.volume = Mathf.Lerp(0f, targetVolume, t);

            // Fade out lagu lama (jika ada)
            if (oldBGM != null && oldBGM.source != null)
            {
                oldBGM.source.volume = Mathf.Lerp(oldBGM.volume * bgmMasterVolume, 0f, t);
            }

            yield return null;
        }

        // Pastikan nilai final tepat
        newBGM.source.volume = targetVolume;

        if (oldBGM != null && oldBGM.source != null)
        {
            oldBGM.source.volume = 0f;
            oldBGM.source.Stop();
        }

        crossfadeCoroutine = null;
    }

    /// <summary>
    /// Coroutine untuk fade out sebuah Sound secara perlahan, lalu menjalankan callback.
    /// </summary>
    private IEnumerator FadeOutSound(Sound sound, float duration, Action onComplete = null)
    {
        if (sound == null || sound.source == null)
        {
            onComplete?.Invoke();
            yield break;
        }

        float startVolume = sound.source.volume;
        float timer = 0f;

        while (timer < duration)
        {
            timer += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(timer / duration);
            sound.source.volume = Mathf.Lerp(startVolume, 0f, t);
            yield return null;
        }

        sound.source.volume = 0f;
        sound.source.Stop();

        onComplete?.Invoke();
        crossfadeCoroutine = null;
    }

    /// <summary>
    /// Mencari Sound dari array berdasarkan nama.
    /// </summary>
    private Sound FindSound(Sound[] sounds, string name)
    {
        if (sounds == null) return null;

        foreach (Sound s in sounds)
        {
            if (s.name == name) return s;
        }
        return null;
    }
}
