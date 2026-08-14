using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Mengambil dan mengembalikan data quest dari QuestManager.
/// Pasang di GameObject yang sama dengan QuestManager, atau di GameObject yang persist.
/// </summary>
public class QuestSaveProvider : MonoBehaviour, ISaveDataProvider
{
    public void PopulateSaveData(SaveData data)
    {
        if (QuestManager.Instance == null)
        {
            Debug.LogWarning("[QuestSaveProvider] QuestManager.Instance null, skip populate.");
            return;
        }

        QuestManager qm = QuestManager.Instance;

        // Simpan current quest
        if (qm.currentQuest != null && qm.currentQuest.data != null)
        {
            data.currentQuestId = qm.currentQuest.data.questId;

            // Simpan progress tiap objective
            data.objectiveProgress = new List<ObjectiveSaveData>();
            foreach (var obj in qm.currentQuest.objectives)
            {
                data.objectiveProgress.Add(new ObjectiveSaveData
                {
                    objectiveId = obj.objectiveId,
                    description = obj.description,
                    targetAmount = obj.targetAmount,
                    currentAmount = obj.currentAmount,
                    isCompleted = obj.isCompleted
                });
            }

            // Cari index quest di database
            if (QuestDatabase.Instance != null && QuestDatabase.Instance.allQuests != null)
            {
                data.questDatabaseIndex = QuestDatabase.Instance.allQuests.IndexOf(qm.currentQuest.data);
            }
        }
        else
        {
            data.currentQuestId = "";
            data.questDatabaseIndex = -1;
        }

        // Simpan completed objectives history
        data.completedObjectives = new List<string>(qm.allCompletedObjectives);
    }

    public void RestoreFromSaveData(SaveData data)
    {
        if (QuestManager.Instance == null)
        {
            Debug.LogWarning("[QuestSaveProvider] QuestManager.Instance null, skip restore.");
            return;
        }

        QuestManager qm = QuestManager.Instance;

        // Restore completed objectives history
        qm.allCompletedObjectives = new List<string>(data.completedObjectives);

        // Restore current quest
        if (!string.IsNullOrEmpty(data.currentQuestId) && QuestDatabase.Instance != null)
        {
            QuestData questData = QuestDatabase.Instance.GetQuest(data.currentQuestId);

            if (questData != null)
            {
                // Buat QuestInstance secara manual (tidak lewat StartQuest agar tidak trigger events)
                QuestManager.QuestInstance instance = new QuestManager.QuestInstance();
                instance.data = questData;
                instance.isCompleted = false;

                // Restore objective progress dari save data
                instance.objectives = new List<ObjectiveProgress>();
                foreach (var savedObj in data.objectiveProgress)
                {
                    instance.objectives.Add(new ObjectiveProgress
                    {
                        objectiveId = savedObj.objectiveId,
                        description = savedObj.description,
                        targetAmount = savedObj.targetAmount,
                        currentAmount = savedObj.currentAmount,
                        isCompleted = savedObj.isCompleted
                    });
                }

                qm.currentQuest = instance;
                Debug.Log($"[QuestSaveProvider] Restored quest: {data.currentQuestId}");
            }
            else
            {
                Debug.LogWarning($"[QuestSaveProvider] Quest '{data.currentQuestId}' tidak ditemukan di QuestDatabase!");
            }
        }
        else
        {
            qm.currentQuest = null;
        }
    }
}
