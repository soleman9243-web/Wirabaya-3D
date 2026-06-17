using System.Collections.Generic;
using UnityEngine;

public class QuestDatabase : MonoBehaviour
{
    public static QuestDatabase Instance;

    public List<QuestData> allQuests;

    private void Awake()
    {
        Instance = this;
    }

    public QuestData GetQuest(string questId)
    {
        return allQuests.Find(q => q.questId == questId);
    }
}