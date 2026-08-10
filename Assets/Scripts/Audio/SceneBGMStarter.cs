using UnityEngine;

/// <summary>
/// Komponen kecil yang ditaruh di setiap Scene untuk menentukan BGM apa yang akan dimainkan di scene tersebut.
/// 
/// Cara Pakai:
///   1. Buat GameObject kosong di scene (misal: "SceneAudio")
///   2. Tempelkan script ini
///   3. Di Inspector, ketik nama BGM yang sesuai (harus sama dengan nama di AudioManager)
///   4. Selesai! BGM akan otomatis berubah saat scene dimuat.
///
/// Contoh:
///   Scene "Hutan"   -> bgmName = "HutanBGM"
///   Scene "MainMenu" -> bgmName = "MainMenuBGM"
///   Scene "Boss"     -> bgmName = "BossBGM"
/// </summary>
public class SceneBGMStarter : MonoBehaviour
{
    [Header("=== BGM untuk Scene ini ===")]
    [Tooltip("Nama BGM yang akan dimainkan (harus sama persis dengan nama di daftar BGM pada AudioManager)")]
    [SerializeField] private string bgmName;

    [Tooltip("Jika true, BGM akan langsung berhenti saat scene ini di-unload (misalnya untuk scene cutscene tanpa musik)")]
    [SerializeField] private bool stopBGMOnDestroy = false;

    private void Start()
    {
        if (!string.IsNullOrEmpty(bgmName))
        {
            AudioManager.Instance.PlayBGM(bgmName);
        }
    }

    private void OnDestroy()
    {
        // Opsional: hentikan BGM saat scene dihancurkan
        if (stopBGMOnDestroy && AudioManager.Instance != null)
        {
            AudioManager.Instance.StopBGM();
        }
    }
}
