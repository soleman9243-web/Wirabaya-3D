using System.Collections.Generic;

public class QuestInstance
{
    public QuestData data;
    public bool isCompleted;

    public List<ObjectiveProgress> objectives = new List<ObjectiveProgress>();
}