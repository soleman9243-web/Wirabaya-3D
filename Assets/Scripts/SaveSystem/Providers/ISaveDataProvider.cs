/// <summary>
/// Interface untuk sistem yang ingin "berkontribusi" data saat save dan "menerima" data saat load.
/// Implementasi interface ini agar SaveManager bisa otomatis collect/restore data dari berbagai sistem
/// tanpa perlu hardcode setiap sistem satu per satu.
/// </summary>
public interface ISaveDataProvider
{
    /// <summary>
    /// Dipanggil saat SAVE — isi SaveData dengan data dari sistem ini.
    /// </summary>
    void PopulateSaveData(SaveData data);

    /// <summary>
    /// Dipanggil saat LOAD — ambil data dari SaveData dan restore state sistem ini.
    /// </summary>
    void RestoreFromSaveData(SaveData data);
}
