using System.IO;
using UnityEngine;

/// <summary>
/// Handles read/write save data sebagai JSON ke disk.
/// Lokasi: Application.persistentDataPath/Saves/
/// </summary>
public static class SaveFileHandler
{
    private const string SAVE_FOLDER = "Saves";
    private const string AUTO_SAVE_PREFIX = "autosave_chapter";
    private const string MANUAL_SAVE_PREFIX = "manual_slot";
    private const string FILE_EXTENSION = ".json";

    /// <summary>
    /// Mendapatkan path folder saves. Folder otomatis dibuat jika belum ada.
    /// </summary>
    private static string GetSaveFolderPath()
    {
        string path = Path.Combine(Application.persistentDataPath, SAVE_FOLDER);
        if (!Directory.Exists(path))
        {
            Directory.CreateDirectory(path);
        }
        return path;
    }

    /// <summary>
    /// Generate filename untuk auto-save berdasarkan chapter ID.
    /// Contoh: "autosave_chapter1.json"
    /// </summary>
    public static string GetAutoSaveFileName(int chapterId)
    {
        return $"{AUTO_SAVE_PREFIX}{chapterId}{FILE_EXTENSION}";
    }

    /// <summary>
    /// Generate filename untuk manual save berdasarkan slot index.
    /// Contoh: "manual_slot0.json"
    /// </summary>
    public static string GetManualSaveFileName(int slotIndex)
    {
        return $"{MANUAL_SAVE_PREFIX}{slotIndex}{FILE_EXTENSION}";
    }

    /// <summary>
    /// Simpan SaveData ke file JSON.
    /// </summary>
    public static bool Save(string fileName, SaveData data)
    {
        string filePath = Path.Combine(GetSaveFolderPath(), fileName);

        try
        {
            string json = JsonUtility.ToJson(data, true); // prettyPrint = true untuk debugging
            File.WriteAllText(filePath, json);
            Debug.Log($"[SaveFileHandler] Berhasil save ke: {filePath}");
            return true;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[SaveFileHandler] Gagal save ke {filePath}: {e.Message}");
            return false;
        }
    }

    /// <summary>
    /// Load SaveData dari file JSON. Return null jika file tidak ada atau error.
    /// </summary>
    public static SaveData Load(string fileName)
    {
        string filePath = Path.Combine(GetSaveFolderPath(), fileName);

        if (!File.Exists(filePath))
        {
            Debug.LogWarning($"[SaveFileHandler] File tidak ditemukan: {filePath}");
            return null;
        }

        try
        {
            string json = File.ReadAllText(filePath);
            SaveData data = JsonUtility.FromJson<SaveData>(json);
            Debug.Log($"[SaveFileHandler] Berhasil load dari: {filePath}");
            return data;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[SaveFileHandler] Gagal load dari {filePath}: {e.Message}");
            return null;
        }
    }

    /// <summary>
    /// Hapus file save.
    /// </summary>
    public static bool Delete(string fileName)
    {
        string filePath = Path.Combine(GetSaveFolderPath(), fileName);

        if (!File.Exists(filePath))
        {
            Debug.LogWarning($"[SaveFileHandler] File tidak ditemukan untuk dihapus: {filePath}");
            return false;
        }

        try
        {
            File.Delete(filePath);
            Debug.Log($"[SaveFileHandler] Berhasil hapus: {filePath}");
            return true;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[SaveFileHandler] Gagal hapus {filePath}: {e.Message}");
            return false;
        }
    }

    /// <summary>
    /// Cek apakah file save ada.
    /// </summary>
    public static bool Exists(string fileName)
    {
        string filePath = Path.Combine(GetSaveFolderPath(), fileName);
        return File.Exists(filePath);
    }

    /// <summary>
    /// Mendapatkan semua file save yang ada (auto + manual).
    /// </summary>
    public static string[] GetAllSaveFiles()
    {
        string folderPath = GetSaveFolderPath();
        
        if (!Directory.Exists(folderPath))
            return new string[0];

        string[] files = Directory.GetFiles(folderPath, $"*{FILE_EXTENSION}");
        
        // Return hanya nama file, bukan full path
        for (int i = 0; i < files.Length; i++)
        {
            files[i] = Path.GetFileName(files[i]);
        }

        return files;
    }

    /// <summary>
    /// Mendapatkan full path folder saves (untuk debugging).
    /// </summary>
    public static string GetSaveDirectory()
    {
        return GetSaveFolderPath();
    }
}
