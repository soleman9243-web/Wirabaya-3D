using UnityEngine;

/// <summary>
/// Trigger area untuk mengganti BGM secara otomatis dengan crossfade halus saat Player masuk ke zona tertentu.
/// Pasang di GameObject yang memiliki Collider (Is Trigger = true).
/// </summary>
public class BGMZoneTrigger : MonoBehaviour
{
    [Header("=== Pengaturan BGM Zona ===")]
    [Tooltip("Nama BGM di AudioManager yang akan diputar saat Player masuk area ini (contoh: BGM_1, BGM_2, BGM_3).")]
    [SerializeField] private string bgmName = "BGM_2";

    [Tooltip("Tag objek yang memicu pergantian BGM (biasanya 'Player').")]
    [SerializeField] private string targetTag = "Player";

    private void OnTriggerEnter(Collider other)
    {
        if (string.IsNullOrEmpty(bgmName)) return;
        if (!string.IsNullOrEmpty(targetTag) && !other.CompareTag(targetTag)) return;

        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayBGM(bgmName);
        }
    }
}
