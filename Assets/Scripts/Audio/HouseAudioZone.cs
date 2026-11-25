using UnityEngine;

/// <summary>
/// Zona Trigger untuk area dalam rumah (indoor).
/// Saat Player masuk ke dalam rumah:
/// 1. Mematikan (fade out) BGM tertentu yang didaftarkan di Inspector (misal BGM_2, BGM_3 / ambience hutan).
/// 2. Memaksa suara langkah kaki (SFX step) menggunakan HouseFootstepAudioClips dan menghentikan suara rumput.
/// Saat Player keluar dari rumah, BGM tersebut otomatis menyala (fade in) kembali.
/// </summary>
[RequireComponent(typeof(Collider))]
public class HouseAudioZone : MonoBehaviour
{
    private static int _activeHouseCount = 0;

    /// <summary>
    /// Bernilai true jika Player sedang berada di dalam trigger zona rumah manapun.
    /// </summary>
    public static bool IsPlayerInside => _activeHouseCount > 0;

    [Header("=== BGM yang Dimatikan Saat Masuk Rumah ===")]
    [Tooltip("Daftar nama BGM di AudioManager yang akan dimatikan/fade-out saat masuk rumah.")]
    [SerializeField] private string[] bgmNamesToMute = new string[] { "BGM_2", "BGM_3" };

    [Tooltip("Durasi fade out / fade in saat masuk dan keluar rumah (detik).")]
    [SerializeField] private float fadeDuration = 1.0f;

    [Header("=== Footstep Rumah ===")]
    [Tooltip("Jika aktif, suara langkah kaki otomatis berubah menjadi HouseFootstepAudioClips selama di dalam zona ini.")]
    [SerializeField] private bool forceHouseFootsteps = true;

    private bool _playerInsideThisZone = false;

    private void Reset()
    {
        Collider col = GetComponent<Collider>();
        if (col != null) col.isTrigger = true;
    }

    private void Awake()
    {
        Collider col = GetComponent<Collider>();
        if (col != null && !col.isTrigger)
        {
            col.isTrigger = true;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!IsPlayerCollider(other) || _playerInsideThisZone) return;

        _playerInsideThisZone = true;
        if (forceHouseFootsteps)
        {
            _activeHouseCount++;
        }

        SetZoneBGMMuted(true);
    }

    private void OnTriggerExit(Collider other)
    {
        if (!IsPlayerCollider(other) || !_playerInsideThisZone) return;

        _playerInsideThisZone = false;
        if (forceHouseFootsteps)
        {
            _activeHouseCount = Mathf.Max(0, _activeHouseCount - 1);
        }

        SetZoneBGMMuted(false);
    }

    private void OnDisable()
    {
        if (_playerInsideThisZone)
        {
            _playerInsideThisZone = false;
            if (forceHouseFootsteps)
            {
                _activeHouseCount = Mathf.Max(0, _activeHouseCount - 1);
            }
            SetZoneBGMMuted(false);
        }
    }

    private void SetZoneBGMMuted(bool muted)
    {
        if (AudioManager.Instance == null || bgmNamesToMute == null) return;

        for (int i = 0; i < bgmNamesToMute.Length; i++)
        {
            string bgmName = bgmNamesToMute[i];
            if (!string.IsNullOrEmpty(bgmName))
            {
                AudioManager.Instance.SetBGMMuted(bgmName, muted, fadeDuration);
            }
        }
    }

    private static bool IsPlayerCollider(Collider other)
    {
        if (other == null) return false;
        if (other.CompareTag("Player")) return true;
        return other.GetComponentInParent<StarterAssets.ThirdPersonController>() != null;
    }
}
