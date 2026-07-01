using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class DialogueChoice
{
    public string choiceText;
    
    [Tooltip("Dialog lanjutan khusus jika pemain memilih opsi ini.")]
    [SerializeReference]
    public List<DialogueNode> branchNodes = new List<DialogueNode>();

    [Header("Old Jump Logic")]
    [Tooltip("ID (Index) of the DialogueNode to jump to. If -1, dialogue ends.")]
    public int nextNodeId = -1;
}

public enum DialogueCameraType
{
    NPC,
    Player,
    Choice,
    None
}

[Serializable]
public class DialogueNode : ISerializationCallbackReceiver
{
    public string speakerName;
    [TextArea(3, 5)]
    public string dialogueText;
    
    [Tooltip("If checked, options will appear after this text.")]
    public bool hasChoices;
    
    public DialogueChoice[] choices;

    [Header("Cinemachine")]
    [Tooltip("Pilih kamera mana yang ingin diaktifkan saat dialog ini muncul.")]
    public DialogueCameraType cameraType = DialogueCameraType.NPC;
    
    [HideInInspector]
    public string cameraID; // Disembunyikan, hanya untuk migrasi data lama

    public void OnBeforeSerialize() {}
    
    public void OnAfterDeserialize()
    {
        // Migrasi data otomatis: jika ada string lama, ubah ke Enum lalu hapus string-nya
        if (!string.IsNullOrEmpty(cameraID))
        {
            string lower = cameraID.ToLower();
            if (lower.Contains("npc")) cameraType = DialogueCameraType.NPC;
            else if (lower.Contains("player")) cameraType = DialogueCameraType.Player;
            else if (lower.Contains("choice")) cameraType = DialogueCameraType.Choice;
            else cameraType = DialogueCameraType.None;
            
            cameraID = ""; 
        }
    }

    [Header("Linear Flow")]
    [Tooltip("Jika dicentang, dialog akan LANGSUNG BERHENTI setelah teks ini selesai dibaca.")]
    public bool endDialogueAfterThis = false;
    
    [Tooltip("Jika dicentang, dialog akan MELOMPAT ke Index yang kamu ketik di bawah, BUKAN lanjut ke bawahnya.")]
    public bool jumpToNode = false;
    public int nextNodeId = 0;
}
