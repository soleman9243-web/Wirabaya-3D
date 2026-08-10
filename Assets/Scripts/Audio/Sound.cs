using UnityEngine;

/// <summary>
/// Class data untuk menyimpan informasi sebuah suara.
/// Akan muncul di Inspector sebagai daftar di AudioManager.
/// </summary>
[System.Serializable]
public class Sound
{
    [Tooltip("Nama unik untuk memanggil suara ini dari script (contoh: 'MainMenuBGM', 'ButtonClick')")]
    public string name;

    [Tooltip("File AudioClip (.wav / .mp3) yang akan dimainkan")]
    public AudioClip clip;

    [Range(0f, 1f)]
    [Tooltip("Volume suara (0 = mute, 1 = penuh)")]
    public float volume = 1f;

    [Range(0.5f, 2f)]
    [Tooltip("Pitch suara (1 = normal, <1 = lambat/rendah, >1 = cepat/tinggi)")]
    public float pitch = 1f;

    [Tooltip("Apakah suara ini di-loop terus-menerus? (Biasanya true untuk BGM)")]
    public bool loop = false;

    /// <summary>
    /// AudioSource yang akan di-generate otomatis oleh AudioManager saat Awake().
    /// Tidak perlu diisi manual di Inspector.
    /// </summary>
    [HideInInspector]
    public AudioSource source;
}
