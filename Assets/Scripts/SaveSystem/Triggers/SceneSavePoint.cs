using UnityEngine;

/// <summary>
/// Dipasang di awal setiap scene.
/// Saat player masuk scene ini, manual save jadi tersedia.
/// SaveManager akan mengecek flag ini sebelum mengizinkan manual save.
/// </summary>
public class SceneSavePoint : MonoBehaviour
{
    [Header("Chapter Reference")]
    [Tooltip("ChapterDefinition yang memiliki scene ini")]
    public ChapterDefinition chapter;

    [Header("Settings")]
    [Tooltip("Apakah manual save diizinkan di scene ini? (biasanya true kecuali di cutscene)")]
    public bool allowManualSave = true;

    private void Start()
    {
        if (SaveManager.Instance != null)
        {
            SaveManager.Instance.RegisterSavePoint(this);
            Debug.Log($"[SceneSavePoint] Save point registered. Manual save: {allowManualSave}");
        }
    }

    private void OnDestroy()
    {
        if (SaveManager.Instance != null)
        {
            SaveManager.Instance.UnregisterSavePoint(this);
        }
    }
}
