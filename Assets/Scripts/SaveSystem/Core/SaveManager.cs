using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Singleton utama untuk mengatur Save & Load.
/// Orchestrator yang mengumpulkan data dari ISaveDataProvider saat save,
/// dan mendistribusikan data saat load.
/// 
/// SETUP: Taruh sebagai ROOT GameObject di scene Loader (atau scene pertama).
/// SaveManager akan DontDestroyOnLoad dan hidup sepanjang game.
/// JANGAN taruh di dalam prefab PlayerManager/GameManager — harus root GameObject sendiri.
/// </summary>
public class SaveManager : MonoBehaviour
{
    public static SaveManager Instance { get; private set; }

    [Header("Settings")]
    [Tooltip("Master toggle: matikan untuk main tanpa save system (testing mode)")]
    public bool saveSystemEnabled = true;

    [Tooltip("Jumlah slot manual save yang tersedia")]
    public int maxManualSlots = 5;

    [Header("Auto Save UI (Opsional)")]
    [Tooltip("GameObject ikon auto-save yang akan muncul sesaat saat auto-save")]
    public GameObject autoSaveIcon;

    [Tooltip("Berapa lama ikon auto-save ditampilkan (detik)")]
    public float autoSaveIconDuration = 2f;

    // Tracking
    private float playTimeAccumulator = 0f;
    private SceneSavePoint currentSavePoint;
    private List<ISaveDataProvider> providers = new List<ISaveDataProvider>();

    // Flag agar restore hanya dilakukan sekali setelah scene load
    private SaveData pendingLoadData = null;

    // Event untuk UI subscribe
    public event Action<SaveData> OnSaveCompleted;
    public event Action<SaveData> OnLoadCompleted;

    private void Awake()
    {
        // Singleton — yang pertama bertahan, duplikat dihancurkan
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        if (autoSaveIcon != null)
            autoSaveIcon.SetActive(false);

        // Subscribe ke scene loaded event untuk restore data setelah scene baru dimuat
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        if (Instance == this) Instance = null;
    }

    private void Update()
    {
        playTimeAccumulator += Time.unscaledDeltaTime;
    }

    // ================================================================
    // PROVIDER MANAGEMENT
    // ================================================================

    /// <summary>
    /// Daftarkan ISaveDataProvider agar data-nya ikut di-save/load.
    /// Biasanya dipanggil di Awake() atau Start() provider.
    /// </summary>
    public void RegisterProvider(ISaveDataProvider provider)
    {
        if (!providers.Contains(provider))
        {
            providers.Add(provider);
            Debug.Log($"[SaveManager] Provider registered: {provider.GetType().Name}");
        }
    }

    /// <summary>
    /// Hapus provider dari daftar (biasanya saat provider di-destroy).
    /// </summary>
    public void UnregisterProvider(ISaveDataProvider provider)
    {
        providers.Remove(provider);
    }

    // ================================================================
    // SAVE POINT MANAGEMENT
    // ================================================================

    public void RegisterSavePoint(SceneSavePoint savePoint)
    {
        currentSavePoint = savePoint;
    }

    public void UnregisterSavePoint(SceneSavePoint savePoint)
    {
        if (currentSavePoint == savePoint)
            currentSavePoint = null;
    }

    /// <summary>
    /// Apakah manual save diizinkan saat ini?
    /// </summary>
    public bool CanManualSave()
    {
        if (!saveSystemEnabled) return false;
        return currentSavePoint != null && currentSavePoint.allowManualSave;
    }

    // ================================================================
    // AUTO SAVE
    // ================================================================

