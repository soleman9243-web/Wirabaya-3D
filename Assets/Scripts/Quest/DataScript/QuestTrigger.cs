using UnityEngine;

public class QuestTrigger : MonoBehaviour
{
    public string objectiveId;
    public int amount = 1;

    public void Trigger()
    {
        QuestManager.Instance.AddProgress(objectiveId, amount);
    }
}