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

    // Kita tidak butuh variabel SceneFader lagi, karena sudah pakai ScreenFader.Instance yang serba otomatis!

    // Flag untuk mencegah double-loading saat transisi sedang berjalan
    private bool isTransitioning = false;

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

        if (isTransitioning)
        {
            Debug.LogWarning("GameSceneManager: Transisi scene sedang berjalan, abaikan request baru.");
            return;
        }

        if (ScreenFader.Instance != null)
        {
            // Menggunakan ScreenFader baru untuk transisi
            StartCoroutine(ChangeSceneWithFade(sceneName));
        }
        else
        {
            // Jika tidak ada fader, langsung async load
            StartCoroutine(LoadSceneAsyncDirect(sceneName));
        }
    }

    private System.Collections.IEnumerator ChangeSceneWithFade(string sceneName)
    {
        isTransitioning = true;

        // 1. Tunggu animasi FadeOut selesai (layar jadi GELAP)
        yield return StartCoroutine(ScreenFader.Instance.FadeOut());

        // 2. Mulai async loading SAAT LAYAR SUDAH GELAP — player nggak sadar loading!
        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(sceneName);
        asyncLoad.allowSceneActivation = false;

        // 3. Tunggu sampai loading hampir selesai (progress 0.9 = siap diaktifkan)
        while (asyncLoad.progress < 0.9f)
        {
            yield return null;
        }

        // 4. Aktifkan scene baru
        asyncLoad.allowSceneActivation = true;

        // isTransitioning akan di-reset karena GameObject ini akan di-destroy saat scene baru dimuat
    }

    private System.Collections.IEnumerator LoadSceneAsyncDirect(string sceneName)
    {
        isTransitioning = true;

        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(sceneName);

        while (!asyncLoad.isDone)
        {
            yield return null;
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
            // Tetap async agar tidak freeze, tapi tanpa fade
            StartCoroutine(LoadSceneAsyncDirect(sceneName));
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
