using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Sistem utama dan terpusat (Scalable) untuk memanajemen perpindahan Scene.
/// Bisa dipanggil dari Trigger, UI Button, InteractObject (lewat UnityEvents OnInteract), maupun script Quest.
/// </summary>
public class GameSceneManager : MonoBehaviour
{
    // Singleton agar script lain bisa mengakses fungsi ini dengan mudah (Contoh: GameSceneManager.Instance.ChangeScene(...))
    public static GameSceneManager Instance { get; private set; }

    [Header("Dependencies")]
    [Tooltip("Referensi ke SceneFader UI. Jika dipasang, pindah scene akan disertai efek layar gelap (fade out) yang halus.")]
    public SceneFader sceneFader;

    private void Awake()
    {
        // Inisialisasi Singleton
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// Pindah ke scene baru. (Akan otomatis menggunakan FadeOut jika SceneFader terpasang di Inspector).
    /// Fungsi ini sangat cocok dipasangkan pada UnityEvents (seperti OnClick di Button, atau OnInteract di InteractObject).
    /// </summary>
    /// <param name="sceneName">Nama scene tujuan (Pastikan scene sudah ada di Build Settings)</param>
    public void ChangeScene(string sceneName)
    {
        if (string.IsNullOrEmpty(sceneName))
        {
            Debug.LogWarning("GameSceneManager: Perintah pindah scene digagalkan karena nama scene kosong!");
            return;
        }

        if (sceneFader != null)
        {
            // Pindah dengan transisi halus
            sceneFader.LoadScene(sceneName);
        }
        else
        {
            // Jika lupa pasang SceneFader, tetap pindah scene secara instan sebagai backup
            SceneManager.LoadScene(sceneName);
        }
    }

    /// <summary>
    /// Memaksa pindah scene secara instan (tanpa efek fade) meskipun ada SceneFader.
    /// Biasanya digunakan untuk perpindahan mendesak atau fast travel instant.
    /// </summary>
    public void ChangeSceneInstant(string sceneName)
    {
        if (!string.IsNullOrEmpty(sceneName))
        {
            SceneManager.LoadScene(sceneName);
        }
    }

    /// <summary>
    /// Memuat ulang (Restart) scene yang sedang aktif saat ini.
    /// Sangat cocok digunakan saat kondisi Game Over atau pemain mati.
    /// </summary>
    public void ReloadCurrentScene()
    {
        string currentScene = SceneManager.GetActiveScene().name;
        ChangeScene(currentScene);
    }
}
