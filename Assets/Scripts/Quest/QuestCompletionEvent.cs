using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Script ini berfungsi sebagai pendengar (listener) ketika sebuah Quest tertentu selesai.
/// Sangat berguna untuk menjalankan fungsi lain (seperti pindah scene, memunculkan bos, memberi item) langsung dari Inspector.
/// </summary>
public class QuestCompletionEvent : MonoBehaviour
{
    [Header("Quest Target")]
    [Tooltip("Ketikkan ID Quest secara persis (harus sama dengan ID di file ScriptableObject Quest-mu)")]
    public string targetQuestId;

    [Header("Actions to Run")]
    [Tooltip("Masukkan perintah (fungsi) apa saja yang ingin dijalankan saat quest di atas selesai.")]
    public UnityEvent onQuestFinished;

    private void Start()
    {
        // Berlangganan (subscribe) ke event utama milik QuestManager
        if (QuestManager.Instance != null)
        {
            QuestManager.Instance.onQuestCompleted.AddListener(OnAnyQuestCompleted);
        }
        else
        {
            Debug.LogWarning("QuestManager tidak ditemukan di scene ini!");
        }
    }

    private void OnDestroy()
    {
        // Berhenti berlangganan saat objek ini dihancurkan (untuk mencegah error)
        if (QuestManager.Instance != null)
        {
            QuestManager.Instance.onQuestCompleted.RemoveListener(OnAnyQuestCompleted);
        }
    }

    private void OnAnyQuestCompleted(QuestData questData)
    {
        // Cek apakah quest yang baru saja selesai adalah quest target kita
        if (questData != null && questData.questId == targetQuestId)
        {
            // Jalankan semua fungsi yang sudah didaftarkan di Inspector!
            onQuestFinished?.Invoke();
        }
    }
}
