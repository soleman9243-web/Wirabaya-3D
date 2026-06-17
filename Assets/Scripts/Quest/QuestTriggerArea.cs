using UnityEngine;

public class QuestTriggerArea : MonoBehaviour
{
    [Header("Quest Settings")]
    [Tooltip("Masukkan ID Objective yang ingin diselesaikan saat menabrak area ini (Contoh: obj_SelesaikanParkour atau obj_DatangTempat)")]
    public string objectiveId;
    public int progressAmount = 1;
    
    [Tooltip("Apakah objek collider ini hancur/hilang setelah ditabrak?")]
    public bool destroyOnTrigger = true;

    private bool hasTriggered = false;

    private void OnTriggerEnter(Collider other)
    {
        if (hasTriggered) return;

        if (other.CompareTag("Player"))
        {
            if (QuestManager.Instance != null && QuestManager.Instance.IsObjectiveActive(objectiveId))
            {
                hasTriggered = true;
                QuestManager.Instance.AddProgress(objectiveId, progressAmount);
                
                if (destroyOnTrigger)
                {
                    Destroy(gameObject);
                }
            }
        }
    }
}
