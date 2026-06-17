using UnityEngine;
using UnityEngine.Events;
using Cinemachine;

public class NPCDialogueTrigger : MonoBehaviour
{
    [Header("Dialogue Data")]
    public DialogueData dialogue;

    [Header("Cameras")]
    [Tooltip("Camera to activate when speaker says something with Camera ID 'NPC'")]
    public CinemachineVirtualCameraBase npcCamera;
    [Tooltip("Camera to activate when speaker says something with Camera ID 'Player'")]
    public CinemachineVirtualCameraBase playerCamera;
    [Tooltip("Camera to activate during choices, Camera ID 'Choice'")]
    public CinemachineVirtualCameraBase choiceCamera;

    [Header("Animator")]
    [Tooltip("Masukkan Animator milik NPC ini. (Animator Player otomatis terdeteksi)")]
    public Animator npcAnimator;

    [Header("Events")]
    [Tooltip("Event dipanggil setelah dialog selesai. Bisa disambungkan ke CompleteQuestProgress di ObjectInteract.")]
    public UnityEvent onDialogueEnd;

    [Header("Quest Integration")]
    [Tooltip("ID objektif yang akan diselesaikan secara otomatis saat dialog berakhir (contoh: obj_BicaraKePelatih)")]
    public string completeObjectiveIdOnEnd;

    [Header("State (Otomatis)")]
    [Tooltip("Sistem akan menyimpan jalur (path) pilihan agar tidak mengulang dari awal jika sudah pernah memilih opsi bersarang.")]
    public System.Collections.Generic.List<int> savedPath = new System.Collections.Generic.List<int>();

    // Fungsi ini dipanggil dari UnityEvent OnInteract milik ObjectInteract.cs
    public void StartDialogue()
    {
        if (DialogueManager.Instance == null)
        {
            Debug.LogError("DialogueManager belum ada di scene! Buat GameObject dan tambahkan script DialogueManager.");
            return;
        }

        if (dialogue == null)
        {
            Debug.LogWarning("DialogueData masih kosong di NPC ini!");
            return;
        }

        DialogueManager.Instance.StartDialogue(dialogue, OnDialogueComplete, npcCamera, playerCamera, choiceCamera, npcAnimator, transform, savedPath, OnDialogueStateChanged);
    }

    private void OnDialogueStateChanged(System.Collections.Generic.List<int> newPath)
    {
        // Simpan jalur pilihan bersarang
        savedPath = new System.Collections.Generic.List<int>(newPath);
    }

    private void OnDialogueComplete()
    {
        if (!string.IsNullOrEmpty(completeObjectiveIdOnEnd) && QuestManager.Instance != null)
        {
            if (QuestManager.Instance.IsObjectiveActive(completeObjectiveIdOnEnd))
            {
                QuestManager.Instance.AddProgress(completeObjectiveIdOnEnd, 1);
            }
        }

        onDialogueEnd?.Invoke();
    }
}
