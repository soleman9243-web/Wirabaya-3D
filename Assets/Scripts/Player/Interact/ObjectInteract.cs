using UnityEngine;
using UnityEngine.Events;

public class InteractObject : MonoBehaviour
{
    [Header("Interaction")]
    public string interactionText = "Interact";
    [Tooltip("Centang ini jika kamu ingin interaksi menggunakan sistem Area (tanpa dibidik). Biarkan kosong jika ingin dibidik pakai Raycast.")]
    public bool useAreaTrigger = false;
    public UnityEvent onInteract;

    [Header("Story (Optional)")]
    public StoryEvent storyEvent;

    [Header("Quest")]
    public string objectiveId;
    public int questAmount = 1;

    [Header("Lock Requirement (Opsional)")]
    [Tooltip("Isi dengan ID objektif (misal obj_Campfire) agar objek/NPC ini HANYA BISA diinteract SETELAH objektif tersebut selesai.")]
    public string requireCompletedObjectiveId;

    [Header("Minigame (Optional)")]
    public bool useMinigame;
    public bool disableQuestProgressUntilMinigameComplete = true;

    private void Awake()
    {
        // Otomatis matikan efek Outline di awal agar tidak perlu repot uncheck manual di Inspector
        Outline outline = GetComponent<Outline>();
        if (outline == null) outline = GetComponentInChildren<Outline>();

        if (outline != null)
        {
            outline.enabled = false;
        }
    }

    public string GetInteractionText()
    {
        // Cek apakah interaksi ini dikunci oleh syarat objektif tertentu
        if (!string.IsNullOrEmpty(requireCompletedObjectiveId) && QuestManager.Instance != null)
        {
            if (!QuestManager.Instance.HasCompletedObjective(requireCompletedObjectiveId))
            {
                return string.Empty; // Sembunyikan teks interact karena masih terkunci
            }
        }

        return interactionText;
    }

    public void Interact()
    {
        // Cek gembok
        if (!string.IsNullOrEmpty(requireCompletedObjectiveId) && QuestManager.Instance != null)
        {
            if (!QuestManager.Instance.HasCompletedObjective(requireCompletedObjectiveId))
            {
                Debug.Log("Interaksi diblokir! Syarat objektif belum selesai.");
                return;
            }
        }

        Debug.Log("INTERACT: " + gameObject.name);

        onInteract?.Invoke();

        if (storyEvent != null)
        {
            Debug.Log("TRIGGER STORY EVENT");
            storyEvent.Trigger();
            return;
        }

        if (useMinigame)
        {
            // MinigameManager.Instance.StartMinigame(this);
            Debug.Log("MINIGAME TRIGGERED");
            return;
        }
        CompleteQuestProgress();
    }

    public void CompleteQuestProgress()
    {
        if (!string.IsNullOrEmpty(objectiveId))
        {
            QuestManager.Instance.AddProgress(objectiveId, questAmount);
        }
    }
}