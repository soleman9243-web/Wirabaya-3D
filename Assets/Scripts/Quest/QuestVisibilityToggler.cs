using UnityEngine;

public class QuestVisibilityToggler : MonoBehaviour
{
    [Header("Muncul Jika...")]
    [Tooltip("Objek ini baru akan MUNCUL jika SEMUA objektif di bawah ini SUDAH SELESAI. (Kosongkan jika ingin muncul sejak awal)")]
    public string[] showAfterObjectivesCompleted;

    [Tooltip("Objek ini HANYA akan MUNCUL saat SALAH SATU objektif di bawah ini sedang AKTIF berjalan.")]
    public string[] showDuringObjectives;

    [Header("Hilang Jika...")]
    [Tooltip("Objek ini akan HILANG secara permanen jika SALAH SATU objektif di bawah ini SUDAH SELESAI.")]
    public string[] hideAfterObjectivesCompleted;

    private void Start()
    {
        // Cek awal saat script jalan
        CheckVisibility(null);

        // Langganan ke event update dari QuestManager
        if (QuestManager.Instance != null)
        {
            QuestManager.Instance.onQuestUpdated.AddListener(CheckVisibility);
            QuestManager.Instance.onQuestStarted.AddListener(CheckVisibility);
        }
    }

    private void CheckVisibility(QuestData data)
    {
        if (QuestManager.Instance == null || QuestManager.Instance.currentQuest == null)
        {
            gameObject.SetActive(false);
            return;
        }

        bool shouldBeVisible = true;

        // 1. Cek syarat kemunculan (HARUS SELESAI SEMUA)
        if (showAfterObjectivesCompleted != null && showAfterObjectivesCompleted.Length > 0)
        {
            foreach (string objId in showAfterObjectivesCompleted)
            {
                if (!IsObjectiveCompleted(objId))
                {
                    shouldBeVisible = false; // Ada syarat yang belum selesai, jangan muncul
                    break;
                }
            }
        }

        // 2. Cek syarat aktif (MUNCUL JIKA SALAH SATU AKTIF)
        // Jika shouldBeVisible masih true sejauh ini, kita cek juga rule during
        if (shouldBeVisible && showDuringObjectives != null && showDuringObjectives.Length > 0)
        {
            bool hasAnyActive = false;
            foreach (string objId in showDuringObjectives)
            {
                if (QuestManager.Instance.IsObjectiveActive(objId))
                {
                    hasAnyActive = true;
                    break;
                }
            }
            // Jika dikunci rule ini dan tidak ada yang aktif, maka sembunyikan
            if (!hasAnyActive) shouldBeVisible = false;
        }

        // 3. Cek syarat menghilang (HILANG JIKA SALAH SATU SELESAI)
        if (shouldBeVisible && hideAfterObjectivesCompleted != null && hideAfterObjectivesCompleted.Length > 0)
        {
            foreach (string objId in hideAfterObjectivesCompleted)
            {
                if (IsObjectiveCompleted(objId))
                {
                    shouldBeVisible = false; // Sudah disuruh hilang
                    break;
                }
            }
        }

        gameObject.SetActive(shouldBeVisible);
    }

    private bool IsObjectiveCompleted(string objectiveId)
    {
        if (QuestManager.Instance == null) return false;
        
        // Gunakan metode riwayat baru agar objek tidak nge-glitch saat quest selesai sepenuhnya
        return QuestManager.Instance.HasCompletedObjective(objectiveId);
    }

    private void OnDestroy()
    {
        // Bersihkan langganan event saat objek dihancurkan untuk mencegah error
        if (QuestManager.Instance != null)
        {
            QuestManager.Instance.onQuestUpdated.RemoveListener(CheckVisibility);
            QuestManager.Instance.onQuestStarted.RemoveListener(CheckVisibility);
        }
    }
}
