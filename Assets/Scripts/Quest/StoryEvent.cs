using UnityEngine;

public class StoryEvent : MonoBehaviour
{
    public string questToStart;

    public void Trigger()
    {
        Debug.Log("Triggering Story Event");
        QuestManager.Instance.StartQuest(
            QuestDatabase.Instance.GetQuest(questToStart)
        );
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            Trigger();
            Destroy(gameObject);
        }
    }
}