    /// <summary>
    /// Trigger auto-save untuk chapter tertentu. Dipanggil oleh AutoSaveTrigger.
    /// </summary>
    public void AutoSave(ChapterDefinition chapter)
    {
        if (!saveSystemEnabled)
        {
            Debug.Log("[SaveManager] Save system disabled, auto-save skipped.");
            return;
        }

        SaveData data = CollectSaveData(SaveType.AutoSave, chapter);
        string fileName = SaveFileHandler.GetAutoSaveFileName(chapter.chapterId);

        if (SaveFileHandler.Save(fileName, data))
        {
            Debug.Log($"[SaveManager] AUTO SAVE berhasil — {chapter.chapterTitle}");
            OnSaveCompleted?.Invoke(data);
            ShowAutoSaveIcon();
        }
    }

    // ================================================================
    // MANUAL SAVE
    // ================================================================

    /// <summary>
    /// Manual save ke slot tertentu. Dipanggil oleh UI.
    /// </summary>
    public bool ManualSave(int slotIndex)
    {
        if (!saveSystemEnabled)
        {
            Debug.Log("[SaveManager] Save system disabled, manual save skipped.");
            return false;
        }

        if (slotIndex < 0 || slotIndex >= maxManualSlots)
        {
            Debug.LogError($"[SaveManager] Slot index {slotIndex} di luar range (0-{maxManualSlots - 1})!");
            return false;
        }

        // Dapatkan chapter dari current save point
        ChapterDefinition chapter = currentSavePoint != null ? currentSavePoint.chapter : null;

        SaveData data = CollectSaveData(SaveType.ManualSave, chapter);
        data.saveId = $"manual_slot{slotIndex}";

        string fileName = SaveFileHandler.GetManualSaveFileName(slotIndex);

        if (SaveFileHandler.Save(fileName, data))
        {
            Debug.Log($"[SaveManager] MANUAL SAVE slot {slotIndex} berhasil");
            OnSaveCompleted?.Invoke(data);
            return true;
        }
        return false;
    }

    // ================================================================
    // LOAD
    // ================================================================

    /// <summary>
    /// Load game dari file tertentu. Akan pindah scene dan restore state.
    /// </summary>
    public void LoadGame(string fileName)
    {
        SaveData data = SaveFileHandler.Load(fileName);
        if (data == null)
        {
            Debug.LogError($"[SaveManager] Gagal load file: {fileName}");
            return;
        }

        // Simpan data — SaveManager persist (DDOL) jadi aman
        pendingLoadData = data;
        playTimeAccumulator = data.playTime;

        Debug.Log($"[SaveManager] Loading scene: {data.sceneName} dari {fileName}");

        // Pindah ke scene yang tersimpan
        SceneManager.LoadScene(data.sceneName);
    }

    /// <summary>
    /// Load auto-save untuk chapter tertentu.
    /// </summary>
    public void LoadAutoSave(int chapterId)
    {
        string fileName = SaveFileHandler.GetAutoSaveFileName(chapterId);
        LoadGame(fileName);
    }

    /// <summary>
    /// Load manual save dari slot tertentu.
    /// </summary>
    public void LoadManualSave(int slotIndex)
    {
        string fileName = SaveFileHandler.GetManualSaveFileName(slotIndex);
        LoadGame(fileName);
    }

