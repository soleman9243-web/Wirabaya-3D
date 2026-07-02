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

    // Simpan riwayat agar objektif bisa dicek meski quest sudah selesai dan currentQuest jadi null
    public List<string> allCompletedObjectives = new List<string>();

    private void Awake()
    {
        Instance = this;
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
        onQuestCompleted?.Invoke(currentQuest.data);
        Debug.Log("Quest Completed: " + currentQuest.data.title);
        currentQuest = null;
    }
}