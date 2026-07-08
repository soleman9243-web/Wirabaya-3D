using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class QuestManager : MonoBehaviour
{
    public static QuestManager Instance;

    [System.Serializable]
    public class QuestInstance
    {
        public QuestData data;
        public bool isCompleted;
        public List<ObjectiveProgress> objectives = new List<ObjectiveProgress>();
    }

    public QuestInstance currentQuest;

    public UnityEvent<QuestData> onQuestStarted;
    public UnityEvent<QuestData> onQuestCompleted;
    public UnityEvent<QuestData> onQuestUpdated; 
    
    [Header("End Game Events")]
    public UnityEvent onAllQuestsCompleted;

    // Simpan riwayat agar objektif bisa dicek meski quest sudah selesai dan currentQuest jadi null
    public List<string> allCompletedObjectives = new List<string>();

    [Header("Auto Start Settings")]
    [Tooltip("Masukkan QuestData ke sini jika ingin quest ini otomatis berjalan saat scene/game dimulai.")]
    public QuestData autoStartQuest;

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        // Jalankan quest otomatis jika sudah di-set di Inspector
        if (autoStartQuest != null)
        {
            StartQuest(autoStartQuest);
        }
    }

    public void StartQuest(QuestData questData)
    {
        QuestInstance instance = new QuestInstance();
        instance.data = questData;

        foreach (var obj in questData.objectives)
        {
            instance.objectives.Add(new ObjectiveProgress
            {
                objectiveId = obj.objectiveId,
                description = obj.description,
                targetAmount = obj.targetAmount,
                currentAmount = 0,
                isCompleted = false
            });
        }

        currentQuest = instance;
        onQuestStarted?.Invoke(questData);
    }

    public void AddProgress(string objectiveId, int amount = 1)
    {
        if (currentQuest == null) return;

        foreach (var obj in currentQuest.objectives)
        {
            if (!obj.isCompleted)
            {
                // Pastikan objektif ini adalah objektif yang SEDANG berjalan (harus berurutan)
                if (obj.objectiveId == objectiveId)
                {
                    obj.currentAmount += amount;

                    if (obj.currentAmount >= obj.targetAmount)
                    {
                        obj.isCompleted = true;
                        if (!allCompletedObjectives.Contains(obj.objectiveId))
                        {
                            allCompletedObjectives.Add(obj.objectiveId);
                        }
                    }

                    CheckQuest();

                    if (currentQuest != null)
                    {
                        onQuestUpdated?.Invoke(currentQuest.data);
                    }
                }
                
                // Mencegah mengecek/menyelesaikan objektif selanjutnya sebelum yang ini selesai
                return; 
            }
        }
    }

    // Method gembok pencegah eksploitasi item
    public bool IsObjectiveActive(string objectiveId)
    {
        if (currentQuest == null) return false;

        foreach (var obj in currentQuest.objectives)
        {
            if (!obj.isCompleted)
            {
                // Hanya mengembalikan true jika objektif yang diminta adalah objektif aktif (pertama yg belum selesai)
                return obj.objectiveId == objectiveId;
            }
        }
        return false;
    }

    // Method baru untuk mengecek apakah objektif PERNAH diselesaikan (berguna untuk unlock sesuatu)
    public bool HasCompletedObjective(string objectiveId)
    {
        return allCompletedObjectives.Contains(objectiveId);
    }

    private void CheckQuest()
    {
        foreach (var obj in currentQuest.objectives)
        {
            if (!obj.isCompleted) return;
        }

        currentQuest.isCompleted = true;
        QuestData completedQuestData = currentQuest.data;
        
        onQuestCompleted?.Invoke(completedQuestData);
        Debug.Log("Quest Completed: " + completedQuestData.title);
        
        currentQuest = null;

        // Auto Start Next Quest logic
        if (QuestDatabase.Instance != null && QuestDatabase.Instance.allQuests != null)
        {
            int currentIndex = QuestDatabase.Instance.allQuests.IndexOf(completedQuestData);
            
            // Jika quest ini terdaftar di database dan masih ada quest selanjutnya
            if (currentIndex >= 0 && currentIndex + 1 < QuestDatabase.Instance.allQuests.Count)
            {
                Debug.Log("Memulai quest selanjutnya secara otomatis...");
                StartQuest(QuestDatabase.Instance.allQuests[currentIndex + 1]);
            }
            // Jika ini adalah quest terakhir di dalam database
            else if (currentIndex == QuestDatabase.Instance.allQuests.Count - 1)
            {
                Debug.Log("Semua Quest di database telah selesai!");
                onAllQuestsCompleted?.Invoke();
            }
        }
    }
}