    // ================================================================
    // SCENE LOADED CALLBACK — Restore state setelah scene baru dimuat
    // ================================================================

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (pendingLoadData != null)
        {
            StartCoroutine(RestoreAfterSceneLoad());
        }
    }

    private IEnumerator RestoreAfterSceneLoad()
    {
        // Tunggu 2 frame agar semua MonoBehaviour di scene baru sudah Awake() dan Start()
        yield return null;
        yield return null;

        // Re-collect providers karena scene baru punya prefab/provider baru
        RefreshProviders();

        // Distribute data ke semua provider
        foreach (var provider in providers)
        {
            try
            {
                provider.RestoreFromSaveData(pendingLoadData);
            }
            catch (Exception e)
            {
                Debug.LogError($"[SaveManager] Error restoring {provider.GetType().Name}: {e.Message}");
            }
        }

        Debug.Log($"[SaveManager] State restored untuk scene: {pendingLoadData.sceneName}");
        OnLoadCompleted?.Invoke(pendingLoadData);
        pendingLoadData = null;
    }

    // ================================================================
    // HELPER METHODS
    // ================================================================

    /// <summary>
    /// Kumpulkan semua data dari providers ke dalam SaveData baru.
    /// </summary>
    private SaveData CollectSaveData(SaveType type, ChapterDefinition chapter)
    {
        // Refresh providers sebelum collect
        RefreshProviders();

        SaveData data = new SaveData();

        // Meta info
        data.saveType = type;
        data.timestamp = DateTime.Now.ToString("dd MMM yyyy HH:mm");
        data.playTime = playTimeAccumulator;
        data.sceneName = SceneManager.GetActiveScene().name;

        // Chapter info
        if (chapter != null)
        {
            data.chapterId = chapter.chapterId;
            data.chapterTitle = chapter.chapterTitle;
            data.isCutscene = chapter.IsSceneCutscene(data.sceneName);
            data.saveId = type == SaveType.AutoSave
                ? $"autosave_chapter{chapter.chapterId}"
                : data.saveId; // Manual save ID diset di ManualSave()
        }
        else
        {
            data.chapterId = -1;
            data.chapterTitle = "Unknown Chapter";
            data.isCutscene = false;
        }

        // Kumpulkan data dari semua provider
        foreach (var provider in providers)
        {
            try
            {
                provider.PopulateSaveData(data);
            }
            catch (Exception e)
            {
                Debug.LogError($"[SaveManager] Error collecting from {provider.GetType().Name}: {e.Message}");
            }
        }

        return data;
    }

    /// <summary>
    /// Cari ulang semua ISaveDataProvider di scene (karena tiap scene punya prefab berbeda).
    /// </summary>
    private void RefreshProviders()
    {
        providers.Clear();

        // Cari semua MonoBehaviour yang implement ISaveDataProvider
        MonoBehaviour[] allBehaviours = FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None);
        foreach (var mb in allBehaviours)
        {
            if (mb is ISaveDataProvider provider)
            {
                providers.Add(provider);
            }
        }

        Debug.Log($"[SaveManager] Found {providers.Count} save data providers");
    }

    /// <summary>
    /// Tampilkan ikon auto-save sesaat.
    /// </summary>
    private void ShowAutoSaveIcon()
    {
        if (autoSaveIcon != null)
        {
            autoSaveIcon.SetActive(true);
            CancelInvoke(nameof(HideAutoSaveIcon));
            Invoke(nameof(HideAutoSaveIcon), autoSaveIconDuration);
        }
    }

    private void HideAutoSaveIcon()
    {
        if (autoSaveIcon != null)
            autoSaveIcon.SetActive(false);
    }

    // ================================================================
    // UTILITY — Untuk UI
    // ================================================================

    /// <summary>
    /// Mendapatkan info semua save slot (auto + manual) untuk ditampilkan di UI.
    /// </summary>
    public List<SaveSlotInfo> GetAllSaveSlots()
    {
        List<SaveSlotInfo> slots = new List<SaveSlotInfo>();

        string[] allFiles = SaveFileHandler.GetAllSaveFiles();
        foreach (string fileName in allFiles)
        {
            SaveData data = SaveFileHandler.Load(fileName);
            if (data != null)
            {
                slots.Add(new SaveSlotInfo
                {
                    fileName = fileName,
                    data = data
                });
            }
        }

        return slots;
    }

    /// <summary>
    /// Hapus save file.
    /// </summary>
    public bool DeleteSave(string fileName)
    {
        return SaveFileHandler.Delete(fileName);
    }

    /// <summary>
    /// Mendapatkan path folder saves (untuk debugging).
    /// </summary>
    public string GetSaveDirectory()
    {
        return SaveFileHandler.GetSaveDirectory();
    }
}

/// <summary>
/// Info ringkas sebuah save slot untuk UI.
/// </summary>
[System.Serializable]
public class SaveSlotInfo
{
    public string fileName;
    public SaveData data;
}
