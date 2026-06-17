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

[Serializable]
public class DialogueNode
{
    public string speakerName;
    [TextArea(3, 5)]
    public string dialogueText;
    
    [Tooltip("If checked, options will appear after this text.")]
    public bool hasChoices;
    
    public DialogueChoice[] choices;

    [Header("Cinemachine")]
    [Tooltip("Optional: Tag or name of the virtual camera to switch to (e.g., 'PlayerCam', 'NPCCam', 'ChoiceCam')")]
    public string cameraID;

    [Header("Linear Flow")]
    [Tooltip("Jika dicentang, dialog akan LANGSUNG BERHENTI setelah teks ini selesai dibaca.")]
    public bool endDialogueAfterThis = false;
    
    [Tooltip("Jika dicentang, dialog akan MELOMPAT ke Index yang kamu ketik di bawah, BUKAN lanjut ke bawahnya.")]
    public bool jumpToNode = false;
    public int nextNodeId = 0;
}
