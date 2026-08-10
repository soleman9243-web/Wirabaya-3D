using UnityEngine;
using System.Collections.Generic;

[System.Serializable]
public class SoundGroup
{
    [Tooltip("Nama grup (misal: 'Step', 'Slash', 'Chop')")]
    public string groupName;
    [Tooltip("Daftar variasi suara untuk grup ini")]
    public AudioClip[] clips;
}

/// <summary>
/// Komponen untuk membunyikan suara melalui Animation Event.
/// Mendukung Grup Suara agar bisa memutar variasi suara secara acak berdasarkan kategorinya.
/// </summary>
[RequireComponent(typeof(AudioSource))]
public class AnimationSoundPlayer : MonoBehaviour
{
    [Header("=== Daftar Grup Suara ===")]
    [Tooltip("Buat grup untuk mengelompokkan suara (misal: grup 'Step' isi 3 suara langkah, grup 'Slash' isi 2 suara tebasan)")]
    [SerializeField] private SoundGroup[] soundGroups;

    [Header("=== Pengaturan ===")]
    [Range(0f, 1f)]
    [SerializeField] private float volume = 1f;

    [Tooltip("Apakah pitch akan sedikit diacak setiap kali suara dimainkan?")]
    [SerializeField] private bool randomizePitch = true;

    [Range(0f, 0.3f)]
    [SerializeField] private float pitchVariation = 0.1f;

    private AudioSource audioSource;

    private void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        audioSource.playOnAwake = false;
        audioSource.loop = false;
    }

    // ========================
    // FUNGSI UNTUK ANIMATION EVENT
    // ========================

    /// <summary>
    /// Memainkan suara acak dari grup tertentu.
    /// Panggil dari Animation Event dengan parameter String (nama grup).
    /// Contoh: PlayRandomFromGroup("Step")
    /// </summary>
    public void PlayRandomFromGroup(string groupName)
    {
        if (soundGroups == null || soundGroups.Length == 0) return;

        foreach (var group in soundGroups)
        {
            if (group.groupName == groupName && group.clips != null && group.clips.Length > 0)
            {
                // Pilih suara acak dari dalam grup ini
                int randomIndex = Random.Range(0, group.clips.Length);
                AudioClip clipToPlay = group.clips[randomIndex];

                if (clipToPlay != null)
                {
                    PlayClip(clipToPlay);
                }
                return;
            }
        }
        
        Debug.LogWarning($"AnimationSoundPlayer: Grup dengan nama '{groupName}' tidak ditemukan atau kosong!");
    }

    /// <summary>
    /// Memainkan 1 suara spesifik berdasarkan nama clip-nya.
    /// Panggil dari Animation Event dengan parameter String (nama file audio).
    /// </summary>
    public void PlaySoundByName(string clipName)
    {
        if (soundGroups == null || soundGroups.Length == 0) return;

        foreach (var group in soundGroups)
        {
            if (group.clips == null) continue;
            
            foreach (var clip in group.clips)
            {
                if (clip != null && clip.name == clipName)
                {
                    PlayClip(clip);
                    return;
                }
            }
        }
    }

    // ========================
    // INTERNAL
    // ========================

    private void PlayClip(AudioClip clip)
    {
        if (randomizePitch)
        {
            audioSource.pitch = 1f + Random.Range(-pitchVariation, pitchVariation);
        }
        else
        {
            audioSource.pitch = 1f;
        }

        audioSource.PlayOneShot(clip, volume);
    }
}
