using UnityEngine;

/// <summary>
/// Dipasang di scene pertama sebuah chapter.
/// Saat scene dimulai, otomatis trigger auto-save.
/// </summary>
public class AutoSaveTrigger : MonoBehaviour
{
    [Header("Chapter Reference")]
    [Tooltip("ChapterDefinition yang mewakili chapter ini")]
    public ChapterDefinition chapter;

    [Header("Settings")]
    [Tooltip("Delay sebelum auto-save (dalam detik) agar scene sempat initialize")]
    public float autoSaveDelay = 1.0f;

    private void Start()
    {
        if (chapter == null)
        {
            Debug.LogWarning("[AutoSaveTrigger] ChapterDefinition belum di-assign!");
            return;
        }

        if (SaveManager.Instance == null)
        {
            Debug.LogWarning("[AutoSaveTrigger] SaveManager.Instance belum ada di scene!");
            return;
        }

        // Delay sedikit agar semua system sempat Awake/Start terlebih dahulu
        Invoke(nameof(DoAutoSave), autoSaveDelay);
    }

    private void DoAutoSave()
    {
        if (SaveManager.Instance != null)
        {
            SaveManager.Instance.AutoSave(chapter);
            Debug.Log($"[AutoSaveTrigger] Auto-save triggered untuk {chapter.chapterTitle}");
        }
    }
}
